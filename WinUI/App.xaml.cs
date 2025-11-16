namespace Moba.WinUI;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;

using Moba.SharedUI.Service;
using Moba.WinUI.Service;
using Moba.Backend.Network;

using View;

public partial class App
{
    private Window? _window;
    private ServiceProvider? _serviceProvider;

    public App()
    {
        InitializeComponent();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        var services = new ServiceCollection();
        ConfigureServices(services);
        _serviceProvider = services.BuildServiceProvider();

        var factory = _serviceProvider.GetRequiredService<IJourneyViewModelFactory>();
        Service.TreeViewBuilder.JourneyVmFactory = factory;

        _window = _serviceProvider.GetRequiredService<MainWindow>();

        var ioService = _serviceProvider.GetRequiredService<IIoService>() as IoService;
        ioService?.SetWindowId(_window.AppWindow.Id);

        _window.Activate();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<IIoService, IoService>();

        // Backend services explicit
        services.AddSingleton<IUdpClientWrapper, UdpWrapper>();
        services.AddSingleton<Backend.Interface.IZ21, Backend.Z21>();
        services.AddSingleton<Backend.Interface.IJourneyManagerFactory, Backend.Manager.JourneyManagerFactory>();

        services.AddSingleton<IJourneyViewModelFactory, WinUIJourneyViewModelFactory>();
        services.AddTransient<SharedUI.ViewModel.WinUI.MainWindowViewModel>();
        services.AddTransient<MainWindow>();
    }
}