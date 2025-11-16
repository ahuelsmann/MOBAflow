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
using System.IO;

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

        _window = _serviceProvider.GetRequiredService<MainWindow>();

        var ioService = _serviceProvider.GetRequiredService<IIoService>() as IoService;
        ioService?.SetWindowId(_window.AppWindow.Id);

        _window.Activate();
    }

    private IConfiguration BuildConfiguration()
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
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

        // Dispatcher + factory via DI
        services.AddSingleton<IUiDispatcher, UiDispatcher>();
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
        
        services.AddTransient<SharedUI.ViewModel.WinUI.MainWindowViewModel>();
        services.AddTransient<MainWindow>();
    }
}