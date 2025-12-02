// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Smart;

using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using Moba.Backend.Network;
using Moba.SharedUI.Service;
using Moba.MAUI.Service;

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
        builder.Services.AddSingleton<SharedUI.Service.IBackgroundService, MAUI.Service.BackgroundService>();

        // ViewModels (CounterViewModel now requires IUiDispatcher and optional INotificationService)
        builder.Services.AddSingleton<SharedUI.ViewModel.CounterViewModel>();
        builder.Services.AddTransient<SharedUI.ViewModel.JourneyViewModel>();

        // Backend services - Register in dependency order
        builder.Services.AddSingleton<IUdpClientWrapper, UdpWrapper>();
        builder.Services.AddSingleton<Backend.Interface.IZ21, Backend.Z21>();
        builder.Services.AddSingleton<Backend.Services.ActionExecutor>(sp =>
        {
            var z21 = sp.GetRequiredService<Backend.Interface.IZ21>();
            return new Backend.Services.ActionExecutor(z21);
        });
        builder.Services.AddSingleton<Backend.Services.WorkflowService>();
        builder.Services.AddSingleton<Backend.Interface.IJourneyManagerFactory, Backend.Manager.JourneyManagerFactory>();

        // ✅ DataManager as Singleton (master data loaded on first access)
        // Note: MAUI doesn't have IIoService yet - using simplified approach
        builder.Services.AddSingleton(sp => new Backend.Data.DataManager());

        // ✅ Solution as Singleton (initialized empty, can be loaded later by user)
        builder.Services.AddSingleton<Domain.Solution>(sp => new Domain.Solution());

        // Views
        builder.Services.AddTransient<MainPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
