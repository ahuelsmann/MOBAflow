// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.MAUI;

using Backend.Extensions;
using Common.Configuration;
using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
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
            .UseMauiCommunityToolkitMediaElement() // ← Enable MediaElement
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

        // Configuration (AppSettings + ISettingsService)
        builder.Services.AddSingleton<AppSettings>();
        builder.Services.AddSingleton<ISettingsService, SettingsService>();

        // ✅ Audio Services (NullObject - MAUI doesn't support audio yet)
        // TODO: Replace with platform-specific implementations when MAUI audio is needed
        builder.Services.AddSingleton<ISoundPlayer, NullSoundPlayer>();
        builder.Services.AddSingleton<ISpeakerEngine, NullSpeakerEngine>();

        // ✅ REST-API Discovery + Photo Upload Services
        builder.Services.AddSingleton<RestApiDiscoveryService>();

        // ✅ Configure HttpClient with proper timeout and Android-specific handler
        builder.Services.AddSingleton<HttpClient>(_ =>
        {
#if ANDROID
            // Use platform-specific message handler for Android
            var handler = new AndroidMessageHandler
            {
                // Allow HTTP (cleartext) connections to local network
                AllowAutoRedirect = true,
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
                ServerCertificateCustomValidationCallback = (_, _, _, _) => true // Trust all certificates for local dev
            };
            var httpClient = new HttpClient(handler);
#else
            var httpClient = new HttpClient();
#endif
            // Set reasonable timeouts for photo uploads (large files)
            httpClient.Timeout = TimeSpan.FromMinutes(5);
            return httpClient;
        });

        builder.Services.AddSingleton<PhotoUploadService>();
        builder.Services.AddSingleton<IRestDiscoveryService, RestDiscoveryAdapter>();
        builder.Services.AddSingleton<IPhotoUploadService, PhotoUploadAdapter>();
        builder.Services.AddSingleton<IPhotoCaptureService, PhotoCaptureService>();

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