// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Backend.Manager;

using Common.Configuration;
using Common.Events;
using Common.Extension;

using Domain;

using Interface;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using Service;

/// <summary>
/// Groups optional journey runtime collaborators so the manager constructor stays focused on required dependencies.
/// </summary>
public sealed class JourneyManagerDependencies
{
    /// <summary>Gets the service that applies stop transitions.</summary>
    public IJourneyStopTransitionService? StopTransitionService { get; init; }

    /// <summary>Gets the store used to persist journey runtime checkpoints.</summary>
    public IJourneyRuntimeStateStore? RuntimeStateStore { get; init; }

    /// <summary>Gets the time source used by workflow coordination.</summary>
    public TimeProvider? TimeProvider { get; init; }

    /// <summary>Gets the event bus used to publish immutable journey runtime transitions.</summary>
    public IEventBus? EventBus { get; init; }

    /// <summary>Gets an optional pre-composed workflow execution coordinator.</summary>
    public IWorkflowExecutionCoordinator? ExecutionCoordinator { get; init; }

    /// <summary>Gets the application-owned input counter service.</summary>
    public InPortCounterService? InPortCounterService { get; init; }
}

/// <summary>
/// Evaluates the events of all active journeys against the InPort session counters and runs their workflows.
/// Platform-independent: No UI thread dispatching (that's handled by platform-specific ViewModels).
/// Uses SessionState to separate runtime state from domain objects.
/// </summary>
public partial class JourneyManager : IJourneyManager
{
    private readonly Lock _stateSync = new();
    private readonly ActionExecutionContextFactory _executionContextFactory;
    private readonly IWorkflowExecutionCoordinator _executionCoordinator;
    private readonly bool _ownsExecutionCoordinator;
    private readonly Dictionary<Guid, JourneySessionState> _states = [];
    private readonly Project _project;
    private Project _executionProject;
    private readonly ILogger<JourneyManager> _logger;
    private readonly IJourneyStopTransitionService _stopTransitionService;
    private readonly IJourneyRuntimeStateStore _runtimeStateStore;
    private readonly IEventBus? _eventBus;
    private readonly InPortCounterService _inPortCounterService;
    private readonly bool _ownsInPortCounterService;
    private bool _disposed;

    /// <summary>
    /// Event raised when a journey reaches a new station.
    /// ViewModels can subscribe to this event to update UI.
    /// </summary>
    public event EventHandler<StationChangedEventArgs>? StationChanged;

    /// <summary>
    /// Event raised when a journey event matched an InPort count or the journey state otherwise changed.
    /// </summary>
    public event EventHandler<JourneyFeedbackEventArgs>? FeedbackReceived;

    /// <summary>
    /// Raises the StationChanged event. Protected for testing purposes.
    /// </summary>
    protected virtual void OnStationChanged(StationChangedEventArgs e)
    {
        StationChanged?.Invoke(this, e);
    }

    /// <summary>
    /// Raises the FeedbackReceived event.
    /// </summary>
    protected virtual void OnFeedbackReceived(JourneyFeedbackEventArgs e)
    {
        FeedbackReceived?.Invoke(this, e);
    }

    /// <summary>
    /// Initializes a new instance of the JourneyManager class.
    /// </summary>
    /// <param name="z21">Z21 command station used by the default counter service and action context</param>
    /// <param name="project">Project containing journeys, stations, and workflows for reference resolution</param>
    /// <param name="workflowService">Service for executing workflows</param>
    /// <param name="executionContext">Optional execution context; if null, a new context with Z21 will be created</param>
    /// <param name="logger">Optional logger for structured diagnostics</param>
    /// <param name="dependencies">Optional collaborators</param>
    public JourneyManager(
        IZ21 z21,
        Project project,
        IWorkflowService workflowService,
        ActionExecutionContext? executionContext = null,
        ILogger<JourneyManager>? logger = null,
        JourneyManagerDependencies? dependencies = null)
    {
        dependencies ??= new JourneyManagerDependencies();
        _project = project;
        _executionProject = project;
        _logger = logger ?? NullLogger<JourneyManager>.Instance;
        _stopTransitionService = dependencies.StopTransitionService ?? new JourneyStopTransitionService();
        _runtimeStateStore = dependencies.RuntimeStateStore ?? new NullJourneyRuntimeStateStore();
        _eventBus = dependencies.EventBus;
        _ownsExecutionCoordinator = dependencies.ExecutionCoordinator is null;
        _ownsInPortCounterService = dependencies.InPortCounterService is null;
        _inPortCounterService = dependencies.InPortCounterService
            ?? new InPortCounterService(z21, new AppSettings { Counter = { UseTimerFilter = false } }, dependencies.TimeProvider);
        _executionContextFactory = new ActionExecutionContextFactory(executionContext ?? new ActionExecutionContext { Z21 = z21 });
        _executionCoordinator = dependencies.ExecutionCoordinator
            ?? new WorkflowExecutionCoordinator(workflowService, dependencies.TimeProvider ?? TimeProvider.System);

        foreach (var journey in project.Journeys)
        {
            var checkpoint = _runtimeStateStore.Load(project.Id, journey.Id);
            var checkpointPosition = journey.Stations.FindIndex(station => station.Id == checkpoint?.CurrentStationId);
            var position = checkpointPosition >= 0 ? checkpointPosition : 0;
            var station = journey.Stations.ElementAtOrDefault(position);
            _states[journey.Id] = new JourneySessionState
            {
                JourneyId = journey.Id,
                RunId = checkpointPosition < 0 || checkpoint!.JourneyRunId == Guid.Empty ? Guid.NewGuid() : checkpoint.JourneyRunId,
                CurrentPos = position,
                CurrentStationId = station?.Id,
                CurrentStationName = station?.Name ?? string.Empty,
                IsActive = journey.IsActive
            };
        }

        _inPortCounterService.SetJourneyFeedbackHandler(OnInPortCounted);
    }

    /// <inheritdoc />
    public void UpdateEvents(Project definitions, Guid journeyId)
    {
        ArgumentNullException.ThrowIfNull(definitions);
        lock (_stateSync)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            var definition = definitions.Journeys.Find(item => item.Id == journeyId);
            var journey = _project.Journeys.Find(item => item.Id == journeyId);
            if (definitions.Id != _project.Id || definition is null || journey is null
                || !_states.TryGetValue(journeyId, out var state)) return;

            journey.EventPlan = new JourneyEventPlan
            {
                Events = definition.EventPlan.Events.Select(item => new JourneyEvent
                {
                    Id = item.Id, InPort = item.InPort, Count = item.Count,
                    WorkflowId = item.WorkflowId, Enabled = item.Enabled
                }).ToList()
            };
            journey.IsActive = definition.IsActive;
            state.IsActive = definition.IsActive;
            // Definitions are isolated by the runtime. Accepted executions keep their previous snapshot.
            _executionProject = definitions;
        }
    }

    private void OnInPortCounted(object? sender, InPortCountedEventArgs args)
    {
        ProcessCountedFeedbackAsync(args).Observe(ex =>
            LogEventProcessingFailed(_logger, ex, args.Snapshot.InPort));
    }

    /// <summary>Starts the workflows of all enabled events of active journeys that match this InPort count.</summary>
    protected virtual async Task ProcessCountedFeedbackAsync(InPortCountedEventArgs args)
    {
        ArgumentNullException.ThrowIfNull(args);
        var queuedExecutions = new List<Task<WorkflowExecutionResult>>();
        var matches = new List<(Journey Journey, JourneySessionState State, JourneyEvent[] Events, JourneySessionState Snapshot, Project Definitions)>();
        lock (_stateSync)
        {
            // Counts queued before an explicit reset belong to the previous session.
            if (_disposed || args.Generation != _inPortCounterService.Generation)
            {
                return;
            }

            foreach (var journey in _project.Journeys)
            {
                if (_states.TryGetValue(journey.Id, out var state) && state.IsActive)
                {
                    var events = journey.EventPlan.Events.Where(item => item.Enabled
                        && item.InPort == args.Snapshot.InPort && item.Count == args.Snapshot.Count).ToArray();
                    if (events.Length == 0) continue;
                    state.LastFeedbackTime = args.Snapshot.LastFeedbackTime?.LocalDateTime;
                    matches.Add((journey, state, events, state.Snapshot(), _executionProject));
                }
            }
        }

        foreach (var match in matches)
        {
            PublishTransition(match.Journey, match.Snapshot, JourneyRuntimeTransitionKind.FeedbackAccepted, (int)args.Snapshot.InPort);
            OnFeedbackReceived(new JourneyFeedbackEventArgs
            {
                JourneyId = match.Journey.Id, SessionState = match.Snapshot, IsCounterFeedback = true
            });
            lock (_stateSync)
            {
                if (_disposed || match.State.ResetVersion != match.Snapshot.ResetVersion) continue;
                QueueMatchingEvents(match.Journey, match.State, match.Events, match.Definitions, args, queuedExecutions);
            }
        }

        if (queuedExecutions.Count > 0)
        {
            await Task.WhenAll(queuedExecutions).ConfigureAwait(false);
        }
    }

    private void QueueMatchingEvents(
        Journey journey,
        JourneySessionState state,
        JourneyEvent[] events,
        Project executionProject,
        InPortCountedEventArgs args,
        List<Task<WorkflowExecutionResult>> queuedExecutions)
    {
        foreach (var journeyEvent in events)
        {
            if (journeyEvent.WorkflowId is not Guid workflowId)
            {
                continue;
            }

            var workflow = executionProject.Workflows.FirstOrDefault(candidate => candidate.Id == workflowId);
            if (workflow is null)
            {
                LogWorkflowNotFound(_logger, workflowId, journey.Name);
                continue;
            }

            var inPort = journeyEvent.InPort;
            var resetVersion = state.ResetVersion;
            _inPortCounterService.QueueIfCurrent(args.Generation, () => queuedExecutions.Add(_executionCoordinator.EnqueueAsync(new QueuedWorkflowExecution
            {
                // Workflows of one journey share its stop state and run in order; other journeys stay independent.
                SourceKey = $"journey:{journey.Id}",
                OwnerId = journey.Id,
                WorkflowId = workflow.Id,
                SourceCorrelationId = args.CorrelationId,
                RequestFactory = () => new WorkflowExecutionRequest
                {
                    Project = executionProject,
                    Workflow = workflow,
                    Context = CreateWorkflowContext(executionProject, journey, state, inPort, resetVersion, journeyEvent.Id, args.CorrelationId),
                    Mode = WorkflowRunMode.Live,
                    SourceCorrelationId = args.CorrelationId
                }
            })));
        }
    }

    private ActionExecutionContext CreateWorkflowContext(Project executionProject, Journey journey, JourneySessionState state, uint inPort, long resetVersion, Guid eventId, Guid correlationId)
    {
        lock (_stateSync)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            var currentStation = GetCurrentStation(journey, state);
            if (state.ResetVersion != resetVersion) throw new OperationCanceledException("Journey was reset.");
            return _executionContextFactory.Create(new ActionExecutionContextState
            {
                CurrentProject = executionProject,
                CurrentJourney = journey,
                CurrentJourneySessionState = state,
                CurrentStation = currentStation,
                JourneyTemplateText = journey.Text,
                CurrentStationIndex = currentStation is null ? 1 : journey.Stations.IndexOf(currentStation) + 1,
                FeedbackInPort = inPort,
                SourceEvent = new FeedbackReceivedEvent((int)inPort, correlationId),
                SourceEventDefinitionId = eventId,
                ApplyJourneyStopTransition = transition => ApplyStopTransition(journey, state, transition, resetVersion)
            });
        }
    }

    private JourneyStopTransitionResult ApplyStopTransition(Journey journey, JourneySessionState state, JourneyStopTransition transition, long resetVersion)
    {
        JourneyStopTransitionResult result;
        JourneySessionState snapshot;
        lock (_stateSync)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (state.ResetVersion != resetVersion) throw new OperationCanceledException("Journey was reset.");
            result = _stopTransitionService.Apply(journey, state, transition);
            _runtimeStateStore.Save(_project.Id, state);
            snapshot = state.Snapshot();
        }
        if (result.Changed && result.CurrentStation is not null)
        {
            PublishTransition(journey, snapshot, JourneyRuntimeTransitionKind.StopChanged);
            OnStationChanged(new StationChangedEventArgs
            {
                JourneyId = journey.Id, Station = result.CurrentStation, SessionState = snapshot
            });
        }
        return result;
    }

    private static Station? GetCurrentStation(Journey journey, JourneySessionState state)
    {
        var currentStationIndex = state.CurrentStationId.HasValue
            ? journey.Stations.FindIndex(station => station.Id == state.CurrentStationId!.Value)
            : state.CurrentPos;
        if (currentStationIndex < 0 || currentStationIndex >= journey.Stations.Count)
        {
            return null;
        }

        return journey.Stations[currentStationIndex];
    }

    /// <summary>
    /// Resets a specific journey to its first stop.
    /// </summary>
    /// <param name="journey">The journey to reset</param>
    public void Reset(Journey journey)
    {
        ArgumentNullException.ThrowIfNull(journey);
        JourneySessionState snapshot;
        lock (_stateSync)
        {
            if (!_states.TryGetValue(journey.Id, out var state))
            {
                return;
            }

            var station = journey.Stations.FirstOrDefault();
            state.CurrentPos = 0;
            state.CurrentStationId = station?.Id;
            state.CurrentStationName = station?.Name ?? string.Empty;
            state.LastFeedbackTime = null;
            state.RunId = Guid.NewGuid();
            state.ResetVersion++;
            _executionCoordinator.CancelOwner(journey.Id);
            _runtimeStateStore.Reset(_project.Id, journey.Id);
            LogJourneyReset(_logger, journey.Name, state.CurrentPos);
            snapshot = state.Snapshot();
        }
        PublishTransition(journey, snapshot, JourneyRuntimeTransitionKind.Reset);
    }

    private void PublishTransition(
        Journey journey,
        JourneySessionState state,
        JourneyRuntimeTransitionKind kind,
        int? inPort = null)
    {
        if (_eventBus is null) return;

        var stationIndex = state.CurrentStationId is Guid stationId
            ? journey.Stations.FindIndex(station => station.Id == stationId)
            : state.CurrentPos;
        _eventBus.Publish(new JourneyRuntimeTransitionEvent(
            _project.Id,
            journey.Id,
            state.RunId,
            kind,
            inPort,
            state.CurrentStationId,
            stationIndex >= 0 && stationIndex < journey.Stations.Count ? stationIndex : -1,
            state.IsActive));
    }

    /// <summary>
    /// Gets the current session state for a specific journey.
    /// </summary>
    /// <param name="journeyId">The journey ID</param>
    /// <returns>The journey session state, or null if not found</returns>
    public JourneySessionState? GetState(Guid journeyId)
    {
        lock (_stateSync)
        {
            return _states.GetValueOrDefault(journeyId);
        }
    }

    /// <inheritdoc/>
    public void ResetAll()
    {
        foreach (var journey in _project.Journeys)
        {
            Reset(journey);
        }
    }

    /// <inheritdoc />
    public void CancelPendingWork()
    {
        _executionCoordinator.CancelPending();
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>Releases the subscriptions and managed resources owned by this manager.</summary>
    /// <param name="disposing">Whether managed resources should be released.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!disposing)
        {
            return;
        }

        lock (_stateSync)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _inPortCounterService.RemoveJourneyFeedbackHandler(OnInPortCounted);
            if (_ownsExecutionCoordinator)
            {
                _executionCoordinator.Dispose();
            }
            else
            {
                foreach (var journeyId in _states.Keys)
                {
                    _executionCoordinator.CancelOwner(journeyId);
                }
            }

            if (_ownsInPortCounterService)
            {
                _inPortCounterService.Dispose();
            }
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Journey event processing failed for InPort {InPort}")]
    private static partial void LogEventProcessingFailed(ILogger logger, Exception exception, uint inPort);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Workflow {WorkflowId} of journey '{Journey}' was not found")]
    private static partial void LogWorkflowNotFound(ILogger logger, Guid workflowId, string journey);

    [LoggerMessage(Level = LogLevel.Information, Message = "Journey '{Journey}' reset to position {Position}")]
    private static partial void LogJourneyReset(ILogger logger, string journey, int position);
}
