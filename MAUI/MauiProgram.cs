// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Smart;

using CommunityToolkit.Maui;

using Microsoft.Extensions.Logging;

using Moba.Backend.Network;
using Moba.Backend.Service;
using Moba.SharedUI.Interface;

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
        builder.Services.AddSingleton<SharedUI.Interface.IBackgroundService, MAUI.Service.BackgroundService>();

        // Configuration (AppSettings + ISettingsService)
        builder.Services.AddSingleton<Common.Configuration.AppSettings>();
        builder.Services.AddSingleton<ISettingsService, MAUI.Service.SettingsService>();

        // ViewModels (CounterViewModel now requires IUiDispatcher and ISettingsService)
        builder.Services.AddSingleton<SharedUI.ViewModel.CounterViewModel>();
        builder.Services.AddTransient<SharedUI.ViewModel.JourneyViewModel>();

        // Backend services - Register in dependency order
        builder.Services.AddSingleton<IUdpClientWrapper, UdpWrapper>();
        builder.Services.AddSingleton<Backend.Interface.IZ21, Backend.Z21>();
        builder.Services.AddSingleton<ActionExecutor>();
        builder.Services.AddSingleton<WorkflowService>();

        // ✅ DataManager as Singleton (master data loaded on first access)
        builder.Services.AddSingleton<Backend.Data.DataManager>();

        // ✅ Solution as Singleton (initialized empty, can be loaded later by user)
        builder.Services.AddSingleton<Domain.Solution>();

        // Views
        builder.Services.AddTransient<MainPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}