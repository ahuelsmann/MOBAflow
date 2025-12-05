// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.UI.Xaml;

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
        Services = ConfigureServices();
        InitializeComponent();
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
        services.AddSingleton<Backend.Interface.IZ21, Backend.Z21>();
        services.AddSingleton<Backend.Network.IUdpClientWrapper, Backend.Network.UdpWrapper>();
        
        // Backend Services - Register in dependency order
        services.AddSingleton<Backend.Services.ActionExecutor>(sp =>
        {
            var z21 = sp.GetRequiredService<Backend.Interface.IZ21>();
            return new Backend.Services.ActionExecutor(z21);
        });
        services.AddSingleton<Backend.Services.WorkflowService>();
        services.AddSingleton<Backend.Interface.IJourneyManagerFactory, Backend.Manager.JourneyManagerFactory>();
        
        // Domain.Solution - Pure POCO, no Settings initialization needed
        services.AddSingleton<Domain.Solution>(sp => new Domain.Solution());

        // WinUI Services (Interfaces are in SharedUI.Service)
        services.AddSingleton<SharedUI.Interface.IIoService, Service.IoService>();
        services.AddSingleton<SharedUI.Interface.INotificationService, Service.NotificationService>();
        services.AddSingleton<SharedUI.Interface.IPreferencesService, Service.PreferencesService>();
        services.AddSingleton<SharedUI.Interface.IUiDispatcher, Service.UiDispatcher>();
        services.AddSingleton<SharedUI.Interface.ICityService, Service.CityService>();
        services.AddSingleton<SharedUI.Interface.ISettingsService, Service.SettingsService>();
        
        // Sound Services (required by HealthCheckService)
        services.AddSingleton<Sound.SpeechHealthCheck>();
        services.AddSingleton<Service.HealthCheckService>();

        // SharedUI Services
        services.AddSingleton<SharedUI.Service.ValidationService>(sp =>
        {
            var solution = sp.GetRequiredService<Domain.Solution>();
            // ValidationService needs the first project (simplified for now)
            var project = solution.Projects.FirstOrDefault() ?? new Domain.Project { Name = "Default Project" };
            return new SharedUI.Service.ValidationService(project);
        });

        // ViewModels
        services.AddSingleton<SharedUI.ViewModel.MainWindowViewModel>();
        services.AddTransient<SharedUI.ViewModel.JourneyViewModel>();
        services.AddSingleton<SharedUI.ViewModel.CounterViewModel>();
        

        return services.BuildServiceProvider();
    }

    /// <summary>
    /// Invoked when the application is launched.
    /// </summary>
    /// <param name="args">Details about the launch request and process.</param>
    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        var mainWindowViewModel = Services.GetRequiredService<SharedUI.ViewModel.MainWindowViewModel>();
        var counterViewModel = Services.GetRequiredService<SharedUI.ViewModel.CounterViewModel>();
        var healthCheckService = Services.GetRequiredService<Service.HealthCheckService>();
        var uiDispatcher = Services.GetRequiredService<SharedUI.Interface.IUiDispatcher>();

        _window = new View.MainWindow(mainWindowViewModel, counterViewModel, healthCheckService, uiDispatcher);
        _window.Activate();
        
        // Ã¢Å“â€¦ Auto-load last solution if enabled
        _ = AutoLoadLastSolutionAsync(mainWindowViewModel);
    }
    
    /// <summary>
    /// Automatically loads the last used solution if AutoLoadLastSolution preference is enabled.
    /// </summary>
    private async Task AutoLoadLastSolutionAsync(SharedUI.ViewModel.MainWindowViewModel mainWindowViewModel)
    {
        try
        {
            var preferencesService = Services.GetService<SharedUI.Interface.IPreferencesService>();
            if (preferencesService == null)
            {
                System.Diagnostics.Debug.WriteLine(" PreferencesService not available - skipping auto-load");
                return;
            }

            if (!preferencesService.AutoLoadLastSolution)
            {
                System.Diagnostics.Debug.WriteLine(" Auto-load disabled - skipping");
                return;
            }

            var lastPath = preferencesService.LastSolutionPath;
            if (string.IsNullOrEmpty(lastPath))
            {
                System.Diagnostics.Debug.WriteLine(" No last solution path - skipping auto-load");
                return;
            }

            if (!System.IO.File.Exists(lastPath))
            {
                System.Diagnostics.Debug.WriteLine($" Last solution file not found: {lastPath}");
                return;
            }

            System.Diagnostics.Debug.WriteLine($" Auto-loading last solution: {lastPath}");
            
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
                mainWindowViewModel.CurrentSolutionPath = path;
                mainWindowViewModel.HasUnsavedChanges = false;
                
                System.Diagnostics.Debug.WriteLine(" Auto-load completed successfully");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($" Auto-load failed: {ex.Message}");
            // Don't crash the application if auto-load fails
        }
    }
}
