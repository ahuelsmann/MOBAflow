// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
using Moba.Domain;
using Moba.Backend.Services;

using System.Diagnostics;

namespace Moba.Backend.Manager;

/// <summary>
/// Manages the execution of workflows and their actions related to a station based on feedback events (track feedback points).
/// </summary>
public class StationManager : BaseFeedbackManager<Station>
{
    private readonly SemaphoreSlim _processingLock = new(1, 1);
    private readonly WorkflowService _workflowService;

    /// <summary>
    /// Initializes a new instance of the StationManager class.
    /// </summary>
    /// <param name="z21">Z21 command station for receiving feedback events</param>
    /// <param name="stations">List of stations to manage</param>
    /// <param name="workflowService">Service for executing workflows</param>
    /// <param name="executionContext">Optional execution context; if null, a new context with Z21 will be created</param>
    public StationManager(
        Z21 z21, 
        List<Station> stations, 
        WorkflowService workflowService,
        ActionExecutionContext? executionContext = null)
    : base(z21, stations, executionContext)
    {
        _workflowService = workflowService;
    }

    protected override async Task ProcessFeedbackAsync(FeedbackResult feedback)
    {
        // Wait for lock (blocking) - queues feedbacks sequentially
        await _processingLock.WaitAsync();

        try
        {
            Debug.WriteLine($"📡 Feedback received: InPort {feedback.InPort}");

            foreach (var station in Entities)
            {
                if (GetInPort(station) == feedback.InPort)
                {
                    if (ShouldIgnoreFeedback(station))
                    {
                        Debug.WriteLine($"⏭ Feedback for station '{GetEntityName(station)}' ignored (timer active)");
                        continue;
                    }

                    UpdateLastFeedbackTime(GetInPort(station));
                    await HandleFeedbackAsync(station);
                }
            }
        }
        finally
        {
            _processingLock.Release();
        }
    }

    private async Task HandleFeedbackAsync(Station station)
    {
        Debug.WriteLine($"▶ Executing station workflow for '{station.Name}'");

        await _workflowService.ExecuteAsync(station.Flow, ExecutionContext);
        Debug.WriteLine($"✅ Station workflow '{station.Name}' completed");
    }

    protected override uint GetInPort(Station entity)
    {
        return entity.Flow?.InPort ?? throw new InvalidOperationException();
    }

    protected override bool IsUsingTimerToIgnoreFeedbacks(Station entity) => entity.Flow is { IsUsingTimerToIgnoreFeedbacks: true };

    protected override double GetIntervalForTimerToIgnoreFeedbacks(Station entity)
    {
        return entity.Flow?.IntervalForTimerToIgnoreFeedbacks ?? throw new InvalidOperationException();
    }

    protected override string GetEntityName(Station entity)
    {
        return entity.Flow?.Name ?? throw new InvalidOperationException();
    }
}