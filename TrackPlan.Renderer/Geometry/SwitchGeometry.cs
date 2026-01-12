// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.TrackPlan.Renderer.Geometry;

using Moba.TrackPlan.Renderer.World;
using Moba.TrackPlan.TrackSystem;

public static class SwitchGeometry
{
    public static IEnumerable<IGeometryPrimitive> Render(
        Point2D start,
        double startAngleDeg,
        TrackGeometrySpec spec,
        SwitchRoutingModel routing,
        bool isLeftSwitch)
    {
        var primitives = new List<IGeometryPrimitive>();

        double lengthMm = spec.LengthMm!.Value;
        double radiusMm = spec.RadiusMm!.Value;
        double angleDeg = spec.AngleDeg!.Value;
        double junctionOffsetMm = spec.JunctionOffsetMm!.Value;

        double startRad = DegToRad(startAngleDeg);
        double sweepRad = DegToRad(angleDeg);

        var straightEnd = new Point2D(
            start.X + lengthMm * Math.Cos(startRad),
            start.Y + lengthMm * Math.Sin(startRad)
        );

        primitives.Add(new LinePrimitive(start, straightEnd));

        var junction = new Point2D(
            start.X + junctionOffsetMm * Math.Cos(startRad),
            start.Y + junctionOffsetMm * Math.Sin(startRad)
        );

        double side = isLeftSwitch ? +1.0 : -1.0;

        var normal = new Point2D(
            -Math.Sin(startRad) * side,
            Math.Cos(startRad) * side
        );

        var center = new Point2D(
            junction.X + normal.X * radiusMm,
            junction.Y + normal.Y * radiusMm
        );

        double startAngleArcRad = Math.Atan2(
            junction.Y - center.Y,
            junction.X - center.X
        );

        double sweepArcRad = sweepRad * side;

        primitives.Add(new ArcPrimitive(
            Center: center,
            Radius: radiusMm,
            StartAngleRad: startAngleArcRad,
            SweepAngleRad: sweepArcRad
        ));

        return primitives;
    }

    private static double DegToRad(double deg) => deg * Math.PI / 180.0;
}