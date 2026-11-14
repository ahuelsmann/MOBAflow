// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.MAUI;

using CommunityToolkit.Maui;

using Extensions;

using SharedUI.Extensions;
using SharedUI.Mobile;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        builder.Services
            .AddMobiPlatformServices()
            .AddMobiNetworkServices()
            .AddMobiViews();

        var app = builder.Build();
        app.Services.GetRequiredService<MobiStartupService>().Initialize();
        app.Services.ValidateMobaMobileServices();
        return app;
    }
}