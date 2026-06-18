// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.MAUI;

using CommunityToolkit.Maui;

using Extensions;

using Service;

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
            .AddMobiConfiguration()
            .AddMobiNetworkServices()
            .AddMobiRemoteRuntimeServices()
            .AddMobiViewModels()
            .AddMobiViews()
            .AddMobiStartupServices();

        var app = builder.Build();
        app.Services.GetRequiredService<MobiStartupService>().Initialize();
        MobiDiContainerValidator.Validate(app.Services);
        return app;
    }
}
