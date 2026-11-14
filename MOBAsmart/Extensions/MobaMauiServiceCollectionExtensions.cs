// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.MAUI.Extensions;

using Common.Discovery;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Storage;
using Service;
using SharedUI.Extensions;
using SharedUI.Interface;
using View;

/// <summary>
/// Dependency injection registrations for the MOBAsmart MAUI host. The Android implementations and the pages are
/// registered here; <see cref="MobileServiceCollectionExtensions.AddMobaMobileServices"/> in SharedUI registers
/// the platform-neutral runtime, ViewModels and startup service, where a portable test resolves them.
/// </summary>
public static class MobaMauiServiceCollectionExtensions
{
    /// <summary>
    /// Registers logging, the Android platform services and the platform-neutral mobile composition.
    /// </summary>
    public static IServiceCollection AddMobiPlatformServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddLogging();
        services.AddSingleton<IUiDispatcher, UiDispatcher>();
        services.AddSingleton<IBackgroundService, BackgroundService>();
        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddSingleton<NetworkInterfaceIpv4AddressSource>();
        // Registered before the shared composition, whose backend registration would otherwise add the default source.
        services.AddSingleton<ILocalIpv4AddressSource>(sp =>
            new AndroidLocalIpv4AddressSource(sp.GetRequiredService<NetworkInterfaceIpv4AddressSource>()));
        services.AddMobaMobileServices(
            () => FileSystem.AppDataDirectory,
            sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient(MobiHttpClientNames.Platform));

        return services;
    }

    /// <summary>
    /// Registers LAN discovery, photo upload, the runtime hub client and named HTTP clients.
    /// </summary>
    public static IServiceCollection AddMobiNetworkServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddMobiHttpClients();
        services.AddSingleton<RestApiDiscoveryService>();
        services.AddSingleton<IRestDiscoveryService>(sp => sp.GetRequiredService<RestApiDiscoveryService>());
        services.AddSingleton<IPhotoUploadService, PhotoUploadService>();
        services.AddSingleton<IPhotoCaptureService, PhotoCaptureService>();
        services.AddSingleton<IPhotoUriResolver, MauiPhotoUriResolver>();
        services.AddSingleton<IRestApiClientRegistration, RestApiClientRegistrationService>();
        services.AddSingleton<INetworkProfileChangeNotifier, MauiNetworkProfileChangeNotifier>();
        services.AddSingletonWithInterface<RuntimeHubRemoteClient, IRuntimeHubRemoteClient>();

        return services;
    }

    /// <summary>
    /// Registers transient MAUI pages and shell navigation targets.
    /// </summary>
    public static IServiceCollection AddMobiViews(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddTransient<AppShell>();
        services.AddTransient<SplashPage>();
        services.AddTransient<AppTabHostPage>();
        services.AddTransient<CounterPage>();
        services.AddTransient<SignalBoxPage>();
        services.AddTransient<EnginePage>();
        services.AddTransient<ControlPage>();

        return services;
    }
}
