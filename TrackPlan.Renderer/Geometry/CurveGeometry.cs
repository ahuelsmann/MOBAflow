namespace Moba.TrackPlan.Renderer.Geometry;

using Moba.TrackPlan.TrackSystem;

public static class CurveGeometry
{
    public static IEnumerable<IGeometryPrimitive> Render(Point2D a, Point2D b, TrackGeometrySpec spec)
    {
        var radius = spec.RadiusMm!.Value;
        var angleRad = spec.AngleDeg!.Value * Math.PI / 180.0;

        var mid = (a + b) * 0.5;
        var dir = (b - a).Normalized();
        var normal = new Point2D(-dir.Y, dir.X);

        var chord = (b - a).Length;
        var h = Math.Sqrt(radius * radius - (chord * chord / 4));

        var center = mid + normal * h;

        return [new ArcPrimitive(center, radius, 0, angleRad)];
    }
}