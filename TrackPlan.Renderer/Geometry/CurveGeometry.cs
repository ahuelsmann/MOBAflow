// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.TrackPlan.Geometry;

using Moba.TrackPlan.TrackSystem;

public static class CurveGeometry
{
    /// <summary>
    /// Renders a curve track template to arc primitives.
    /// Works with any track library (Piko R1-R9, Roco curves, Peco curves, etc.)
    /// as long as the template provides RadiusMm and AngleDeg in its geometry spec.
    /// </summary>
    public static IEnumerable<IGeometryPrimitive> Render(
        TrackTemplate template,
        Point2D start,
        double startAngleDeg)
    {
        var spec = template.Geometry;

        double radius = spec.RadiusMm ?? throw new InvalidOperationException(
            $"Curve template '{template.Id}' is missing RadiusMm specification");
        double sweepDeg = spec.AngleDeg ?? throw new InvalidOperationException(
            $"Curve template '{template.Id}' is missing AngleDeg specification");

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