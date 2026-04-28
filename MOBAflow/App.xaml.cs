// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.WinUI;

using Backend.Data;
using Backend.Extensions;
using Backend.Interface;
using Backend.Service;

using Common.Configuration;
using Common.Events;
using Common.Extension;
using Common.Navigation;
using Common.Serilog;

using Converter;

using Domain;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.UI.Xaml;

using Display.Rendering;
using Display.Runtime;
using Display.Transport;

using Serilog;
using Serilog.Events;

using Service;

using SharedUI.Extensions;
using SharedUI.Interface;
using SharedUI.Service;
using SharedUI.Shell;
using SharedUI.ViewModel;

using Sound;

using TrackLibrary.PikoA;

using TrackPlan.Renderer;

using View;

using ViewModel;

/// <summary>
/// Provides application-specific behavior to supplement the default Application class.
/// </summary>
public partial class App
{
    private Window? _window;
    private readonly ILogger<App> _logger;

    /// <summary>
    /// Gets the main application window (for folder/file pickers and similar).
    /// </summary>
    public static Window? MainWindow => Current._window;

    /// <summary>
    /// Initializes the singleton application object. This is the first line of authored code
    /// executed, and as such is the logical equivalent of main() or WinMain().
    /// 
    /// PERFORMANCE NOTE: Kept minimal - heavy initialization deferred to PostStartupInitializationService
    /// after main window is visible.
    /// </summary>
    public App()
    {
        try
        {
            Services = ConfigureServices();
            _logger = Services.GetRequiredService<ILogger<App>>();

            InitializeComponent();

            // Register global UnhandledException handler for better diagnostics
            UnhandledException += OnUnhandledException;
        }
        catch (Exception ex)
        {
            // Serilog is configured first in ConfigureServices; use it when DI or ILogger<App> is not available yet.
            Log.Fatal(ex, "FATAL ERROR during App initialization");
            throw;
        }
    }

    private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        // Log the exception with full details before the debugger breaks
        var message = $"UNHANDLED EXCEPTION: {e.Exception.GetType().Name}: {e.Exception.Message}";

        _logger.LogCritical(e.Exception, "Unhandled exception in WinUI application: {Message}", message);

        // Mark as handled to prevent immediate termination (allows logging)
        // The debugger will still break due to App.g.i.cs handler
        e.Handled = false;
    }

    /// <summary>
    /// Gets the current <see cref="App"/> instance in use.
    /// </summary>
    public new static App Current => (App)Application.Current;

    /// <summary>
    /// Gets the <see cref="IServiceProvider"/> instance to resolve application services.
    /// </summary>
    public IServiceProvider Services { get; }

    /// <summary>
    /// Configures the services for the application.
    /// 
    /// PERFORMANCE OPTIMIZATION: Removed heavy operations from startup:
    /// - HealthCheckService: Deferred to PostStartupInitializationService
    /// - WebApp: Deferred to PostStartupInitializationService
    /// - Configuration validation: Deferred to background
    /// 
    /// Kept: Essential services for MainWindow and navigation
    /// </summary>
    private static IServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        // Serilog first so bootstrap diagnostics go to the same sinks as the rest of the app.
        ConfigureSerilog();

        // Load appsettings.json configuration (fast, file-based)
        var basePath = AppContext.BaseDirectory;
        var devJsonPath = Path.Combine(basePath, "appsettings.Development.json");
        var devJsonExists = File.Exists(devJsonPath);

        Log.Information("CONFIG BaseDirectory: {BasePath}", basePath);
        Log.Information("CONFIG appsettings.Development.json exists: {Exists}", devJsonExists);

        var configBuilder = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
#if DEBUG
            .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true)
#endif
            ;

        // Add User Secrets in Development (for developers without Azure App Config)
#if DEBUG
        configBuilder.AddUserSecrets<App>(optional: true);
        Log.Information("CONFIG User Secrets loaded (if configured)");
#endif

        // Add Azure App Configuration (if connection string is set)
        var azureAppConfigConnection = Environment.GetEnvironmentVariable("AZURE_APPCONFIG_CONNECTION");
        if (!string.IsNullOrWhiteSpace(azureAppConfigConnection))
        {
            try
            {
                configBuilder.AddAzureAppConfiguration(azureAppConfigConnection);
                Log.Information("CONFIG Azure App Configuration loaded");
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "CONFIG Azure App Configuration failed");
            }
        }
        else
        {
            Log.Information("CONFIG Azure App Configuration skipped (no connection string)");
        }

        var configuration = configBuilder.Build();

        Log.Information(
            "CONFIG IsTrainControlPageAvailable: {Value}",
            configuration["FeatureToggles:IsTrainControlPageAvailable"]);
        Log.Information(
            "CONFIG IsTrackPlanEditorPageAvailable: {Value}",
            configuration["FeatureToggles:IsTrackPlanEditorPageAvailable"]);

        // Register IConfiguration
        services.AddSingleton<IConfiguration>(configuration);

        // Register AppSettings with IOptions pattern
        services.Configure<AppSettings>(configuration);
        services.AddSingleton(sp => sp.GetRequiredService<IOptions<AppSettings>>().Value);

        // Register SpeechOptions (Sound service configuration)
        services.Configure<SpeechOptions>(configuration.GetSection("Speech"));

        // Logging (required by HealthCheckService and SpeechHealthCheck)
        services.AddLogging(loggingBuilder => loggingBuilder.AddSerilog(Log.Logger, dispose: true));

        services.AddUiDispatcher();

        services.AddEventBusWithUiDispatch();

        var corePages = NavigationRegistration.RegisterPages(services);
        services.AddSingleton(corePages);

        services.AddSingleton<IFeatureTogglePageProvider>(sp =>
            new FeatureTogglePageProvider(
                sp.GetRequiredService<List<PageMetadata>>(),
                sp.GetRequiredService<AppSettings>()));

        services.AddSingleton<SpeakerEngineFactory>();

        services.AddSingleton<ISpeakerEngine>(sp =>
        {
            var factory = sp.GetRequiredService<SpeakerEngineFactory>();
            return factory.CreateEngineFromOptions();
        });

        services.AddMobaBackendServices();
        services.AddSingleton<IMobaRuntime, MobaRuntimeService>();

        services.AddSingleton<IIoService, IoService>();
        services.AddSingleton<IUiDispatcher, UiDispatcher>();
        services.AddSingleton<PhotoHubClient>();

        services.AddHttpClient();

        services.AddSingleton<RestApiStatusService>(sp =>
        {
            var factory = sp.GetRequiredService<IHttpClientFactory>();
            return new RestApiStatusService(
                factory.CreateClient(),
                sp.GetRequiredService<AppSettings>(),
                sp.GetRequiredService<RestApiProcessService>(),
                sp.GetRequiredService<PhotoHubClient>(),
                sp.GetRequiredService<IRestApiStatusSink>(),
                sp.GetRequiredService<IUiDispatcher>(),
                sp.GetRequiredService<ILogger<RestApiStatusService>>());
        });

        services.AddSingleton<RestApiProcessService>();

        services.AddSingleton<ICityService>(sp => new CityService(
            sp.GetRequiredService<MasterDataStore>(),
            sp.GetRequiredService<ILogger<CityService>>()));

        services.AddSingleton<ILocomotiveService>(sp => new LocomotiveService(
            sp.GetRequiredService<MasterDataStore>(),
            sp.GetRequiredService<ILogger<LocomotiveService>>()));

        services.AddSingleton<ViessmannSignalService>(sp =>
            new ViessmannSignalService(sp.GetRequiredService<MasterDataStore>()));

        services.AddSingleton<ISettingsService>(sp => new SettingsService(
            sp.GetRequiredService<AppSettings>(),
            sp.GetRequiredService<ILogger<SettingsService>>()));

        services.AddSingleton<NavigationService>();
        services.AddSingleton<INavigationService>(sp => sp.GetRequiredService<NavigationService>());
        services.AddSingleton<IShellService, ShellService>();

        services.AddSingleton<ISoundPlayer, WindowsSoundPlayer>();
        services.AddSingleton<HealthCheckService>();

        services.AddSingleton<LayoutColumnWidthsViewModel>();
        services.AddSingleton(sp => new MainWindowViewModel(
            sp.GetRequiredService<LayoutColumnWidthsViewModel>(),
            sp.GetRequiredService<IMobaRuntime>(),
            sp.GetRequiredService<IEventBus>(),
            sp.GetRequiredService<IUiDispatcher>(),
            sp.GetRequiredService<AppSettings>(),
            sp.GetRequiredService<Solution>(),
            sp.GetRequiredService<ActionExecutionContext>(),
            sp.GetRequiredService<ILogger<MainWindowViewModel>>(),
            sp.GetRequiredService<IIoService>(),
            sp.GetRequiredService<ICityService>(),
            sp.GetRequiredService<ISettingsService>(),
            sp.GetRequiredService<AnnouncementService>(),
            sp.GetRequiredService<PhotoHubClient>(),
            sp.GetService<IFeatureTogglePageProvider>(),
            loggerFactory: sp.GetRequiredService<ILoggerFactory>()
        ));

        services.AddSingleton<IRestApiStatusSink>(sp => sp.GetRequiredService<MainWindowViewModel>());

        services.AddSingleton<JourneyMapViewModel>();
        services.AddTransient<MonitorPageViewModel>();
        services.AddSingleton<IFrameRenderer, SkiaFrameRenderer>();
        services.AddSingleton<UdpLineFrameSender>();
        services.AddSingleton<SerialLineFrameSender>();
        services.AddSingleton<IFrameSender, RoutingFrameSender>();
        services.AddTransient<FrameLoopScheduler>();
        services.AddTransient<DisplayPageViewModel>();
        services.AddSingleton(sp => new TrainControlViewModel(
            sp.GetRequiredService<IMobaRuntime>(),
            sp.GetRequiredService<ISettingsService>(),
            sp.GetRequiredService<MainWindowViewModel>(),
            sp.GetService<ILogger<TrainControlViewModel>>(),
            sp.GetService<IUiDispatcher>()
        ));

        services.AddSingleton<TrackPlan>();
        services.AddSingleton(sp => new TrackPlanViewModel(
            sp.GetRequiredService<TrackPlan>(),
            sp.GetRequiredService<AppSettings>(),
            sp.GetRequiredService<ISettingsService>(),
            sp.GetRequiredService<ILogger<TrackPlanViewModel>>()));
        services.AddSingleton<EditableTrackPlan>();
        services.AddSingleton<TrackPlanSolutionBinder>();
        services.AddSingleton<TrackPlanFeedbackHighlighter>();

        services.AddSingleton(sp => new MainWindow(
            sp.GetRequiredService<MainWindowViewModel>(),
            sp.GetRequiredService<NavigationService>(),
            sp.GetRequiredService<IUiDispatcher>(),
            sp.GetRequiredService<IIoService>(),
            sp.GetRequiredService<List<PageMetadata>>(),
            sp.GetRequiredService<AppSettings>(),
            sp.GetRequiredService<RestApiStatusService>(),
            sp.GetRequiredService<RestApiProcessService>(),
            sp.GetRequiredService<ILogger<MainWindow>>()));

        services.AddSingleton<PostStartupInitializationService>();
        services.AddSingleton<SpeechHealthCheck>();

        return services.BuildServiceProvider();
    }

    /// <summary>
    /// Configures Serilog for file logging to bin\Debug\logs folder and in-memory sink for MonitorPage.
    /// </summary>
    private static void ConfigureSerilog()
    {
        var logDirectory = Path.Combine(AppContext.BaseDirectory, "logs");
        Directory.CreateDirectory(logDirectory);

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .WriteTo.InMemory()  // Custom sink for MonitorPage real-time display
            .WriteTo.File(
                Path.Combine(logDirectory, "mobaflow-.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7,
                outputTemplate: "[{Timestamp:HH:mm:ss.fff} {Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();
    }

    /// <summary>
    /// Invoked when the application is launched.
    /// </summary>
    /// <param name="args">Details about the launch request and process.</param>
    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        try
        {
            // Load settings (including Layout) first so the singleton has persisted values before any View/ViewModel is created
            _ = Services.GetRequiredService<ISettingsService>();

            var appSettings = Services.GetRequiredService<AppSettings>();

            PhotoPathToImageConverter.SetPhotoBasePath(appSettings.Application.PhotoStoragePath);

            // Expose LayoutColumnWidths as app resource before MainWindow so pages can bind to it (e.g. TrackPlanPage with its own ViewModel)
            var layoutColumnWidths = Services.GetRequiredService<LayoutColumnWidthsViewModel>();
            Current.Resources["LayoutColumnWidths"] = layoutColumnWidths;

            _window = Services.GetRequiredService<MainWindow>();

            // Bridge EditableTrackPlan ↔ Project.TrackPlan (hybrid solution.json persistence).
            // Must be activated AFTER MainWindow/MainWindowViewModel are materialized.
            Services.GetRequiredService<TrackPlanSolutionBinder>().Activate();

            // Start listening for Z21 R-Bus feedback so placed tracks with a matching InPort pulse in the UI.
            Services.GetRequiredService<TrackPlanFeedbackHighlighter>().Activate();

            _window.Activate();

            // DEFERRED INITIALIZATION (async, doesn't block UI):
            // After MainWindow is visible, start deferred services (incl. RestApi process when Auto-start enabled)
            InitializePostStartupServicesAsync()
                .SafeFireAndForget(ex => _logger.LogError(ex, "Post-startup initialization failed unexpectedly"));

            // Auto-load last solution if enabled (async, non-blocking)
            AutoLoadLastSolutionAsync(((MainWindow)_window).ViewModel)
                .SafeFireAndForget(ex => _logger.LogError(ex, "Auto-load last solution failed unexpectedly"));

            _logger.LogInformation("Application UI launched (main window activated)");
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "OnLaunched failed");
            throw;
        }
    }

    /// <summary>
    /// Initializes deferred services after MainWindow is visible.
    /// This runs asynchronously and doesn't block the UI thread.
    /// </summary>
    private async Task InitializePostStartupServicesAsync()
    {
        try
        {
            var postStartupService = Services.GetRequiredService<PostStartupInitializationService>();
            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30)); // 30s timeout
            await postStartupService.InitializeAsync(cts.Token);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Post-startup initialization failed");
            // Continue - app should remain functional
        }
    }

    /// <summary>
    /// Automatically loads the last used solution if AutoLoadLastSolution preference is enabled.
    /// Delegates to MainWindowViewModel.LoadSolutionFromPathAsync() to ensure all initialization happens correctly.
    /// </summary>
    private async Task AutoLoadLastSolutionAsync(MainWindowViewModel mainWindowViewModel)
    {
        try
        {
            var settingsService = Services.GetService<ISettingsService>();
            if (settingsService == null)
            {
                _logger.LogWarning("SettingsService not available - skipping auto-load");
                return;
            }

            if (!settingsService.AutoLoadLastSolution)
            {
                _logger.LogInformation("Auto-load disabled - skipping");
                return;
            }

            var lastPath = settingsService.LastSolutionPath;
            if (string.IsNullOrEmpty(lastPath))
            {
                _logger.LogInformation("No last solution path - skipping auto-load");
                return;
            }

            if (!File.Exists(lastPath))
            {
                _logger.LogWarning("Last solution file not found: {LastPath}", lastPath);
                return;
            }

            _logger.LogInformation("Auto-loading last solution: {LastPath}", lastPath);

            // Use the SAME code path as manual loading!
            // This ensures JourneyManager and all other initialization happens correctly.
            await mainWindowViewModel.LoadSolutionFromPathAsync(lastPath);

            _logger.LogInformation("Auto-load completed: {LastPath}", lastPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Auto-load failed");
        }
    }
}