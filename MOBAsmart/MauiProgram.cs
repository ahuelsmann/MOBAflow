// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.MAUI;

using CommunityToolkit.Maui;

using Extensions;

using Service;
using SharedUI.Localization;
using System.Globalization;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var culture = UiLanguagePolicy.Resolve(
            UiLanguagePolicy.ReadSavedPreference(SettingsService.GetSettingsFilePath()), CultureInfo.CurrentUICulture);
        UiLanguagePolicy.ApplyResourceCulture(culture);
#if ANDROID
        LanIpv4AddressHelperAndroidBootstrap.Register();
#endif
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
        MobiDiContainerValidator.ValidateCoreServices(app.Services);
        return app;
    }
}
