// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Domain;

/// <summary>
/// Platform - Pure Data Object (POCO).
/// </summary>
public class Platform
{
    public Platform()
    {
        Name = "New Platform";
    }

    public string Name { get; set; }
    public string? Description { get; set; }
    public string? Track { get; set; }
    public int? FeedbackInPort { get; set; }
    public bool IsUsingTimerToIgnoreFeedbacks { get; set; }
    public double IntervalForTimerToIgnoreFeedbacks { get; set; }
    public uint InPort { get; set; }

    /// <summary>
    /// Reference to Workflow (by reference, resolved by WorkflowService).
    /// NOTE: This is a navigation property that gets resolved after deserialization.
    /// </summary>
    public Workflow? Flow { get; set; }
    
    /// <summary>
    /// Workflow ID for serialization/deserialization.
    /// </summary>
    public Guid? WorkflowId { get; set; }
}
