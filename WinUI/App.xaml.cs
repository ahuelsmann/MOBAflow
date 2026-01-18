// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.WinUI;

using Backend.Extensions;
using Backend.Interface;
using Backend.Service;

using Common.Configuration;
using Common.Serilog;

using Controls;

using Domain;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.UI.Xaml;

using Moba.SharedUI.Service;
using Moba.SharedUI.Shell;
using Moba.TrackPlan.Editor;

using Serilog;
using Serilog.Events;

using Service;

using SharedUI.Interface;
using SharedUI.ViewModel;

using Sound;

using System.IO;

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
            Services = ConfigureServices();
            _logger = Services.GetRequiredService<ILogger<App>>();
            InitializeComponent();

            // Register global UnhandledException handler for better diagnostics
            UnhandledException += OnUnhandledException;
        }
        catch (Exception ex)
        {
            _logger?.LogCritical(ex, "FATAL ERROR during App initialization");
            throw;
        }
    }

    private void OnUnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        // Log the exception with full details before the debugger breaks
        var message = $"UNHANDLED EXCEPTION: {e.Exception.GetType().Name}: {e.Exception.Message}";
        var stackTrace = e.Exception.StackTrace ?? "(no stack trace)";

        System.Diagnostics.Debug.WriteLine(message);
        System.Diagnostics.Debug.WriteLine(stackTrace);

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

        System.Diagnostics.Debug.WriteLine($"[CONFIG] BaseDirectory: {basePath}");
        System.Diagnostics.Debug.WriteLine($"[CONFIG] appsettings.Development.json exists: {devJsonExists}");

        var configuration = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
#if DEBUG
            .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true)
#endif
            .Build();

        // Debug: Print loaded FeatureToggle values
        System.Diagnostics.Debug.WriteLine($"[CONFIG] IsTrainControlPageAvailable: {configuration["FeatureToggles:IsTrainControlPageAvailable"]}");
        System.Diagnostics.Debug.WriteLine($"[CONFIG] IsTrackPlanEditorPageAvailable: {configuration["FeatureToggles:IsTrackPlanEditorPageAvailable"]}");
        System.Diagnostics.Debug.WriteLine($"[CONFIG] IsJourneyMapPageAvailable: {configuration["FeatureToggles:IsJourneyMapPageAvailable"]}");
        System.Diagnostics.Debug.WriteLine($"[CONFIG] IsMonitorPageAvailable: {configuration["FeatureToggles:IsMonitorPageAvailable"]}");

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

        // Register ISpeakerEngine with lazy initialization
        // Only creates the CONFIGURED engine, not both
        // Decision is made at registration time based on AppSettings
        services.AddSingleton<ISpeakerEngine>(sp =>
        {
            var settings = sp.GetRequiredService<AppSettings>();
            sp.GetService<ILogger>();

            // Check if Azure Cognitive Services is configured
            var selectedEngine = settings.Speech.SpeakerEngineName;
            if (!string.IsNullOrEmpty(selectedEngine) &&
                selectedEngine.Contains("Azure", StringComparison.OrdinalIgnoreCase))
            {
                // Only create Azure engine if explicitly configured
                var options = sp.GetRequiredService<IOptions<SpeechOptions>>();
                return new CognitiveSpeechEngine(options, sp.GetService<ILogger<CognitiveSpeechEngine>>()!);
            }

            // Default: Windows SAPI (always available, no Azure SDK needed)
            return new SystemSpeechEngine(sp.GetService<ILogger<SystemSpeechEngine>>()!);
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

        // TrackPlan.Editor Services (new TopologyGraph-based architecture)
        // Replaces old TrackPlan.Domain/Service/Renderer architecture
        services.AddTrackPlanServices();

        // ViewModels
        // Note: Wrapper ViewModels (SolutionViewModel, ProjectViewModel, JourneyViewModel, etc.)
        // are created with 'new' at runtime because they wrap Domain models.
        // Only "standalone" ViewModel that don't wrap models are registered here.

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
        services.AddSingleton<MonitorPageViewModel>();
        services.AddSingleton(sp => new TrainControlViewModel(
            sp.GetRequiredService<IZ21>(),
            sp.GetRequiredService<IUiDispatcher>(),
            sp.GetRequiredService<ISettingsService>(),
            sp.GetService<ILogger<TrainControlViewModel>>()
        ));

        // Pages (Transient = new instance per navigation)
        // Registration: tag, title, iconGlyph, pageType, source, category, order, featureToggleKey, badgeLabelKey, pathIconData, isBold

        // Core: Overview
        services.AddTransient<OverviewPage>();
        navigationRegistry.Register("overview", "Overview", "\uE80F", typeof(OverviewPage), "Shell",
            NavigationCategory.Core, 10, "IsOverviewPageAvailable", "OverviewPageLabel");

        // TrainControl: Train Control (bold, prominent)
        services.AddTransient<TrainControlPage>();
        navigationRegistry.Register("traincontrol", "Train Control", "\uEC49", typeof(TrainControlPage), "Shell",
            NavigationCategory.TrainControl, 10, "IsTrainControlPageAvailable", "TrainControlPageLabel", null, true);

        // Journey: Journeys (bold)
        services.AddTransient<JourneysPage>();
        navigationRegistry.Register("journeys", "Journeys", "\uE7C1", typeof(JourneysPage), "Shell",
            NavigationCategory.Journey, 10, "IsJourneysPageAvailable", "JourneysPageLabel", null, true);

        // Journey: Journey Map
        services.AddTransient<JourneyMapPage>();
        navigationRegistry.Register("journeymap", "Journey Map", "\uE81D", typeof(JourneyMapPage), "Shell",
            NavigationCategory.Journey, 20, "IsJourneyMapPageAvailable", "JourneyMapPageLabel");

        // Solution: Solution, Workflows, Trains
        services.AddTransient<SolutionPage>();
        navigationRegistry.Register("solution", "Solution", "\uE8B7", typeof(SolutionPage), "Shell",
            NavigationCategory.Solution, 10, "IsSolutionPageAvailable", "SolutionPageLabel");

        services.AddTransient<WorkflowsPage>();
        navigationRegistry.Register("workflows", "Workflows", "\uE945", typeof(WorkflowsPage), "Shell",
            NavigationCategory.Solution, 20, "IsWorkflowsPageAvailable", "WorkflowsPageLabel");

        services.AddTransient<TrainsPage>();
        navigationRegistry.Register("trains", "Trains", "\uE7C0", typeof(TrainsPage), "Shell",
            NavigationCategory.Solution, 30, "IsTrainsPageAvailable", "TrainsPageLabel");

        // TrackManagement: MOBAtps, MOBAesb
        services.AddTransient<TrackPlanPage>();
        navigationRegistry.Register("trackplaneditor", "MOBAtps", null, typeof(TrackPlanPage), "Shell",
            NavigationCategory.TrackManagement, 10, "IsTrackPlanEditorPageAvailable", "TrackPlanEditorPageLabel",
            "M2,3 L4,3 L14,13 L12,13 Z M12,3 L14,3 L4,13 L2,13 Z");

        services.AddSingleton<SignalBoxPage>();
        navigationRegistry.Register("signalbox", "MOBAesb", null, typeof(SignalBoxPage), "Shell",
            NavigationCategory.TrackManagement, 20, null, null,
            "M7,2 A2,2 0 1,1 11,2 A2,2 0 1,1 7,2 M3,10 A2,2 0 1,1 7,10 A2,2 0 1,1 3,10 M11,10 A2,2 0 1,1 15,10 A2,2 0 1,1 11,10");

        // Monitoring: Monitor
        services.AddTransient<MonitorPage>();
        navigationRegistry.Register("monitor", "Monitor", "\uE7F4", typeof(MonitorPage), "Shell",
            NavigationCategory.Monitoring, 10, "IsMonitorPageAvailable", "MonitorPageLabel");

        // Help: Help, Info, Settings
        services.AddTransient<HelpPage>();
        navigationRegistry.Register("help", "Help", "\uE897", typeof(HelpPage), "Shell",
            NavigationCategory.Help, 10);

        services.AddTransient<InfoPage>();
        navigationRegistry.Register("info", "Info", "\uE946", typeof(InfoPage), "Shell",
            NavigationCategory.Help, 20);

        services.AddTransient<SettingsPage>();
        navigationRegistry.Register("settings", "Settings", "\uE115", typeof(SettingsPage), "Shell",
            NavigationCategory.Help, 30, "IsSettingsPageAvailable", "SettingsPageLabel");

        // Theme Provider
        services.AddSingleton<IThemeProvider, ThemeProvider>();
        // NOTE: ThemeSelectorControl wird dynamisch in SettingsPage erstellt


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
        // Initialize ThemeProvider with saved settings before creating MainWindow
        var themeProvider = Services.GetRequiredService<IThemeProvider>();
        var appSettings = Services.GetRequiredService<AppSettings>();
        themeProvider.Initialize(appSettings);

        _window = Services.GetRequiredService<MainWindow>();
        _window.Activate();

        // Optionally start WebApp (REST/API) alongside WinUI
        _ = StartWebAppIfEnabledAsync();

        // Auto-load last solution if enabled
        _ = AutoLoadLastSolutionAsync(((MainWindow)_window).ViewModel);
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
            if (!Utilities.PortChecker.IsPortAvailable(restPort))
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
                        endpoints.MapHub<Hubs.PhotoHub>("/photos-hub");
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
                    .AddApplicationPart(typeof(Controllers.PhotoUploadController).Assembly);

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
                var dialog = new Microsoft.UI.Xaml.Controls.ContentDialog
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