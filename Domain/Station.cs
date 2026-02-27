// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Domain;

using System.Text.Json.Serialization;

/// <summary>
/// Station - Pure Data Object (POCO).
/// Represents a physical station with hardware address (InPort).
/// </summary>
public class Station
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Station"/> class with a new identifier and default values.
    /// </summary>
    public Station()
    {
        Id = Guid.NewGuid();
        Name = "New Station";
        Connections = [];
    }

    /// <summary>
    /// Gets or sets the unique identifier of the station.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the display name of the station.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Id of the city this station belongs to (for persistence).
    /// </summary>
    public Guid? CityId { get; set; }

    /// <summary>
    /// Reference to the city (resolved at runtime; not serialized).
    /// </summary>
    [JsonIgnore]
    public City? City { get; set; }

    /// <summary>
    /// Gets or sets an optional description of the station.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Hardware feedback address (Z21 InPort).
    /// Used for feedback detection on the physical model railroad.
    /// </summary>
    public uint InPort { get; set; }

    /// <summary>
    /// Number of laps before stopping at this station.
    /// </summary>
    public uint NumberOfLapsToStop { get; set; } = 1;

    /// <summary>
    /// Workflow ID for this station in this journey.
    /// Different journeys can trigger different workflows at the same station.
    /// </summary>
    public Guid? WorkflowId { get; set; }

    /// <summary>
    /// Exit orientation - true if exit is on left side.
    /// </summary>
    public bool IsExitOnLeft { get; set; }

    /// <summary>
    /// Upcoming feature: Track/Platform number.
    /// </summary>
    public uint? Track { get; set; } = 1;

    /// <summary>
    /// Upcoming feature: Arrival time.
    /// </summary>
    public DateTime? Arrival { get; set; }

    /// <summary>
    /// Upcoming feature: Departure time.
    /// </summary>
    public DateTime? Departure { get; set; }

    /// <summary>
    /// Upcoming feature: Travel connections.
    /// </summary>
    public List<ConnectingService> Connections { get; set; }
}