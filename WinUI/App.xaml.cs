namespace Moba.WinUI;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;

using Moba.SharedUI.Service;

using Service;

using SharedUI.ViewModel;

using View;

/// <summary>
/// Provides application-specific behavior to supplement the default Application class.
/// </summary>
public partial class App
{
    private Window? _window;
    private ServiceProvider? _serviceProvider;

    /// <summary>
    /// Initializes the singleton application object.  This is the first line of authored code
    /// executed, and as such is the logical equivalent of main() or WinMain().
    /// </summary>
    public App()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Invoked when the application is launched.
    /// </summary>
    /// <param name="args">Details about the launch request and process.</param>
    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        // FeedbackApi is started by Visual Studio (Multiple Startup Projects)
        // No need to start it here
        
        // Configure services
        var services = new ServiceCollection();
        ConfigureServices(services);
        _serviceProvider = services.BuildServiceProvider();

        // Create MainWindow
        _window = _serviceProvider.GetRequiredService<MainWindow>();

        // Set the WindowId in the IoService after window is created
        var ioService = _serviceProvider.GetRequiredService<IIoService>() as IoService;
        ioService?.SetWindowId(_window.AppWindow.Id);

        _window.Activate();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        // Register IoService (WindowId will be set after MainWindow creation)
        services.AddSingleton<IIoService, IoService>();

        // Register ViewModels
        services.AddTransient<MainWindowViewModel>();

        // Register Views
        services.AddTransient<MainWindow>();
    }
}