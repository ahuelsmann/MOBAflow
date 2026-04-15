// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.MAUI;

using Backend.Extensions;
using Backend.Interface;
using Backend.Service;

using Common.Configuration;

using CommunityToolkit.Maui;

using Service;

using SharedUI.Extensions;
using SharedUI.Interface;
using SharedUI.ViewModel;

using Sound;

using System.Net;

using UraniumUI;

using Xamarin.Android.Net;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit() // ← Enable CommunityToolkit.Maui
            .UseMauiCommunityToolkitMediaElement(isAndroidForegroundServiceEnabled: true) // ← Enable MediaElement with foreground service
            .UseUraniumUI() // ← Enable UraniumUI Material Design
            .UseUraniumUIMaterial() // ← Enable Material Components
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // Platform services (MUST be registered before ViewModels that depend on them)
        builder.Services.AddUiDispatcher();
        builder.Services.AddSingleton<IBackgroundService, BackgroundService>();

        // Event Bus with UI-thread marshalling (required by backend services)
        builder.Services.AddEventBusWithUiDispatch();

        // Configuration (AppSettings + ISettingsService)
        builder.Services.AddSingleton<AppSettings>();
        builder.Services.AddSingleton<ISettingsService, SettingsService>();

        // Audio Services (NullObject - MAUI doesn't support audio yet)
        builder.Services.AddSingleton<ISoundPlayer, NullSoundPlayer>();
        builder.Services.AddSingleton<ISpeakerEngine, NullSpeakerEngine>();

        // REST-API discovery (multicast + subnet HTTP); uses its own HttpClient (no proxy) to avoid LAN issues.
        builder.Services.AddSingleton<RestApiDiscoveryService>(sp =>
            new RestApiDiscoveryService(
                sp.GetRequiredService<AppSettings>()));

        // Configure HttpClient with proper timeout and Android-specific handler
        builder.Services.AddSingleton<HttpClient>(_ =>
        {
#if ANDROID
            var handler = new AndroidMessageHandler
            {
                AllowAutoRedirect = true,
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
                ServerCertificateCustomValidationCallback = (_, _, _, _) => true,
                // Route RFC1918 MOBApi/health checks directly; system VPN/proxy often breaks LAN (see SocksSocketImpl in logs).
                UseProxy = false
            };
            var httpClient = new HttpClient(handler);
#else
            var httpClient = new HttpClient();
#endif
            httpClient.Timeout = TimeSpan.FromMinutes(5);
            return httpClient;
        });

        builder.Services.AddSingleton<PhotoUploadService>();
        builder.Services.AddSingleton<IRestDiscoveryService, RestDiscoveryAdapter>();
        builder.Services.AddSingleton<IZ21DiscoveryService, Z21DiscoveryService>();
        builder.Services.AddSingleton<IPhotoUploadService, PhotoUploadAdapter>();
        builder.Services.AddSingleton<IPhotoCaptureService, PhotoCaptureService>();
        builder.Services.AddSingleton<IRestApiClientRegistration, RestApiClientRegistrationService>();
        builder.Services.AddSingleton<INetworkProfileChangeNotifier, MauiNetworkProfileChangeNotifier>();

        // ViewModels
        builder.Services.AddSingleton<MauiViewModel>();

        // Backend services - Register in dependency order
        // PERFORMANCE: Backend services (IZ21, WorkflowService, etc.) are configured
        // to defer connection/initialization until explicitly needed
        builder.Services.AddMobaBackendServices();
        builder.Services.AddSingleton<IMobaRuntime, MobaRuntimeService>();

        // Views
        builder.Services.AddTransient<View.SplashPage>();
        builder.Services.AddTransient<MainPage>();

        return builder.Build();
    }
}
