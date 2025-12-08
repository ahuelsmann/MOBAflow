// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
using Moba.Backend.Services;
using Moba.Domain;

using System.Diagnostics;

namespace Moba.Backend.Manager;

/// <summary>
/// OBSOLETE: StationManager is deprecated after JourneyStation refactoring.
/// WorkflowId is now Journey-specific (in JourneyStation), not Station-specific.
/// Use JourneyManager for workflow execution on stations.
/// This class is kept for backward compatibility but does nothing.
/// </summary>
[Obsolete("StationManager is obsolete. Use JourneyManager for station-related workflows. WorkflowId is now in JourneyStation.")]
public class StationManager : BaseFeedbackManager<Station>
{
    private readonly SemaphoreSlim _processingLock = new(1, 1);
    private readonly WorkflowService _workflowService;
    private readonly Project _project;

    /// <summary>
    /// Initializes a new instance of the StationManager class.
    /// NOTE: This manager is obsolete after JourneyStation refactoring.
    /// </summary>
    public StationManager(
        Z21 z21, 
        List<Station> stations, 
        WorkflowService workflowService,
        Project project,
        ActionExecutionContext? executionContext = null)
    : base(z21, stations, executionContext)
    {
        _workflowService = workflowService;
        _project = project;
    }

    protected override async Task ProcessFeedbackAsync(FeedbackResult feedback)
    {
        // OBSOLETE: Do nothing - workflows are now executed via JourneyManager
        await Task.CompletedTask;
        Debug.WriteLine($"⚠ StationManager is obsolete. Use JourneyManager instead.");
    }

    protected override uint GetInPort(Station entity)
    {
        return entity.InPort;
    }

    protected override bool IsUsingTimerToIgnoreFeedbacks(Station entity)
    {
        return false; // Obsolete
    }

    protected override double GetIntervalForTimerToIgnoreFeedbacks(Station entity)
    {
        return 0; // Obsolete
    }

    protected override string GetEntityName(Station entity)
    {
        return entity.Name;
    }
}