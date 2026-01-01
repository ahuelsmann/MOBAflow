// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.TrackPlan.Geometry;

/// <summary>
/// 2D affine transformation matrix for track segment positioning.
/// Pure topology-first: No coordinate storage - only runtime transformation matrices.
/// </summary>
public record Transform2D
{
    /// <summary>
    /// Translation X component (world position).
    /// </summary>
    public double TranslateX { get; init; }

    /// <summary>
    /// Translation Y component (world position).
    /// </summary>
    public double TranslateY { get; init; }

    /// <summary>
    /// Rotation in degrees (0-360).
    /// </summary>
    public double RotationDegrees { get; init; }

    /// <summary>
    /// Identity transform (no translation, no rotation).
    /// </summary>
    public static Transform2D Identity { get; } = new Transform2D { TranslateX = 0, TranslateY = 0, RotationDegrees = 0 };

    /// <summary>
    /// Combine two transforms: result = this * other.
    /// Used for chaining parent transform + connector transform.
    /// </summary>
    public Transform2D Multiply(Transform2D other)
    {
        var thisRad = RotationDegrees * Math.PI / 180.0;
        var otherRad = other.RotationDegrees * Math.PI / 180.0;

        var cos = Math.Cos(thisRad);
        var sin = Math.Sin(thisRad);
        var rotatedX = other.TranslateX * cos - other.TranslateY * sin;
        var rotatedY = other.TranslateX * sin + other.TranslateY * cos;

        return new Transform2D
        {
            TranslateX = TranslateX + rotatedX,
            TranslateY = TranslateY + rotatedY,
            RotationDegrees = (RotationDegrees + other.RotationDegrees) % 360
        };
    }

    /// <summary>
    /// Invert this transform.
    /// Used for GetInverseConnectorTransform.
    /// </summary>
    public Transform2D Invert()
    {
        var rad = -RotationDegrees * Math.PI / 180.0;
        var cos = Math.Cos(rad);
        var sin = Math.Sin(rad);

        var invertedX = -TranslateX * cos + TranslateY * sin;
        var invertedY = -TranslateX * sin - TranslateY * cos;

        return new Transform2D
        {
            TranslateX = invertedX,
            TranslateY = invertedY,
            RotationDegrees = (-RotationDegrees + 360) % 360
        };
    }

    /// <summary>
    /// Transform a point (x, y) by this transformation.
    /// </summary>
    public (double X, double Y) TransformPoint(double x, double y)
    {
        var rad = RotationDegrees * Math.PI / 180.0;
        var cos = Math.Cos(rad);
        var sin = Math.Sin(rad);

        var rotatedX = x * cos - y * sin;
        var rotatedY = x * sin + y * cos;

        return (TranslateX + rotatedX, TranslateY + rotatedY);
    }
}
