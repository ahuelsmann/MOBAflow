// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Domain;

/// <summary>
/// Workflow - Pure Data Object (POCO).
/// NO BUSINESS LOGIC! StartAsync moved to WorkflowService in Backend.
/// </summary>
public class Workflow
{
    public Workflow()
    {
        Id = Guid.NewGuid();
        Name = "New Flow";
        Description = string.Empty;
        Actions = [];
    }

    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    
    /// <summary>
    /// Actions as data objects (execution moved to ActionExecutor)
    /// </summary>
    public List<WorkflowAction> Actions { get; set; }
    
    public uint InPort { get; set; } = 1;
    public bool IsUsingTimerToIgnoreFeedbacks { get; set; }
    public double IntervalForTimerToIgnoreFeedbacks { get; set; }
}