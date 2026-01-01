// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.TrackPlan.Domain;

/// <summary>
/// Represents a complete track layout as a topology graph.
/// Contains segments (nodes) and connections (edges).
/// No coordinates - the renderer calculates positions from the graph.
/// </summary>
public class TrackLayout
{
    /// <summary>
    /// Display name for this layout.
    /// </summary>
    public string Name { get; set; } = "Untitled Layout";

    /// <summary>
    /// Description of the layout.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Track system used (e.g., "Piko A-Gleis").
    /// </summary>
    public string TrackSystem { get; set; } = "Piko A-Gleis";

    /// <summary>
    /// All track segments in this layout.
    /// </summary>
    public List<TrackSegment> Segments { get; set; } = [];

    /// <summary>
    /// All connections between track segments (the topology graph edges).
    /// </summary>
    public List<TrackConnection> Connections { get; set; } = [];
}
