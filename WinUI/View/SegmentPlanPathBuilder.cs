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
public static class SegmentPlanPathBuilder
{
    private const double TrackStrokeWidth = 4;

    /// <summary>
    /// Erstellt ein Path für ein PlacedSegment (Plan-Anzeige, Ghost oder Auswahl).
    /// Position und Rotation werden per Canvas.SetLeft/SetTop und RenderTransform gesetzt.
    /// Der Ursprung (Port A) liegt bei (0,0) der Geometrie, damit die Rotation korrekt ist.
    /// </summary>
    public static Path CreatePath(PlacedSegment placed, bool isGhost, bool isSelected, char entryPort = 'A')
    {
        var pathCommands = SegmentLocalPathBuilder.GetPath(placed.Segment, entryPort);
        var (minX, minY, _, _) = SegmentLocalPathBuilder.GetBounds(pathCommands);
        var geometry = BuildPathGeometry(pathCommands, ScaleMmToPx, -minX, -minY);

        var path = new Path
        {
            Data = geometry,
            Stretch = Stretch.None,
            StrokeThickness = isSelected ? TrackStrokeWidth + 6 : TrackStrokeWidth,
            StrokeLineJoin = PenLineJoin.Round,
            StrokeStartLineCap = PenLineCap.Round,
            StrokeEndLineCap = PenLineCap.Round,
            Tag = placed.Segment.No,
            RenderTransformOrigin = new Point(0, 0),
            RenderTransform = new RotateTransform
            {
                Angle = placed.RotationDegrees,
                CenterX = 0,
                CenterY = 0
            }
        };

        if (isGhost)
        {
            path.Fill = null;
            path.Stroke = new SolidColorBrush(Windows.UI.Color.FromArgb(180, 0, 120, 215));
        }
        else
        {
            path.Fill = null;
            path.Stroke = isSelected
                ? new SolidColorBrush(Windows.UI.Color.FromArgb(255, 0, 120, 215))
                : new SolidColorBrush(Windows.UI.Color.FromArgb(255, 51, 51, 51));
        }

        return path;
    }

    /// <summary>Skalierungsfaktor mm → Pixel (muss mit TrackPlanPage.ScaleMmToPx übereinstimmen).</summary>
    public static double ScaleMmToPx { get; set; } = 1.0;

    /// <summary>
    /// Baut eine PathGeometry direkt aus PathCommands ohne XAML-Parsing.
    /// Transformiert mit Skalierung und optionalem Offset.
    /// </summary>
    private static PathGeometry BuildPathGeometry(
        IReadOnlyList<SegmentLocalPathBuilder.PathCommand> commands,
        double scale,
        double offsetX,
        double offsetY)
    {
        var figures = new PathFigureCollection();
        double currentX = 0, currentY = 0;

        foreach (var cmd in commands)
        {
            switch (cmd)
            {
                case SegmentLocalPathBuilder.MoveTo move:
                    currentX = move.X;
                    currentY = move.Y;
                    break;

                case SegmentLocalPathBuilder.LineTo line:
                    {
                        var figure = new PathFigure
                        {
                            StartPoint = new Point(
                                (currentX + offsetX) * scale,
                                (currentY + offsetY) * scale),
                            IsClosed = false
                        };
                        figure.Segments.Add(new LineSegment
                        {
                            Point = new Point(
                                (line.X + offsetX) * scale,
                                (line.Y + offsetY) * scale)
                        });
                        figures.Add(figure);
                        currentX = line.X;
                        currentY = line.Y;
                        break;
                    }

                case SegmentLocalPathBuilder.ArcTo arc:
                    {
                        var figure = new PathFigure
                        {
                            StartPoint = new Point(
                                (currentX + offsetX) * scale,
                                (currentY + offsetY) * scale),
                            IsClosed = false
                        };
                        figure.Segments.Add(new ArcSegment
                        {
                            Point = new Point(
                                (arc.EndX + offsetX) * scale,
                                (arc.EndY + offsetY) * scale),
                            Size = new Size(arc.Radius * scale, arc.Radius * scale),
                            RotationAngle = 0,
                            IsLargeArc = false,
                            SweepDirection = arc.Clockwise ? SweepDirection.Clockwise : SweepDirection.Counterclockwise
                        });
                        figures.Add(figure);
                        currentX = arc.EndX;
                        currentY = arc.EndY;
                        break;
                    }
            }
        }

        return new PathGeometry { Figures = figures };
    }
}
