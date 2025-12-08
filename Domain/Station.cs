// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Domain;

/// <summary>
/// Station - Pure Data Object (POCO).
/// </summary>
public class Station
{
    public Station()
    {
        Id = Guid.NewGuid();
        Name = "New Station";
        PlatformIds = [];
    }

    public Guid Id { get; set; }
    public string Name { get; set; }
    public string? Description { get; set; }
    
    /// <summary>
    /// List of Platform IDs (GUID references only).
    /// Resolved at runtime via Project.Platforms lookup.
    /// </summary>
    public List<Guid> PlatformIds { get; set; }
    
    public uint InPort { get; set; } = 1;

    // --- Phase 1 Properties (Simplified Platform representation) ---
    // TODO: Move to Platform when Phase 2 is implemented

    /// <summary>
    /// Track/Platform number (simplified for Phase 1).
    /// In Phase 2, this will move to Platform entity.
    /// </summary>
    public uint? Track { get; set; } = 1;

    /// <summary>
    /// Arrival time (simplified for Phase 1).
    /// In Phase 2, this will move to Platform entity.
    /// </summary>
    public DateTime? Arrival { get; set; }

    /// <summary>
    /// Departure time (simplified for Phase 1).
    /// In Phase 2, this will move to Platform entity.
    /// </summary>
    public DateTime? Departure { get; set; }

    /// <summary>
    /// Exit orientation - true if exit is on left side (simplified for Phase 1).
    /// In Phase 2, this will move to Platform entity.
    /// </summary>
    public bool IsExitOnLeft { get; set; }
}