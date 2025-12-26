// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.UI.Xaml;

using Moba.Backend.Service;
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

        // ✅ Register both Speech Engines as singletons
        // SystemSpeechEngine - Windows SAPI (offline, no Azure needed)
        services.AddSingleton(sp =>
        {
            var logger = sp.GetService<Microsoft.Extensions.Logging.ILogger<SystemSpeechEngine>>();
            return new SystemSpeechEngine(logger!);
        });

        // CognitiveSpeechEngine - Azure Cognitive Services (cloud-based)
        services.AddSingleton(sp =>
        {
            var options = sp.GetRequiredService<IOptions<SpeechOptions>>();
            var logger = sp.GetService<Microsoft.Extensions.Logging.ILogger<CognitiveSpeechEngine>>();
            return new CognitiveSpeechEngine(options, logger!);
        });

        // ✅ Register SpeakerEngineFactory - selects engine based on Settings
        services.AddSingleton<ISpeakerEngineFactory>(sp =>
        {
            var settings = sp.GetRequiredService<AppSettings>();
            var systemEngine = sp.GetRequiredService<SystemSpeechEngine>();
            var cognitiveEngine = sp.GetRequiredService<CognitiveSpeechEngine>();
            var logger = sp.GetService<Microsoft.Extensions.Logging.ILogger<SpeakerEngineFactory>>();
            return new SpeakerEngineFactory(settings, systemEngine, cognitiveEngine, logger!);
        });

        // Backend Services (Interfaces are in Backend.Interface and Backend.Network)
        services.AddSingleton<Z21Monitor>();
        services.AddSingleton<Backend.Interface.IZ21, Backend.Z21>(sp =>
        {
            var udp = sp.GetRequiredService<Backend.Network.IUdpClientWrapper>();
            var logger = sp.GetService<Microsoft.Extensions.Logging.ILogger<Backend.Z21>>();
            var trafficMonitor = sp.GetRequiredService<Z21Monitor>();
            return new Backend.Z21(udp, logger, trafficMonitor);
        });
        services.AddSingleton<Backend.Network.IUdpClientWrapper, Backend.Network.UdpWrapper>();

        // Backend Services - Register in dependency order
        // ✅ AnnouncementService (uses ISpeakerEngineFactory for runtime engine selection)
        services.AddSingleton(sp =>
        {
            var speakerEngineFactory = sp.GetService<ISpeakerEngineFactory>();
            var logger = sp.GetService<Microsoft.Extensions.Logging.ILogger<AnnouncementService>>();
            return new AnnouncementService(speakerEngineFactory, logger);
        });
        
        // ✅ ActionExecutor with AnnouncementService for Announcement actions
        services.AddSingleton(sp =>
        {
            var announcementService = sp.GetRequiredService<AnnouncementService>();
            return new ActionExecutor(announcementService);
        });
        
        services.AddSingleton<WorkflowService>();
        
        services.AddSingleton<SharedUI.Interface.IIoService, Service.IoService>();
        services.AddSingleton<SharedUI.Interface.IUiDispatcher, Service.UiDispatcher>();
        services.AddSingleton<SharedUI.Interface.ICityService, Service.CityService>();
        services.AddSingleton<SharedUI.Interface.ISettingsService, Service.SettingsService>();
        services.AddSingleton<Service.NavigationService>();
        services.AddSingleton<Service.SnapToConnectService>();

        // Sound Services (required by HealthCheckService)
        services.AddSingleton<SpeechHealthCheck>();
        services.AddSingleton<Service.HealthCheckService>();

        // ViewModels
        // Note: Wrapper ViewModels (SolutionViewModel, ProjectViewModel, JourneyViewModel, etc.)
        // are created with 'new' at runtime because they wrap Domain models.
        // Only "standalone" ViewModels that don't wrap models are registered here.

        // Domain.Solution as singleton (shared across application lifetime)
        services.AddSingleton<Domain.Solution>();

        services.AddSingleton(sp => new SharedUI.ViewModel.MainWindowViewModel(
            sp.GetRequiredService<SharedUI.Interface.IIoService>(),
            sp.GetRequiredService<Backend.Interface.IZ21>(),
            sp.GetRequiredService<WorkflowService>(),
            sp.GetRequiredService<SharedUI.Interface.IUiDispatcher>(),
            sp.GetRequiredService<AppSettings>(),
            sp.GetRequiredService<Domain.Solution>(),
            sp.GetService<SharedUI.Interface.ICityService>(),
            sp.GetService<SharedUI.Interface.ISettingsService>(),
            sp.GetService<AnnouncementService>()  // For TestSpeech command
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
