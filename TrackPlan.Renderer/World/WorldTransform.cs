// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.TrackPlan.Renderer.World;

using Moba.TrackPlan.Geometry;

public sealed class WorldTransform
{
    public double Scale { get; set; } = 1.0;
    public Point2D Offset { get; set; } = new(0, 0);

    public Point2D Transform(Point2D p)
        => new(p.X * Scale + Offset.X, p.Y * Scale + Offset.Y);
}