// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.TrackPlan.Renderer.Geometry;

using Moba.TrackPlan.Renderer.World;
using Moba.TrackPlan.TrackSystem;

public static class CurveGeometry
{
    public static IEnumerable<IGeometryPrimitive> Render(
        Point2D start,
        double startAngleDeg,
        TrackGeometrySpec spec)
    {
        double radius = spec.RadiusMm!.Value;
        double sweepDeg = spec.AngleDeg!.Value;

        double tangentRad = DegToRad(startAngleDeg);
        double sweepRad = DegToRad(sweepDeg);

        // Normal vector direction: left for positive sweep, right for negative sweep
        int normalDir = sweepRad >= 0 ? 1 : -1;
        
        // Normal perpendicular to tangent, pointing left (for positive) or right (for negative)
        var normal = new Point2D(
            normalDir * -Math.Sin(tangentRad),
            normalDir * Math.Cos(tangentRad)
        );

        // Mittelpunkt liegt auf dem Normalenvektor
        var center = start + normal * radius;

        // StartAngle des Bogens (vom Zentrum zum Startpunkt)
        // For positive sweep: tangentRad - π/2
        // For negative sweep: tangentRad + π/2  
        double arcStartRad = tangentRad - normalDir * Math.PI / 2.0;

        yield return new ArcPrimitive(
            Center: center,
            Radius: radius,
            StartAngleRad: arcStartRad,
            SweepAngleRad: sweepRad
        );
    }

    private static double DegToRad(double deg) => deg * Math.PI / 180.0;
}