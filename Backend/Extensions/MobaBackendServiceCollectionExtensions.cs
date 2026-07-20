// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Backend;

using Common.Configuration;
using Common.Discovery;
using Common.Events;
using Common.IO;
using Common.Multiplex;
using Data;
using Discovery;
using Interface;
using Manager;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Network;
using Service;
using Service.Validation;
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
        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton<IVehicleUsageService, VehicleUsageService>();
        services.TryAddSingleton<IVehicleMaintenanceService, VehicleMaintenanceService>();
        services.TryAddSingleton<IDecoderCvService, DecoderCvService>();
        services.TryAddSingleton<ILocomotiveLibraryService, LocomotiveLibraryService>();
        services.TryAddSingleton<ILocomotivePassportHtmlRenderer, LocomotivePassportHtmlRenderer>();
        services.TryAddSingleton<IDigitalAddressConflictDetector, DigitalAddressConflictDetector>();
        services.TryAddSingleton<IProjectDiagnosticsService, ProjectDiagnosticsService>();
        services.TryAddSingleton<Z21Monitor>();
        services.TryAddSingleton<IUdpClientWrapper, UdpWrapper>();
        services.TryAddSingleton<IZ21DiscoveryService, Z21DiscoveryService>();
        services.TryAddSingleton<IZ21, Z21>();
        services.TryAddSingleton<IWorkflowValidator, WorkflowValidator>();
        services.TryAddSingleton<IProjectValidator, ProjectValidator>();
        services.TryAddSingleton<IJourneyStopTransitionService, JourneyStopTransitionService>();
        services.TryAddSingleton<IJourneyRuntimeStateStore, FileJourneyRuntimeStateStore>();
        services.TryAddSingleton<AnnouncementService>();
        services.TryAddSingleton<IAnnouncementService>(sp => sp.GetRequiredService<AnnouncementService>());
        services.TryAddSingleton<IWorkflowEffectPlanner, WorkflowEffectPlanner>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IWorkflowActionHandler, CommandWorkflowActionHandler>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IWorkflowActionHandler, AudioWorkflowActionHandler>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IWorkflowActionHandler, AnnouncementWorkflowActionHandler>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IWorkflowActionHandler, ExecuteScriptWorkflowActionHandler>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IWorkflowActionHandler, SelectSignalAspectWorkflowActionHandler>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IWorkflowActionHandler, TrainDestinationDisplayWorkflowActionHandler>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IWorkflowActionHandler, ChangeJourneyStopWorkflowActionHandler>());
        services.TryAddSingleton<IActionExecutor, ActionExecutor>();
        services.TryAddSingleton<IWorkflowService, WorkflowService>();
        services.TryAddSingleton(sp => new ActionExecutionContext
        {
            Z21 = sp.GetRequiredService<IZ21>(),
            SpeakerEngine = sp.GetService<ISpeakerEngine>(),
            SoundPlayer = sp.GetService<ISoundPlayer>()
        });
        services.TryAddSingleton<ActionExecutionContextFactory>();
        services.TryAddSingleton<IMobaRuntime>(sp => new MobaRuntimeService(
            sp.GetRequiredService<IZ21>(),
            sp.GetRequiredService<IWorkflowService>(),
            sp.GetRequiredService<ActionExecutionContextFactory>(),
            sp.GetRequiredService<AppSettings>(),
            sp.GetRequiredService<ILogger<MobaRuntimeService>>(),
            sp.GetService<IEventBus>(),
            journeyManagerFactory: new JourneyManagerFactory(
                sp.GetRequiredService<IZ21>(),
                sp.GetRequiredService<IWorkflowService>(),
                sp.GetRequiredService<IJourneyStopTransitionService>(),
                sp.GetRequiredService<IJourneyRuntimeStateStore>(),
                sp.GetService<ILogger<JourneyManager>>()),
            z21Discovery: sp.GetRequiredService<IZ21DiscoveryService>()));
        services.TryAddSingleton<ILocomotiveFunctionCommandGateway, MobaRuntimeLocomotiveFunctionCommandGateway>();
        services.TryAddSingleton<ILocomotiveWhistleAutomationService, LocomotiveWhistleAutomationService>();

        return services;
    }
}
