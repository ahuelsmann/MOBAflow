// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.WinUI.View;

using System.Numerics;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Geometry;
using TrackLibrary.PikoA;

/// <summary>
/// Konvertiert Pfad-Befehle aus <see cref="SegmentLocalPathBuilder"/> in Win2D <see cref="CanvasGeometry"/>.
/// Geometrie in lokalen Koordinaten (mm) – keine Platzierungs-Transform.
/// </summary>
internal static class PathToCanvasGeometryConverter
{
    /// <summary>
    /// Erstellt eine CanvasGeometry in Weltkoordinaten – identische Transformation wie
    /// <see cref="SegmentPlanPathBuilder.BuildPathGeometryInWorldCoords"/> und PathToSvgConverter.
    /// </summary>
    public static CanvasGeometry ToCanvasGeometryInWorldCoords(
        ICanvasResourceCreator resourceCreator,
        IReadOnlyList<SegmentLocalPathBuilder.PathCommand> commands,
        double originX, double originY, double angleDegrees, double scale)
    {
        var angleRad = angleDegrees * Math.PI / 180;
        var cos = Math.Cos(angleRad);
        var sin = Math.Sin(angleRad);
        float Tx(double lx, double ly) => (float)((originX + lx * cos - ly * sin) * scale);
        float Ty(double lx, double ly) => (float)((originY + lx * sin + ly * cos) * scale);

        var pathBuilder = new CanvasPathBuilder(resourceCreator);
        double x = 0, y = 0;
        var figureOpen = false;

        foreach (var cmd in commands)
        {
            switch (cmd)
            {
                case SegmentLocalPathBuilder.MoveTo move:
                    if (figureOpen)
                        pathBuilder.EndFigure(CanvasFigureLoop.Open);
                    pathBuilder.BeginFigure(Tx(move.X, move.Y), Ty(move.X, move.Y));
                    figureOpen = true;
                    x = move.X;
                    y = move.Y;
                    break;
                case SegmentLocalPathBuilder.LineTo line:
                    if (!figureOpen)
                    {
                        pathBuilder.BeginFigure(Tx(x, y), Ty(x, y));
                        figureOpen = true;
                    }
                    pathBuilder.AddLine(Tx(line.X, line.Y), Ty(line.X, line.Y));
                    x = line.X;
                    y = line.Y;
                    break;
                case SegmentLocalPathBuilder.ArcTo arc:
                    if (!figureOpen)
                    {
                        pathBuilder.BeginFigure(Tx(x, y), Ty(x, y));
                        figureOpen = true;
                    }
                    pathBuilder.AddArc(
                        new Vector2(Tx(arc.EndX, arc.EndY), Ty(arc.EndX, arc.EndY)),
                        (float)(arc.Radius * scale),
                        (float)(arc.Radius * scale),
                        0f,
                        arc.Clockwise ? CanvasSweepDirection.Clockwise : CanvasSweepDirection.CounterClockwise,
                        arc.LargeArc ? CanvasArcSize.Large : CanvasArcSize.Small);
                    x = arc.EndX;
                    y = arc.EndY;
                    break;
            }
        }

        if (figureOpen)
            pathBuilder.EndFigure(CanvasFigureLoop.Open);
        return CanvasGeometry.CreatePath(pathBuilder);
    }

    /// <summary>
    /// Erstellt eine <see cref="CanvasGeometry"/> aus Pfad-Befehlen in lokalen Koordinaten.
    /// </summary>
    /// <param name="resourceCreator">CanvasControl or DrawingSession (for ICanvasResourceCreator)</param>
    /// <param name="commands">Pfad-Befehle in lokalen Koordinaten (Port A = Ursprung)</param>
    /// <returns>CanvasGeometry in mm</returns>
    public static CanvasGeometry ToCanvasGeometry(ICanvasResourceCreator resourceCreator, IReadOnlyList<SegmentLocalPathBuilder.PathCommand> commands)
    {
        var pathBuilder = new CanvasPathBuilder(resourceCreator);
        var figureOpen = false;
        double x = 0, y = 0;

        foreach (var cmd in commands)
        {
            switch (cmd)
            {
                case SegmentLocalPathBuilder.MoveTo move:
                    if (figureOpen)
                        pathBuilder.EndFigure(CanvasFigureLoop.Open);
                    pathBuilder.BeginFigure((float)move.X, (float)move.Y);
                    figureOpen = true;
                    x = move.X;
                    y = move.Y;
                    break;

                case SegmentLocalPathBuilder.LineTo line:
                    if (!figureOpen)
                    {
                        pathBuilder.BeginFigure((float)x, (float)y);
                        figureOpen = true;
                    }
                    pathBuilder.AddLine((float)line.X, (float)line.Y);
                    x = line.X;
                    y = line.Y;
                    break;

                case SegmentLocalPathBuilder.ArcTo arc:
                    if (!figureOpen)
                    {
                        pathBuilder.BeginFigure((float)x, (float)y);
                        figureOpen = true;
                    }
                    pathBuilder.AddArc(
                        new Vector2((float)arc.EndX, (float)arc.EndY),
                        (float)arc.Radius,
                        (float)arc.Radius,
                        0f,
                        arc.Clockwise ? CanvasSweepDirection.Clockwise : CanvasSweepDirection.CounterClockwise,
                        arc.LargeArc ? CanvasArcSize.Large : CanvasArcSize.Small);
                    x = arc.EndX;
                    y = arc.EndY;
                    break;
            }
        }

        if (figureOpen)
            pathBuilder.EndFigure(CanvasFigureLoop.Open);
        return CanvasGeometry.CreatePath(pathBuilder);
    }
}
