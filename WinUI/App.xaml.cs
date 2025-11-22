// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;

using Moba.SharedUI.Service;
using Moba.WinUI.Service;
using Moba.Backend.Network;
using Moba.Sound;

using View;

public partial class App
{
    private Window? _window;
    private ServiceProvider? _serviceProvider;
    private IConfiguration? _configuration;

    /// <summary>
    /// Gets the service provider for DI resolution (e.g., in MainWindow for page navigation).
    /// </summary>
    public IServiceProvider Services => _serviceProvider!;

    public App()
    {
        InitializeComponent();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        // Build configuration from appsettings.json
        _configuration = BuildConfiguration();

        var services = new ServiceCollection();
        ConfigureServices(services);
        _serviceProvider = services.BuildServiceProvider();

        // Perform health check on startup
        var healthCheck = _serviceProvider.GetRequiredService<SpeechHealthCheck>();
        var statusMessage = healthCheck.GetStatusMessage();
        System.Diagnostics.Debug.WriteLine($"Azure Speech Service: {statusMessage}");

        // Initialize CounterViewModel (subscribes to Z21 feedback)
        // This must be done after ServiceProvider is built so it can subscribe to Z21 events
        _ = _serviceProvider.GetRequiredService<SharedUI.ViewModel.CounterViewModel>();
        System.Diagnostics.Debug.WriteLine("✅ CounterViewModel initialized and listening for Z21 feedback");

        // ✅ Load DataManager (master data: Cities, etc.)
        _ = LoadDataManagerAsync();

        _window = _serviceProvider.GetRequiredService<MainWindow>();

        var ioService = _serviceProvider.GetRequiredService<IIoService>() as IoService;
        ioService?.SetWindowId(_window.AppWindow.Id);

        _window.Activate();
    }

    /// <summary>
    /// Loads master data (DataManager) from disk asynchronously.
    /// DataManager is registered as singleton in DI container for access across the app.
    /// </summary>
    private async Task LoadDataManagerAsync()
    {
        try
        {
            var ioService = _serviceProvider!.GetRequiredService<IIoService>();
            var (dataManager, path, error) = await ioService.LoadDataManagerAsync();

            if (error != null)
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ DataManager konnte nicht geladen werden: {error}");
                System.Diagnostics.Debug.WriteLine("ℹ️ Es wird ein leerer DataManager verwendet.");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"✅ DataManager erfolgreich geladen aus: {path ?? "Standard-Pfad"}");
                
                // Optional: Log statistics
                if (dataManager != null)
                {
                    var cityCount = dataManager.Cities?.Count ?? 0;
                    System.Diagnostics.Debug.WriteLine($"📊 DataManager enthält {cityCount} Städte");
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Fehler beim Laden des DataManagers: {ex.Message}");
        }
    }

    private static IConfiguration BuildConfiguration()
    {
        // Get the directory where the executable is located
        var basePath = AppContext.BaseDirectory;
        
        var builder = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables(); // Environment variables override appsettings

        return builder.Build();
    }

    private void ConfigureServices(IServiceCollection services)
    {
        // Register configuration
        services.AddSingleton(_configuration!);

        // Logging from configuration
        services.AddLogging(builder =>
        {
            builder.AddDebug();
            builder.SetMinimumLevel(LogLevel.Information);
        });

        services.AddSingleton<IIoService, IoService>();

        // Backend services explicit
        services.AddSingleton<IUdpClientWrapper, UdpWrapper>();
        services.AddSingleton<Backend.Interface.IZ21, Backend.Z21>();
        services.AddSingleton<Backend.Interface.IJourneyManagerFactory, Backend.Manager.JourneyManagerFactory>();

        // ✅ DataManager as Singleton (master data loaded on startup)
        services.AddSingleton(sp =>
        {
            var ioService = sp.GetRequiredService<IIoService>();
            var (dataManager, _, _) = ioService.LoadDataManagerAsync().GetAwaiter().GetResult();
            return dataManager ?? new Backend.Data.DataManager();
        });

        // ✅ Solution as Singleton (initialized with one empty project for immediate use)
        services.AddSingleton<Backend.Model.Solution>(sp =>
        {
            var solution = new Backend.Model.Solution
            {
                Name = "New Solution"
            };
            
            // Add one empty project so navigation works immediately
            solution.Projects.Add(new Backend.Model.Project
            {
                Name = "(Untitled Project)"
            });
            
            return solution;
        });

        // Dispatcher + Notification + factories via DI
        services.AddSingleton<IUiDispatcher, UiDispatcher>();
        services.AddSingleton<INotificationService, NotificationService>();
        
        // ✅ All ViewModel Factories (WinUI-specific) - NEW NAMESPACES
        services.AddSingleton<SharedUI.Interface.IJourneyViewModelFactory, WinUI.Factory.WinUIJourneyViewModelFactory>();
        services.AddSingleton<SharedUI.Interface.IStationViewModelFactory, WinUI.Factory.WinUIStationViewModelFactory>();
        services.AddSingleton<SharedUI.Interface.IWorkflowViewModelFactory, WinUI.Factory.WinUIWorkflowViewModelFactory>();
        services.AddSingleton<SharedUI.Interface.ILocomotiveViewModelFactory, WinUI.Factory.WinUILocomotiveViewModelFactory>();
        services.AddSingleton<SharedUI.Interface.ITrainViewModelFactory, WinUI.Factory.WinUITrainViewModelFactory>();
        services.AddSingleton<SharedUI.Interface.IWagonViewModelFactory, WinUI.Factory.WinUIWagonViewModelFactory>();
        
        // TreeViewBuilder service (now with all factories)
        services.AddSingleton<SharedUI.Service.TreeViewBuilder>();
        
        // ValidationService for delete operations
        services.AddSingleton<SharedUI.Service.ValidationService>(sp =>
        {
            var solution = sp.GetRequiredService<Backend.Model.Solution>();
            var project = solution.Projects.FirstOrDefault() ?? new Backend.Model.Project { Name = "(No Project Loaded)" };
            return new SharedUI.Service.ValidationService(project);
        });

        // Sound services
        services.AddSingleton<ISoundPlayer, WindowsSoundPlayer>();
        services.AddSingleton<ISpeakerEngine, CognitiveSpeechEngine>();
        services.AddSingleton<SpeechHealthCheck>();
        services.AddSingleton<HealthCheckService>();
        
        // Configure SpeechOptions from appsettings.json with environment variable fallback
        services.Configure<SpeechOptions>(options =>
        {
            _configuration!.GetSection("SpeechOptions").Bind(options);
            
            // Fallback to environment variables if not set in config
            if (string.IsNullOrEmpty(options.Key))
            {
                options.Key = Environment.GetEnvironmentVariable("SPEECH_KEY");
            }
            if (string.IsNullOrEmpty(options.Region))
            {
                options.Region = Environment.GetEnvironmentVariable("SPEECH_REGION");
            }
        });
        
        // ViewModels - CounterViewModel now requires IUiDispatcher and optional INotificationService
        services.AddSingleton<SharedUI.ViewModel.WinUI.MainWindowViewModel>();
        services.AddSingleton<SharedUI.ViewModel.CounterViewModel>();
        
        // Editor ViewModels
        services.AddTransient<SharedUI.ViewModel.EditorPageViewModel>(sp =>
        {
            var solution = sp.GetRequiredService<Backend.Model.Solution>();
            var validationService = sp.GetRequiredService<SharedUI.Service.ValidationService>();
            return new SharedUI.ViewModel.EditorPageViewModel(solution, validationService);
        });
        
        // Views
        services.AddTransient<MainWindow>();
    }
}