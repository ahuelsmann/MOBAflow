// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.MAUI.Extensions;

using Backend;
using Backend.Interface;

using Common.Configuration;
using Common.Events;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Service;

using SharedUI.Extensions;
using SharedUI.Interface;
using SharedUI.Service;
using SharedUI.ViewModel;

using Sound;

using View;

/// <summary>
/// Dependency injection registrations for the MOBAsmart MAUI host.
/// </summary>
public static class MobaMauiServiceCollectionExtensions
{
    /// <summary>
    /// Registers platform-specific services required before backend and ViewModel wiring.
    /// </summary>
    public static IServiceCollection AddMobiPlatformServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<IUiDispatcher, UiDispatcher>();
        services.AddSingleton<IBackgroundService, BackgroundService>();
        services.AddEventBusWithUiDispatch();

        return services;
    }

    /// <summary>
    /// Registers configuration, logging, null audio implementations, and shared backend services.
    /// </summary>
    public static IServiceCollection AddMobiConfiguration(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<AppSettings>();
        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddLogging();
        services.AddSingleton<ISoundPlayer, NullSoundPlayer>();
        services.AddSingleton<ISpeakerEngine, NullSpeakerEngine>();
        services.AddMobaBackendServices();

        return services;
    }

    /// <summary>
    /// Registers LAN discovery, photo upload, Z21 discovery, and named HTTP clients.
    /// </summary>
    public static IServiceCollection AddMobiNetworkServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddMobiHttpClients();
        services.AddSingleton<RestApiDiscoveryService>();
        services.AddSingleton<PhotoUploadService>();
        services.AddSingleton<IRestDiscoveryService, RestDiscoveryAdapter>();
        services.AddSingleton<IZ21DiscoveryService, Z21DiscoveryService>();
        services.AddSingleton<IPhotoUploadService, PhotoUploadAdapter>();
        services.AddSingleton<IPhotoCaptureService, PhotoCaptureService>();
        services.AddSingleton<IRestApiClientRegistration, RestApiClientRegistrationService>();
        services.AddSingleton<IRuntimeSettingsClient, RuntimeSettingsClient>();
        services.AddSingleton<INetworkProfileChangeNotifier, MauiNetworkProfileChangeNotifier>();

        return services;
    }

    /// <summary>
    /// Registers the remote runtime hub, solution sync, and command gateway stack.
    /// </summary>
    public static IServiceCollection AddMobiRemoteRuntimeServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingletonWithInterface<MobileSolutionContext, IProjectContext>();
        services.AddSingleton<SolutionRemoteLoader>(sp => new SolutionRemoteLoader(
            sp.GetRequiredService<IMobaRuntime>(),
            sp.GetRequiredService<MobileSolutionContext>(),
            sp.GetRequiredService<IEventBus>(),
            sp.GetRequiredService<ILogger<SolutionRemoteLoader>>(),
            sp.GetRequiredService<IHttpClientFactory>().CreateClient(MobiHttpClientNames.Platform)));
        services.AddSingleton<ISolutionRemoteLoader>(sp => sp.GetRequiredService<SolutionRemoteLoader>());
        services.AddSingletonWithInterface<RuntimeHubRemoteClient, IRuntimeHubRemoteClient>();
        services.AddSingleton<RemoteRuntimeBridge>();
        services.AddSingletonWithInterface<RemoteRuntimeCommandGateway, IRuntimeCommandGateway>();

        return services;
    }

    /// <summary>
    /// Registers MOBAsmart ViewModels.
    /// </summary>
    public static IServiceCollection AddMobiViewModels(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<MauiViewModel>();
        services.AddSingleton(new TrainControlViewModelOptions { UseRemoteRuntimeSnapshots = true });
        services.AddSingleton<TrainControlViewModel>();

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
        services.AddTransient<ControlPage>();

        return services;
    }

    /// <summary>
    /// Registers explicit startup services that must be initialized after the container is built.
    /// </summary>
    public static IServiceCollection AddMobiStartupServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<MobiStartupService>();

        return services;
    }

    private static IServiceCollection AddSingletonWithInterface<TService, TInterface>(this IServiceCollection services)
        where TService : class, TInterface
        where TInterface : class
    {
        services.AddSingleton<TService>();
        services.AddSingleton<TInterface>(sp => sp.GetRequiredService<TService>());
        return services;
    }
}
