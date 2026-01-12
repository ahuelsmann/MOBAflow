// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.TrackPlan.Renderer.Geometry;

using Moba.TrackPlan.Renderer.World;

public static class StraightGeometry
{
    public static IEnumerable<IGeometryPrimitive> Render(
        Point2D start,
        double startAngleDeg,
        double lengthMm)
    {
        var angleRad = DegToRad(startAngleDeg);

        var end = new Point2D(
            start.X + lengthMm * Math.Cos(angleRad),
            start.Y + lengthMm * Math.Sin(angleRad)
        );

        yield return new LinePrimitive(start, end);
    }

    private static double DegToRad(double deg) => deg * Math.PI / 180.0;
}