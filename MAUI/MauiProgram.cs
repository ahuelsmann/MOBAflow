namespace Moba.Smart;

using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
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

		// ViewModels
		builder.Services.AddTransient<SharedUI.ViewModel.CounterViewModel>();
		builder.Services.AddTransient<SharedUI.ViewModel.MAUI.JourneyViewModel>();

		// Platform dispatcher
		builder.Services.AddSingleton<MAUI.Service.UiDispatcher>();

		// Views
		builder.Services.AddTransient<MainPage>();

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
