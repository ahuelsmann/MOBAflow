// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.UI.Xaml;

using Moba.Backend.Service;
using Moba.Backend.Extensions;
using Moba.Common.Configuration;
using Moba.Sound;

namespace Moba.WinUI;

/// <summary>
/// Provides application-specific behavior to supplement the default Application class.
/// </summary>
public partial class App
{
    private Window? _window;

    /// <summary>
    /// Initializes the singleton application object. This is the first line of authored code
    /// executed, and as such is the logical equivalent of main() or WinMain().
    /// </summary>
    public App()
    {
        try
        {
            Services = ConfigureServices();
            InitializeComponent();
        }
        catch (Exception ex)
        {
            // Log the exception before crashing
            System.Diagnostics.Debug.WriteLine("🚨 FATAL ERROR during App initialization:");
            System.Diagnostics.Debug.WriteLine($"   Exception Type: {ex.GetType().Name}");
            System.Diagnostics.Debug.WriteLine($"   Message: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"   StackTrace: {ex.StackTrace}");
            if (ex.InnerException != null)
            {
                System.Diagnostics.Debug.WriteLine($"   Inner Exception: {ex.InnerException.Message}");
                System.Diagnostics.Debug.WriteLine($"   Inner StackTrace: {ex.InnerException.StackTrace}");
            }

            // Re-throw to get Windows Error Reporting
            throw;
        }
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
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
#if DEBUG
            .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true)
#endif
            .Build();

        // Register IConfiguration
        services.AddSingleton<IConfiguration>(configuration);

        // Register AppSettings with IOptions pattern
        services.Configure<AppSettings>(configuration);
        services.AddSingleton(sp => sp.GetRequiredService<IOptions<AppSettings>>().Value);

        // Register SpeechOptions (Sound service configuration)
        services.Configure<SpeechOptions>(configuration.GetSection("Speech"));

        // Logging (required by HealthCheckService and SpeechHealthCheck)
        services.AddLogging();

        // ✅ Register ISpeakerEngine with lazy initialization
        // Only creates the CONFIGURED engine, not both
        // Decision is made at registration time based on AppSettings
        services.AddSingleton<ISpeakerEngine>(sp =>
        {
            var settings = sp.GetRequiredService<AppSettings>();
            var logger = sp.GetService<Microsoft.Extensions.Logging.ILogger>();
            
            // Check if Azure Cognitive Services is configured
            var selectedEngine = settings.Speech.SpeakerEngineName;
            if (!string.IsNullOrEmpty(selectedEngine) &&
                selectedEngine.Contains("Azure", System.StringComparison.OrdinalIgnoreCase))
            {
                // Only create Azure engine if explicitly configured
                var options = sp.GetRequiredService<IOptions<SpeechOptions>>();
                return new CognitiveSpeechEngine(options, sp.GetService<Microsoft.Extensions.Logging.ILogger<CognitiveSpeechEngine>>()!);
            }
            
            // Default: Windows SAPI (always available, no Azure SDK needed)
            return new SystemSpeechEngine(sp.GetService<Microsoft.Extensions.Logging.ILogger<SystemSpeechEngine>>()!);
        });

        // Backend Services (Interfaces are in Backend.Interface and Backend.Network)
        // ✅ Use shared extension method for platform-consistent registration
        services.AddMobaBackendServices();

        // ✅ AnnouncementService (uses ISpeakerEngine directly, not Factory)
        services.AddSingleton(sp =>
        {
            var speakerEngine = sp.GetRequiredService<ISpeakerEngine>();
            var logger = sp.GetService<Microsoft.Extensions.Logging.ILogger<AnnouncementService>>();
            return new AnnouncementService(speakerEngine, logger);
        });
        
        // ✅ ActionExecutor with AnnouncementService for Announcement actions
        services.AddSingleton<Backend.Interface.IActionExecutor>(sp =>
        {
            var announcementService = sp.GetRequiredService<AnnouncementService>();
            return new ActionExecutor(announcementService);
        });
        
        services.AddSingleton<WorkflowService>();
        
        services.AddSingleton<SharedUI.Interface.IIoService, Service.IoService>();
        services.AddSingleton<SharedUI.Interface.IUiDispatcher, Service.UiDispatcher>();
        
        // ✅ ICityService with NullObject fallback
        // Always register a service (real or no-op)
        services.AddSingleton<SharedUI.Interface.ICityService>(sp =>
        {
            try
            {
                var appSettings = sp.GetRequiredService<AppSettings>();
                return new Service.CityService(appSettings);
            }
            catch
            {
                // Fallback to NullObject if city data unavailable
                return new Service.NullCityService();
            }
        });

        // ✅ ISettingsService with NullObject fallback
        // Always register a service (real or no-op)
        services.AddSingleton<SharedUI.Interface.ISettingsService>(sp =>
        {
            try
            {
                var appSettings = sp.GetRequiredService<AppSettings>();
                return new Service.SettingsService(appSettings);
            }
            catch
            {
                // Fallback to NullObject if settings file unavailable
                return new Service.NullSettingsService();
            }
        });
        
        services.AddSingleton<Service.NavigationService>();
        services.AddSingleton<Service.SnapToConnectService>();

        // Sound Services (required by HealthCheckService)
        services.AddSingleton<SpeechHealthCheck>();
        services.AddSingleton<Service.HealthCheckService>();

        // ViewModels
        // Note: Wrapper ViewModels (SolutionViewModel, ProjectViewModel, JourneyViewModel, etc.)
        // are created with 'new' at runtime because they wrap Domain models.
        // Only "standalone" ViewModel that don't wrap models are registered here.

        // Domain.Solution is registered in AddMobaBackendServices()

        services.AddSingleton(sp => new SharedUI.ViewModel.MainWindowViewModel(
            sp.GetRequiredService<Backend.Interface.IZ21>(),
            sp.GetRequiredService<WorkflowService>(),
            sp.GetRequiredService<SharedUI.Interface.IUiDispatcher>(),
            sp.GetRequiredService<AppSettings>(),
            sp.GetRequiredService<Domain.Solution>(),
            sp.GetRequiredService<SharedUI.Interface.IIoService>(),
            sp.GetRequiredService<SharedUI.Interface.ICityService>(),      // Now guaranteed (NullObject if unavailable)
            sp.GetRequiredService<SharedUI.Interface.ISettingsService>(),  // Now guaranteed (NullObject if unavailable)
            sp.GetRequiredService<AnnouncementService>()  // For TestSpeech command
        ));
        services.AddSingleton<SharedUI.ViewModel.TrackPlanEditorViewModel>();
        services.AddSingleton<SharedUI.ViewModel.JourneyMapViewModel>();
        services.AddSingleton<SharedUI.ViewModel.MonitorPageViewModel>();

        // Pages (Transient = new instance per navigation)
        services.AddTransient<View.OverviewPage>();
        services.AddTransient<View.SolutionPage>();
        services.AddTransient<View.JourneysPage>();
        services.AddTransient<View.WorkflowsPage>();
        services.AddTransient<View.TrackPlanEditorPage>();
        services.AddTransient<View.JourneyMapPage>();
        services.AddTransient<View.SettingsPage>();
        services.AddTransient<View.MonitorPage>();

        // MainWindow (Singleton = one instance for app lifetime)
        services.AddSingleton<View.MainWindow>();

        return services.BuildServiceProvider();
    }

    /// <summary>
    /// Invoked when the application is launched.
    /// </summary>
    /// <param name="args">Details about the launch request and process.</param>
    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {

        _window = Services.GetRequiredService<View.MainWindow>();
        _window.Activate();

        // Auto-load last solution if enabled
        _ = AutoLoadLastSolutionAsync(((View.MainWindow)_window).ViewModel);
    }

    /// <summary>
    /// Automatically loads the last used solution if AutoLoadLastSolution preference is enabled.
    /// Delegates to MainWindowViewModel.LoadSolutionFromPathAsync() to ensure all initialization happens correctly.
    /// </summary>
    private async Task AutoLoadLastSolutionAsync(SharedUI.ViewModel.MainWindowViewModel mainWindowViewModel)
    {
        try
        {
            var settingsService = Services.GetService<SharedUI.Interface.ISettingsService>();
            if (settingsService == null)
            {
                System.Diagnostics.Debug.WriteLine("⚠️ SettingsService not available - skipping auto-load");
                return;
            }

            if (!settingsService.AutoLoadLastSolution)
            {
                System.Diagnostics.Debug.WriteLine("ℹ️ Auto-load disabled - skipping");
                return;
            }

            var lastPath = settingsService.LastSolutionPath;
            if (string.IsNullOrEmpty(lastPath))
            {
                System.Diagnostics.Debug.WriteLine("ℹ️ No last solution path - skipping auto-load");
                return;
            }

            if (!File.Exists(lastPath))
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ Last solution file not found: {lastPath}");
                return;
            }

            System.Diagnostics.Debug.WriteLine($"📂 Auto-loading last solution: {lastPath}");

            // ✅ Use the SAME code path as manual loading!
            // This ensures JourneyManager and all other initialization happens correctly.
            await mainWindowViewModel.LoadSolutionFromPathAsync(lastPath);

            System.Diagnostics.Debug.WriteLine($"✅ Auto-load completed: {lastPath}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Auto-load failed: {ex.Message}");
            // Don't crash the application if auto-load fails
        }
    }
}
