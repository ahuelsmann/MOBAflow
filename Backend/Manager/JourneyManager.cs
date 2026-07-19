// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Backend.Manager;

using Common.Extension;
using Domain;
using Domain.Enum;

using Interface;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using Service;

using System.Diagnostics.CodeAnalysis;

/// <summary>
/// Manages the execution of workflows and their actions related to a journey or stop (station) based on feedback events (track feedback points).
/// Platform-independent: No UI thread dispatching (that's handled by platform-specific ViewModels).
/// Uses SessionState to separate runtime state from domain objects.
/// </summary>
public class JourneyManager : IJourneyManager
{
    private readonly SemaphoreSlim _processingLock = new(1, 1);
    private readonly IZ21 _z21;
    private readonly ActionExecutionContextFactory _executionContextFactory;
    private readonly IWorkflowService _workflowService;
    private readonly Dictionary<Guid, JourneySessionState> _states = [];
    private readonly Project _project;
    private readonly ILogger<JourneyManager> _logger;
    private readonly IJourneyStopTransitionService _stopTransitionService;
    private readonly IJourneyRuntimeStateStore _runtimeStateStore;
    private bool _disposed;

    /// <summary>
    /// Event raised when a journey reaches a new station.
    /// ViewModels can subscribe to this event to update UI.
    /// </summary>
    public event EventHandler<StationChangedEventArgs>? StationChanged;

    /// <summary>
    /// Event raised when a journey receives a feedback (counter incremented).
    /// Fired on every feedback, not just when a station is reached.
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
    /// <param name="z21">Z21 command station for receiving feedback events</param>
    /// <param name="project">Project containing journeys, stations, and workflows for reference resolution</param>
    /// <param name="workflowService">Service for executing workflows</param>
    /// <param name="executionContext">Optional execution context; if null, a new context with Z21 will be created</param>
    /// <param name="logger">Optional logger for structured diagnostics</param>
    public JourneyManager(
        IZ21 z21,
        Project project,
        IWorkflowService workflowService,
        ActionExecutionContext? executionContext = null,
        ILogger<JourneyManager>? logger = null,
        IJourneyStopTransitionService? stopTransitionService = null,
        IJourneyRuntimeStateStore? runtimeStateStore = null)
    {
        _z21 = z21;
        _project = project;
        _workflowService = workflowService;
        _logger = logger ?? NullLogger<JourneyManager>.Instance;
        _stopTransitionService = stopTransitionService ?? new JourneyStopTransitionService();
        _runtimeStateStore = runtimeStateStore ?? new NullJourneyRuntimeStateStore();
        _executionContextFactory = new ActionExecutionContextFactory(executionContext ?? new ActionExecutionContext { Z21 = z21 });
        _z21.Received += OnZ21FeedbackReceived;

        // Initialize SessionState for all journeys
        foreach (var journey in project.Journeys)
        {
            _states[journey.Id] = new JourneySessionState
            {
                JourneyId = journey.Id,
                CurrentPos = (int)journey.FirstPos,
                CurrentStationId = journey.Stations.ElementAtOrDefault((int)journey.FirstPos)?.Id,
                CurrentStationName = journey.Stations.ElementAtOrDefault((int)journey.FirstPos)?.Name ?? string.Empty,
                IsActive = true
            };
            var checkpoint = _runtimeStateStore.Load(project.Id, journey.Id);
            if (checkpoint != null)
            {
                _states[journey.Id].CurrentFeedbackIndex = Math.Clamp(checkpoint.CurrentFeedbackIndex, 0, journey.FeedbackSequence.Count);
                _states[journey.Id].CurrentStepOccurrence = checkpoint.CurrentStepOccurrence;
            }
        }
    }

    /// <inheritdoc/>
    private void OnZ21FeedbackReceived(FeedbackResult feedback)
    {
        ProcessFeedbackAsync(feedback).Observe(ex => _logger.LogWarning(ex, "Journey feedback processing failed for InPort {InPort}", feedback.InPort));
    }

    protected virtual async Task ProcessFeedbackAsync(FeedbackResult feedback)
    {
        if (_disposed)
        {
            _logger.LogWarning("JourneyManager already disposed - ignoring feedback");
            return;
        }

        try
        {
            await _processingLock.WaitAsync().ConfigureAwait(false);

            try
            {
                if (_disposed)
                {
                    _logger.LogWarning("JourneyManager disposed during lock acquisition");
                    return;
                }

                _logger.LogInformation("Feedback received: InPort {InPort}", feedback.InPort);

                foreach (var journey in _project.Journeys)
                {
                    if (!_states.TryGetValue(journey.Id, out var state) || !state.IsActive)
                    {
                        continue;
                    }

                    var expectedStep = GetExpectedStep(journey, state);
                    if (expectedStep == null || expectedStep.InPort != feedback.InPort)
                    {
                        continue;
                    }

                    await HandleFeedbackAsync(journey, expectedStep).ConfigureAwait(false);
                }
            }
            finally
            {
                if (!_disposed)
                {
                    _processingLock.Release();
                }
            }
        }
        catch (ObjectDisposedException)
        {
            _logger.LogWarning("JourneyManager SemaphoreSlim disposed during feedback processing");
        }
    }

    private async Task HandleFeedbackAsync(Journey journey, JourneyFeedbackStep feedbackStep)
    {
        var state = _states[journey.Id];

        state.CurrentStepOccurrence++;
        _runtimeStateStore.Save(_project.Id, state);
        state.LastFeedbackTime = DateTime.Now;
        _logger.LogInformation(
            "Journey '{Journey}': Feedback step {FeedbackIndex} at InPort {InPort}",
            journey.Name,
            state.CurrentFeedbackIndex,
            feedbackStep.InPort);

        // Fire FeedbackReceived event on every feedback (for UI counter updates)
        OnFeedbackReceived(new JourneyFeedbackEventArgs
        {
            JourneyId = journey.Id,
            SessionState = state
        });

        if (state.CurrentStepOccurrence < Math.Max(feedbackStep.RepeatCount, 1u))
        {
            return;
        }

        ApplyStopTransition(journey, feedbackStep, state);

        if (feedbackStep.DelayMs > 0)
        {
            await Task.Delay(feedbackStep.DelayMs).ConfigureAwait(false);
        }

        if (feedbackStep.WorkflowId.HasValue)
        {
            await ExecuteFeedbackWorkflowAsync(journey, feedbackStep).ConfigureAwait(false);
        }

        state.CurrentFeedbackIndex++;
        state.CurrentStepOccurrence = 0;
        _runtimeStateStore.Save(_project.Id, state);

        if (state.IsJourneyCompletionRequested)
        {
            state.IsJourneyCompletionRequested = false;
            await HandleLastStationAsync(journey).ConfigureAwait(false);
        }

        OnFeedbackReceived(new JourneyFeedbackEventArgs { JourneyId = journey.Id, SessionState = state });
    }

    private JourneyFeedbackStep? GetExpectedStep(Journey journey, JourneySessionState state)
    {
        while (state.CurrentFeedbackIndex < journey.FeedbackSequence.Count)
        {
            var step = journey.FeedbackSequence[state.CurrentFeedbackIndex];
            if (step.Enabled)
            {
                return ConditionsMatch(step, state) ? step : null;
            }

            state.CurrentFeedbackIndex++;
            state.CurrentStepOccurrence = 0;
        }

        return null;
    }

    private static bool ConditionsMatch(JourneyFeedbackStep step, JourneySessionState state) =>
        step.Conditions.All(condition => condition.Type switch
        {
            JourneyFeedbackConditionType.CurrentStationIs => condition.StationId == state.CurrentStationId,
            _ => false
        });

    private void ApplyStopTransition(Journey journey, JourneyFeedbackStep step, JourneySessionState state)
    {
        var result = _stopTransitionService.Apply(journey, state, step.StopTransition);
        if (result.Changed && result.CurrentStation != null)
        {
            OnStationChanged(new StationChangedEventArgs
            {
                JourneyId = journey.Id,
                Station = result.CurrentStation,
                SessionState = state
            });
        }
    }

    private async Task HandleLastStationAsync(Journey journey)
    {
        var state = _states[journey.Id];

        _logger.LogInformation("Last station of journey '{Journey}' reached", journey.Name);

        switch (journey.BehaviorOnLastStop)
        {
            case BehaviorOnLastStop.BeginAgainFromFistStop:
                _logger.LogInformation("Journey will restart from beginning");
                state.CurrentPos = 0;
                state.CurrentStationId = journey.Stations.FirstOrDefault()?.Id;
                state.CurrentStationName = journey.Stations.FirstOrDefault()?.Name ?? string.Empty;
                break;

            case BehaviorOnLastStop.GotoJourney:
                if (journey.NextJourneyId.HasValue)
                {
                    TryActivateNextJourney(journey.NextJourneyId.Value);
                }
                else
                {
                    _logger.LogWarning("NextJourneyId not set for journey '{Journey}'", journey.Name);
                }
                break;

            case BehaviorOnLastStop.None:
                _logger.LogInformation("Journey stops");
                state.IsActive = false;
                break;
        }

        await Task.CompletedTask.ConfigureAwait(false);
    }

    private bool TryGetCurrentStation(
        Journey journey,
        JourneySessionState state,
        [NotNullWhen(true)] out Station? currentStation)
    {
        if (journey.Stations.Count == 0)
        {
            _logger.LogWarning("Journey '{Journey}' has no stations configured", journey.Name);
            currentStation = null;
            return false;
        }

        var currentStationIndex = state.CurrentStationId.HasValue
            ? journey.Stations.FindIndex(station => station.Id == state.CurrentStationId!.Value)
            : state.CurrentPos;
        if (currentStationIndex < 0 || currentStationIndex >= journey.Stations.Count)
        {
            _logger.LogWarning(
                "CurrentPos {CurrentPos} is out of range for journey '{Journey}' (station count: {Count})",
                currentStationIndex,
                journey.Name,
                journey.Stations.Count);
            currentStation = null;
            return false;
        }

        currentStation = journey.Stations[currentStationIndex];
        return true;
    }

    private async Task ExecuteFeedbackWorkflowAsync(Journey journey, JourneyFeedbackStep feedbackStep)
    {
        var workflowId = feedbackStep.WorkflowId ?? throw new InvalidOperationException("A feedback workflow requires an identifier.");
        var workflow = _project.Workflows.FirstOrDefault(w => w.Id == workflowId);
        if (workflow == null)
        {
            _logger.LogWarning("Workflow with ID {WorkflowId} not found", workflowId);
            return;
        }

        TryGetCurrentStation(journey, _states[journey.Id], out var currentStation);

        var stationIndex = currentStation == null ? 0 : journey.Stations.IndexOf(currentStation) + 1;
        var executionContext = _executionContextFactory.Create(new ActionExecutionContextState
        {
            CurrentProject = _project,
            CurrentJourney = journey,
            CurrentJourneySessionState = _states[journey.Id],
            CurrentStation = currentStation,
            JourneyTemplateText = journey.Text,
            CurrentStationIndex = stationIndex > 0 ? stationIndex : 1
        });

        await _workflowService.ExecuteAsync(workflow, executionContext).ConfigureAwait(false);
    }

    private void TryActivateNextJourney(Guid nextJourneyId)
    {
        var nextJourney = _project.Journeys.FirstOrDefault(j => j.Id == nextJourneyId);
        if (nextJourney == null || !_states.TryGetValue(nextJourney.Id, out var nextState))
        {
            _logger.LogWarning("NextJourney with ID {NextJourneyId} not found or state missing", nextJourneyId);
            return;
        }

        _logger.LogInformation("Switching to journey: {Journey}", nextJourney.Name);
        nextState.CurrentPos = (int)nextJourney.FirstPos;
        nextState.CurrentStationId = nextJourney.Stations.ElementAtOrDefault((int)nextJourney.FirstPos)?.Id;
        nextState.CurrentStationName = nextJourney.Stations.ElementAtOrDefault((int)nextJourney.FirstPos)?.Name ?? string.Empty;
        nextState.CurrentFeedbackIndex = 0;
        nextState.IsActive = true;
        _logger.LogInformation("Journey '{Journey}' activated at position {Position}", nextJourney.Name, nextState.CurrentPos);
    }

    /// <summary>
    /// Resets a specific journey to its initial state.
    /// </summary>
    /// <param name="journey">The journey to reset</param>
    public void Reset(Journey journey)
    {
        if (_states.TryGetValue(journey.Id, out var state))
        {
            state.CurrentPos = (int)journey.FirstPos;
            state.CurrentStationId = journey.Stations.ElementAtOrDefault((int)journey.FirstPos)?.Id;
            state.CurrentStationName = journey.Stations.ElementAtOrDefault((int)journey.FirstPos)?.Name ?? string.Empty;
            state.CurrentFeedbackIndex = 0;
            state.CurrentStepOccurrence = 0;
            state.IsJourneyCompletionRequested = false;
            state.IsActive = true;
            _runtimeStateStore.Reset(_project.Id, journey.Id);
            _logger.LogInformation("Journey '{Journey}' reset to position {Position}", journey.Name, state.CurrentPos);
        }
    }

    /// <summary>
    /// Gets the current session state for a specific journey.
    /// </summary>
    /// <param name="journeyId">The journey ID</param>
    /// <returns>The journey session state, or null if not found</returns>
    public JourneySessionState? GetState(Guid journeyId)
    {
        return _states.GetValueOrDefault(journeyId);
    }

    /// <inheritdoc/>
    public void ResetAll()
    {
        foreach (var journey in _project.Journeys)
        {
            Reset(journey);
        }
        _logger.LogInformation("All journeys reset");
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _z21.Received -= OnZ21FeedbackReceived;
        _processingLock.Dispose();
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
