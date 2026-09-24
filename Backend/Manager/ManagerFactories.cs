// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Backend.Manager;

using Common.Events;

using Domain;

using Interface;

using Microsoft.Extensions.Logging;

using Service;

public interface IJourneyManager : IDisposable
{
    event EventHandler<StationChangedEventArgs>? StationChanged;

    event EventHandler<JourneyFeedbackEventArgs>? FeedbackReceived;

    event EventHandler<JourneyCompletedEventArgs>? JourneyCompleted;

    JourneySessionState? GetState(Guid journeyId);

    void Reset(Journey journey);

    void CancelPendingWork();
}

public interface IPlatformManager : IDisposable
{
    event EventHandler<PlatformChangedEventArgs>? PlatformChanged;

    IReadOnlyDictionary<Guid, PlatformSessionState> States { get; }

    PlatformSessionState? GetState(Guid platformId);

    void ResetAll();
}

public sealed class JourneyManagerFactory(
    IZ21 z21,
    IWorkflowService workflowService,
    JourneyManagerDependencies dependencies,
    ILogger<JourneyManager>? logger)
{
    private readonly JourneyManagerDependencies _dependencies = dependencies ?? throw new ArgumentNullException(nameof(dependencies));

    public JourneyManagerFactory(
        IZ21 z21,
        IWorkflowService workflowService,
        IJourneyStopTransitionService? stopTransitionService = null,
        IJourneyRuntimeStateStore? runtimeStateStore = null,
        ILogger<JourneyManager>? logger = null,
        TimeProvider? timeProvider = null,
        IEventBus? eventBus = null)
        : this(z21, workflowService, new JourneyManagerDependencies
        {
            StopTransitionService = stopTransitionService,
            RuntimeStateStore = runtimeStateStore,
            TimeProvider = timeProvider,
            EventBus = eventBus
        }, logger)
    {
    }

    public IJourneyManager Create(Project project, ActionExecutionContext executionContext,
        InPortCounterService? counters = null) =>
        new JourneyManager(
            z21,
            project,
            workflowService,
            executionContext,
            logger,
            new JourneyManagerDependencies
            {
                StopTransitionService = _dependencies.StopTransitionService,
                RuntimeStateStore = _dependencies.RuntimeStateStore,
                TimeProvider = _dependencies.TimeProvider,
                EventBus = _dependencies.EventBus,
                ExecutionCoordinator = _dependencies.ExecutionCoordinator,
                InPortCounterService = counters ?? _dependencies.InPortCounterService
            });
}

public sealed class PlatformManagerFactory(
    IZ21 z21,
    IWorkflowService workflowService,
    ILogger<PlatformManager>? logger = null)
{
    public IPlatformManager Create(Project project, Station station, ActionExecutionContext? executionContext = null) =>
        new PlatformManager(z21, project, station, workflowService, executionContext, logger);
}
