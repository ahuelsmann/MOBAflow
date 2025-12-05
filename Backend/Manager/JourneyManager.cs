// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
using Moba.Backend.Interface;
using Moba.Backend.Services;
using Moba.Domain;
using Moba.Domain.Enum;

using System.Diagnostics;

namespace Moba.Backend.Manager;

/// <summary>
/// Manages the execution of workflows and their actions related to a journey or stop (station) based on feedback events (track feedback points).
/// Platform-independent: No UI thread dispatching (that's handled by platform-specific ViewModels).
/// Uses SessionState to separate runtime state from domain objects.
/// </summary>
public class JourneyManager : BaseFeedbackManager<Journey>
{
    private readonly SemaphoreSlim _processingLock = new(1, 1);
    private readonly WorkflowService _workflowService;
    private readonly Dictionary<Guid, JourneySessionState> _states = [];

    /// <summary>
    /// Event raised when a journey reaches a new station.
    /// ViewModels can subscribe to this event to update UI.
    /// </summary>
    public event EventHandler<StationChangedEventArgs>? StationChanged;

    /// <summary>
    /// Raises the StationChanged event. Protected for testing purposes.
    /// </summary>
    protected virtual void OnStationChanged(StationChangedEventArgs e)
    {
        StationChanged?.Invoke(this, e);
    }

    /// <summary>
    /// Initializes a new instance of the JourneyManager class.
    /// </summary>
    /// <param name="z21">Z21 command station for receiving feedback events</param>
    /// <param name="journeys">List of journeys to manage</param>
    /// <param name="workflowService">Service for executing workflows</param>
    /// <param name="executionContext">Optional execution context; if null, a new context with Z21 will be created</param>
    public JourneyManager(
        IZ21 z21, 
        List<Journey> journeys, 
        WorkflowService workflowService,
        ActionExecutionContext? executionContext = null)
    : base(z21, journeys, executionContext)
    {
        _workflowService = workflowService;
        
        // Initialize SessionState for all journeys
        foreach (var journey in journeys)
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
        // Wait for lock (blocking) - queues feedbacks sequentially
        await _processingLock.WaitAsync().ConfigureAwait(false);

        try
        {
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

                    UpdateLastFeedbackTime(GetInPort(journey));
                    await HandleFeedbackAsync(journey).ConfigureAwait(false);
                }
            }
        }
        finally
        {
            _processingLock.Release();
        }
    }

    private async Task HandleFeedbackAsync(Journey journey)
    {
        // Get state
        var state = _states[journey.Id];
        
        state.Counter++;
        state.LastFeedbackTime = DateTime.Now;
        Debug.WriteLine($"🔄 Journey '{journey.Name}': Round {state.Counter}, Position {state.CurrentPos}");

        if (state.CurrentPos >= journey.Stations.Count)
        {
            Debug.WriteLine($"⚠ CurrentPos out of Stations list bounds");
            return;
        }

        var currentStation = journey.Stations[state.CurrentPos];

        if (state.Counter >= currentStation.NumberOfLapsToStop)
        {
            Debug.WriteLine($"🚉 Station reached: {currentStation.Name}");
            
            // Update SessionState with current station
            state.CurrentStationName = currentStation.Name;
            
            // Fire StationChanged event (ViewModels will react)
            OnStationChanged(new StationChangedEventArgs
            {
                JourneyId = journey.Id,
                Station = currentStation,
                SessionState = state
            });

            // Execute station workflow if present
            if (currentStation.Flow != null)
            {
                // Set template context for announcements
                ExecutionContext.JourneyTemplateText = journey.Text;
                ExecutionContext.CurrentStation = currentStation;

                await _workflowService.ExecuteAsync(currentStation.Flow, ExecutionContext).ConfigureAwait(false);

                // Clear context after workflow execution
                ExecutionContext.JourneyTemplateText = null;
                ExecutionContext.CurrentStation = null;
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
                if (journey.NextJourney != null)
                {
                    // Get next journey's state
                    var nextState = _states[journey.NextJourney.Id];
                    Debug.WriteLine($"➡ Switching to journey: {journey.NextJourney.Name}");
                    nextState.CurrentPos = (int)journey.NextJourney.FirstPos;
                    Debug.WriteLine($"✅ Journey '{journey.NextJourney.Name}' activated at position {nextState.CurrentPos}");
                }
                else
                {
                    Debug.WriteLine($"⚠ NextJourney not set");
                }
                break;

            case BehaviorOnLastStop.None:
                Debug.WriteLine("⏹ Journey stops");
                state.IsActive = false;
                break;
        }

        await Task.CompletedTask;
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
            state.CurrentStationName = string.Empty;
            state.LastFeedbackTime = null;
            state.IsActive = true;
            Debug.WriteLine($"🔄 Journey '{journey.Name}' reset");
        }
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

    /// <summary>
    /// Gets the runtime state for a journey.
    /// Returns null if journey is not registered.
    /// </summary>
    /// <param name="journeyId">The journey ID</param>
    /// <returns>SessionState or null if not found</returns>
    public JourneySessionState? GetState(Guid journeyId)
    {
        return _states.GetValueOrDefault(journeyId);
    }

    protected override uint GetInPort(Journey entity) => entity.InPort;

    protected override bool IsUsingTimerToIgnoreFeedbacks(Journey entity) => entity.IsUsingTimerToIgnoreFeedbacks;

    protected override double GetIntervalForTimerToIgnoreFeedbacks(Journey entity) => entity.IntervalForTimerToIgnoreFeedbacks;

    protected override string GetEntityName(Journey entity) => entity.Name;

    protected override void CleanupResources()
    {
        _processingLock.Dispose();
    }
}