// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.TrackPlan.Renderer.World;

public readonly record struct Point2D(double X, double Y)
{
    public static Point2D operator +(Point2D a, Point2D b)
        => new(a.X + b.X, a.Y + b.Y);

    public static Point2D operator -(Point2D a, Point2D b)
        => new(a.X - b.X, a.Y - b.Y);

    public static Point2D operator *(Point2D a, double s)
        => new(a.X * s, a.Y * s);

    public double Length => Math.Sqrt(X * X + Y * Y);

    public Point2D Normalized()
        => Length == 0 ? this : new Point2D(X / Length, Y / Length);
}