// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.TrackPlan.Geometry;

using Moba.TrackLibrary.Base.TrackSystem;

/// <summary>
/// Renders a three-way switch (Dreiwegweiche / W3) geometry.
/// 
/// Structure:
/// - Port A: Entry point (Input)
/// - Port B: Straight through (Straight output)
/// - Port C: Left diverging (+angle deviation)
/// - Port D: Right diverging (-angle deviation)
/// 
/// Renders 3 primitives:
/// [0] LinePrimitive: Straight through (A → B)
/// [1] ArcPrimitive: Left diverging (A → C, positive sweep)
/// [2] ArcPrimitive: Right diverging (A → D, negative sweep)
/// </summary>
public static class ThreeWaySwitchGeometry
{
    /// <summary>
    /// Renders a three-way switch to geometry primitives.
    /// Produces one straight line and two diverging arcs.
    /// </summary>
    public static IEnumerable<IGeometryPrimitive> Render(
        Point2D start,
        double startAngleDeg,
        TrackGeometrySpec spec)
    {
        var primitives = new List<IGeometryPrimitive>();

        double lengthMm = spec.LengthMm!.Value;
        double radiusMm = spec.RadiusMm!.Value;
        double angleDeg = spec.AngleDeg!.Value;

        double startRad = DegToRad(startAngleDeg);
        double sweepRad = DegToRad(angleDeg);

        // 1. Straight through (Port B)
        var straightEnd = new Point2D(
            start.X + lengthMm * Math.Cos(startRad),
            start.Y + lengthMm * Math.Sin(startRad)
        );
        primitives.Add(new LinePrimitive(start, straightEnd));

        // 2. Left diverging arc (Port C, +angle)
        var normalLeft = new Point2D(
            -Math.Sin(startRad),
            Math.Cos(startRad)
        );
        var centerLeft = new Point2D(
            start.X + normalLeft.X * radiusMm,
            start.Y + normalLeft.Y * radiusMm
        );
        double startAngleArcLeft = Math.Atan2(
            start.Y - centerLeft.Y,
            start.X - centerLeft.X
        );
        primitives.Add(new ArcPrimitive(
            Center: centerLeft,
            Radius: radiusMm,
            StartAngleRad: startAngleArcLeft,
            SweepAngleRad: sweepRad
        ));

        // 3. Right diverging arc (Port D, -angle)
        var normalRight = new Point2D(
            Math.Sin(startRad),
            -Math.Cos(startRad)
        );
        var centerRight = new Point2D(
            start.X + normalRight.X * radiusMm,
            start.Y + normalRight.Y * radiusMm
        );
        double startAngleArcRight = Math.Atan2(
            start.Y - centerRight.Y,
            start.X - centerRight.X
        );
        primitives.Add(new ArcPrimitive(
            Center: centerRight,
            Radius: radiusMm,
            StartAngleRad: startAngleArcRight,
            SweepAngleRad: -sweepRad
        ));

        return primitives;
    }

    private static double DegToRad(double deg) => deg * Math.PI / 180.0;
}
