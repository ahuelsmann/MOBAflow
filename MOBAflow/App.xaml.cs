// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.WinUI;

using Backend.Data;
using Backend.Interface;
using Backend.Service;

using Common.Configuration;
using Common.Events;
using Common.Extension;
using Common.Navigation;
using Common.Serilog;

using Converter;

using Display.Rendering;
using Display.Runtime;
using Display.Transport;

using Domain;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.UI.Xaml;

using Moba.Backend;

using Serilog;
using Serilog.Events;

using Service;

using SharedUI.Extensions;
using SharedUI.Interface;
using SharedUI.Service;
using SharedUI.Shell;
using SharedUI.ViewModel;

using Sound;

using System.Diagnostics;

using TrackLibrary.PikoA;

using TrackPlan.Renderer;

using View;

/// <summary>
/// Provides application-specific behavior to supplement the default Application class.
/// </summary>
public partial class App
{
    private static readonly Stopwatch StartupStopwatch = Stopwatch.StartNew();
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
            LogStartupCheckpoint("App constructor started");
            Services = ConfigureServices();
            _logger = Services.GetRequiredService<ILogger<App>>();
            LogStartupCheckpoint("Services configured");

            InitializeComponent();
            LogStartupCheckpoint("App XAML initialized");

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
        var configureTimer = Stopwatch.StartNew();
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
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
#if DEBUG
            .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: false)
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

        services.AddSingleton<IUiDispatcher, UiDispatcher>();

        services.AddEventBusWithUiDispatch();

        var corePages = NavigationRegistration.RegisterPages(services);
        services.AddSingleton(corePages);

        services.AddSingleton<IFeatureTogglePageProvider>(sp =>
            new FeatureTogglePageProvider(
                sp.GetRequiredService<List<PageMetadata>>(),
                sp.GetRequiredService<AppSettings>()));

        services.AddSingleton<ISpeakerEngineRegistration, PiperSpeakerEngineRegistration>();
        services.AddSingleton<ISpeakerEngineRegistration, SystemSpeechEngineRegistration>();
        services.AddSingleton<SpeakerEngineFactory>();
        services.AddSingleton<ISpeakerEngineFactory>(sp => sp.GetRequiredService<SpeakerEngineFactory>());

        services.AddSingleton(sp =>
        {
            var factory = sp.GetRequiredService<SpeakerEngineFactory>();
            return factory.CreateEngineFromOptions();
        });

        services.AddSingleton<Solution>();
        services.AddMobaBackendServices();
        services.AddSingleton(sp => new AnnouncementService(
            sp.GetRequiredService<ISpeakerEngineFactory>(),
            sp.GetRequiredService<ILogger<AnnouncementService>>()));

        services.AddSingleton<IIoService, IoService>();
        services.AddSingleton<ISolutionIoService>(sp => sp.GetRequiredService<IIoService>());
        services.AddSingleton<IFilePickerService>(sp => sp.GetRequiredService<IIoService>());
        services.AddSingleton<IPhotoStorageService>(sp => sp.GetRequiredService<IIoService>());
        services.AddSingleton<PhotoHubClient>();
        services.AddSingleton<IPhotoHubClient>(sp => sp.GetRequiredService<PhotoHubClient>());

        services.AddHttpClient();

        services.AddSingleton(sp =>
        {
            var factory = sp.GetRequiredService<IHttpClientFactory>();
            return new RestApiStatusService(
                factory.CreateClient(),
                sp.GetRequiredService<AppSettings>(),
                sp.GetRequiredService<RestApiProcessService>(),
                sp.GetRequiredService<IPhotoHubClient>(),
                sp.GetRequiredService<IEventBus>(),
                sp.GetRequiredService<ILogger<RestApiStatusService>>());
        });

        services.AddSingleton<RestApiProcessService>();

        services.AddSingleton<ICityService>(sp => new CityService(
            sp.GetRequiredService<MasterDataStore>(),
            sp.GetRequiredService<ILogger<CityService>>()));

        services.AddSingleton<ILocomotiveService>(sp => new LocomotiveService(
            sp.GetRequiredService<MasterDataStore>(),
            sp.GetRequiredService<ILogger<LocomotiveService>>()));

        services.AddSingleton(sp =>
            new ViessmannSignalService(sp.GetRequiredService<MasterDataStore>()));

        services.AddSingleton<ISettingsService>(sp => new SettingsService(
            sp.GetRequiredService<AppSettings>(),
            sp.GetRequiredService<ILogger<SettingsService>>()));

        services.AddSingleton<NavigationService>();
        services.AddSingleton<INavigationService>(sp => sp.GetRequiredService<NavigationService>());
        services.AddSingleton<IShellService, ShellService>();

        // DialogService mit lazy XamlRoot-Auflösung (verhindert Startup-Deadlock)
        services.AddSingleton<IDialogService, DialogService>();

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
            featureTogglePageProvider: sp.GetService<IFeatureTogglePageProvider>(),
            loggerFactory: sp.GetRequiredService<ILoggerFactory>(),
            dialogService: sp.GetService<IDialogService>(),
            speechTestAction: async message =>
            {
                var speakerEngine = sp.GetRequiredService<SpeakerEngineFactory>().CreateEngineFromOptions();
                await speakerEngine.AnnouncementAsync(message, voiceName: null).ConfigureAwait(false);
            }
        ));

        services.AddSingleton<IJourneySelectionContext>(sp => sp.GetRequiredService<MainWindowViewModel>());
        services.AddSingleton<JourneyMapViewModel>();
        services.AddTransient<MonitorPageViewModel>();
        services.AddSingleton<IFrameRenderer, SkiaFrameRenderer>();
        services.AddSingleton<UdpLineFrameSender>();
        services.AddSingleton<IFrameSender, UdpLineFrameSender>();
        services.AddTransient<FrameLoopScheduler>();
        services.AddSingleton(sp => new TrainControlViewModel(
            sp.GetRequiredService<IMobaRuntime>(),
            sp.GetRequiredService<ISettingsService>(),
            sp.GetRequiredService<MainWindowViewModel>(),
            sp.GetService<ILogger<TrainControlViewModel>>(),
            sp.GetService<IUiDispatcher>(),
            sp.GetRequiredService<IEventBus>()
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
            sp.GetRequiredService<ILogger<WindowActivationService>>(),
            sp.GetRequiredService<ILogger<MainWindow>>()));

        services.AddSingleton<PostStartupInitializationService>();
        services.AddSingleton<SpeechHealthCheck>();

        var serviceProvider = services.BuildServiceProvider();
        Log.Information("[Startup] ConfigureServices completed in {ElapsedMs}ms", configureTimer.ElapsedMilliseconds);
        return serviceProvider;
    }

    private static void LogStartupCheckpoint(string checkpoint)
    {
        Log.Information("[Startup] {Checkpoint} at {ElapsedMs}ms", checkpoint, StartupStopwatch.ElapsedMilliseconds);
    }

    /// <summary>
    /// Configures Serilog with async file logging, environment and process enrichment.
    /// Uses Async-Sink for non-blocking file I/O, InMemory sink for MonitorPage display.
    /// </summary>
    private static void ConfigureSerilog()
    {
        var logDirectory = Path.Combine(AppContext.BaseDirectory, "logs");
        Directory.CreateDirectory(logDirectory);

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .Enrich.WithMachineName()
            .Enrich.WithProcessId()
            .Enrich.WithProcessName()
            .Enrich.WithThreadId()
            .WriteTo.InMemory()  // Custom sink for MonitorPage real-time display
            .WriteTo.Async(a => a.File(
                Path.Combine(logDirectory, "mobaflow-.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7,
                outputTemplate: "[{Timestamp:HH:mm:ss.fff} {Level:u3}] [{MachineName}] [{ProcessId}:{ProcessName}] [{ThreadId}] [{SourceContext}] {Message:lj}{NewLine}{Exception}"),
                bufferSize: 1000)  // Bounded buffer for memory safety under high load
            .CreateLogger();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        _ = args;

        try
        {
            var launchTimer = Stopwatch.StartNew();
            LogStartupCheckpoint("OnLaunched started");

            // Load settings first so the singleton has persisted values before any View/ViewModel is created.
            _ = Services.GetRequiredService<ISettingsService>();
            LogStartupCheckpoint("Settings service resolved");

            var appSettings = Services.GetRequiredService<AppSettings>();
            PhotoPathToImageConverter.SetPhotoBasePath(appSettings.Application.PhotoStoragePath);

            // Expose LayoutColumnWidths before MainWindow so pages can bind to it (e.g. TrackPlanPage).
            var layoutColumnWidths = Services.GetRequiredService<LayoutColumnWidthsViewModel>();
            Current.Resources["LayoutColumnWidths"] = layoutColumnWidths;
            LogStartupCheckpoint("Layout resources prepared");

            _window = Services.GetRequiredService<MainWindow>();
            LogStartupCheckpoint("MainWindow resolved");

            _window.Closed += OnWindowClosed;

            _window.Activate();
            LogStartupCheckpoint("MainWindow activated");

            if (_window is MainWindow mainWindow)
            {
                mainWindow.ScheduleDeferredUiStartup();
            }

            InitializePostStartupServicesAsync()
                .SafeFireAndForget(ex => _logger.LogError(ex, "Post-startup initialization failed unexpectedly"));

            AutoLoadLastSolutionAsync(((MainWindow)_window).ViewModel)
                .SafeFireAndForget(ex => _logger.LogError(ex, "Auto-load last solution failed unexpectedly"));

            _logger.LogInformation(
                "Application UI launched (main window activated) in {ElapsedMs}ms",
                launchTimer.ElapsedMilliseconds);
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
            var timer = Stopwatch.StartNew();
            LogStartupCheckpoint("Post-startup initialization queued");

            var runtime = Services.GetRequiredService<IMobaRuntime>();
            await runtime.StartAsync().ConfigureAwait(false);

            // Bridge EditableTrackPlan <-> Project.TrackPlan after the first window activation.
            Services.GetRequiredService<TrackPlanSolutionBinder>().Activate();

            // Start listening for Z21 R-Bus feedback after the visible shell is up.
            Services.GetRequiredService<TrackPlanFeedbackHighlighter>().Activate();

            var postStartupService = Services.GetRequiredService<PostStartupInitializationService>();
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            await postStartupService.InitializeAsync(cts.Token).ConfigureAwait(false);

            _logger.LogInformation(
                "[Startup] Post-startup initialization completed in {ElapsedMs}ms",
                timer.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Post-startup initialization failed");
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
            await mainWindowViewModel.LoadSolutionFromPathAsync(lastPath).ConfigureAwait(false);
            _logger.LogInformation("Auto-load completed: {LastPath}", lastPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Auto-load failed");
        }
    }

    /// <summary>
    /// Windows App SDK 2.0: Explicit window closing for better resource cleanup.
    /// Ensures proper disposal of services and graceful shutdown.
    /// </summary>
    private void OnWindowClosed(object sender, WindowEventArgs args)
    {
        _ = sender;
        _ = args;
        _logger.LogInformation("Window closed - performing cleanup");

        try
        {
            if (Services is IDisposable disposableServices)
            {
                disposableServices.Dispose();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Service disposal during window close failed");
        }

        Current.Exit();
    }
}