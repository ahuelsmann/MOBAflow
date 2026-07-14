// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.WinUI.Service;

using Backend.Interface;
using Backend.Service.TrackPlan;

using Common.Extension;

using Converter;

using Extensions;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;

using Moba.SharedUI.Interface;
using Moba.SharedUI.Service;
using Moba.SharedUI.ViewModel;

using Serilog;

using System.Diagnostics;

using View;

/// <summary>
/// Result of the synchronous WinUI launch phase up to main window activation.
/// </summary>
internal sealed record WinUiLaunchResult(
    IServiceProvider Services,
    Window MainWindow,
    ILogger<App> Logger);

/// <summary>
/// Orchestrates MOBAflow startup: configuration, DI, main window activation, and deferred initialization.
/// </summary>
internal sealed class WinUiAppStartupService
{
    private static readonly Stopwatch StartupStopwatch = Stopwatch.StartNew();

    private readonly App _app;
    private Window? _startupSplashDismissWindow;

    public WinUiAppStartupService(App app)
    {
        _app = app ?? throw new ArgumentNullException(nameof(app));
    }

    /// <summary>
    /// Runs the synchronous launch path: splash dismiss, DI, main window activation.
    /// Post-startup work continues asynchronously after the window is visible.
    /// </summary>
    public WinUiLaunchResult Launch(LaunchActivatedEventArgs args)
    {
        _ = args;

        var launchTimer = Stopwatch.StartNew();
        LogStartupCheckpoint("OnLaunched started");

        DismissStartupSplash();

        var services = BuildServiceProvider();
        var logger = services.GetRequiredService<ILogger<App>>();
        LogStartupCheckpoint("Services configured");

        _ = services.GetRequiredService<ISettingsService>();
        LogStartupCheckpoint("Settings service resolved");

        var appSettings = services.GetRequiredService<Common.Configuration.AppSettings>();
        PhotoPathToImageConverter.SetPhotoBasePath(appSettings.Application.PhotoStoragePath);

        var layoutColumnWidths = services.GetRequiredService<LayoutColumnWidthsViewModel>();
        _app.Resources["LayoutColumnWidths"] = layoutColumnWidths;
        LogStartupCheckpoint("Layout resources prepared");

        var mainWindow = services.GetRequiredService<MainWindow>();
        LogStartupCheckpoint("MainWindow resolved");

        mainWindow.Closed += (_, _) => HandleWindowClosed(services, logger);

        mainWindow.Activate();
        CloseStartupSplashDismissWindow();
        LogStartupCheckpoint("MainWindow activated");

        mainWindow.ScheduleDeferredUiStartup();

        RunPostStartupPipelineAsync(services, mainWindow, logger)
            .SafeFireAndForget(ex => logger.LogError(ex, "Post-startup initialization failed unexpectedly"));

        logger.LogInformation(
            "Application UI launched (main window activated) in {ElapsedMs}ms",
            launchTimer.ElapsedMilliseconds);

        return new WinUiLaunchResult(services, mainWindow, logger);
    }

    /// <summary>
    /// Closes the temporary splash-dismiss window when launch fails before main window activation.
    /// </summary>
    public void AbortLaunch()
    {
        CloseStartupSplashDismissWindow();
    }

    /// <summary>
    /// Builds and validates the application service provider.
    /// </summary>
    public static IServiceProvider BuildServiceProvider()
    {
        var configureTimer = Stopwatch.StartNew();
        WinUiSerilogConfigurator.EnsureConfigured();

        var configuration = WinUiConfigurationBuilder.Build();
        var services = new ServiceCollection();

        services
            .AddMobaWinUiConfiguration(configuration)
            .AddMobaWinUiPlatformServices()
            .AddMobaWinUiSpeechServices()
            .AddMobaWinUiBackendServices()
            .AddMobaWinUiIoAndNetworkServices()
            .AddMobaWinUiDomainServices()
            .AddMobaWinUiShellServices()
            .AddMobaWinUiViewModelsAndWindow()
            .AddMobaWinUiStartupServices();

        var serviceProvider = services.BuildServiceProvider();
        WinUiDiContainerValidator.ValidateCoreServices(serviceProvider);

        Log.Information("[Startup] ConfigureServices completed in {ElapsedMs}ms", configureTimer.ElapsedMilliseconds);
        return serviceProvider;
    }

    private void DismissStartupSplash()
    {
        _startupSplashDismissWindow = new Window
        {
            Title = "MOBAflow"
        };

        if (_startupSplashDismissWindow.AppWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.IsMaximizable = false;
            presenter.IsMinimizable = false;
            presenter.IsResizable = false;
        }

        _startupSplashDismissWindow.Activate();
        LogStartupCheckpoint("Startup splash dismissed");
    }

    private void CloseStartupSplashDismissWindow()
    {
        if (_startupSplashDismissWindow is null)
        {
            return;
        }

        _startupSplashDismissWindow.Close();
        _startupSplashDismissWindow = null;
    }

    private static void LogStartupCheckpoint(string checkpoint)
    {
        Log.Information("[Startup] {Checkpoint} at {ElapsedMs}ms", checkpoint, StartupStopwatch.ElapsedMilliseconds);
    }

    private static async Task RunPostStartupPipelineAsync(
        IServiceProvider services,
        MainWindow mainWindow,
        ILogger<App> logger)
    {
        try
        {
            var timer = Stopwatch.StartNew();
            LogStartupCheckpoint("Post-startup initialization queued");

            var runtime = services.GetRequiredService<IMobaRuntime>();
            await runtime.StartAsync().ConfigureAwait(false);

            services.GetRequiredService<TrackPlanSolutionBinder>().Activate();
            services.GetRequiredService<TrackPlanFeedbackHighlighter>().Activate();
            services.GetRequiredService<TrackPlanRailroadStateProjector>().Activate();

            _ = services.GetRequiredService<RestApiRuntimeCommandConsumerService>();
            _ = services.GetRequiredService<RestApiRuntimeHubService>();

            var postStartupService = services.GetRequiredService<PostStartupInitializationService>();
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            await postStartupService.InitializeAsync(cts.Token).ConfigureAwait(false);

            await AutoLoadLastSolutionAsync(services, mainWindow.ViewModel, logger).ConfigureAwait(false);

            logger.LogInformation(
                "[Startup] Post-startup pipeline completed in {ElapsedMs}ms",
                timer.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Post-startup initialization failed");
        }
    }

    private static async Task AutoLoadLastSolutionAsync(
        IServiceProvider services,
        MainWindowViewModel mainWindowViewModel,
        ILogger<App> logger)
    {
        try
        {
            var settingsService = services.GetService<ISettingsService>();
            if (settingsService == null)
            {
                logger.LogWarning("SettingsService not available - skipping auto-load");
                return;
            }

            if (!settingsService.AutoLoadLastSolution)
            {
                logger.LogInformation("Auto-load disabled - skipping");
                return;
            }

            var lastPath = settingsService.LastSolutionPath;
            if (string.IsNullOrEmpty(lastPath))
            {
                logger.LogInformation("No last solution path - skipping auto-load");
                return;
            }

            if (!File.Exists(lastPath))
            {
                logger.LogWarning("Last solution file not found: {LastPath}", lastPath);
                return;
            }

            logger.LogInformation("Auto-loading last solution: {LastPath}", lastPath);
            await mainWindowViewModel.LoadSolutionFromPathAsync(lastPath).ConfigureAwait(false);
            logger.LogInformation("Auto-load completed: {LastPath}", lastPath);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Auto-load failed");
        }
    }

    private static void HandleWindowClosed(IServiceProvider services, ILogger<App> logger)
    {
        logger.LogInformation("Window closed - performing cleanup");

        try
        {
            if (services is IDisposable disposableServices)
            {
                disposableServices.Dispose();
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Service disposal during window close failed");
        }

        Application.Current.Exit();
    }
}
