// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.TrackPlan.Geometry;

/// <summary>
/// Represents a 2D transformation (scaling and translation) from world coordinates to view coordinates.
/// Used for viewport transformations (zoom + pan).
/// </summary>
public sealed class WorldTransform
{
    /// <summary>
    /// Scale factor (zoom level). Default: 1.0 (no zoom).
    /// </summary>
    public double Scale { get; set; } = 1.0;

    /// <summary>
    /// Translation offset in view coordinates. Default: (0, 0) (no pan).
    /// </summary>
    public Point2D Offset { get; set; } = new(0, 0);

    /// <summary>
    /// Transforms a world point to view coordinates by applying scale and offset.
    /// Formula: (worldPoint.X * Scale + Offset.X, worldPoint.Y * Scale + Offset.Y)
    /// </summary>
    public Point2D Transform(Point2D p)
        => new(p.X * Scale + Offset.X, p.Y * Scale + Offset.Y);
}
