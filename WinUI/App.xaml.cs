// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;

namespace Moba.WinUI;

/// <summary>
/// Provides application-specific behavior to supplement the default Application class.
/// </summary>
public partial class App : Application
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

        // Backend Services (Interfaces are in Backend.Interface and Backend.Network)
        services.AddSingleton<Backend.Interface.IZ21, Backend.Z21>();
        services.AddSingleton<Backend.Network.IUdpClientWrapper, Backend.Network.UdpWrapper>();
        services.AddSingleton<Domain.Solution>();

        // WinUI Services (Interfaces are in SharedUI.Service)
        services.AddSingleton<SharedUI.Service.IIoService, Service.IoService>();
        services.AddSingleton<SharedUI.Service.INotificationService, Service.NotificationService>();
        services.AddSingleton<SharedUI.Service.IPreferencesService, Service.PreferencesService>();
        services.AddSingleton<SharedUI.Service.IUiDispatcher, Service.UiDispatcher>();
        services.AddSingleton<Service.HealthCheckService>();

        // ViewModels
        services.AddSingleton<SharedUI.ViewModel.WinUI.MainWindowViewModel>();
        services.AddTransient<SharedUI.ViewModel.WinUI.JourneyViewModel>();
        services.AddSingleton<SharedUI.ViewModel.CounterViewModel>();

        return services.BuildServiceProvider();
    }

    /// <summary>
    /// Invoked when the application is launched.
    /// </summary>
    /// <param name="args">Details about the launch request and process.</param>
    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        var mainWindowViewModel = Services.GetRequiredService<SharedUI.ViewModel.WinUI.MainWindowViewModel>();
        var counterViewModel = Services.GetRequiredService<SharedUI.ViewModel.CounterViewModel>();
        var healthCheckService = Services.GetRequiredService<Service.HealthCheckService>();
        var uiDispatcher = Services.GetRequiredService<SharedUI.Service.IUiDispatcher>();
        
        _window = new View.MainWindow(mainWindowViewModel, counterViewModel, healthCheckService, uiDispatcher);
        _window.Activate();
    }
}
