// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

/// <summary>
/// Builds a single semicircle arc path for gauge value indicators.
/// Matches the background track geometry: center (171,130), radius 100, left to right over the top.
/// </summary>
internal static class GaugeArcGeometryBuilder
{
    private const double CenterX = GaugeVisualRules.GaugeCenterX;
    private const double CenterY = GaugeVisualRules.GaugeCenterY;
    private const double Radius = 100;
    private const double MinSweepNormalized = 0.001;
    public const double ArcStrokeThickness = 16;
    internal readonly record struct SweepArcDefinition(
        Point StartPoint,
        Point EndPoint,
        double Radius,
        bool IsLargeArc);
    public static bool TryGetSweepArcDefinition(double normalizedValue, out SweepArcDefinition definition)
    {
        definition = default;
        if (!IsRenderableNormalizedValue(normalizedValue))
        {
            return false;
        }

        var clamped = Math.Clamp(normalizedValue, 0, 1);
        var sweepAngle = clamped * 180;
        var endAngleRad = (180 - sweepAngle) * Math.PI / 180;
        var endX = CenterX + (Radius * Math.Cos(endAngleRad));
        var endY = CenterY - (Radius * Math.Sin(endAngleRad));
        if (!IsFinitePoint(endX, endY))
        {
            return false;
        }

        definition = new SweepArcDefinition(
            new Point(CenterX - Radius, CenterY),
            new Point(endX, endY),
            Radius,
            sweepAngle > 180);
        return true;
    }

    public static PathGeometry? CreateSweepArc(double normalizedValue)
    {
        if (!TryGetSweepArcDefinition(normalizedValue, out var definition))
        {
            return null;
        }

        var figure = new PathFigure
        {
            StartPoint = definition.StartPoint,
            IsClosed = false
        };
        figure.Segments.Add(new ArcSegment
        {
            Point = definition.EndPoint,
            Size = new Size(definition.Radius, definition.Radius),
            IsLargeArc = definition.IsLargeArc,
            SweepDirection = SweepDirection.Clockwise
        });
        var geometry = new PathGeometry();
        geometry.Figures.Add(figure);
        return geometry;
    }

    internal static bool IsRenderableNormalizedValue(double normalizedValue) =>
        !double.IsNaN(normalizedValue)
        && !double.IsInfinity(normalizedValue)
        && normalizedValue > MinSweepNormalized;
    private static bool IsFinitePoint(double x, double y) =>
        !double.IsNaN(x)
        && !double.IsNaN(y)
        && !double.IsInfinity(x)
        && !double.IsInfinity(y);
}