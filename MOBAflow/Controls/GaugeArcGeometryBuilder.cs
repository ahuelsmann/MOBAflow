// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.Controls;

using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

/// <summary>
/// Builds a single semicircle arc path for gauge value indicators.
/// Matches the background track geometry: center (130,130), radius 100, left to right over the top.
/// </summary>
internal static class GaugeArcGeometryBuilder
{
    private const double CenterX = 130;
    private const double CenterY = 130;
    private const double Radius = 100;

    public const double ArcStrokeThickness = 16;

    public static PathGeometry? CreateSweepArc(double normalizedValue)
    {
        if (normalizedValue <= 0.001)
        {
            return null;
        }

        var sweepAngle = normalizedValue * 180;
        var endAngleRad = (180 - sweepAngle) * Math.PI / 180;
        var endX = CenterX + (Radius * Math.Cos(endAngleRad));
        var endY = CenterY - (Radius * Math.Sin(endAngleRad));

        var figure = new PathFigure
        {
            StartPoint = new Point(CenterX - Radius, CenterY),
            IsClosed = false
        };
        figure.Segments.Add(new ArcSegment
        {
            Point = new Point(endX, endY),
            Size = new Size(Radius, Radius),
            IsLargeArc = sweepAngle > 180,
            SweepDirection = SweepDirection.Clockwise
        });

        var geometry = new PathGeometry();
        geometry.Figures.Add(figure);
        return geometry;
    }
}
