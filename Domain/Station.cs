// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Domain;

/// <summary>
/// Station - Pure Data Object (POCO).
/// </summary>
public class Station
{
    public Station()
    {
        Name = "New Station";
        Platforms = [];
    }

    public string Name { get; set; }
    public string? Description { get; set; }
    public List<Platform> Platforms { get; set; }
    public int? FeedbackInPort { get; set; }

    /// <summary>
    /// Number of laps before stopping at this station.
    /// Used for repeat journey functionality.
    /// </summary>
    public int NumberOfLapsToStop { get; set; }

    /// <summary>
    /// Reference to Workflow (by reference, resolved by WorkflowService).
    /// NOTE: This is a navigation property that gets resolved after deserialization.
    /// </summary>
    public Workflow? Flow { get; set; }
    
    /// <summary>
    /// Workflow ID for serialization/deserialization.
    /// </summary>
    public Guid? WorkflowId { get; set; }
    
    // --- Phase 1 Properties (Simplified Platform representation) ---
    // TODO: Move to Platform when Phase 2 is implemented
    
    /// <summary>
    /// Track/Platform number (simplified for Phase 1).
    /// In Phase 2, this will move to Platform entity.
    /// </summary>
    public string? Track { get; set; }
    
    /// <summary>
    /// Arrival time or track designation (simplified for Phase 1).
    /// In Phase 2, this will move to Platform entity.
    /// </summary>
    public string? Arrival { get; set; }
    
    /// <summary>
    /// Departure time or track designation (simplified for Phase 1).
    /// In Phase 2, this will move to Platform entity.
    /// </summary>
    public string? Departure { get; set; }
    
    /// <summary>
    /// Exit orientation - true if exit is on left side (simplified for Phase 1).
    /// In Phase 2, this will move to Platform entity.
    /// </summary>
    public bool IsExitOnLeft { get; set; }
}
