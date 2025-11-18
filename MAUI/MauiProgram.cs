namespace Moba.Smart;

using CommunityToolkit.Maui;

using Microsoft.Extensions.Logging;

using Moba.Backend.Network;
using Moba.SharedUI.Service;
using Moba.SharedUI.Interface; // ✅ Factory interfaces

using UraniumUI;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit() // ← Enable CommunityToolkit.Maui
            .UseMauiCommunityToolkitMediaElement() // ← Enable MediaElement
            .UseUraniumUI() // ← Enable UraniumUI Material Design
            .UseUraniumUIMaterial() // ← Enable Material Components
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // Platform services (MUST be registered before ViewModels that depend on them)
        builder.Services.AddSingleton<IUiDispatcher, MAUI.Service.UiDispatcher>();
        builder.Services.AddSingleton<INotificationService, MAUI.Service.NotificationService>();

        // ViewModels (CounterViewModel now requires IUiDispatcher and optional INotificationService)
        builder.Services.AddSingleton<SharedUI.ViewModel.CounterViewModel>();
        builder.Services.AddTransient<SharedUI.ViewModel.MAUI.JourneyViewModel>();

        // Backend services explicit
        builder.Services.AddSingleton<IUdpClientWrapper, UdpWrapper>();
        builder.Services.AddSingleton<Backend.Interface.IZ21, Backend.Z21>();
        builder.Services.AddSingleton<Backend.Interface.IJourneyManagerFactory, Backend.Manager.JourneyManagerFactory>();

        // ✅ DataManager as Singleton (master data loaded on first access)
        // Note: MAUI doesn't have IIoService yet - using simplified approach
        builder.Services.AddSingleton(sp => new Backend.Data.DataManager());

        // ✅ Solution as Singleton (initialized empty, can be loaded later by user)
        builder.Services.AddSingleton<Backend.Model.Solution>(sp => new Backend.Model.Solution());

        // ✅ All ViewModel Factories (MAUI-specific) - NEW NAMESPACES
        builder.Services.AddSingleton<SharedUI.Interface.IJourneyViewModelFactory, MAUI.Factory.MauiJourneyViewModelFactory>();
        builder.Services.AddSingleton<SharedUI.Interface.IStationViewModelFactory, MAUI.Factory.MauiStationViewModelFactory>();
        builder.Services.AddSingleton<SharedUI.Interface.IWorkflowViewModelFactory, MAUI.Factory.MauiWorkflowViewModelFactory>();
        builder.Services.AddSingleton<SharedUI.Interface.ILocomotiveViewModelFactory, MAUI.Factory.MauiLocomotiveViewModelFactory>();
        builder.Services.AddSingleton<SharedUI.Interface.ITrainViewModelFactory, MAUI.Factory.MauiTrainViewModelFactory>();
        builder.Services.AddSingleton<SharedUI.Interface.IWagonViewModelFactory, MAUI.Factory.MauiWagonViewModelFactory>();

        // TreeViewBuilder service (now with all factories)
        builder.Services.AddSingleton<SharedUI.Service.TreeViewBuilder>();

        // Views
        builder.Services.AddTransient<MainPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}