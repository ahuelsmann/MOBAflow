// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.WinUI;

using Backend.Extensions;
using Backend.Interface;
using Backend.Service;
using Common.Configuration;
using Common.Serilog;
using Controllers;
using Domain;
using Hubs;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Moba.WinUI.ViewModel;
using Serilog;
using Serilog.Events;
using Service;
using SharedUI.Interface;
using SharedUI.Service;
using SharedUI.Shell;
using SharedUI.ViewModel;
using Sound;
using System.Diagnostics;
using TrackPlan.Renderer;
using Utilities;
using View;
using ILogger = Microsoft.Extensions.Logging.ILogger;

/// <summary>
/// Provides application-specific behavior to supplement the default Application class.
/// </summary>
public partial class App
{
    private Window? _window;
    private IHost? _webAppHost;
    private UdpDiscoveryResponder? _udpDiscoveryResponder;
    private readonly ILogger<App> _logger;

    /// <summary>
    /// Initializes the singleton application object. This is the first line of authored code
    /// executed, and as such is the logical equivalent of main() or WinMain().
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

        _logger?.LogCritical(e.Exception, "Unhandled exception in WinUI application");

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
    /// </summary>
    private static IServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        // Load appsettings.json configuration
        // In DEBUG mode, also load appsettings.Development.json to enable all feature toggles
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

        // Debug: Print loaded FeatureToggle values
        Debug.WriteLine($"[CONFIG] IsTrainControlPageAvailable: {configuration["FeatureToggles:IsTrainControlPageAvailable"]}");
        Debug.WriteLine($"[CONFIG] IsTrackPlanEditorPageAvailable: {configuration["FeatureToggles:IsTrackPlanEditorPageAvailable"]}");
        Debug.WriteLine($"[CONFIG] IsJourneyMapPageAvailable: {configuration["FeatureToggles:IsJourneyMapPageAvailable"]}");
        Debug.WriteLine($"[CONFIG] IsMonitorPageAvailable: {configuration["FeatureToggles:IsMonitorPageAvailable"]}");

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

        // Navigation infrastructure
        var navigationRegistry = new NavigationRegistry();
        services.AddSingleton(navigationRegistry);

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
        // Backend now registers: ActionExecutionContext, AnnouncementService, ActionExecutor
        services.AddMobaBackendServices();

        services.AddSingleton<IIoService, IoService>();
        services.AddSingleton<IUiDispatcher, UiDispatcher>();
        services.AddSingleton<PhotoHubClient>();  // Real-time photo notifications from MAUI

        // ICityService with NullObject fallback
        // Always register a service (real or no-op)
        services.AddSingleton<ICityService>(sp =>
        {
            try
            {
                var appSettings = sp.GetRequiredService<AppSettings>();
                var logger = sp.GetRequiredService<ILogger<CityService>>();
                return new CityService(appSettings, logger);
            }
            catch
            {
                return new NullCityService();
            }
        });

        // ILocomotiveService with NullObject fallback
        // Always register a service (real or no-op)
        services.AddSingleton<ILocomotiveService>(sp =>
        {
            try
            {
                var appSettings = sp.GetRequiredService<AppSettings>();
                var logger = sp.GetRequiredService<ILogger<LocomotiveService>>();
                return new LocomotiveService(appSettings, logger);
            }
            catch
            {
                return new NullLocomotiveService();
            }
        });

        // ISettingsService with NullObject fallback
        // Always register a service (real or no-op)
        services.AddSingleton<ISettingsService>(sp =>
        {
            try
            {
                var appSettings = sp.GetRequiredService<AppSettings>();
                var logger = sp.GetRequiredService<ILogger<SettingsService>>();
                return new SettingsService(appSettings, logger);
            }
            catch
            {
                return new NullSettingsService();
            }
        });

        services.AddSingleton<NavigationService>();
        services.AddSingleton<INavigationService>(sp => sp.GetRequiredService<NavigationService>());
        services.AddSingleton<IPageFactory, PageFactory>();
        services.AddSingleton<IShellService, ShellService>();
        // SnapToConnectService removed - depends on old TrackPlan.Domain architecture

        // Sound Services (required by HealthCheckService)
        services.AddSingleton<ISoundPlayer, WindowsSoundPlayer>();
        services.AddSingleton<SpeechHealthCheck>();
        services.AddSingleton<HealthCheckService>();

        // ViewModels
        // Note: Wrapper ViewModels (SolutionViewModel, ProjectViewModel, JourneyViewModel, etc.)
        // are created with 'new' at runtime because they wrap Domain models.
        // Only "standalone" ViewModel that don't wrap models are registered here.

        // Signal Box ViewModels (factory-created on demand for each element)
        // These are not registered as singletons because they wrap individual domain elements
        // Instead, they are created dynamically in SignalBoxPage.cs as elements are added

        // Domain.Solution is registered in AddMobaBackendServices()

        services.AddSingleton(sp => new MainWindowViewModel(
            sp.GetRequiredService<IZ21>(),
            sp.GetRequiredService<WorkflowService>(),
            sp.GetRequiredService<IUiDispatcher>(),
            sp.GetRequiredService<AppSettings>(),
            sp.GetRequiredService<Solution>(),
            sp.GetRequiredService<ActionExecutionContext>(),  // Inject context with all audio services
            sp.GetRequiredService<ILogger<MainWindowViewModel>>(),  // Inject ILogger
            sp.GetRequiredService<IIoService>(),
            sp.GetRequiredService<ICityService>(),      // Now guaranteed (NullObject if unavailable)
            sp.GetRequiredService<ISettingsService>(),  // Now guaranteed (NullObject if unavailable)
            sp.GetRequiredService<AnnouncementService>(),  // For TestSpeech command
            sp.GetRequiredService<PhotoHubClient>()  // Real-time photo notifications
        ));
        // Old TrackPlanEditorViewModel removed - now using TrackPlanEditorViewModel2 from TrackPlan.Editor
        services.AddSingleton<JourneyMapViewModel>();
        
        // MonitorPageViewModel: Transient so Dispose() is called when page is navigated away
        // InMemorySink retains logs globally, so they reload on return
        services.AddTransient<MonitorPageViewModel>();
        
        services.AddSingleton<SkinSelectorViewModel>();
        services.AddSingleton(sp => new TrainControlViewModel(
            sp.GetRequiredService<IZ21>(),
            sp.GetRequiredService<IUiDispatcher>(),
            sp.GetRequiredService<ISettingsService>(),
            sp.GetRequiredService<MainWindowViewModel>(),
            sp.GetService<ILogger<TrainControlViewModel>>()
        ));

        // TrackPlan (model and ViewModel)
        services.AddSingleton<TrackPlan>();
        services.AddSingleton<TrackPlanViewModel>();

        // Pages (Transient = new instance per navigation)
        // Registration: tag, title, iconGlyph, pageType, source, category, order, featureToggleKey, badgeLabelKey, pathIconData, isBold
        NavigationRegistration.RegisterPages(services, navigationRegistry);

        // Skin Provider
        services.AddSingleton<ISkinProvider, SkinProvider>();
        // NOTE: SkinSelectorControl wird dynamisch in SettingsPage erstellt


        // MainWindow (Singleton = one instance for app lifetime)
        services.AddSingleton<MainWindow>();

        // Load plugins after core registrations so they can override/add
        var pluginDirectory = Path.Combine(AppContext.BaseDirectory, "Plugins");
        var pluginLoader = new PluginLoader(pluginDirectory, navigationRegistry);

        // Note: We use synchronous Load here because ConfigureServices is synchronous
        // Plugin initialization will happen later in OnLaunched after DI is fully set up

        var loadTask = Task.Run(() => pluginLoader.LoadPluginsAsync(services, logger: null));
        loadTask.Wait(); // Block until plugins are loaded

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

            // Optionally start WebApp (REST/API) alongside WinUI
            _ = StartWebAppIfEnabledAsync();

            // Auto-load last solution if enabled
            _ = AutoLoadLastSolutionAsync(((MainWindow)_window).ViewModel);

            Debug.WriteLine("[OnLaunched] COMPLETE");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[OnLaunched] FATAL ERROR: {ex.GetType().Name}: {ex.Message}");
            Debug.WriteLine($"[OnLaunched] StackTrace: {ex.StackTrace}");
            _logger?.LogCritical(ex, "OnLaunched failed");
            throw;
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

    private async Task StartWebAppIfEnabledAsync()
    {
        try
        {
            var settings = Services.GetRequiredService<AppSettings>();
            if (!settings.Application.AutoStartWebApp)
            {
                _logger.LogInformation("AutoStartWebApp disabled in settings - skipping REST API launch. To enable: set Application.AutoStartWebApp to true.");
                return;
            }

            if (_webAppHost != null)
            {
                _logger.LogInformation("REST API already running");
                return;
            }

            var restPort = settings.RestApi.Port;

            // Check if port is available BEFORE attempting to start
            if (!PortChecker.IsPortAvailable(restPort))
            {
                var errorMessage = $"Cannot start REST API: Port {restPort} is already in use by another application.\n\n" +
                                   $"Please either:\n" +
                                   "1. Close the application using this port, or\n" +
                                   "2. Change the REST API port in Settings, or\n" +
                                   "3. Disable 'Auto-start REST API' in Settings";

                _logger.LogError("Port {RestPort} is already in use - cannot start REST API", restPort);

                // Show error dialog to user
                await ShowPortInUseErrorAsync(restPort, errorMessage);
                return;
            }

            // NOTE: FirewallHelper removed - it caused UAC elevation dialogs on every start.
            // Users should manually configure Windows Firewall if needed.
            // See docs/wiki/FIREWALL-SETUP.md for instructions.

            // Build WebHost for in-process Kestrel hosting
            var builder = Host.CreateDefaultBuilder();

            builder.ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseKestrel(options => options.ListenAnyIP(restPort));

                webBuilder.Configure(app =>
                {
                    // CORS must be before routing (required for MAUI mobile clients)
                    app.UseCors();

                    // Map SignalR Hub for photo notifications
                    app.UseRouting();
                    app.UseEndpoints(endpoints =>
                    {
                        endpoints.MapControllers();
                        endpoints.MapHub<PhotoHub>("/photos-hub");
                    });
                });
            });

            builder.ConfigureServices((_, services) =>
            {
                // Register MOBAflow Web Services
                services.AddSingleton<PhotoStorageService>();

                // Explicitly add controllers from this assembly (WinUI)
                // Required because WinUI apps don't auto-discover controllers like ASP.NET Core web apps
                services.AddControllers()
                    .AddApplicationPart(typeof(PhotoUploadController).Assembly);

                // Register SignalR for real-time photo notifications
                services.AddSignalR();

                // CORS: Allow requests from any origin (required for MAUI mobile clients)
                services.AddCors(options =>
                {
                    options.AddDefaultPolicy(policy =>
                    {
                        policy.AllowAnyOrigin()
                              .AllowAnyMethod()
                              .AllowAnyHeader();
                    });
                });
            });

            builder.ConfigureLogging(loggingBuilder =>
                // Use the same Serilog logger as WinUI
                loggingBuilder.AddSerilog(Log.Logger, dispose: false));

            _webAppHost = builder.Build();

            // Start WebHost asynchronously
            _ = _webAppHost.RunAsync();

            _logger.LogInformation("REST API started successfully on {RestPort}", restPort);
            _logger.LogInformation("Endpoints: POST /api/photos/upload; GET /api/photos/health");

            // Give Kestrel a moment to bind
            await Task.Delay(1000);

            // Start UDP Discovery responder for MAUI auto-discovery
            StartUdpDiscoveryResponder(restPort);

            // Start SignalR PhotoHubClient for real-time photo notifications
            _ = StartPhotoHubClientAsync(restPort);

            // Self-test: Verify server is actually responding
            try
            {
                using var httpClient = new HttpClient();
                httpClient.Timeout = TimeSpan.FromSeconds(5);
                var testResponse = await httpClient.GetAsync($"http://localhost:{restPort}/api/photos/health");
                if (testResponse.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Self-test passed: server responding on localhost:{RestPort}", restPort);
                }
                else
                {
                    _logger.LogWarning("Self-test warning: server returned {StatusCode}", testResponse.StatusCode);
                }
            }
            catch (Exception selfTestEx)
            {
                _logger.LogWarning(selfTestEx, "Self-test failed. This may indicate controller registration issues.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start REST API");
        }
    }

    /// <summary>
    /// Starts the UDP Discovery responder for MAUI auto-discovery.
    /// Allows MAUI clients to find the REST API server on the local network.
    /// </summary>
    private void StartUdpDiscoveryResponder(int restPort)
    {
        try
        {
            _udpDiscoveryResponder?.Dispose();
            _udpDiscoveryResponder = new UdpDiscoveryResponder(
                Services.GetRequiredService<ILogger<UdpDiscoveryResponder>>(),
                restPort);
            _udpDiscoveryResponder.Start();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to start UDP Discovery responder - MAUI auto-discovery will not work");
        }
    }

    /// <summary>
    /// Starts PhotoHubClient for real-time photo notifications from MAUI.
    /// </summary>
    private async Task StartPhotoHubClientAsync(int restPort)
    {
        _logger.LogInformation("StartPhotoHubClientAsync called with port: {RestPort}", restPort);

        try
        {
            _logger.LogDebug("Resolving PhotoHubClient from DI...");
            var photoHubClient = Services.GetRequiredService<PhotoHubClient>();

            _logger.LogDebug("Resolving MainWindowViewModel from DI...");
            var mainWindowViewModel = Services.GetRequiredService<MainWindowViewModel>();

            _logger.LogDebug("Resolving IUiDispatcher from DI...");
            var uiDispatcher = Services.GetRequiredService<IUiDispatcher>();

            _logger.LogInformation("Connecting to SignalR Hub at localhost:{RestPort}...", restPort);
            await photoHubClient.ConnectAsync("localhost", restPort);
            _logger.LogInformation("Connected to SignalR Hub");

            _logger.LogDebug("Subscribing to PhotoUploaded events...");
            photoHubClient.PhotoUploaded += async (photoPath, uploadedAt) =>
            {
                try
                {
                    _logger.LogInformation("[REAL-TIME] Photo uploaded: {PhotoPath} at {UploadedAt}", photoPath, uploadedAt);

                    uiDispatcher.InvokeOnUi(() =>
                    {
                        try
                        {
                            _logger.LogDebug("Assigning photo to selected item");
                            mainWindowViewModel.AssignLatestPhoto(photoPath);
                            _logger.LogInformation("Photo assigned successfully");
                        }
                        catch (Exception innerEx)
                        {
                            _logger.LogError(innerEx, "Error assigning photo");
                        }
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error handling PhotoUploaded event");
                }

                await Task.CompletedTask;
            };
            _logger.LogInformation("Subscribed to PhotoUploaded events");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start PhotoHubClient");
        }
    }

    /// <summary>
    /// Shows an error dialog when the REST API port is already in use.
    /// </summary>
    private async Task ShowPortInUseErrorAsync(int port, string message)
    {
        try
        {
            // Use UI dispatcher to show dialog on UI thread
            var uiDispatcher = Services.GetRequiredService<IUiDispatcher>();
            await uiDispatcher.InvokeOnUiAsync(async () =>
            {
                var dialog = new ContentDialog
                {
                    Title = $"Warning: Port {port} Already In Use",
                    Content = message,
                    CloseButtonText = "OK",
                    XamlRoot = _window?.Content?.XamlRoot
                };

                _ = await dialog.ShowAsync();
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to show port error dialog");
        }
    }
}