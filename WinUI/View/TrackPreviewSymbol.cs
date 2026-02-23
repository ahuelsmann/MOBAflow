// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.WinUI.View;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using TrackLibrary.PikoA;

/// <summary>
/// Erzeugt Vorschau-Symbole für Piko A-Gleistypen in der Toolbox.
/// Nutzt <see cref="SegmentLocalPathBuilder"/> – dieselbe Geometrie wie der TrackPlanSvgRenderer.
/// </summary>
internal static class TrackPreviewSymbol
{
    private const double Width = 40;
    private const double Height = 24;
    private const double Padding = 4;
    private const double StrokeThickness = 2;

    /// <summary>
    /// Erstellt ein Vorschau-Symbol für den angegebenen Katalog-Eintrag.
    /// </summary>
    public static Path CreateSymbol(TrackCatalogEntry entry)
    {
        var segment = entry.CreateInstance();
        var pathCommands = SegmentLocalPathBuilder.GetPath(segment);
        var (minX, minY, maxX, maxY) = SegmentLocalPathBuilder.GetBounds(pathCommands);

        var rangeX = Math.Max(1, maxX - minX);
        var rangeY = Math.Max(1, maxY - minY);
        var scale = Math.Min((Width - 2 * Padding) / rangeX, (Height - 2 * Padding) / rangeY);
        var offsetX = Padding - minX * scale;
        var offsetY = Height / 2 - (minY + maxY) / 2 * scale;

        var geometry = BuildPathGeometry(pathCommands, scale, offsetX, offsetY);

        return new Path
        {
            Width = Width,
            Height = Height,
            Stretch = Stretch.None,
            Stroke = ResolveTrackStrokeBrush(),
            StrokeThickness = StrokeThickness,
            StrokeLineJoin = PenLineJoin.Round,
            StrokeStartLineCap = PenLineCap.Round,
            StrokeEndLineCap = PenLineCap.Round,
            Data = geometry
        };
    }

    private static Geometry BuildPathGeometry(
        IReadOnlyList<SegmentLocalPathBuilder.PathCommand> commands,
        double scale,
        double offsetX,
        double offsetY)
    {
        double ToX(double x) => x * scale + offsetX;
        double ToY(double y) => y * scale + offsetY;

        var pg = new PathGeometry();
        double x = 0, y = 0;

        foreach (var cmd in commands)
        {
            switch (cmd)
            {
                case SegmentLocalPathBuilder.MoveTo move:
                    x = move.X;
                    y = move.Y;
                    break;
                case SegmentLocalPathBuilder.LineTo line:
                    {
                        var pf = new PathFigure
                        {
                            StartPoint = new Windows.Foundation.Point(ToX(x), ToY(y)),
                            IsClosed = false
                        };
                        pf.Segments.Add(new LineSegment { Point = new Windows.Foundation.Point(ToX(line.X), ToY(line.Y)) });
                        pg.Figures.Add(pf);
                        x = line.X;
                        y = line.Y;
                        break;
                    }
                case SegmentLocalPathBuilder.ArcTo arc:
                    {
                        var pf = new PathFigure
                        {
                            StartPoint = new Windows.Foundation.Point(ToX(x), ToY(y)),
                            IsClosed = false
                        };
                        var radius = arc.Radius * scale;
                        pf.Segments.Add(new ArcSegment
                        {
                            Point = new Windows.Foundation.Point(ToX(arc.EndX), ToY(arc.EndY)),
                            Size = new Windows.Foundation.Size(radius, radius),
                            IsLargeArc = arc.LargeArc,
                            SweepDirection = arc.Clockwise ? SweepDirection.Clockwise : SweepDirection.Counterclockwise
                        });
                        pg.Figures.Add(pf);
                        x = arc.EndX;
                        y = arc.EndY;
                        break;
                    }
            }
        }

        return pg;
    }

    private static Brush ResolveTrackStrokeBrush()
    {
        if (Application.Current.Resources.TryGetValue("TrackPlanStrokeBrush", out var obj) && obj is Brush brush)
            return brush;
        return new SolidColorBrush(Windows.UI.Color.FromArgb(255, 26, 26, 26));
    }
}
