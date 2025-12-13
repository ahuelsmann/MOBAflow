// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.UI.Xaml;

using Moba.Backend.Service;
using Moba.Common.Configuration;

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
            System.Diagnostics.Debug.WriteLine($"🚨 FATAL ERROR during App initialization:");
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
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        // Register IConfiguration
        services.AddSingleton<IConfiguration>(configuration);

        // Register AppSettings with IOptions pattern
        services.Configure<AppSettings>(configuration);
        services.AddSingleton(sp => sp.GetRequiredService<IOptions<AppSettings>>().Value);

        // Register SpeechOptions (Sound service configuration)
        services.Configure<Sound.SpeechOptions>(configuration.GetSection("Speech"));

        // Logging (required by HealthCheckService and SpeechHealthCheck)
        services.AddLogging();

        // Backend Services (Interfaces are in Backend.Interface and Backend.Network)
        services.AddSingleton<Backend.Service.Z21Monitor>();
        services.AddSingleton<Backend.Interface.IZ21, Backend.Z21>(sp =>
        {
            var udp = sp.GetRequiredService<Backend.Network.IUdpClientWrapper>();
            var logger = sp.GetService<Microsoft.Extensions.Logging.ILogger<Backend.Z21>>();
            var trafficMonitor = sp.GetRequiredService<Backend.Service.Z21Monitor>();
            return new Backend.Z21(udp, logger, trafficMonitor);
        });
        services.AddSingleton<Backend.Network.IUdpClientWrapper, Backend.Network.UdpWrapper>();

        // Backend Services - Register in dependency order
        services.AddSingleton(sp =>
        {
            var z21 = sp.GetRequiredService<Backend.Interface.IZ21>();
            return new Backend.Service.ActionExecutor(z21);
        });
        services.AddSingleton<WorkflowService>();

        // Domain.Solution - Pure POCO, no Settings initialization needed
        services.AddSingleton(sp => new Domain.Solution());

        // WinUI Services (Interfaces are in SharedUI.Service)
        services.AddSingleton<SharedUI.Interface.IIoService, Service.IoService>();
        services.AddSingleton<SharedUI.Interface.IUiDispatcher, Service.UiDispatcher>();
        services.AddSingleton<SharedUI.Interface.ICityService, Service.CityService>();
        services.AddSingleton<SharedUI.Interface.ISettingsService, Service.SettingsService>();

        // Sound Services (required by HealthCheckService)
        services.AddSingleton<Sound.SpeechHealthCheck>();
        services.AddSingleton<Service.HealthCheckService>();

        // ViewModels
        services.AddSingleton<SharedUI.ViewModel.MainWindowViewModel>();
        services.AddTransient<SharedUI.ViewModel.JourneyViewModel>();
        services.AddSingleton<SharedUI.ViewModel.CounterViewModel>();

        // Pages (Transient = new instance per navigation)
        services.AddTransient<View.OverviewPage>();
        services.AddTransient<View.SolutionPage>();
        services.AddTransient<View.JourneysPage>();
        services.AddTransient<View.WorkflowsPage>();
        services.AddTransient<View.SettingsPage>();

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

            // Load the solution using IIoService
            var ioService = Services.GetRequiredService<SharedUI.Interface.IIoService>();
            var (loadedSolution, path, error) = await ioService.LoadFromPathAsync(lastPath);

            if (!string.IsNullOrEmpty(error))
            {
                System.Diagnostics.Debug.WriteLine($" Auto-load failed: {error}");
                return;
            }

            if (loadedSolution != null)
            {
                // Update the Solution singleton
                mainWindowViewModel.Solution.Projects.Clear();
                foreach (var project in loadedSolution.Projects)
                {
                    mainWindowViewModel.Solution.Projects.Add(project);
                }
                mainWindowViewModel.Solution.Name = loadedSolution.Name;

                // Refresh ViewModel
                mainWindowViewModel.SolutionViewModel?.Refresh();

                // ✅ Set CurrentSolutionPath and HasSolution for StatusBar display
                mainWindowViewModel.CurrentSolutionPath = path;
                mainWindowViewModel.HasSolution = mainWindowViewModel.Solution.Projects.Count > 0;

                System.Diagnostics.Debug.WriteLine($"✅ Auto-load completed: {path}");
                System.Diagnostics.Debug.WriteLine($"   Projects: {loadedSolution.Projects.Count}, HasSolution: {mainWindowViewModel.HasSolution}");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($" Auto-load failed: {ex.Message}");
            // Don't crash the application if auto-load fails
        }
    }
}
