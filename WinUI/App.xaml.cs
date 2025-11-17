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
        var counterViewModel = _serviceProvider.GetRequiredService<SharedUI.ViewModel.CounterViewModel>();
        System.Diagnostics.Debug.WriteLine("✅ CounterViewModel initialized and listening for Z21 feedback");

        _window = _serviceProvider.GetRequiredService<MainWindow>();

        var ioService = _serviceProvider.GetRequiredService<IIoService>() as IoService;
        ioService?.SetWindowId(_window.AppWindow.Id);

        _window.Activate();
    }

    private IConfiguration BuildConfiguration()
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

        // Dispatcher + Notification + factory via DI
        services.AddSingleton<IUiDispatcher, UiDispatcher>();
        services.AddSingleton<INotificationService, NotificationService>();
        services.AddSingleton<IJourneyViewModelFactory, WinUIJourneyViewModelFactory>();
        
        // TreeViewBuilder service
        services.AddSingleton<SharedUI.Service.TreeViewBuilder>();

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
        services.AddTransient<SharedUI.ViewModel.WinUI.MainWindowViewModel>();
        services.AddSingleton<SharedUI.ViewModel.CounterViewModel>();
        
        // Views
        services.AddTransient<MainWindow>();
    }
}