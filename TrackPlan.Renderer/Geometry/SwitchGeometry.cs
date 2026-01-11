namespace Moba.TrackPlan.Renderer.Geometry;

using Moba.TrackPlan.Renderer.World;
using Moba.TrackPlan.TrackSystem;

/// <summary>
/// Renders switch (turnout/Weiche) geometry.
/// Piko A-Gleis switches: 15 degree diverging angle, R=907.97mm radius.
/// </summary>
public static class SwitchGeometry
{
    /// <summary>
    /// Renders a switch with main route (straight) and diverging route (curved).
    /// </summary>
    /// <param name="origin">Switch frog point (where rails diverge).</param>
    /// <param name="straightEnd">End of straight route.</param>
    /// <param name="spec">Geometry specification with radius and angle.</param>
    /// <param name="isLeft">True for left-hand switch, false for right-hand.</param>
    /// <returns>Geometry primitives for both routes.</returns>
    public static IEnumerable<IGeometryPrimitive> Render(
        Point2D origin,
        Point2D straightEnd,
        TrackGeometrySpec spec,
        bool isLeft = true)
    {
        // Main route: straight line from origin to straight end
        yield return new LinePrimitive(origin, straightEnd);

        // Diverging route: curved arc
        var radius = spec.RadiusMm ?? 907.97; // Piko A default
        var angleDeg = spec.AngleDeg ?? 15.0;
        var angleRad = angleDeg * Math.PI / 180.0;

        // Calculate direction from origin to straight end
        var direction = (straightEnd - origin).Normalized();
        var length = (straightEnd - origin).Length;

        // Normal vector (perpendicular to direction)
        // Left switch: normal points left; Right switch: normal points right
        var normal = isLeft
            ? new Point2D(-direction.Y, direction.X)
            : new Point2D(direction.Y, -direction.X);

        // Arc center is at radius distance in normal direction from origin
        var center = origin + normal * radius;

        // Calculate start and sweep angles
        // Start angle: direction from center to origin
        var toOrigin = origin - center;
        var startAngleRad = Math.Atan2(toOrigin.Y, toOrigin.X);

        // Sweep angle: positive for counterclockwise (left), negative for clockwise (right)
        var sweepAngleRad = isLeft ? angleRad : -angleRad;

        yield return new ArcPrimitive(center, radius, startAngleRad, sweepAngleRad);

        // Calculate diverging end point for visual reference
        var divergingEnd = CalculateDivergingEnd(origin, direction, normal, radius, angleRad, isLeft);

        // Short straight at diverging end (frog extension)
        var frogExtension = length * 0.1; // 10% of main track length
        var divergingDirection = RotateVector(direction, isLeft ? angleRad : -angleRad);
        var frogEnd = divergingEnd + divergingDirection * frogExtension;

        yield return new LinePrimitive(divergingEnd, frogEnd);
    }

    /// <summary>
    /// Simplified render for basic visualization (two lines showing switch shape).
    /// </summary>
    public static IEnumerable<IGeometryPrimitive> RenderSimple(Point2D origin, Point2D straightEnd, bool isLeft = true)
    {
        const double angleDeg = 15.0;
        var angleRad = angleDeg * Math.PI / 180.0;

        // Main route: straight
        yield return new LinePrimitive(origin, straightEnd);

        // Diverging route: angled line
        var direction = (straightEnd - origin).Normalized();
        var length = (straightEnd - origin).Length;

        var divergingDirection = RotateVector(direction, isLeft ? angleRad : -angleRad);
        var divergingEnd = origin + divergingDirection * length;

        yield return new LinePrimitive(origin, divergingEnd);
    }

    private static Point2D CalculateDivergingEnd(
        Point2D origin,
        Point2D direction,
        Point2D normal,
        double radius,
        double angleRad,
        bool isLeft)
    {
        // Point on arc at angle from origin
        var cosA = Math.Cos(angleRad);
        var sinA = Math.Sin(angleRad);

        // Rotate the radius vector by the sweep angle
        var radiusToOrigin = origin - (origin + normal * radius);
        var rotatedX = radiusToOrigin.X * cosA - radiusToOrigin.Y * sinA * (isLeft ? 1 : -1);
        var rotatedY = radiusToOrigin.X * sinA * (isLeft ? 1 : -1) + radiusToOrigin.Y * cosA;

        return origin + normal * radius + new Point2D(rotatedX, rotatedY);
    }

    private static Point2D RotateVector(Point2D v, double angleRad)
    {
        var cos = Math.Cos(angleRad);
        var sin = Math.Sin(angleRad);
        return new Point2D(
            v.X * cos - v.Y * sin,
            v.X * sin + v.Y * cos);
    }
}