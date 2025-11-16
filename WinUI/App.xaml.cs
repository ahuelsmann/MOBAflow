namespace Moba.WinUI;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;

using Moba.SharedUI.Service;
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
        // Configure services
        var services = new ServiceCollection();
        ConfigureServices(services);
        _serviceProvider = services.BuildServiceProvider();

        // Wire factory for TreeViewBuilder
        var factory = _serviceProvider.GetRequiredService<IJourneyViewModelFactory>();
        Services.TreeViewBuilder.JourneyVmFactory = factory;

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

        // Backend services
        services.AddSingleton<Moba.Backend.Interface.IZ21, Moba.Backend.Z21>();
        services.AddSingleton<Moba.Backend.Interface.IJourneyManagerFactory, Moba.Backend.Manager.JourneyManagerFactory>();

        // Factories
        services.AddSingleton<IJourneyViewModelFactory, WinUIJourneyViewModelFactory>();

        // Register WinUI-specific ViewModel (uses WinUI JourneyViewModel with DispatcherQueue)
        services.AddTransient<Moba.SharedUI.ViewModel.WinUI.MainWindowViewModel>();

        // Register Views (MainWindow expects MainWindowViewModel)
        services.AddTransient<MainWindow>();
    }
}