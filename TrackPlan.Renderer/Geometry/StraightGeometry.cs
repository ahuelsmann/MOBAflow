namespace Moba.TrackPlan.Renderer.Geometry;

public static class StraightGeometry
{
    public static IEnumerable<IGeometryPrimitive> Render(Point2D a, Point2D b)
        => [new LinePrimitive(a, b)];
}