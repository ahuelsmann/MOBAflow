// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.TrackPlan.Graph;

/// <summary>
/// Represents a track section (block) for controlling train occupancy.
/// A section is a group of tracks electrically isolated from adjacent sections
/// by isolators at specific ports.
/// </summary>
public sealed class Section
{
    public required Guid Id { get; init; }

    /// <summary>
    /// Human-readable name for this section (e.g., "Block 1", "Station Track 3").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Color used to visualize this section in the editor (hex format, e.g., "#FF5500").
    /// </summary>
    public string Color { get; set; } = "#0078D4";

    /// <summary>
    /// Function/type of this section (Track, Station, Siding, Shunting, Main, Branch).
    /// Used for operational categorization.
    /// </summary>
    public string Function { get; set; } = "Track";

    /// <summary>
    /// Tracks that belong to this section.
    /// </summary>
    public HashSet<Guid> TrackIds { get; init; } = [];
}
