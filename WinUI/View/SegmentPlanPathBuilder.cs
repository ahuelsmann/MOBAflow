// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.WinUI.View;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using TrackLibrary.PikoA;
using Windows.Foundation;

/// <summary>
/// Erzeugt Path-Elemente für platzierter Gleissegmente auf dem Plan und als Ghost.
/// Baut die Geometrie direkt aus Path-Befehlen auf, ohne XAML-Parsing.
/// </summary>
internal static class SegmentPlanPathBuilder
{
    private const double TrackStrokeWidth = 4;

    /// <summary>
    /// Erstellt ein Path für ein PlacedSegment (Plan-Anzeige, Ghost oder Auswahl).
    /// Verwendet dieselbe Transformationslogik wie PathToSvgConverter: Position und Rotation werden
    /// in die Geometrie eingerechnet, damit der Entry-Port stets korrekt am Verbindungspunkt liegt.
    /// </summary>
    public static Path CreatePath(PlacedSegment placed, bool isGhost, bool isSelected, char entryPort = 'A')
    {
        var pathCommands = SegmentLocalPathBuilder.GetPath(placed.Segment, entryPort);
        var geometry = BuildPathGeometryInWorldCoords(pathCommands, placed.X, placed.Y, placed.RotationDegrees, ScaleMmToPx);

        var path = new Path
        {
            Data = geometry,
            Stretch = Stretch.None,
            StrokeThickness = isSelected ? TrackStrokeWidth + 6 : TrackStrokeWidth,
            StrokeLineJoin = PenLineJoin.Round,
            StrokeStartLineCap = PenLineCap.Round,
            StrokeEndLineCap = PenLineCap.Round,
            Tag = placed.Segment.No
        };

        if (isGhost)
        {
            path.Fill = null;
            var accent = (SolidColorBrush)Application.Current.Resources["AccentFillColorDefaultBrush"]!;
            path.Stroke = new SolidColorBrush(Windows.UI.Color.FromArgb(180, accent.Color.R, accent.Color.G, accent.Color.B));
        }
        else
        {
            path.Fill = null;
            path.Stroke = isSelected ? ResolveTrackStrokeSelectedBrush() : ResolveTrackStrokeBrush();
        }

        return path;
    }

    /// <summary>Skalierungsfaktor mm → Pixel (muss mit TrackPlanPage.ScaleMmToPx übereinstimmen).</summary>
    public static double ScaleMmToPx { get; set; } = 1.0;

    /// <summary>
    /// Baut PathGeometry in Weltkoordinaten (wie PathToSvgConverter) – Translation und Rotation
    /// werden pro Punkt angewendet. Damit liegt der Entry-Port immer korrekt am Verbindungspunkt.
    /// </summary>
    private static PathGeometry BuildPathGeometryInWorldCoords(
        IReadOnlyList<SegmentLocalPathBuilder.PathCommand> commands,
        double originX,
        double originY,
        double angleDegrees,
        double scale)
    {
        var angleRad = angleDegrees * Math.PI / 180;
        var cos = Math.Cos(angleRad);
        var sin = Math.Sin(angleRad);
        double Tx(double lx, double ly) => (originX + lx * cos - ly * sin) * scale;
        double Ty(double lx, double ly) => (originY + lx * sin + ly * cos) * scale;

        var figures = new PathFigureCollection();
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
                        figures.Add(new PathFigure
                        {
                            StartPoint = new Point(Tx(x, y), Ty(x, y)),
                            IsClosed = false,
                            Segments = { new LineSegment { Point = new Point(Tx(line.X, line.Y), Ty(line.X, line.Y)) } }
                        });
                        x = line.X;
                        y = line.Y;
                        break;
                    }

                case SegmentLocalPathBuilder.ArcTo arc:
                    {
                        figures.Add(new PathFigure
                        {
                            StartPoint = new Point(Tx(x, y), Ty(x, y)),
                            IsClosed = false,
                            Segments =
                            {
                                new ArcSegment
                                {
                                    Point = new Point(Tx(arc.EndX, arc.EndY), Ty(arc.EndX, arc.EndY)),
                                    Size = new Size(arc.Radius * scale, arc.Radius * scale),
                                    RotationAngle = 0,
                                    IsLargeArc = arc.LargeArc,
                                    SweepDirection = arc.Clockwise ? SweepDirection.Clockwise : SweepDirection.Counterclockwise
                                }
                            }
                        });
                        x = arc.EndX;
                        y = arc.EndY;
                        break;
                    }
            }
        }

        return new PathGeometry { Figures = figures };
    }

    private static Brush ResolveTrackStrokeBrush()
    {
        if (Application.Current.Resources.TryGetValue("TrackPlanStrokeBrush", out var obj) && obj is Brush brush)
            return brush;
        return new SolidColorBrush(Windows.UI.Color.FromArgb(255, 26, 26, 26));
    }

    private static Brush ResolveTrackStrokeSelectedBrush()
    {
        if (Application.Current.Resources.TryGetValue("TrackPlanStrokeSelectedBrush", out var obj) && obj is Brush brush)
            return brush;
        return new SolidColorBrush(Windows.UI.Color.FromArgb(255, 0, 120, 215));
    }
}
