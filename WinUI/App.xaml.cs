// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.WinUI;

using Backend.Extensions;
using Backend.Interface;
using Backend.Service;

using Common.Configuration;
using Common.Events;
using Common.Serilog;

using Domain;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.UI.Xaml;

using Serilog;
using Serilog.Events;

using Service;

using SharedUI.Extensions;
using SharedUI.Interface;
using SharedUI.Shell;
using SharedUI.ViewModel;

using Sound;

using System.Diagnostics;

using TrackLibrary.PikoA;

using TrackPlan.Renderer;

using View;

using ViewModel;

using Common.Extension;

/// <summary>
/// Provides application-specific behavior to supplement the default Application class.
/// </summary>
public partial class App
{
    private Window? _window;
    private readonly ILogger<App> _logger;

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
            Debug.WriteLine("[App] Constructor START");

            Services = ConfigureServices();
            Debug.WriteLine("[App] Services configured");

            _logger = Services.GetRequiredService<ILogger<App>>();
            Debug.WriteLine("[App] Logger resolved");

            InitializeComponent();
            Debug.WriteLine("[App] InitializeComponent completed");

            // Register global UnhandledException handler for better diagnostics
            UnhandledException += OnUnhandledException;
            Debug.WriteLine("[App] UnhandledException handler registered");

            Debug.WriteLine("[App] Constructor COMPLETE");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[App] FATAL ERROR: {ex.GetType().Name}: {ex.Message}");
            Debug.WriteLine($"[App] StackTrace: {ex.StackTrace}");
            _logger?.LogCritical(ex, "FATAL ERROR during App initialization");
            throw;
        }
    }

    private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        // Log the exception with full details before the debugger breaks
        var message = $"UNHANDLED EXCEPTION: {e.Exception.GetType().Name}: {e.Exception.Message}";
        var stackTrace = e.Exception.StackTrace ?? "(no stack trace)";

        Debug.WriteLine(message);
        Debug.WriteLine(stackTrace);

        _logger.LogCritical(e.Exception, "Unhandled exception in WinUI application");

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

        // Load appsettings.json configuration (fast, file-based)
        var basePath = AppContext.BaseDirectory;
        var devJsonPath = Path.Combine(basePath, "appsettings.Development.json");
        var devJsonExists = File.Exists(devJsonPath);

        Debug.WriteLine($"[CONFIG] BaseDirectory: {basePath}");
        Debug.WriteLine($"[CONFIG] appsettings.Development.json exists: {devJsonExists}");

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
        Debug.WriteLine("[CONFIG] User Secrets loaded (if configured)");
#endif

        // Add Azure App Configuration (if connection string is set)
        var azureAppConfigConnection = Environment.GetEnvironmentVariable("AZURE_APPCONFIG_CONNECTION");
        if (!string.IsNullOrWhiteSpace(azureAppConfigConnection))
        {
            try
            {
                configBuilder.AddAzureAppConfiguration(azureAppConfigConnection);
                Debug.WriteLine("[CONFIG] Azure App Configuration loaded");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[CONFIG] Azure App Configuration failed: {ex.Message}");
            }
        }
        else
        {
            Debug.WriteLine("[CONFIG] Azure App Configuration skipped (no connection string)");
        }

        var configuration = configBuilder.Build();

        Debug.WriteLine($"[CONFIG] IsTrainControlPageAvailable: {configuration["FeatureToggles:IsTrainControlPageAvailable"]}");
        Debug.WriteLine($"[CONFIG] IsTrackPlanEditorPageAvailable: {configuration["FeatureToggles:IsTrackPlanEditorPageAvailable"]}");

        // Register IConfiguration
        services.AddSingleton<IConfiguration>(configuration);

        // Register AppSettings with IOptions pattern
        services.Configure<AppSettings>(configuration);
        services.AddSingleton(sp => sp.GetRequiredService<IOptions<AppSettings>>().Value);

        // Register SpeechOptions (Sound service configuration)
        services.Configure<SpeechOptions>(configuration.GetSection("Speech"));

        // Configure Serilog for file logging
        ConfigureSerilog();

        // Logging (required by HealthCheckService and SpeechHealthCheck)
        services.AddLogging(loggingBuilder => loggingBuilder.AddSerilog(Log.Logger, dispose: true));

        // UI Thread Dispatcher (platform-specific) – vor EventBus, da Decorator ihn benötigt
        services.AddUiDispatcher();

        // Event Bus mit UI-Thread-Marshalling: alle Handler laufen auf dem UI-Thread
        services.AddEventBusWithUiDispatch();

        // Navigation infrastructure - discover and register pages
        // Pages (Auto-discovery + Custom DI)
        var corePages = NavigationRegistration.RegisterPages(services);
        services.AddSingleton(corePages);

        // Register Speech Engine Factory for dynamic engine switching
        services.AddSingleton<SpeakerEngineFactory>();

        // Register ISpeakerEngine that uses factory (for ActionExecutionContext)
        // This provides a default engine, but AnnouncementService will create fresh engines
        services.AddSingleton<ISpeakerEngine>(sp =>
        {
            var factory = sp.GetRequiredService<SpeakerEngineFactory>();
            return factory.CreateEngineFromOptions();
        });

        // Backend Services (Interfaces are in Backend.Interface and Backend.Network)
        // Use shared extension method for platform-consistent registration
        services.AddMobaBackendServices();

        services.AddSingleton<IIoService, IoService>();
        services.AddSingleton<IUiDispatcher, UiDispatcher>();
        services.AddSingleton<PhotoHubClient>();  // Real-time photo notifications from MAUI

        // ICityService mit DataManager (Stammdaten aus gemeinsamer Datei)
        services.AddSingleton<ICityService>(sp =>
        {
            try
            {
                var dataManager = sp.GetRequiredService<Backend.Data.DataManager>();
                var logger = sp.GetRequiredService<ILogger<CityService>>();
                return new CityService(dataManager, logger);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[DI] CityService failed, using NullCityService: {ex.Message}");
                return new NullCityService();
            }
        });

        // ILocomotiveService mit DataManager (Stammdaten aus gemeinsamer Datei)
        services.AddSingleton<ILocomotiveService>(sp =>
        {
            try
            {
                var dataManager = sp.GetRequiredService<Backend.Data.DataManager>();
                var logger = sp.GetRequiredService<ILogger<LocomotiveService>>();
                return new LocomotiveService(dataManager, logger);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[DI] LocomotiveService failed, using NullLocomotiveService: {ex.Message}");
                return new NullLocomotiveService();
            }
        });

        // Viessmann Multiplex-Signale aus data.json (Stellwerk ComboBox-Optionen)
        services.AddSingleton<ViessmannSignalService>(sp =>
            new ViessmannSignalService(sp.GetRequiredService<Backend.Data.DataManager>()));

        // ISettingsService with NullObject fallback
        services.AddSingleton<ISettingsService>(sp =>
        {
            try
            {
                var appSettings = sp.GetRequiredService<AppSettings>();
                var logger = sp.GetRequiredService<ILogger<SettingsService>>();
                return new SettingsService(appSettings, logger);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[DI] SettingsService failed, using NullSettingsService: {ex.Message}");
                return new NullSettingsService();
            }
        });

        services.AddSingleton<NavigationService>();
        services.AddSingleton<INavigationService>(sp => sp.GetRequiredService<NavigationService>());
        services.AddSingleton<IPageFactory, PageFactory>();
        services.AddSingleton<IShellService, ShellService>();

        // Sound Services (required by HealthCheckService)
        // HealthCheckService initialization deferred to PostStartupInitializationService
        services.AddSingleton<ISoundPlayer, WindowsSoundPlayer>();
        services.AddSingleton<HealthCheckService>();
        // NOTE: SpeechHealthCheck and HealthCheckService are initialized during post-startup

        // ViewModels
        services.AddSingleton(sp => new MainWindowViewModel(
            sp.GetRequiredService<IZ21>(),
            sp.GetRequiredService<IEventBus>(),
            sp.GetRequiredService<IWorkflowService>(),
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
            sp.GetService<ITripLogService>()
        ));

        services.AddSingleton<JourneyMapViewModel>();
        services.AddTransient<MonitorPageViewModel>();
        services.AddSingleton<SkinSelectorViewModel>();
        services.AddSingleton(sp => new TrainControlViewModel(
            sp.GetRequiredService<IZ21>(),
            sp.GetRequiredService<IEventBus>(),
            sp.GetRequiredService<ISettingsService>(),
            sp.GetRequiredService<MainWindowViewModel>(),
            sp.GetService<ITripLogService>(),
            sp.GetService<ILogger<TrainControlViewModel>>(),
            sp.GetService<IUiDispatcher>()
        ));

        // TrackPlan (model and ViewModel)
        services.AddSingleton<TrackPlan>();
        services.AddSingleton<TrackPlanViewModel>();
        services.AddSingleton<EditableTrackPlan>();

        // Skin Provider
        services.AddSingleton<ISkinProvider, SkinProvider>();

        // MainWindow (Singleton = one instance for app lifetime)
        services.AddSingleton<MainWindow>();

        // DEFERRED: PostStartupInitializationService (runs after MainWindow.Loaded)
        // This calls pluginLoader.InitializePluginsAsync() to complete plugin initialization
        services.AddSingleton<PostStartupInitializationService>();
        services.AddSingleton<SpeechHealthCheck>();  // Lazy init in deferred service

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
            Debug.WriteLine("[OnLaunched] START");

            // Initialize SkinProvider with saved settings before creating MainWindow
            var skinProvider = Services.GetRequiredService<ISkinProvider>();
            Debug.WriteLine("[OnLaunched] SkinProvider resolved");

            var appSettings = Services.GetRequiredService<AppSettings>();
            Debug.WriteLine("[OnLaunched] AppSettings resolved");

            skinProvider.Initialize(appSettings);
            Debug.WriteLine("[OnLaunched] SkinProvider initialized");

            _window = Services.GetRequiredService<MainWindow>();
            Debug.WriteLine("[OnLaunched] MainWindow created");

            _window.Activate();
            Debug.WriteLine("[OnLaunched] MainWindow activated");

            // DEFERRED INITIALIZATION (async, doesn't block UI):
            // After MainWindow is visible, start deferred services
            InitializePostStartupServicesAsync()
                .SafeFireAndForget(ex => _logger.LogError(ex, "Post-startup initialization failed unexpectedly"));

            // Auto-load last solution if enabled (async, non-blocking)
            AutoLoadLastSolutionAsync(((MainWindow)_window).ViewModel)
                .SafeFireAndForget(ex => _logger.LogError(ex, "Auto-load last solution failed unexpectedly"));

            Debug.WriteLine("[OnLaunched] COMPLETE");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[OnLaunched] FATAL ERROR: {ex.GetType().Name}: {ex.Message}");
            Debug.WriteLine($"[OnLaunched] StackTrace: {ex.StackTrace}");
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