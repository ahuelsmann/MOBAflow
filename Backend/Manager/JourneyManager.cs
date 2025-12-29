// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Backend.Manager;

using Domain;
using Domain.Enum;
using Interface;
using Service;
using System.Diagnostics;

/// <summary>
/// Manages the execution of workflows and their actions related to a journey or stop (station) based on feedback events (track feedback points).
/// Platform-independent: No UI thread dispatching (that's handled by platform-specific ViewModels).
/// Uses SessionState to separate runtime state from domain objects.
/// </summary>
public class JourneyManager : BaseFeedbackManager<Journey>, IJourneyManager
{
    private readonly SemaphoreSlim _processingLock = new(1, 1);
    private readonly WorkflowService _workflowService;
    private readonly Dictionary<Guid, JourneySessionState> _states = [];
    private readonly Project _project;

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
    public JourneyManager(
        IZ21 z21,
        Project project,
        WorkflowService workflowService,
        ActionExecutionContext? executionContext = null)
    : base(z21, project.Journeys, executionContext)
    {
        _project = project;
        _workflowService = workflowService;

        // Initialize SessionState for all journeys
        foreach (var journey in project.Journeys)
        {
            _states[journey.Id] = new JourneySessionState
            {
                JourneyId = journey.Id,
                CurrentPos = (int)journey.FirstPos,
                Counter = 0,
                IsActive = true
            };
        }
    }

    protected override async Task ProcessFeedbackAsync(FeedbackResult feedback)
    {
        // ✅ Check if disposed before accessing SemaphoreSlim
        if (Disposed)
        {
            Debug.WriteLine("⚠️ JourneyManager already disposed - ignoring feedback");
            return;
        }

        try
        {
            // Wait for lock (blocking) - queues feedbacks sequentially
            await _processingLock.WaitAsync().ConfigureAwait(false);

            try
            {
                // ✅ Double-check after acquiring lock (disposal might have happened while waiting)
                if (Disposed)
                {
                    Debug.WriteLine("⚠️ JourneyManager disposed during lock acquisition");
                    return;
                }

                Debug.WriteLine($"📡 Feedback received: InPort {feedback.InPort}");

                foreach (var journey in Entities)
                {
                    if (GetInPort(journey) == feedback.InPort)
                    {
                        if (ShouldIgnoreFeedback(journey))
                        {
                            Debug.WriteLine($"⏭ Feedback for journey '{GetEntityName(journey)}' ignored (timer active)");
                            continue;
                        }

                        // Process feedback FIRST, THEN update timer
                        // This ensures the first feedback increments the counter,
                        // and subsequent feedbacks are blocked by the timer
                        await HandleFeedbackAsync(journey).ConfigureAwait(false);
                        UpdateLastFeedbackTime(GetInPort(journey));
                    }
                }
            }
            finally
            {
                // ✅ Only release if not disposed
                if (!Disposed)
                {
                    _processingLock.Release();
                }
            }
        }
        catch (ObjectDisposedException)
        {
            // SemaphoreSlim was disposed while we were waiting - this is expected during shutdown
            Debug.WriteLine("⚠️ JourneyManager SemaphoreSlim disposed during feedback processing");
        }
    }

    private async Task HandleFeedbackAsync(Journey journey)
    {
        // Get state
        var state = _states[journey.Id];

        state.Counter++;
        state.LastFeedbackTime = DateTime.Now;
        Debug.WriteLine($"🔄 Journey '{journey.Name}': Round {state.Counter}, Position {state.CurrentPos}");

        // Fire FeedbackReceived event on every feedback (for UI counter updates)
        OnFeedbackReceived(new JourneyFeedbackEventArgs
        {
            JourneyId = journey.Id,
            SessionState = state
        });

        // Get current Station directly from Journey
        if (state.CurrentPos >= journey.Stations.Count)
        {
            Debug.WriteLine("⚠ CurrentPos out of Stations list bounds");
            return;
        }

        var currentStation = journey.Stations[state.CurrentPos];

        if (state.Counter >= currentStation.NumberOfLapsToStop)
        {
            Debug.WriteLine($"🚉 Station reached: {currentStation.Name}");

            // Update SessionState with current station
            state.CurrentStationName = currentStation.Name;

            // ✅ Fire StationChanged event FIRST (UI updates immediately)
            OnStationChanged(new StationChangedEventArgs
            {
                JourneyId = journey.Id,
                Station = currentStation,
                SessionState = state
            });

            // ✅ THEN execute station workflow (async announcements run after UI is updated)
            if (currentStation.WorkflowId.HasValue)
            {
                var workflow = _project.Workflows.FirstOrDefault(w => w.Id == currentStation.WorkflowId.Value);
                if (workflow != null && ExecutionContext != null)
                {
                    // Set template context for actions (e.g., Announcement action)
                    ExecutionContext.JourneyTemplateText = journey.Text;
                    ExecutionContext.CurrentStation = currentStation;

                    await _workflowService.ExecuteAsync(workflow, ExecutionContext).ConfigureAwait(false);

                    // Clear context after workflow execution
                    ExecutionContext.JourneyTemplateText = null;
                    ExecutionContext.CurrentStation = null;
                }
                else if (workflow == null)
                {
                    Debug.WriteLine($"⚠ Workflow with ID {currentStation.WorkflowId.Value} not found");
                }
            }

            state.Counter = 0;

            bool isLastStation = state.CurrentPos == journey.Stations.Count - 1;

            if (isLastStation)
            {
                await HandleLastStationAsync(journey).ConfigureAwait(false);
            }
            else
            {
                state.CurrentPos++;
            }
        }
    }

    private async Task HandleLastStationAsync(Journey journey)
    {
        // Get state
        var state = _states[journey.Id];

        Debug.WriteLine($"🏁 Last station of journey '{journey.Name}' reached");

        switch (journey.BehaviorOnLastStop)
        {
            case BehaviorOnLastStop.BeginAgainFromFistStop:
                Debug.WriteLine("🔄 Journey will restart from beginning");
                state.CurrentPos = 0;
                break;

            case BehaviorOnLastStop.GotoJourney:
                if (journey.NextJourneyId.HasValue)
                {
                    var nextJourney = _project.Journeys.FirstOrDefault(j => j.Id == journey.NextJourneyId.Value);
                    if (nextJourney != null && _states.TryGetValue(nextJourney.Id, out var nextState))
                    {
                        Debug.WriteLine($"➡ Switching to journey: {nextJourney.Name}");
                        nextState.CurrentPos = (int)nextJourney.FirstPos;
                        Debug.WriteLine($"✅ Journey '{nextJourney.Name}' activated at position {nextState.CurrentPos}");
                    }
                    else
                    {
                        Debug.WriteLine($"⚠ NextJourney with ID {journey.NextJourneyId.Value} not found or state missing");
                    }
                }
                else
                {
                    Debug.WriteLine("⚠ NextJourneyId not set");
                }
                break;

            case BehaviorOnLastStop.None:
                Debug.WriteLine("⏹ Journey stops");
                state.IsActive = false;
                break;
        }

        await Task.CompletedTask.ConfigureAwait(false);
    }

    /// <summary>
    /// Resets a specific journey to its initial state.
    /// </summary>
    /// <param name="journey">The journey to reset</param>
    public void Reset(Journey journey)
    {
        if (_states.TryGetValue(journey.Id, out var state))
        {
            state.Counter = 0;
            state.CurrentPos = (int)journey.FirstPos;
            state.IsActive = true;
            Debug.WriteLine($"🔄 Journey '{journey.Name}' reset to position {state.CurrentPos}");
        }
    }

    /// <summary>
    /// Gets the current session state for a specific journey.
    /// </summary>
    /// <param name="journeyId">The journey ID</param>
    /// <returns>The journey session state, or null if not found</returns>
    public JourneySessionState? GetState(Guid journeyId)
    {
        return _states.TryGetValue(journeyId, out var state) ? state : null;
    }

    public override void ResetAll()
    {
        foreach (var journey in Entities)
        {
            Reset(journey);
        }
        base.ResetAll();
        Debug.WriteLine("🔄 All journeys reset");
    }

    protected override uint GetInPort(Journey entity) => entity.InPort;

    protected override bool IsUsingTimerToIgnoreFeedbacks(Journey entity) => entity.IsUsingTimerToIgnoreFeedbacks;

    protected override double GetIntervalForTimerToIgnoreFeedbacks(Journey entity) => entity.IntervalForTimerToIgnoreFeedbacks;

    protected override string GetEntityName(Journey entity) => entity.Name;

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _processingLock.Dispose();
        }
        base.Dispose(disposing);
    }
}
