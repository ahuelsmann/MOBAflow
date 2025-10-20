namespace Moba.WinUI;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;

using Moba.SharedUI.Service;
using SharedUI.ViewModel;
using Service;

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
        // Create the window first
        _window = new MainWindow(null!);
        
        // Get the window handle
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(_window);
        
        // Configure services with the window handle
        var services = new ServiceCollection();
        ConfigureServices(services, hwnd);
        _serviceProvider = services.BuildServiceProvider();
        
        // Get the view model with properly injected IoService
        var viewModel = _serviceProvider.GetRequiredService<MainWindowViewModel>();
        
        // Create the actual window with the view model
        _window = new MainWindow(viewModel);
        _window.Activate();
    }

    private static void ConfigureServices(IServiceCollection services, nint hwnd)
    {
        // Register services with window handle
        services.AddSingleton<IIoService>(_ => new IoService(hwnd));

        // Register view models
        services.AddTransient<MainWindowViewModel>();
    }
}