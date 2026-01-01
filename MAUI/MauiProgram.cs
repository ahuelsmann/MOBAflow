// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.MAUI;

using Backend.Extensions;

using Common.Configuration;

using CommunityToolkit.Maui;

using Microsoft.Extensions.Logging;

using Service;

using SharedUI.Interface;
using SharedUI.ViewModel;

using Sound;

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
        builder.Services.AddSingleton<IUiDispatcher, UiDispatcher>();
        builder.Services.AddSingleton<IBackgroundService, BackgroundService>();

        // Configuration (AppSettings + ISettingsService)
        builder.Services.AddSingleton<AppSettings>();
        builder.Services.AddSingleton<ISettingsService, SettingsService>();

        // ✅ Audio Services (NullObject - MAUI doesn't support audio yet)
        // TODO: Replace with platform-specific implementations when MAUI audio is needed
        builder.Services.AddSingleton<ISoundPlayer, NullSoundPlayer>();
        builder.Services.AddSingleton<ISpeakerEngine, NullSpeakerEngine>();

        // ViewModels
        builder.Services.AddSingleton<MauiViewModel>();  // ✅ Mobile-optimized ViewModel

        // Backend services - Register in dependency order
        // ✅ Use shared extension method for platform-consistent registration
        // Backend expects ISoundPlayer/ISpeakerEngine from above (NullObject for now)
        builder.Services.AddMobaBackendServices();

        // Views
        builder.Services.AddTransient<MainPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}