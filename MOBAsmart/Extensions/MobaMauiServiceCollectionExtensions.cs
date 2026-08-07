// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.MAUI.Extensions;

using Backend;
using Backend.Interface;
using Common.Configuration;
using Common.Discovery;
using Common.Events;
using Common.Security;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Storage;
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
        services.AddSingleton<IRestDiscoveryService>(sp => sp.GetRequiredService<RestApiDiscoveryService>());
        services.AddSingleton<IAuthenticatedRestDiscoveryService>(sp => sp.GetRequiredService<RestApiDiscoveryService>());
        services.AddSingleton<ISecureStorage>(_ => SecureStorage.Default);
        services.AddSingleton<IRemoteControlCredentialStore, MauiRemoteControlCredentialStore>();
        services.AddSingleton<PinnedRemoteControlTransport>();
        services.AddSingleton<IRemoteControlTransport>(sp =>
            sp.GetRequiredService<PinnedRemoteControlTransport>());
        services.AddSingleton<IRemoteControlHttpClientFactory>(sp =>
            sp.GetRequiredService<PinnedRemoteControlTransport>());
        services.AddSingleton<RemoteControlSessionService>();
        services.AddSingleton<RemoteControlAuthenticatedHttpClient>();
        services.AddSingleton<IRemoteControlAuthenticatedHttpClient>(sp =>
            sp.GetRequiredService<RemoteControlAuthenticatedHttpClient>());
        services.AddSingleton<IPhotoUploadService, PhotoUploadService>();
        services.AddSingleton<IPhotoCaptureService, PhotoCaptureService>();
        services.AddSingleton<IPhotoUriResolver, MauiPhotoUriResolver>();
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
        services.AddSingleton<IMobileSolutionStore>(sp => new MobileSolutionStore(
            Path.Combine(FileSystem.AppDataDirectory, "mobile-cache"),
            sp.GetRequiredService<ILogger<MobileSolutionStore>>()));
        services.AddSingletonWithInterface<RuntimeHubRemoteClient, IRuntimeHubRemoteClient>();
        services.AddSingleton<RemoteRuntimeBridge>();
        services.AddSingleton<MobileRuntimeCoordinator>(sp => new MobileRuntimeCoordinator(
            sp.GetRequiredService<IMobaRuntime>(),
            sp.GetRequiredService<IRuntimeHubRemoteClient>()));
        services.AddSingleton<IMobileRuntimeCoordinator>(sp => sp.GetRequiredService<MobileRuntimeCoordinator>());
        // The mobile coordinator remains the mode owner while recording wraps only explicit command execution.
        services.AddSingleton(sp => new RecordingRuntimeCommandGateway(
            sp.GetRequiredService<MobileRuntimeCoordinator>(),
            sp.GetRequiredService<IRecordingSessionService>()));
        services.AddSingleton<IRuntimeCommandGateway>(sp => sp.GetRequiredService<RecordingRuntimeCommandGateway>());
        services.AddSingleton<SolutionRemoteLoader>(sp => new SolutionRemoteLoader(
            sp.GetRequiredService<IMobaRuntime>(),
            sp.GetRequiredService<MobileSolutionContext>(),
            sp.GetRequiredService<IEventBus>(),
            sp.GetRequiredService<ILogger<SolutionRemoteLoader>>(),
            sp.GetRequiredService<IHttpClientFactory>().CreateClient(MobiHttpClientNames.Platform),
            sp.GetRequiredService<IMobileRuntimeCoordinator>(),
            sp.GetRequiredService<IUiDispatcher>(),
            sp.GetRequiredService<IMobileSolutionStore>(),
            sp.GetRequiredService<IRemoteControlAuthenticatedHttpClient>()));
        services.AddSingleton<ISolutionRemoteLoader>(sp => sp.GetRequiredService<SolutionRemoteLoader>());

        return services;
    }

    /// <summary>
    /// Registers MOBAsmart ViewModels.
    /// </summary>
    public static IServiceCollection AddMobiViewModels(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<MauiViewModel>(sp => new MauiViewModel(
            sp.GetRequiredService<IMobaRuntime>(),
            sp.GetRequiredService<IUiDispatcher>(),
            sp.GetRequiredService<AppSettings>(),
            sp.GetRequiredService<ISettingsService>(),
            sp.GetRequiredService<IRestDiscoveryService>(),
            sp.GetRequiredService<IZ21DiscoveryService>(),
            sp.GetRequiredService<IPhotoUploadService>(),
            sp.GetRequiredService<IPhotoCaptureService>(),
            sp.GetRequiredService<INetworkProfileChangeNotifier>(),
            sp.GetRequiredService<ILogger<MauiViewModel>>(),
            sp.GetRequiredService<IEventBus>(),
            sp.GetRequiredService<IRestApiClientRegistration>(),
            sp.GetRequiredService<IRuntimeSettingsClient>(),
            sp.GetRequiredService<ISolutionRemoteLoader>(),
            sp.GetRequiredService<IMobileSolutionStore>(),
            sp.GetRequiredService<IRuntimeHubRemoteClient>(),
            sp.GetRequiredService<IRuntimeCommandGateway>(),
            sp.GetRequiredService<IMobileRuntimeCoordinator>(),
            sp.GetRequiredService<IProjectContext>(),
            sp.GetRequiredService<IBackgroundService>()));
        services.AddSingleton<RemotePairingViewModel>();
        services.AddSingleton<IPairingCameraPermission, PairingCameraPermission>();
        services.AddSingleton(new TrainControlViewModelOptions
        {
            HybridRuntimeSnapshots = true,
            Host = TrainControlHost.Maui
        });
        services.AddSingleton<TrainControlViewModel>(sp =>
        {
            var coordinator = sp.GetRequiredService<IMobileRuntimeCoordinator>();
            return new TrainControlViewModel(
                sp.GetRequiredService<IMobaRuntime>(),
                sp.GetRequiredService<ISettingsService>(),
                sp.GetRequiredService<IProjectContext>(),
                sp.GetRequiredService<ILogger<TrainControlViewModel>>(),
                sp.GetRequiredService<IUiDispatcher>(),
                sp.GetRequiredService<IEventBus>(),
                runtimeCommandGateway: sp.GetRequiredService<IRuntimeCommandGateway>(),
                mobileRuntimeCoordinator: coordinator,
                options: sp.GetRequiredService<TrainControlViewModelOptions>());
        });

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
        services.AddTransient<PairingPage>();

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
}