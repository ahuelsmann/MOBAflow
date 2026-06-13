// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Backend;

using Data;
using Interface;
using Manager;
using Network;
using Service;

using Common.Configuration;
using Common.Events;
using Common.IO;
using Common.Multiplex;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

using Sound;

/// <summary>
/// Dependency injection registrations shared by all MOBAflow hosts that use the backend runtime.
/// </summary>
public static class MobaBackendServiceCollectionExtensions
{
    /// <summary>
    /// Registers the platform-neutral backend runtime, Z21 transport and workflow services.
    /// </summary>
    public static IServiceCollection AddMobaBackendServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton<MasterDataStore>();
        services.TryAddSingleton<IFileSystem>(SystemFileSystem.Instance);
        services.TryAddSingleton<IMultiplexerProvider, DefaultMultiplexerProvider>();
        services.TryAddSingleton<Z21Monitor>();
        services.TryAddSingleton<IUdpClientWrapper, UdpWrapper>();
        services.TryAddSingleton<IZ21, Z21>();
        services.TryAddSingleton<IZ21Connection>(sp => sp.GetRequiredService<IZ21>());
        services.TryAddSingleton<ILocoControl>(sp => sp.GetRequiredService<IZ21>());
        services.TryAddSingleton<IAccessoryControl>(sp => sp.GetRequiredService<IZ21>());
        services.TryAddSingleton<IZ21Diagnostics>(sp => sp.GetRequiredService<IZ21>());
        services.TryAddSingleton<IPlatformManagerFactory, PlatformManagerFactory>();
        services.TryAddSingleton<IStationManagerFactory, StationManagerFactory>();
        services.TryAddSingleton<IJourneyManagerFactory, JourneyManagerFactory>();
        services.TryAddSingleton<IProjectValidator, ProjectValidator>();
        services.TryAddSingleton<AnnouncementService>();
        services.TryAddSingleton<IAnnouncementService>(sp => sp.GetRequiredService<AnnouncementService>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IWorkflowActionHandler, CommandWorkflowActionHandler>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IWorkflowActionHandler, AudioWorkflowActionHandler>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IWorkflowActionHandler, AnnouncementWorkflowActionHandler>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IWorkflowActionHandler, ExecuteScriptWorkflowActionHandler>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IWorkflowActionHandler, SelectSignalAspectWorkflowActionHandler>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IWorkflowActionHandler, TrainDestinationDisplayWorkflowActionHandler>());
        services.TryAddSingleton<IActionExecutor, ActionExecutor>();
        services.TryAddSingleton<IWorkflowService, WorkflowService>();
        services.TryAddSingleton(sp => new ActionExecutionContext
        {
            Z21 = sp.GetRequiredService<IZ21>(),
            SpeakerEngine = sp.GetService<ISpeakerEngine>(),
            SoundPlayer = sp.GetService<ISoundPlayer>()
        });
        services.TryAddSingleton<IActionExecutionContextFactory, ActionExecutionContextFactory>();
        services.TryAddSingleton<IMobaRuntime>(sp => new MobaRuntimeService(
            sp.GetRequiredService<IZ21>(),
            sp.GetRequiredService<IWorkflowService>(),
            sp.GetRequiredService<IActionExecutionContextFactory>(),
            sp.GetRequiredService<AppSettings>(),
            sp.GetRequiredService<ILogger<MobaRuntimeService>>(),
            sp.GetService<IEventBus>(),
            sp.GetRequiredService<IJourneyManagerFactory>()));
        services.TryAddSingleton<IRuntimeSnapshotProvider>(sp => sp.GetRequiredService<IMobaRuntime>());
        services.TryAddSingleton<IConnectionRuntime>(sp => sp.GetRequiredService<IMobaRuntime>());
        services.TryAddSingleton<ILocomotiveRuntime>(sp => sp.GetRequiredService<IMobaRuntime>());
        services.TryAddSingleton<ISignalTurnoutRuntime>(sp => sp.GetRequiredService<IMobaRuntime>());
        services.TryAddSingleton<ITrafficMonitor>(sp => sp.GetRequiredService<IMobaRuntime>());

        return services;
    }
}
