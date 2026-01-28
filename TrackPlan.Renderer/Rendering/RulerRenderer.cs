// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.TrackPlan.Renderer.Rendering;

using Moba.TrackPlan.Geometry;

using SkiaSharp;

using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Renders horizontal and vertical rulers (measures) for the track plan.
/// Supports zoom-dependent tick marks, labels, and theme-aware styling.
/// 
/// Usage:
/// - Fixed rulers: rendered at top and left edges of viewport
/// - Movable ruler: rendered at specified position with rotation
/// </summary>
public sealed class RulerRenderer
{
    private const float DefaultRulerHeight = 24.0f;
    private const float DefaultRulerWidth = 16.0f;
    private const float TextSize = 10.0f;

    /// <summary>
    /// Render a horizontal ruler at the top edge.
    /// </summary>
    public void RenderHorizontalRuler(
        SKCanvas canvas,
        RulerGeometry geometry,
        float canvasWidth,
        float y,
        SKColor textColor,
        SKColor tickColor,
        SKColor backgroundColor)
    {
        // Draw background
        using var bgPaint = new SKPaint
        {
            Color = backgroundColor,
            IsAntialias = true
        };
        canvas.DrawRect(
            new SKRect(0, y, canvasWidth, y + DefaultRulerHeight),
            bgPaint);

        // Draw border
        using var borderPaint = new SKPaint
        {
            Color = SKColors.Gray,
            IsAntialias = true,
            StrokeWidth = 1.0f,
            Style = SKPaintStyle.Stroke
        };
        canvas.DrawRect(
            new SKRect(0, y, canvasWidth, y + DefaultRulerHeight),
            borderPaint);

        // Draw ticks and labels
        RenderRulerTicks(canvas, geometry, y, 0, textColor, tickColor, isHorizontal: true);
    }

    /// <summary>
    /// Render a vertical ruler at the left edge.
    /// </summary>
    public void RenderVerticalRuler(
        SKCanvas canvas,
        RulerGeometry geometry,
        float canvasHeight,
        float x,
        SKColor textColor,
        SKColor tickColor,
        SKColor backgroundColor)
    {
        // Draw background
        using var bgPaint = new SKPaint
        {
            Color = backgroundColor,
            IsAntialias = true
        };
        canvas.DrawRect(
            new SKRect(x, 0, x + DefaultRulerWidth, canvasHeight),
            bgPaint);

        // Draw border
        using var borderPaint = new SKPaint
        {
            Color = SKColors.Gray,
            IsAntialias = true,
            StrokeWidth = 1.0f,
            Style = SKPaintStyle.Stroke
        };
        canvas.DrawRect(
            new SKRect(x, 0, x + DefaultRulerWidth, canvasHeight),
            borderPaint);

        // Draw ticks and labels
        RenderRulerTicks(canvas, geometry, 0, x, textColor, tickColor, isHorizontal: false);
    }

    /// <summary>
    /// Render individual tick marks and labels.
    /// </summary>
    private void RenderRulerTicks(
        SKCanvas canvas,
        RulerGeometry geometry,
        float rulerY,
        float rulerX,
        SKColor textColor,
        SKColor tickColor,
        bool isHorizontal)
    {
        using var tickPaint = new SKPaint
        {
            Color = tickColor,
            IsAntialias = true,
            StrokeWidth = 1.0f,
            Style = SKPaintStyle.Stroke
        };

        using var textPaint = new SKPaint
        {
            Color = textColor,
            IsAntialias = true,
            TextSize = TextSize,
            TextAlign = SKTextAlign.Center,
            Typeface = SKTypeface.FromFamilyName("Segoe UI")
        };

        foreach (var tick in geometry.Ticks)
        {
            if (isHorizontal)
            {
                // Draw vertical tick line
                canvas.DrawLine(
                    (float)tick.Position,
                    rulerY,
                    (float)tick.Position,
                    rulerY + (float)tick.Height,
                    tickPaint);

                // Draw label if present
                if (tick.Label != null)
                {
                    canvas.DrawText(
                        tick.Label,
                        (float)tick.Position,
                        rulerY + (float)tick.Height + TextSize + 2,
                        textPaint);
                }
            }
            else
            {
                // Draw horizontal tick line (vertical ruler)
                canvas.DrawLine(
                    rulerX,
                    (float)tick.Position,
                    rulerX + (float)tick.Height,
                    (float)tick.Position,
                    tickPaint);

                // Draw label if present (rotated 90 degrees)
                if (tick.Label != null)
                {
                    canvas.Save();
                    canvas.RotateDegrees(270, rulerX + (float)tick.Height + 8, (float)tick.Position);
                    canvas.DrawText(
                        tick.Label,
                        rulerX + (float)tick.Height + 8,
                        (float)tick.Position,
                        textPaint);
                    canvas.Restore();
                }
            }
        }
    }

    /// <summary>
    /// Render a movable ruler at specified position with rotation.
    /// Used for interactive measurement tool.
    /// </summary>
    public void RenderMovableRuler(
        SKCanvas canvas,
        RulerGeometry geometry,
        float startX,
        float startY,
        float length,
        float rotationDegrees,
        SKColor textColor,
        SKColor tickColor,
        SKColor backgroundColor)
    {
        canvas.Save();

        // Apply rotation around start point
        canvas.RotateDegrees(rotationDegrees, startX, startY);

        // Draw ruler background (thin line)
        using var bgPaint = new SKPaint
        {
            Color = backgroundColor,
            IsAntialias = true,
            StrokeWidth = 8.0f,
            Style = SKPaintStyle.Stroke
        };
        canvas.DrawLine(startX, startY, startX + length, startY, bgPaint);

        // Draw ruler outline
        using var outlinePaint = new SKPaint
        {
            Color = SKColors.Black,
            IsAntialias = true,
            StrokeWidth = 1.0f,
            Style = SKPaintStyle.Stroke
        };
        canvas.DrawLine(startX, startY, startX + length, startY, outlinePaint);

        // Draw ticks
        using var tickPaint = new SKPaint
        {
            Color = tickColor,
            IsAntialias = true,
            StrokeWidth = 1.0f,
            Style = SKPaintStyle.Stroke
        };

        foreach (var tick in geometry.Ticks)
        {
            var posX = startX + (float)tick.Position;
            canvas.DrawLine(
                posX,
                startY - (float)tick.Height / 2,
                posX,
                startY + (float)tick.Height / 2,
                tickPaint);
        }

        canvas.Restore();
    }
}

/// <summary>
/// Represents the geometric data for a ruler, including tick positions and heights.
/// </summary>
public sealed record RulerGeometry(
    IReadOnlyList<RulerTick> Ticks);

/// <summary>
/// Represents a single tick mark on a ruler.
/// </summary>
public sealed record RulerTick(
    double Position,   // Position in pixels
    double Height,     // Tick height in pixels
    string? Label = null);  // Optional label for major ticks
