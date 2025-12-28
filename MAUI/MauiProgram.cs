// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.MAUI;

using Backend;
using Backend.Interface;
using Backend.Network;
using Backend.Service;

using Common.Configuration;

using CommunityToolkit.Maui;

using Microsoft.Extensions.Logging;

using Service;

using SharedUI.Interface;
using SharedUI.ViewModel;

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

        // ViewModels
        builder.Services.AddSingleton<MauiViewModel>();  // ✅ Mobile-optimized ViewModel

        // Backend services - Register in dependency order
        builder.Services.AddSingleton<IUdpClientWrapper, UdpWrapper>();
        builder.Services.AddSingleton<IZ21, Z21>();
        builder.Services.AddSingleton<ActionExecutor>();
        builder.Services.AddSingleton<WorkflowService>();

        // Views
        builder.Services.AddTransient<MainPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}