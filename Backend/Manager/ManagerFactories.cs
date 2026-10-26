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

    JourneySessionState? GetState(Guid journeyId);

    void Reset(Journey journey);

    /// <summary>
    /// Replaces the master-data snapshot. Journey progress is kept by id; new journeys start at their first stop,
    /// removed journeys are dropped and their pending workflows cancelled.
    /// </summary>
    void UpdateDefinitions(Project definitions);

    void CancelPendingWork();
}

public interface IPlatformManager : IDisposable
{
    event EventHandler<PlatformChangedEventArgs>? PlatformChanged;

    IReadOnlyDictionary<Guid, PlatformSessionState> States { get; }

    PlatformSessionState? GetState(Guid platformId);

    void ResetAll();
}

/// <summary>
/// Creates the journey manager of an activated project. Each manager receives its own workflow execution
/// coordinator and disposes it; the shared collaborators belong to the caller.
/// </summary>
public sealed class JourneyManagerFactory(
    IWorkflowService workflowService,
    IJourneyStopTransitionService stopTransitionService,
    IJourneyRuntimeStateStore runtimeStateStore,
    IEventBus eventBus,
    TimeProvider timeProvider,
    ILoggerFactory loggerFactory)
{
    private readonly IWorkflowService _workflowService = workflowService ?? throw new ArgumentNullException(nameof(workflowService));
    private readonly IJourneyStopTransitionService _stopTransitionService = stopTransitionService ?? throw new ArgumentNullException(nameof(stopTransitionService));
    private readonly IJourneyRuntimeStateStore _runtimeStateStore = runtimeStateStore ?? throw new ArgumentNullException(nameof(runtimeStateStore));
    private readonly IEventBus _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
    private readonly TimeProvider _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    private readonly ILoggerFactory _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));

    public IJourneyManager Create(Project project, ActionExecutionContext executionContext, InPortCounterService counters) =>
        new JourneyManager(
            project,
            executionContext,
            new JourneyManagerDependencies
            {
                StopTransitionService = _stopTransitionService,
                RuntimeStateStore = _runtimeStateStore,
                EventBus = _eventBus,
                ExecutionCoordinator = new WorkflowExecutionCoordinator(_workflowService, _timeProvider),
                InPortCounterService = counters
            },
            _loggerFactory.CreateLogger<JourneyManager>());
}

/// <summary>
/// Creates the platform manager of a station; each manager logs under the <see cref="PlatformManager"/> category.
/// </summary>
public sealed class PlatformManagerFactory(
    IZ21 z21,
    IWorkflowService workflowService,
    ILoggerFactory loggerFactory)
{
    private readonly ILoggerFactory _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));

    public IPlatformManager Create(Project project, Station station, ActionExecutionContext executionContext) =>
        new PlatformManager(z21, project, station, workflowService, executionContext, _loggerFactory.CreateLogger<PlatformManager>());
}
