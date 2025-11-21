// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Backend.Model;

using Converter;

using Newtonsoft.Json;

/// <summary>
/// Represents a stop.
/// </summary>
public class Station
{
    public Station()
    {
        Name = "New Station";
        Platforms = new List<Platform>();
    }

    public string Name { get; set; }
    public uint Number { get; set; } = 0;
    public uint NumberOfLapsToStop { get; set; } = 2;
    public DateTime? Arrival { get; set; }
    public DateTime? Departure { get; set; }
    public uint Track { get; set; } = 1;
    public bool IsExitOnLeft { get; set; }
    public string TransferConnections { get; set; } = string.Empty; // (Umsteigemöglichkeiten)

    /// <summary>
    /// Workflow triggered by journey progression when the train reaches this station.
    /// </summary>
    [JsonConverter(typeof(WorkflowConverter))]
    public Workflow? Flow { get; set; }

    /// <summary>
    /// List of platforms associated with this station.
    /// Each platform can have its own feedback-triggered workflow for platform-specific announcements.
    /// </summary>
    public List<Platform> Platforms { get; set; }
}