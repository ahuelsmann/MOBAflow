// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Domain;

using Enum;

/// <summary>
/// Workflow - Pure Data Object (POCO).
/// </summary>
public class Workflow
{
    public Workflow()
    {
        Id = Guid.NewGuid();
        Name = "New Flow";
        Description = string.Empty;
        Actions = [];
        ExecutionMode = WorkflowExecutionMode.Sequential;  // Default: Sequential
    }

    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }

    /// <summary>
    /// Actions as data objects (execution moved to ActionExecutor)
    /// </summary>
    public List<WorkflowAction> Actions { get; set; }

    /// <summary>
    /// Execution mode: Sequential (wait for each action) or Parallel (fire all at once).
    /// Sequential is default and respects DelayAfterMs on actions.
    /// Parallel starts all actions simultaneously (overlapping execution).
    /// </summary>
    public WorkflowExecutionMode ExecutionMode { get; set; }

    public uint InPort { get; set; }
    public bool IsUsingTimerToIgnoreFeedbacks { get; set; }
    public double IntervalForTimerToIgnoreFeedbacks { get; set; }
}