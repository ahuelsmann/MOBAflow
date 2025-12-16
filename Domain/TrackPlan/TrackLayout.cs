// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Domain.TrackPlan;

/// <summary>
/// Represents a complete track layout with all segments.
/// Layouts are typically imported from AnyRail XML files.
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
    public string Name { get; set; } = string.Empty;

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
    /// Canvas width for rendering (from AnyRail layout width).
    /// </summary>
    public double CanvasWidth { get; set; } = 1000;

    /// <summary>
    /// Canvas height for rendering (from AnyRail layout height).
    /// </summary>
    public double CanvasHeight { get; set; } = 600;

    /// <summary>
    /// All track segments in this layout.
    /// </summary>
    public List<TrackSegment> Segments { get; set; } = [];
}
