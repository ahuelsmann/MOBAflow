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
        PlatformTag = string.Empty;
        Connections = [];
        Platforms = [];
    }

    /// <summary>
    /// Gets or sets the unique identifier of the station.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the display name of the station.
    /// </summary>
    public string Name { get; set; }

    public bool IsVirtual { get; set; }

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
    /// Exit orientation - true if exit is on left side.
    /// </summary>
    public bool IsExitOnLeft { get; set; }

    /// <summary>
    /// List of all platform for this station.
    /// </summary>
    public List<Platform> Platforms { get; set; }

    /// <summary>
    /// Gets or sets the track / platform number by platform id.
    /// </summary>
    public Guid? PlatformId { get; set; }

    /// <summary>
    /// Gets or sets the track / platform number.
    /// </summary>
    public uint PlatformNumber { get; set; }

    /// <summary>
    /// Gets or sets the track / platform number as string (e.g. 12A).
    /// </summary>
    public string PlatformTag { get; set; }

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
