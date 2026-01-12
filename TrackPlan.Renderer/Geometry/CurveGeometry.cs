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

        // Normalenvektor (links von der Tangente, zeigt zum Zentrum)
        var normal = new Point2D(
            -Math.Sin(tangentRad),
            Math.Cos(tangentRad)
        );

        // Mittelpunkt liegt auf dem Normalenvektor
        var center = start + normal * radius;

        // StartAngle des Bogens (vom Zentrum zum Startpunkt)
        // = tangentRad - π/2 (senkrecht zur Tangente, zeigt von center zu start)
        double arcStartRad = tangentRad - Math.PI / 2.0;

        yield return new ArcPrimitive(
            Center: center,
            Radius: radius,
            StartAngleRad: arcStartRad,
            SweepAngleRad: sweepRad
        );
    }

    private static double DegToRad(double deg) => deg * Math.PI / 180.0;
}