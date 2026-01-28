// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.TrackPlan.Geometry;

/// <summary>
/// Represents a 2D point in world coordinates.
/// Supports basic vector operations (addition, subtraction, scaling, normalization).
/// </summary>
public readonly record struct Point2D(double X, double Y)
{
    /// <summary>
    /// Vector addition operator.
    /// </summary>
    public static Point2D operator +(Point2D a, Point2D b)
        => new(a.X + b.X, a.Y + b.Y);

    /// <summary>
    /// Vector subtraction operator.
    /// </summary>
    public static Point2D operator -(Point2D a, Point2D b)
        => new(a.X - b.X, a.Y - b.Y);

    /// <summary>
    /// Scalar multiplication operator.
    /// </summary>
    public static Point2D operator *(Point2D a, double s)
        => new(a.X * s, a.Y * s);

    /// <summary>
    /// Euclidean length (magnitude) of the vector.
    /// </summary>
    public double Length => Math.Sqrt(X * X + Y * Y);

    /// <summary>
    /// Returns a normalized (unit length) vector.
    /// Returns the original vector if length is zero.
    /// </summary>
    public Point2D Normalized()
        => Length == 0 ? this : new Point2D(X / Length, Y / Length);
}
