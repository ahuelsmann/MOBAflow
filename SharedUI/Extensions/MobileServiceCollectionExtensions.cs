// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.SharedUI.Extensions;

using Backend;
using Backend.Interface;
using Backend.Service;

using Common.Configuration;
using Common.Events;

using Interface;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Mobile;

using Service;

using Sound;

using Speech.Base;

using TrainControl;

/// <summary>
/// The platform-neutral part of the MOBAsmart composition. It lives here, not in the Android app, so that a
/// portable test can build the container and resolve every registration.
/// </summary>
public static class MobileServiceCollectionExtensions
{
    /// <summary>
    /// Registers the backend runtime, the remote runtime stack, the mobile ViewModels and the startup service.
    /// The host registers logging and its platform services before or after this call:
    /// <see cref="IUiDispatcher"/>, <see cref="ISettingsService"/>, <see cref="IBackgroundService"/>,
    /// <see cref="IRestDiscoveryService"/>, <see cref="IPhotoUploadService"/>, <see cref="IPhotoCaptureService"/>,
    /// <see cref="IRestApiClientRegistration"/>, <see cref="INetworkProfileChangeNotifier"/> and
    /// <see cref="IRuntimeHubRemoteClient"/>. A host with its own <see cref="Common.Discovery.ILocalIpv4AddressSource"/>
    /// registers it before this call.
    /// </summary>
    /// <param name="services">The service collection of the mobile host.</param>
    /// <param name="appDataDirectory">Returns the app's private data directory for counters and the solution cache.</param>
    /// <param name="platformHttpClient">Returns the HTTP client that loads the solution from MOBAflow.</param>
    public static IServiceCollection AddMobaMobileServices(
        this IServiceCollection services,
        Func<string> appDataDirectory,
        Func<IServiceProvider, HttpClient> platformHttpClient)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(appDataDirectory);
        ArgumentNullException.ThrowIfNull(platformHttpClient);

        services.AddEventBusWithUiDispatch();
        services.AddMobileBackendServices(appDataDirectory);
        services.AddMobileRemoteRuntimeServices(appDataDirectory, platformHttpClient);
        services.AddMobileViewModels();
        services.AddSingleton<MobiStartupService>();

        return services;
    }

    /// <summary>
    /// Resolves the services MOBAsmart needs before its first page appears; throws on the first one that is missing.
    /// </summary>
    public static void ValidateMobaMobileServices(this IServiceProvider services)
    {
        ArgumentNullException.ThrowIfNull(services);

        _ = services.GetRequiredService<MobiStartupService>();
        _ = services.GetRequiredService<MauiViewModel>();
        _ = services.GetRequiredService<TrainControlViewModel>();
        _ = services.GetRequiredService<RemoteRuntimeBridge>();
        _ = services.GetRequiredService<IMobaRuntime>();
        _ = services.GetRequiredService<IRuntimeCommandGateway>();
    }

    private static void AddMobileBackendServices(this IServiceCollection services, Func<string> appDataDirectory)
    {
        services.AddSingleton<AppSettings>();
        services.AddSingleton<InPortCounterService>(sp => new InPortCounterService(
            sp.GetRequiredService<IZ21>(), sp.GetRequiredService<AppSettings>(),
            sp.GetRequiredService<TimeProvider>(), sp.GetRequiredService<ILogger<InPortCounterService>>(),
            InPortCounterPersistence.PerProject(projectId => new FileInPortCounterStore(
                Path.Combine(appDataDirectory(), "inport-counters", projectId.ToString("N") + ".json")))));
        services.AddSingleton<ISoundPlayer, NullSoundPlayer>();
        services.AddSingleton<ISpeechOutput, NullSpeechOutput>();
        // The phone has no file picker or function-symbol editor; the view models get the null objects.
        services.AddSingleton<IFilePickerService, NullFilePickerService>();
        services.AddSingleton<IFunctionAppearancePicker, NullFunctionAppearancePicker>();
        services.AddSingleton(sp => new SolutionViewModelServices(
            sp.GetRequiredService<IFilePickerService>(),
            sp.GetRequiredService<ISoundPlayer>(),
            sp.GetRequiredService<ILoggerFactory>()));
        services.AddMobaBackendServices();
    }

    private static void AddMobileRemoteRuntimeServices(
        this IServiceCollection services,
        Func<string> appDataDirectory,
        Func<IServiceProvider, HttpClient> platformHttpClient)
    {
        services.AddSingleton<MobaflowEndpointLocator>();
        services.AddSingleton<MobilePhotoUploader>();
        services.AddSingletonWithInterface<MobileSolutionContext, IProjectContext>();
        // The phone controls the Z21 of the project selected on it; it never searches the network for one.
        services.AddSingleton<IZ21EndpointSource>(sp => new ProjectZ21EndpointSource(
            () => sp.GetRequiredService<IProjectContext>().SelectedProject?.Model,
            solution: () => sp.GetRequiredService<IProjectContext>().SolutionViewModel?.Model));
        services.AddSingleton<IMobileSolutionStore>(sp => new MobileSolutionStore(
            Path.Combine(appDataDirectory(), "mobile-cache"),
            sp.GetRequiredService<ILogger<MobileSolutionStore>>()));
        services.AddSingleton(sp => new MobaflowHubSession(
            sp.GetRequiredService<AppSettings>(),
            sp.GetRequiredService<MobaflowEndpointLocator>(),
            sp.GetRequiredService<IPhotoUploadService>(),
            sp.GetRequiredService<TimeProvider>(),
            sp.GetRequiredService<ILogger<MobaflowHubSession>>(),
            sp.GetRequiredService<IRuntimeHubRemoteClient>(),
            sp.GetRequiredService<IRestApiClientRegistration>()));
        services.AddSingleton<RemoteRuntimeBridge>();
        services.AddSingleton<MobileRuntimeCoordinator>(sp => new MobileRuntimeCoordinator(
            sp.GetRequiredService<IMobaRuntime>(),
            sp.GetRequiredService<IRuntimeHubRemoteClient>()));
        services.AddSingleton<IMobileRuntimeCoordinator>(sp => sp.GetRequiredService<MobileRuntimeCoordinator>());
        services.AddSingleton<IRuntimeRoutingState>(sp => sp.GetRequiredService<MobileRuntimeCoordinator>());
        services.AddSingleton(sp => new MobileRuntimeProjection(
            sp.GetRequiredService<IRuntimeSnapshotProvider>(),
            sp.GetRequiredService<IProjectContext>(),
            sp.GetRequiredService<IMobileRuntimeCoordinator>(),
            sp.GetRequiredService<IMobileSolutionStore>()));
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
            platformHttpClient(sp),
            sp.GetRequiredService<IMobileRuntimeCoordinator>(),
            sp.GetRequiredService<IUiDispatcher>(),
            sp.GetRequiredService<IMobileSolutionStore>()));
        services.AddSingleton<ISolutionRemoteLoader>(sp => sp.GetRequiredService<SolutionRemoteLoader>());
        services.AddSingleton(sp => new MobileSolutionSynchronizer(
            sp.GetRequiredService<ILogger<MobileSolutionSynchronizer>>(),
            sp.GetRequiredService<ISolutionRemoteLoader>()));
    }

    private static void AddMobileViewModels(this IServiceCollection services)
    {
        services.AddSingleton<MauiViewModel>(sp => new MauiViewModel(
            sp.GetRequiredService<IRuntimeSnapshotProvider>(),
            sp.GetRequiredService<IConnectionRuntime>(),
            sp.GetRequiredService<ITrafficMonitor>(),
            sp.GetRequiredService<IRuntimeCommandGateway>(),
            sp.GetRequiredService<IUiDispatcher>(),
            sp.GetRequiredService<AppSettings>(),
            sp.GetRequiredService<ISettingsService>(),
            sp.GetRequiredService<MobaflowEndpointLocator>(),
            sp.GetRequiredService<MobaflowHubSession>(),
            sp.GetRequiredService<MobileRuntimeProjection>(),
            sp.GetRequiredService<MobileSolutionSynchronizer>(),
            sp.GetRequiredService<MobilePhotoUploader>(),
            sp.GetRequiredService<INetworkProfileChangeNotifier>(),
            sp.GetRequiredService<ILogger<MauiViewModel>>(),
            sp.GetRequiredService<IEventBus>(),
            sp.GetRequiredService<IRuntimeHubRemoteClient>(),
            sp.GetRequiredService<IMobileRuntimeCoordinator>(),
            sp.GetRequiredService<IProjectContext>(),
            sp.GetRequiredService<IBackgroundService>()));
        services.AddSingleton(new TrainControlViewModelOptions
        {
            HybridRuntimeSnapshots = true,
            Host = TrainControlHost.Maui
        });
        services.AddSingleton<TrainControlViewModel>(sp => new TrainControlViewModel(
            sp.GetRequiredService<IRuntimeSnapshotProvider>(),
            sp.GetRequiredService<IRuntimeCommandGateway>(),
            sp.GetRequiredService<ISettingsService>(),
            sp.GetRequiredService<IProjectContext>(),
            sp.GetRequiredService<ILogger<TrainControlViewModel>>(),
            sp.GetRequiredService<IUiDispatcher>(),
            sp.GetRequiredService<IEventBus>(),
            sp.GetRequiredService<IFunctionAppearancePicker>(),
            sp.GetRequiredService<IRuntimeRoutingState>(),
            options: sp.GetRequiredService<TrainControlViewModelOptions>()));
    }
}
