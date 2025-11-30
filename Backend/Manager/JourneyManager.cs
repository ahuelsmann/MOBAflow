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
/// </summary>
public class JourneyManager : BaseFeedbackManager<Journey>
{
    private readonly SemaphoreSlim _processingLock = new(1, 1);
    private readonly WorkflowService _workflowService;

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
        journey.CurrentCounter++;
        Debug.WriteLine($"🔄 Journey '{journey.Name}': Round {journey.CurrentCounter}, Position {journey.CurrentPos}");

        if (journey.CurrentPos >= journey.Stations.Count)
        {
            Debug.WriteLine($"⚠ CurrentPos out of Stations list bounds");
            return;
        }

        var currentStation = journey.Stations[(int)journey.CurrentPos];

        if (journey.CurrentCounter >= currentStation.NumberOfLapsToStop)
        {
            Debug.WriteLine($"🚉 Station reached: {currentStation.Name}");

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

            journey.CurrentCounter = 0;

            bool isLastStation = journey.CurrentPos == journey.Stations.Count - 1;

            if (isLastStation)
            {
                await HandleLastStationAsync(journey).ConfigureAwait(false);
            }
            else
            {
                journey.CurrentPos++;
            }
        }
    }

    private async Task HandleLastStationAsync(Journey journey)
    {
        Debug.WriteLine($"🏁 Last station of journey '{journey.Name}' reached");

        switch (journey.OnLastStop)
        {
            case BehaviorOnLastStop.BeginAgainFromFistStop:
                Debug.WriteLine("🔄 Journey will restart from beginning");
                journey.CurrentPos = 0;
                break;

            case BehaviorOnLastStop.GotoJourney:
                Debug.WriteLine($"➡ Switching to journey: {journey.NextJourney}");
                var nextJourney = Entities.FirstOrDefault(j => j.Name == journey.NextJourney);
                if (nextJourney != null)
                {
                    nextJourney.CurrentPos = nextJourney.FirstPos;
                    Debug.WriteLine($"✅ Journey '{nextJourney.Name}' activated at position {nextJourney.FirstPos}");
                }
                else
                {
                    Debug.WriteLine($"⚠ Journey '{journey.NextJourney}' not found");
                }
                break;

            case BehaviorOnLastStop.None:
                Debug.WriteLine("⏹ Journey stops");
                break;
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// Resets a specific journey to its initial state.
    /// </summary>
    /// <param name="journey">The journey to reset</param>
    public static void Reset(Journey journey)
    {
        journey.CurrentCounter = 0;
        journey.CurrentPos = journey.FirstPos;
        Debug.WriteLine($"🔄 Journey '{journey.Name}' reset");
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

    protected override void CleanupResources()
    {
        _processingLock.Dispose();
    }
}