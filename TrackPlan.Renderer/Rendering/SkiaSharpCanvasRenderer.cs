// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.TrackPlan.Renderer.Rendering;

using Moba.TrackPlan.Geometry;
using Moba.TrackPlan.Renderer.World;

using SkiaSharp;

using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Renders track elements to a SKCanvas using SkiaSharp.
/// Handles drawing primitives, transformations, and styling.
/// </summary>
public sealed class SkiaSharpCanvasRenderer
{
    private const float DefaultStrokeWidth = 2f;
    private const float DefaultRailWidth = 1.5f;

    private readonly SKPaint _linePaint;
    private readonly SKPaint _fillPaint;
    private readonly SKPaint _railPaint;

    public SkiaSharpCanvasRenderer()
    {
        _linePaint = new SKPaint
        {
            Color = SKColors.Black,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = DefaultStrokeWidth,
            IsAntialias = true
        };

        _fillPaint = new SKPaint
        {
            Color = SKColors.LightGray.WithAlpha(100),
            Style = SKPaintStyle.Fill,
            IsAntialias = true
        };

        _railPaint = new SKPaint
        {
            Color = SKColors.DarkGray,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = DefaultRailWidth,
            IsAntialias = true
        };
    }

    /// <summary>
    /// Renders all track primitives to a canvas.
    /// </summary>
    public void RenderPrimitives(
        SKCanvas canvas,
        IEnumerable<IGeometryPrimitive> primitives,
        SKRect bounds)
    {
        ArgumentNullException.ThrowIfNull(canvas);
        ArgumentNullException.ThrowIfNull(primitives);

        foreach (var primitive in primitives)
        {
            RenderPrimitive(canvas, primitive);
        }
    }

    /// <summary>
    /// Renders a single geometry primitive.
    /// </summary>
    public void RenderPrimitive(SKCanvas canvas, IGeometryPrimitive primitive)
    {
        ArgumentNullException.ThrowIfNull(canvas);
        ArgumentNullException.ThrowIfNull(primitive);

        switch (primitive)
        {
            case LinePrimitive line:
                RenderLine(canvas, line);
                break;

            case ArcPrimitive arc:
                RenderArc(canvas, arc);
                break;

            default:
                // Unknown primitive type
                break;
        }
    }

    /// <summary>
    /// Renders a line primitive.
    /// </summary>
    public void RenderLine(SKCanvas canvas, LinePrimitive line)
    {
        var start = new SKPoint((float)line.From.X, (float)line.From.Y);
        var end = new SKPoint((float)line.To.X, (float)line.To.Y);

        canvas.DrawLine(start, end, _railPaint);
    }

    /// <summary>
    /// Renders an arc primitive.
    /// </summary>
    public void RenderArc(SKCanvas canvas, ArcPrimitive arc)
    {
        var centerX = (float)arc.Center.X;
        var centerY = (float)arc.Center.Y;
        var radius = (float)arc.Radius;

        var rect = new SKRect(
            centerX - radius,
            centerY - radius,
            centerX + radius,
            centerY + radius);

        var startAngleDeg = (float)(arc.StartAngleRad * 180.0 / Math.PI);
        var sweepAngleDeg = (float)(arc.SweepAngleRad * 180.0 / Math.PI);

        canvas.DrawArc(rect, startAngleDeg, sweepAngleDeg, false, _railPaint);
    }

    /// <summary>
    /// Renders track labels at specified positions.
    /// </summary>
    public void RenderLabel(
        SKCanvas canvas,
        string text,
        Point2D position,
        float fontSize = 10f)
    {
        var labelPaint = new SKPaint
        {
            Color = SKColors.Black,
            TextSize = fontSize,
            IsAntialias = true
        };

        canvas.DrawText(
            text,
            (float)position.X,
            (float)position.Y,
            labelPaint);
    }

    /// <summary>
    /// Renders feedback points (detector positions).
    /// </summary>
    public void RenderFeedbackPoint(
        SKCanvas canvas,
        Point2D position,
        int feedbackNumber,
        float size = 4f)
    {
        var feedbackPaint = new SKPaint
        {
            Color = SKColors.Red,
            Style = SKPaintStyle.Fill,
            IsAntialias = true
        };

        var x = (float)position.X;
        var y = (float)position.Y;

        canvas.DrawCircle(x, y, size, feedbackPaint);
        RenderLabel(canvas, feedbackNumber.ToString(), new Point2D(x + size, y + size), 8f);
    }

    /// <summary>
    /// Renders signal at specified position.
    /// </summary>
    public void RenderSignal(
        SKCanvas canvas,
        Point2D position,
        string signalType,
        float size = 8f)
    {
        var signalPaint = new SKPaint
        {
            Color = SKColors.Blue,
            Style = SKPaintStyle.Fill,
            IsAntialias = true
        };

        var x = (float)position.X;
        var y = (float)position.Y;

        canvas.DrawRect(new SKRect(x - size, y - size, x + size, y + size), signalPaint);
        RenderLabel(canvas, signalType, new Point2D(x + size, y + size), 8f);
    }

    /// <summary>
    /// Creates a bitmap from rendered primitives.
    /// </summary>
    public SKBitmap RenderToBitmap(
        IEnumerable<IGeometryPrimitive> primitives,
        SKRect bounds,
        float padding = 20f)
    {
        var width = (int)(bounds.Width + padding * 2);
        var height = (int)(bounds.Height + padding * 2);

        var bitmap = new SKBitmap(width, height);
        using (var canvas = new SKCanvas(bitmap))
        {
            canvas.Clear(SKColors.White);

            // Translate to account for padding
            canvas.Translate(padding - bounds.Left, padding - bounds.Top);

            RenderPrimitives(canvas, primitives, bounds);
        }

        return bitmap;
    }

    /// <summary>
    /// Exports rendered content to PNG file.
    /// </summary>
    public void ExportToPng(
        string filePath,
        IEnumerable<IGeometryPrimitive> primitives,
        SKRect bounds)
    {
        ArgumentNullException.ThrowIfNull(filePath);
        ArgumentNullException.ThrowIfNull(primitives);

        using (var bitmap = RenderToBitmap(primitives, bounds))
        using (var data = bitmap.Encode(SKEncodedImageFormat.Png, 100))
        {
            using (var stream = System.IO.File.Create(filePath))
            {
                data.SaveTo(stream);
            }
        }
    }

    /// <summary>
    /// Calculates bounds for a set of primitives.
    /// </summary>
    public SKRect CalculateBounds(IEnumerable<IGeometryPrimitive> primitives)
    {
        var points = new List<SKPoint>();

        foreach (var primitive in primitives)
        {
            switch (primitive)
            {
                case LinePrimitive line:
                    points.Add(new SKPoint((float)line.From.X, (float)line.From.Y));
                    points.Add(new SKPoint((float)line.To.X, (float)line.To.Y));
                    break;

                case ArcPrimitive arc:
                    points.Add(new SKPoint((float)arc.Center.X, (float)arc.Center.Y));
                    // Add points on arc boundary
                    var radius = (float)arc.Radius;
                    points.Add(new SKPoint((float)arc.Center.X + radius, (float)arc.Center.Y));
                    points.Add(new SKPoint((float)arc.Center.X - radius, (float)arc.Center.Y));
                    points.Add(new SKPoint((float)arc.Center.X, (float)arc.Center.Y + radius));
                    points.Add(new SKPoint((float)arc.Center.X, (float)arc.Center.Y - radius));
                    break;
            }
        }

        if (points.Count == 0)
            return new SKRect(0, 0, 100, 100);

        var minX = points.Min(p => p.X);
        var maxX = points.Max(p => p.X);
        var minY = points.Min(p => p.Y);
        var maxY = points.Max(p => p.Y);

        return new SKRect(minX, minY, maxX, maxY);
    }
}
