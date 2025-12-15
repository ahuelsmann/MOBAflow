// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Domain.TrackPlan;

/// <summary>
/// Represents a single track segment in the track plan.
/// Each segment can be selected and assigned a feedback sensor (InPort).
/// </summary>
public class TrackSegment
{
    /// <summary>
    /// Unique identifier for this segment.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Display name for this segment (e.g., "G231-001", "R2-Left-01").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Type of track segment.
    /// </summary>
    public TrackSegmentType Type { get; set; }

    /// <summary>
    /// Piko A-Gleis article code (e.g., "G231", "R2", "WL").
    /// </summary>
    public string ArticleCode { get; set; } = string.Empty;

    /// <summary>
    /// SVG path data for rendering this segment.
    /// Uses standard SVG path syntax (M, L, A, C, etc.).
    /// </summary>
    public string PathData { get; set; } = string.Empty;

    /// <summary>
    /// X coordinate of the segment's center point (for label placement).
    /// Normalized coordinates (0-1000).
    /// </summary>
    public double CenterX { get; set; }

    /// <summary>
    /// Y coordinate of the segment's center point (for label placement).
    /// Normalized coordinates (0-1000).
    /// </summary>
    public double CenterY { get; set; }

    /// <summary>
    /// Rotation angle in degrees (0 = horizontal, going right).
    /// </summary>
    public double Rotation { get; set; }

    /// <summary>
    /// Optional: Assigned feedback sensor port (InPort).
    /// Null if no sensor is assigned to this segment.
    /// </summary>
    public uint? AssignedInPort { get; set; }

    /// <summary>
    /// Optional: Track number for multi-track stations (e.g., "1", "2a").
    /// </summary>
    public string? TrackNumber { get; set; }

    /// <summary>
    /// Layer/group this segment belongs to (e.g., "MainLine", "Station", "Yard").
    /// </summary>
    public string Layer { get; set; } = "Default";
}
