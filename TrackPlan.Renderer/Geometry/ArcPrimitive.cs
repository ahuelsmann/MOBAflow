namespace Moba.TrackPlan.Renderer.Geometry;

public sealed record ArcPrimitive(
    Point2D Center,
    double Radius,
    double StartAngleRad,
    double SweepAngleRad
) : IGeometryPrimitive;