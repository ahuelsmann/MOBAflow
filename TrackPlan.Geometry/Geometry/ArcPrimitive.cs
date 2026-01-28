// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.TrackPlan.Geometry;

/// <summary>
/// Represents a circular arc primitive.
/// Used for rendering curved tracks and switches.
/// </summary>
public sealed record ArcPrimitive(
    /// <summary>Center point of the arc in world coordinates.</summary>
    Point2D Center,
    /// <summary>Radius of the arc in mm.</summary>
    double Radius,
    /// <summary>Start angle of the arc in radians (0 = East, π/2 = North).</summary>
    double StartAngleRad,
    /// <summary>Sweep angle of the arc in radians (positive = counter-clockwise).</summary>
    double SweepAngleRad
) : IGeometryPrimitive;
