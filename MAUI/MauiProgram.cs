namespace Moba.Smart;

using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using UraniumUI;
using Moba.Backend.Network;
using Moba.SharedUI.Service;

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

        // Factories
        builder.Services.AddSingleton<Moba.SharedUI.Service.IJourneyViewModelFactory, Moba.MAUI.Service.MauiJourneyViewModelFactory>();
        
        // TreeViewBuilder service
        builder.Services.AddSingleton<SharedUI.Service.TreeViewBuilder>();

		// Views
		builder.Services.AddTransient<MainPage>();

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
