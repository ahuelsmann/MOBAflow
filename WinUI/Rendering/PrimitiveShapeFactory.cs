// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;

using Moba.TrackPlan.Geometry;

using Windows.Foundation;

namespace Moba.WinUI.Rendering;

/// <summary>
/// Factory to convert geometry primitives (world coordinates in mm) to WinUI shapes (screen coordinates in px).
/// </summary>
public static class PrimitiveShapeFactory
{
    /// <summary>
    /// Creates a WinUI Shape from a geometry primitive, applying display scale.
    /// </summary>
    /// <param name="primitive">The geometry primitive in world coordinates (mm).</param>
    /// <param name="displayScale">Scale factor to convert mm to pixels (e.g., 0.5 means 1mm = 0.5px).</param>
    public static Shape CreateShape(IGeometryPrimitive primitive, double displayScale = 1.0)
    {
        return primitive switch
        {
            LinePrimitive line => new Line
            {
                X1 = line.From.X * displayScale,
                Y1 = line.From.Y * displayScale,
                X2 = line.To.X * displayScale,
                Y2 = line.To.Y * displayScale,
                StrokeThickness = 3
            },

            ArcPrimitive arc => CreateArc(arc, displayScale),

            _ => throw new NotSupportedException(
                $"Unsupported geometry primitive type: {primitive.GetType().Name}")
        };
    }

    private static Microsoft.UI.Xaml.Shapes.Path CreateArc(ArcPrimitive arc, double displayScale)
    {
        // Scale center and radius
        double cx = arc.Center.X * displayScale;
        double cy = arc.Center.Y * displayScale;
        double r = arc.Radius * displayScale;

        var startPoint = new Point(
            cx + r * Math.Cos(arc.StartAngleRad),
            cy + r * Math.Sin(arc.StartAngleRad));

        var endPoint = new Point(
            cx + r * Math.Cos(arc.StartAngleRad + arc.SweepAngleRad),
            cy + r * Math.Sin(arc.StartAngleRad + arc.SweepAngleRad));

        var figure = new PathFigure
        {
            StartPoint = startPoint,
            IsClosed = false,
            IsFilled = false
        };

        var segment = new ArcSegment
        {
            Point = endPoint,
            Size = new Size(r, r),
            SweepDirection = arc.SweepAngleRad >= 0
                ? SweepDirection.Clockwise
                : SweepDirection.Counterclockwise,
            IsLargeArc = Math.Abs(arc.SweepAngleRad) > Math.PI
        };

        figure.Segments.Add(segment);

        var geometry = new PathGeometry();
        geometry.Figures.Add(figure);

        return new Microsoft.UI.Xaml.Shapes.Path
        {
            Data = geometry,
            StrokeThickness = 3
        };
    }
}