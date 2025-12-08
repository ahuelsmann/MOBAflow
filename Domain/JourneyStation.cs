// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Domain;

/// <summary>
/// JourneyStation - Junction entity between Journey and Station.
/// Allows Journey-specific properties like IsExitOnLeft.
/// Example: Journey1 has Station "Hauptbahnhof" with IsExitOnLeft=true,
///          Journey2 has same Station with IsExitOnLeft=false.
/// </summary>
public class JourneyStation
{
    public JourneyStation()
    {
        StationId = Guid.Empty;
    }

    /// <summary>
    /// Reference to Station (resolved at runtime via Project.Stations or Journey context).
    /// </summary>
    public Guid StationId { get; set; }

    /// <summary>
    /// Journey-specific property: Exit orientation.
    /// True if exit is on left side for THIS journey.
    /// </summary>
    public bool IsExitOnLeft { get; set; }

    /// <summary>
    /// Number of laps before stopping at this station (Journey-specific).
    /// Used for repeat journey functionality.
    /// </summary>
    public uint NumberOfLapsToStop { get; set; } = 1;

    /// <summary>
    /// Workflow ID for this station in this journey (Journey-specific).
    /// Different journeys can trigger different workflows at the same station.
    /// </summary>
    public Guid? WorkflowId { get; set; }
}
