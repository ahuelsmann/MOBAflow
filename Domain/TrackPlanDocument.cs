// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Domain;

/// <summary>
/// Persisted physical track plan for a <see cref="Project"/>.
/// Serialized inline as <c>projects[].trackPlan</c> in <c>solution.json</c>.
/// The Domain layer is free of any track library / renderer dependencies —
/// track codes (e.g. "WR", "R9", "G239") are plain strings and are resolved
/// against <c>PikoACatalog</c> by the editor when the document is loaded.
/// </summary>
public sealed class TrackPlanDocument
{
    /// <summary>Schema version of this track plan document.</summary>
    public int Version { get; set; } = 1;

    /// <summary>Optional cached draw offset X (mm) for viewport stability.</summary>
    public double? OffsetX { get; set; }

    /// <summary>Optional cached draw offset Y (mm) for viewport stability.</summary>
    public double? OffsetY { get; set; }

    /// <summary>Optional zoom factor the user last left the editor at.</summary>
    public double? ZoomFactor { get; set; }

    /// <summary>All placed track segments.</summary>
    public List<TrackPlanSegment> Segments { get; set; } = [];

    /// <summary>All port connections between placed segments.</summary>
    public List<TrackPlanConnection> Connections { get; set; } = [];
}

/// <summary>Placed track segment entry inside a <see cref="TrackPlanDocument"/>.</summary>
public sealed class TrackPlanSegment
{
    /// <summary>Stable identifier of the placed segment (matches <c>Segment.No</c>).</summary>
    public Guid Id { get; set; }

    /// <summary>Catalog code (e.g. "WR", "R9", "G239") – resolved against PikoACatalog on load.</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>X position in mm.</summary>
    public double X { get; set; }

    /// <summary>Y position in mm.</summary>
    public double Y { get; set; }

    /// <summary>Rotation in degrees (0 = right, 90 = up).</summary>
    public double RotationDegrees { get; set; }

    /// <summary>Optional Z21 R-BUS feedback address assigned to this placed track.</summary>
    public int? InPort { get; set; }
}

/// <summary>Connection between two ports of two placed segments.</summary>
public sealed class TrackPlanConnection
{
    /// <summary>Source segment id.</summary>
    public Guid SourceSegment { get; set; }

    /// <summary>Source port name (e.g. "PortA").</summary>
    public string SourcePort { get; set; } = string.Empty;

    /// <summary>Target segment id.</summary>
    public Guid TargetSegment { get; set; }

    /// <summary>Target port name (e.g. "PortA").</summary>
    public string TargetPort { get; set; } = string.Empty;
}