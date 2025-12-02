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

    /// <summary>
    /// Track number assigned to this platform.
    /// </summary>
    public uint Track { get; set; } = 1;

    public uint InPort { get; set; } = 1; 
    public bool IsUsingTimerToIgnoreFeedbacks { get; set; }
    public double IntervalForTimerToIgnoreFeedbacks { get; set; }

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