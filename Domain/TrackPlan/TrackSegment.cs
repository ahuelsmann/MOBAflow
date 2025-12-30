// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Domain.TrackPlan;

/// <summary>
/// Represents a single track segment in the track plan.
/// Track system agnostic - supports Piko, Roco, Tillig, etc.
/// Contains drawing elements (Lines/Arcs) for rendering.
/// </summary>
public class TrackSegment
{
    /// <summary>
    /// Unique identifier for this segment.
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Track article code (e.g., "G231", "R2", "WL").
    /// Used to look up geometry from the track library (Piko, Roco, Tillig).
    /// </summary>
    public string ArticleCode { get; set; } = string.Empty;

    /// <summary>
    /// Endpoint coordinates for topology and connection matching.
    /// These are the connection points where tracks join.
    /// </summary>
    public List<SegmentEndpoint> Endpoints { get; set; } = [];

    /// <summary>
    /// Line drawing elements (straight sections).
    /// Used by renderer to draw the track.
    /// </summary>
    public List<DrawingLine> Lines { get; set; } = [];

    /// <summary>
    /// Arc drawing elements (curved sections).
    /// Used by renderer to draw curves with proper radius.
    /// </summary>
    public List<DrawingArc> Arcs { get; set; } = [];

    /// <summary>
    /// Optional: Assigned feedback sensor port (InPort 1-2048).
    /// </summary>
    public uint? AssignedInPort { get; set; }

    /// <summary>
    /// Optional: User-defined name for this segment.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Optional: Layer/group this segment belongs to.
    /// </summary>
    public string? Layer { get; set; }
}

/// <summary>
/// Endpoint of a track segment (connection point).
/// </summary>
public class SegmentEndpoint
{
    public double X { get; set; }
    public double Y { get; set; }
    
    /// <summary>
    /// Absolute direction in degrees (from AnyRail XML).
    /// 0° = East, 90° = North, 180° = West, 270° = South.
    /// Optional: Only set when imported from AnyRail.
    /// </summary>
    public double? Direction { get; set; }
}

/// <summary>
/// A straight line drawing element.
/// </summary>
public class DrawingLine
{
    public double X1 { get; set; }
    public double Y1 { get; set; }
    public double X2 { get; set; }
    public double Y2 { get; set; }
}

/// <summary>
/// An arc (curve) drawing element.
/// </summary>
public class DrawingArc
{
    public double X1 { get; set; }
    public double Y1 { get; set; }
    public double X2 { get; set; }
    public double Y2 { get; set; }
    public double Radius { get; set; }
    /// <summary>
    /// SVG sweep flag: 0 = counter-clockwise, 1 = clockwise.
    /// </summary>
    public int Sweep { get; set; }
}