// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Domain.TrackPlan;

/// <summary>
/// Represents a complete track layout with all segments and connections.
/// Can be saved/loaded as part of a Project (JSON serialization).
/// </summary>
public class TrackLayout
{
    /// <summary>
    /// Unique identifier for this layout.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Display name for this layout.
    /// </summary>
    public string Name { get; set; } = "Untitled Layout";

    /// <summary>
    /// Description of the layout.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Track system used (e.g., "Piko A-Gleis", "Tillig Elite").
    /// </summary>
    public string TrackSystem { get; set; } = "Piko A-Gleis";

    /// <summary>
    /// Scale (e.g., "H0", "N", "TT").
    /// </summary>
    public string Scale { get; set; } = "H0";

    /// <summary>
    /// Width of the layout work surface in mm.
    /// </summary>
    public double WidthMm { get; set; } = 2400;

    /// <summary>
    /// Height of the layout work surface in mm.
    /// </summary>
    public double HeightMm { get; set; } = 1600;

    /// <summary>
    /// All track segments in this layout.
    /// </summary>
    public List<TrackSegment> Segments { get; set; } = [];

    /// <summary>
    /// All connections between track segments.
    /// </summary>
    public List<TrackConnection> Connections { get; set; } = [];
}
