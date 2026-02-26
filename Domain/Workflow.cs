// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Domain;

using Enum;

/// <summary>
/// Workflow - Pure Data Object (POCO).
/// </summary>
public class Workflow
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Workflow"/> class with default values.
    /// </summary>
    public Workflow()
    {
        Id = Guid.NewGuid();
        Name = "New Flow";
        Description = string.Empty;
        Actions = [];
        ExecutionMode = WorkflowExecutionMode.Sequential;  // Default: Sequential
    }

    /// <summary>
    /// Gets or sets the unique identifier of the workflow.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the display name of the workflow.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets the description of the workflow.
    /// </summary>
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

    /// <summary>
    /// Gets or sets the hardware feedback input port used to trigger this workflow.
    /// </summary>
    public uint InPort { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether feedbacks are ignored for a certain time after triggering.
    /// </summary>
    public bool IsUsingTimerToIgnoreFeedbacks { get; set; }

    /// <summary>
    /// Gets or sets the interval in seconds for which feedbacks are ignored after triggering.
    /// </summary>
    public double IntervalForTimerToIgnoreFeedbacks { get; set; }
}