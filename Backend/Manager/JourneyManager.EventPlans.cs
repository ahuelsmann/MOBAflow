// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Backend.Manager;

using Common.Events;
using Common.Extension;
using Domain;
using Domain.Enum;
using Interface;
using Microsoft.Extensions.Logging;
using Service;
using System.Text.Json;

public partial class JourneyManager
{
    private readonly Dictionary<Guid, InPortCounterRun> _counterRuns = [];
    private readonly Dictionary<Guid, EventPlanRun> _eventPlanRuns = [];

    /// <summary>Starts an inactive journey using current definitions and atomically captured input bases.</summary>
    public Task StartJourneyAsync(Journey journey, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(journey);
        cancellationToken.ThrowIfCancellationRequested();
        lock (_stateSync)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (_states.TryGetValue(journey.Id, out var state) && state.IsActive)
            {
                return Task.CompletedTask;
            }

            StartJourneyCore(journey);
        }

        return Task.CompletedTask;
    }

    /// <summary>Stops scheduling and cancels queued and running workflows owned by this journey.</summary>
    public Task StopJourneyAsync(Journey journey, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(journey);
        cancellationToken.ThrowIfCancellationRequested();
        lock (_stateSync)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            StopJourneyCore(journey);
        }

        return Task.CompletedTask;
    }

    private void StartJourneyCore(Journey journey)
    {
        var definition = _project.Journeys.FirstOrDefault(candidate => candidate.Id == journey.Id)
            ?? throw new ArgumentException("The journey does not belong to this project.", nameof(journey));
        if (definition.EventPlan is null)
        {
            ResetCore(definition);
            return;
        }

        // Workflow and station definitions are captured together, including nested workflow references.
        // Editing the project therefore cannot change an already armed run.
        var projectSnapshot = JsonSerializer.Deserialize<Project>(JsonSerializer.SerializeToUtf8Bytes(_project))
            ?? throw new InvalidOperationException("The journey definitions could not be captured.");
        StartEventPlanRun(projectSnapshot, projectSnapshot.Journeys.Single(candidate => candidate.Id == journey.Id));
    }

    private void StartEventPlanRun(Project projectSnapshot, Journey journeySnapshot, int? initialPosition = null)
    {
        var events = journeySnapshot.EventPlan!.Events;
        if (events.Any(item => item.Id == Guid.Empty)
            || events.Select(item => item.Id).Distinct().Count() != events.Count
            || events.Any(item => item.Enabled && (item.InPort == 0 || item.InPort > int.MaxValue || item.Count == 0)))
        {
            throw new InvalidOperationException("Journey events require unique identifiers, a positive InPort and a positive count.");
        }

        if (events.Any(item => item.Enabled && item.WorkflowId.HasValue
            && projectSnapshot.Workflows.All(workflow => workflow.Id != item.WorkflowId.Value)))
        {
            throw new InvalidOperationException("A journey event references a workflow that does not exist.");
        }

        _executionCoordinator.CancelOwner(journeySnapshot.Id);
        var firstPosition = initialPosition ?? (int)journeySnapshot.FirstPos;
        var station = journeySnapshot.Stations.ElementAtOrDefault(firstPosition);
        var state = new JourneySessionState
        {
            JourneyId = journeySnapshot.Id,
            CurrentPos = firstPosition,
            CurrentStationId = station?.Id,
            CurrentStationName = station?.Name ?? string.Empty,
            IsActive = true
        };
        _states[journeySnapshot.Id] = state;
        RegisterCounterRun(journeySnapshot.Id, state);
        _eventPlanRuns[journeySnapshot.Id] = new EventPlanRun(projectSnapshot, journeySnapshot, state);
        _runtimeStateStore.Reset(_project.Id, journeySnapshot.Id);
        PublishTransition(journeySnapshot, state, JourneyRuntimeTransitionKind.Activated);
        OnFeedbackReceived(new JourneyFeedbackEventArgs { JourneyId = journeySnapshot.Id, SessionState = state });
    }

    private void StopJourneyCore(Journey journey)
    {
        if (!_states.TryGetValue(journey.Id, out var state))
        {
            return;
        }

        state.IsActive = false;
        state.IsJourneyCompletionRequested = false;
        _eventPlanRuns.Remove(journey.Id);
        _executionCoordinator.CancelOwner(journey.Id);
        ReleaseCounterRun(journey.Id);
        _runtimeStateStore.Save(_project.Id, state);
        PublishTransition(journey, state, JourneyRuntimeTransitionKind.Stopped);
        OnFeedbackReceived(new JourneyFeedbackEventArgs { JourneyId = journey.Id, SessionState = state });
    }

    private void RegisterCounterRun(Guid journeyId, JourneySessionState state)
    {
        _counterRuns.TryGetValue(journeyId, out var previous);
        var registration = _inPortCounterService.BeginJourneyRun();
        _counterRuns[journeyId] = registration;
        state.CurrentEventBases = registration.Bases;
        if (previous is not null)
        {
            _inPortCounterService.EndJourneyRun(previous.RegistrationId);
        }
    }

    private void ReleaseCounterRun(Guid journeyId)
    {
        if (_counterRuns.Remove(journeyId, out var registration))
        {
            _inPortCounterService.EndJourneyRun(registration.RegistrationId);
        }
    }

    private void OnInPortCounted(object? sender, InPortCountedEventArgs args)
    {
        ProcessCountedFeedbackAsync(args).Observe(ex =>
            _logger.LogWarning(ex, "Journey event processing failed for InPort {InPort}", args.Snapshot.InPort));
    }

    /// <summary>Evaluates each armed event against its own input's count since journey start.</summary>
    protected virtual async Task ProcessCountedFeedbackAsync(InPortCountedEventArgs args)
    {
        var queuedExecutions = new List<Task<WorkflowExecutionResult>>();
        lock (_stateSync)
        {
            if (_disposed)
            {
                return;
            }

            foreach (var run in _eventPlanRuns.Values.ToArray())
            {
                var baseCount = run.State.CurrentEventBases.GetValueOrDefault(args.Snapshot.InPort);
                if (!run.State.IsActive || args.Snapshot.Count <= baseCount
                    || _counterRuns.GetValueOrDefault(run.Journey.Id)?.Generation != args.Generation)
                {
                    continue;
                }

                var relativeCount = args.Snapshot.Count - baseCount;
                foreach (var journeyEvent in run.Journey.EventPlan!.Events)
                {
                    if (!IsCurrentRun(run) || !journeyEvent.Enabled || journeyEvent.InPort != args.Snapshot.InPort
                        || journeyEvent.Count != relativeCount || !run.CompletedEvents.Add(journeyEvent.Id))
                    {
                        continue;
                    }

                    run.State.LastFeedbackTime = args.Snapshot.LastFeedbackTime?.LocalDateTime;
                    run.State.CompletedEventIds = Array.AsReadOnly(run.CompletedEvents.ToArray());
                    PublishTransition(run.Journey, run.State, JourneyRuntimeTransitionKind.FeedbackAccepted,
                        inPort: checked((int)journeyEvent.InPort));
                    OnFeedbackReceived(new JourneyFeedbackEventArgs { JourneyId = run.Journey.Id, SessionState = run.State });

                    if (IsCurrentRun(run) && journeyEvent.WorkflowId is Guid workflowId)
                    {
                        queuedExecutions.Add(QueueEventWorkflow(run, journeyEvent, workflowId, args.CorrelationId));
                    }
                }
            }
        }

        if (queuedExecutions.Count > 0)
        {
            await Task.WhenAll(queuedExecutions).ConfigureAwait(false);
        }
    }

    private Task<WorkflowExecutionResult> QueueEventWorkflow(EventPlanRun run, JourneyEvent journeyEvent, Guid workflowId, Guid correlationId)
    {
        return _executionCoordinator.EnqueueAsync(new QueuedWorkflowExecution
        {
            // Every stop action for a journey shares its ordered state; other journeys remain independent.
            SourceKey = $"journey-event-plan:{run.State.RunId}",
            OwnerId = run.Journey.Id,
            ContextFactory = () => CreateEventWorkflowContext(run, journeyEvent.InPort),
            Request = new WorkflowExecutionRequest
            {
                Project = run.Project,
                Workflow = run.Project.Workflows.Single(workflow => workflow.Id == workflowId),
                Context = CreateEventWorkflowContext(run, journeyEvent.InPort),
                Mode = WorkflowRunMode.Live,
                SourceCorrelationId = correlationId
            }
        });
    }

    private ActionExecutionContext CreateEventWorkflowContext(EventPlanRun run, uint inPort)
    {
        lock (_stateSync)
        {
            if (!IsCurrentRun(run))
            {
                throw new OperationCanceledException("The journey run is no longer active.");
            }

            TryGetCurrentStation(run.Journey, run.State, out var currentStation);
            return _executionContextFactory.Create(new ActionExecutionContextState
            {
                CurrentProject = run.Project,
                CurrentJourney = run.Journey,
                CurrentJourneySessionState = run.State,
                CurrentStation = currentStation,
                JourneyTemplateText = run.Journey.Text,
                CurrentStationIndex = currentStation is null ? 1 : run.Journey.Stations.IndexOf(currentStation) + 1,
                FeedbackInPort = inPort,
                ApplyJourneyStopTransition = transition => ApplyEventPlanStopTransition(run, transition)
            });
        }
    }

    private JourneyStopTransitionResult ApplyEventPlanStopTransition(EventPlanRun run, JourneyStopTransition transition)
    {
        lock (_stateSync)
        {
            if (!IsCurrentRun(run))
            {
                throw new OperationCanceledException("The journey run is no longer active.");
            }

            var result = _stopTransitionService.Apply(run.Journey, run.State, transition);
            if (result.Changed && result.CurrentStation is not null)
            {
                PublishTransition(run.Journey, run.State, JourneyRuntimeTransitionKind.StopChanged);
                OnStationChanged(new StationChangedEventArgs
                {
                    JourneyId = run.Journey.Id,
                    Station = result.CurrentStation,
                    SessionState = run.State
                });
            }

            if (result.CompletionRequested)
            {
                CompleteEventPlanRun(run);
            }

            _runtimeStateStore.Save(_project.Id, run.State);
            return result;
        }
    }

    private void CompleteEventPlanRun(EventPlanRun run)
    {
        PublishTransition(run.Journey, run.State, JourneyRuntimeTransitionKind.Completed);
        JourneyCompleted?.Invoke(this, new JourneyCompletedEventArgs
        {
            JourneyId = run.Journey.Id,
            JourneyRunId = run.State.RunId
        });

        if (run.Journey.BehaviorOnLastStop == BehaviorOnLastStop.BeginAgainFromFistStop)
        {
            run.State.IsActive = false;
            run.State.IsJourneyCompletionRequested = false;
            StartEventPlanRun(run.Project, run.Journey, initialPosition: 0);
            PublishTransition(run.Journey, _states[run.Journey.Id], JourneyRuntimeTransitionKind.Restarted);
            return;
        }

        StopJourneyCore(run.Journey);
        if (run.Journey.BehaviorOnLastStop == BehaviorOnLastStop.GotoJourney && run.Journey.NextJourneyId is Guid nextJourneyId)
        {
            TryActivateNextJourney(nextJourneyId);
        }
    }

    private bool IsCurrentRun(EventPlanRun run) => !_disposed && run.State.IsActive
        && _eventPlanRuns.TryGetValue(run.Journey.Id, out var activeRun) && ReferenceEquals(run, activeRun);

    private sealed class EventPlanRun(Project project, Journey journey, JourneySessionState state)
    {
        public Project Project { get; } = project;

        public Journey Journey { get; } = journey;

        public JourneySessionState State { get; } = state;

        public HashSet<Guid> CompletedEvents { get; } = [];
    }
}
