// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.TrackLibrary.PikoA;

using Base;

/// <summary>
/// A placed track segment on the editable track plan.
/// Contains the segment, position (mm), rotation (degrees) and optional Z21 feedback InPort.
/// </summary>
/// <param name="Segment">The track segment (G119, R9, WR, etc.)</param>
/// <param name="X">X position in mm (canvas coordinates)</param>
/// <param name="Y">Y position in mm (canvas coordinates)</param>
/// <param name="RotationDegrees">Rotation in degrees (0 = right, 90 = up)</param>
/// <param name="InPort">
/// Optional Z21 R-BUS feedback address assigned to this placed track.
/// <c>null</c> means the track is not wired to a feedback module.
/// The InPort belongs to the placement — not to the segment type — so identical
/// geometry can occur in the toolbox library without any address information.
/// </param>
public sealed record PlacedSegment(
    Segment Segment,
    double X,
    double Y,
    double RotationDegrees,
    int? InPort = null)
{
    /// <summary>Creates a clone with updated position and rotation (preserves InPort).</summary>
    public PlacedSegment WithPosition(double x, double y, double rotationDegrees) =>
        this with { X = x, Y = y, RotationDegrees = rotationDegrees };

    /// <summary>Creates a clone with updated InPort (preserves position and rotation).</summary>
    public PlacedSegment WithInPort(int? inPort) =>
        this with { InPort = inPort };
}