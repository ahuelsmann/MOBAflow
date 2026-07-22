// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Backend;

using Common.Configuration;
using Common.Discovery;
using Common.Events;
using Common.IO;
using Common.Multiplex;
using Common.Recording;
using Data;
using Discovery;
using Interface;
using Manager;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Network;
using Service;
using Service.Interlocking;
using Service.Recording;
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
        services.TryAddSingleton<IInterlockingDefinitionValidator, InterlockingDefinitionValidator>();
        services.TryAddSingleton<IProjectDiagnosticsService, ProjectDiagnosticsService>();
        services.TryAddSingleton<ITimetableEvaluationService, TimetableEvaluationService>();
        services.TryAddSingleton<ITimetableTimingService, TimetableTimingService>();
        services.TryAddSingleton<ITimetableStateStore, FileTimetableStateStore>();
        services.TryAddSingleton<ITimetableOperationsService, TimetableOperationsService>();
        services.TryAddSingleton<ITimetableRuntimeProjectionService, TimetableRuntimeProjectionService>();
        services.TryAddSingleton<RecorderOptions>();
        services.TryAddSingleton<IRecordingSessionService, RecordingSessionService>();
        services.TryAddSingleton<IRecordingStatusSource>(sp => sp.GetRequiredService<IRecordingSessionService>());
        services.TryAddSingleton<IIsolatedReplayRuntimeFactory, IsolatedReplayRuntimeFactory>();
        services.TryAddSingleton<IRecordingReplayDelayScheduler, TimeProviderRecordingReplayDelayScheduler>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IRecordingEventMapper, Z21RecordingEventMapper>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IRecordingEventMapper, RuntimeSnapshotRecordingEventMapper>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IRecordingEventMapper, JourneyRecordingEventMapper>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IRecordingEventMapper, WorkflowLifecycleRecordingEventMapper>());
        services.TryAddSingleton<RecordingEventMapperRegistry>();
        if (services.All(descriptor => descriptor.ServiceType != typeof(CoreRecordingPayloadValidatorRegistrationMarker)))
        {
            services.AddSingleton<CoreRecordingPayloadValidatorRegistrationMarker>();
            foreach (var validator in CoreRecordingPayloadValidators.Create())
            {
                services.AddSingleton(validator);
            }
        }
        services.TryAddSingleton<RecordingArtifactSerializer>();
        services.TryAddSingleton<Z21Monitor>();
        services.TryAddSingleton<IUdpClientWrapper, UdpWrapper>();
        services.TryAddSingleton<IZ21DiscoveryService, Z21DiscoveryService>();
        services.TryAddSingleton<IZ21, Z21>();
        services.TryAddSingleton<IWorkflowValidator, WorkflowValidator>();
        services.TryAddSingleton<IInterlockingRuntime, InterlockingRuntimeService>();
        services.TryAddSingleton<IProjectValidator, ProjectValidator>();
        services.TryAddSingleton<IJourneyStopTransitionService, JourneyStopTransitionService>();
        services.TryAddSingleton<IJourneyRuntimeStateStore, FileJourneyRuntimeStateStore>();
        services.TryAddSingleton<IVehicleUsageCheckpointStore, FileVehicleUsageCheckpointStore>();
        services.TryAddSingleton<AnnouncementService>();
        services.TryAddSingleton<IAnnouncementService>(sp => sp.GetRequiredService<AnnouncementService>());
        services.TryAddSingleton<IWorkflowEffectPlanner, WorkflowEffectPlanner>();
        services.TryAddSingleton<IWorkflowConditionEvaluator, WorkflowConditionEvaluator>();
        services.TryAddSingleton<IWorkflowTraceStore, WorkflowTraceStore>();
        services.TryAddSingleton(sp => new WorkflowServiceDependencies
        {
            Validator = sp.GetRequiredService<IWorkflowValidator>(),
            EffectPlanner = sp.GetRequiredService<IWorkflowEffectPlanner>(),
            ConditionEvaluator = sp.GetRequiredService<IWorkflowConditionEvaluator>(),
            EventBus = sp.GetService<IEventBus>(),
            TraceStore = sp.GetRequiredService<IWorkflowTraceStore>(),
            TimeProvider = sp.GetRequiredService<TimeProvider>(),
            Logger = sp.GetService<ILogger<WorkflowService>>()
        });
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IWorkflowActionHandler, CommandWorkflowActionHandler>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IWorkflowActionHandler, AudioWorkflowActionHandler>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IWorkflowActionHandler, AnnouncementWorkflowActionHandler>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IWorkflowActionHandler, ExecuteScriptWorkflowActionHandler>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IWorkflowActionHandler, SelectSignalAspectWorkflowActionHandler>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IWorkflowActionHandler, TrainDestinationDisplayWorkflowActionHandler>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IWorkflowActionHandler, ChangeJourneyStopWorkflowActionHandler>());
        services.TryAddSingleton<IActionExecutor, ActionExecutor>();
        services.TryAddSingleton<IWorkflowService>(sp => new WorkflowService(
            sp.GetRequiredService<IActionExecutor>(),
            sp.GetRequiredService<WorkflowServiceDependencies>()));
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
                sp.GetService<ILogger<JourneyManager>>(),
                timeProvider: sp.GetRequiredService<TimeProvider>(),
                eventBus: sp.GetService<IEventBus>()),
            z21Discovery: sp.GetRequiredService<IZ21DiscoveryService>(),
            vehicleUsageCheckpointStore: sp.GetRequiredService<IVehicleUsageCheckpointStore>(),
            timeProvider: sp.GetRequiredService<TimeProvider>(),
            interlockingRuntime: sp.GetRequiredService<IInterlockingRuntime>()));
        services.TryAddSingleton<IRuntimeSnapshotProvider>(sp => sp.GetRequiredService<IMobaRuntime>());
        services.TryAddSingleton<IRecordingReplaySafetyGate, RecordingReplaySafetyGate>();
        services.TryAddSingleton<IRecordingReplayService, RecordingReplayService>();
        services.TryAddSingleton<IRecordingReplayStatusSource>(sp => sp.GetRequiredService<IRecordingReplayService>());
        services.TryAddSingleton<ILocomotiveFunctionCommandGateway, MobaRuntimeLocomotiveFunctionCommandGateway>();
        services.TryAddSingleton<ILocomotiveWhistleAutomationService, LocomotiveWhistleAutomationService>();

        return services;
    }
}
