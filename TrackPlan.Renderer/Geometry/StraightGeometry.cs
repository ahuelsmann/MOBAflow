// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.TrackPlan.Renderer.Geometry;

using Moba.TrackPlan.Renderer.World;
using Moba.TrackPlan.TrackSystem;

public static class StraightGeometry
{
    /// <summary>
    /// Renders a straight track template to line primitives.
    /// Works with any track library (Piko, Roco, Peco, etc.) as long as 
    /// the template provides valid LengthMm in its geometry spec.
    /// </summary>
    public static IEnumerable<IGeometryPrimitive> Render(
        TrackTemplate template,
        Point2D start,
        double startAngleDeg)
    {
        var spec = template.Geometry;
        
        // Get length from spec - all libraries must provide this
        var lengthMm = spec.LengthMm ?? throw new InvalidOperationException(
            $"Straight template '{template.Id}' is missing LengthMm specification");

        var angleRad = DegToRad(startAngleDeg);

        var end = new Point2D(
            start.X + lengthMm * Math.Cos(angleRad),
            start.Y + lengthMm * Math.Sin(angleRad)
        );

        yield return new LinePrimitive(start, end);
    }

    private static double DegToRad(double deg) => deg * Math.PI / 180.0;
}