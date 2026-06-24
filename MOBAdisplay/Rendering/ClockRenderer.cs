// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
using SkiaSharp;

namespace Moba.Display.Rendering;

public sealed class ClockRenderer
{
    // Deutsche Bahn station clock colors (approximated).
    private static readonly SKColor RimColor = new(0x1C, 0x2A, 0x60);
    private static readonly SKColor MarkerColor = new(0x1C, 0x2A, 0x60);
    private static readonly SKColor HandColor = new(0x1C, 0x2A, 0x60);
    private static readonly SKColor SecondHandColor = new(0xD8, 0x1E, 0x1E);
    private static readonly SKColor FaceColor = SKColors.White;

    // Antialiasing is disabled for blue geometry so the rim, markers and hands render
    // in a single crisp blue tone on the low-resolution RGB565 display (no lighter
    // mixed pixels from AA blending with white face or dark background).
    private readonly SKPaint _facePaint = new()
    {
        Color = FaceColor,
        IsAntialias = false,
        Style = SKPaintStyle.Fill
    };

    private readonly SKPaint _rimPaint = new()
    {
        Color = RimColor,
        IsAntialias = false,
        Style = SKPaintStyle.Stroke
    };

    private readonly SKPaint _hourMarkerPaint = new()
    {
        Color = MarkerColor,
        IsAntialias = false,
        Style = SKPaintStyle.Fill
    };

    private readonly SKPaint _minuteTickPaint = new()
    {
        Color = MarkerColor,
        IsAntialias = false,
        Style = SKPaintStyle.Stroke,
        StrokeWidth = 1f,
        StrokeCap = SKStrokeCap.Butt
    };

    private readonly SKPaint _handPaint = new()
    {
        Color = HandColor,
        IsAntialias = false,
        Style = SKPaintStyle.Fill
    };

    private readonly SKPaint _centerCapPaint = new()
    {
        Color = HandColor,
        IsAntialias = false,
        Style = SKPaintStyle.Fill
    };

    private readonly SKPaint _secondHandPaint = new()
    {
        Color = SecondHandColor,
        IsAntialias = true,
        StrokeCap = SKStrokeCap.Round,
        Style = SKPaintStyle.Stroke
    };

    public void DrawClock(SKCanvas canvas, SKRect cellRect, DateTime timestamp)
    {
        ArgumentNullException.ThrowIfNull(canvas);

        var center = new SKPoint(cellRect.MidX, cellRect.MidY);
        var radius = MathF.Min(cellRect.Width, cellRect.Height) * 0.46f;
        if (radius <= 0f)
        {
            return;
        }

        DrawFaceAndRim(canvas, center, radius);
        DrawMinuteTicks(canvas, center, radius);
        DrawHourMarkers(canvas, center, radius);
        DrawHourAndMinuteHands(canvas, center, radius, timestamp);
        DrawSecondHand(canvas, center, radius, timestamp);
        DrawCenterCap(canvas, center, radius);
    }

    public SKPoint ComputeSecondHandEndpoint(SKPoint center, float radius, DateTime timestamp)
    {
        var angle = ((timestamp.Second / 60f) * 360f) - 90f;
        return ComputeEndpoint(center, radius * 0.72f, angle);
    }

    private void DrawFaceAndRim(SKCanvas canvas, SKPoint center, float radius)
    {
        var rimWidth = MathF.Max(2f, radius * 0.07f);
        canvas.DrawCircle(center, radius, _facePaint);

        _rimPaint.StrokeWidth = rimWidth;
        canvas.DrawCircle(center, radius - (rimWidth / 2f), _rimPaint);
    }

    private void DrawMinuteTicks(SKCanvas canvas, SKPoint center, float radius)
    {
        var outer = radius * 0.90f;
        var inner = radius * 0.84f;
        _minuteTickPaint.StrokeWidth = MathF.Max(1f, radius * 0.02f);

        for (var i = 0; i < 60; i++)
        {
            if (i % 5 == 0)
            {
                continue;
            }

            var angle = (i * 6f) - 90f;
            var p1 = ComputeEndpoint(center, inner, angle);
            var p2 = ComputeEndpoint(center, outer, angle);
            canvas.DrawLine(p1, p2, _minuteTickPaint);
        }
    }

    private void DrawHourMarkers(SKCanvas canvas, SKPoint center, float radius)
    {
        var markerLength = radius * 0.22f;
        var markerWidth = MathF.Max(2.5f, radius * 0.08f);
        var outer = radius * 0.92f;

        for (var i = 0; i < 12; i++)
        {
            var angle = (i * 30f) - 90f;
            var radians = angle * (MathF.PI / 180f);

            var dx = MathF.Cos(radians);
            var dy = MathF.Sin(radians);
            var nx = -dy;
            var ny = dx;

            var outerCenter = new SKPoint(center.X + (dx * outer), center.Y + (dy * outer));
            var innerCenter = new SKPoint(
                center.X + (dx * (outer - markerLength)),
                center.Y + (dy * (outer - markerLength)));

            var half = markerWidth / 2f;
            using var path = BuildClosedQuad(
                new SKPoint(outerCenter.X + (nx * half), outerCenter.Y + (ny * half)),
                new SKPoint(outerCenter.X - (nx * half), outerCenter.Y - (ny * half)),
                new SKPoint(innerCenter.X - (nx * half), innerCenter.Y - (ny * half)),
                new SKPoint(innerCenter.X + (nx * half), innerCenter.Y + (ny * half)));
            canvas.DrawPath(path, _hourMarkerPaint);
        }
    }

    private void DrawHourAndMinuteHands(SKCanvas canvas, SKPoint center, float radius, DateTime timestamp)
    {
        var hours = (timestamp.Hour % 12) + (timestamp.Minute / 60f);
        var minutes = timestamp.Minute + (timestamp.Second / 60f);
        var hourAngle = ((hours / 12f) * 360f) - 90f;
        var minuteAngle = ((minutes / 60f) * 360f) - 90f;

        DrawFlatHand(canvas, center, hourAngle, length: radius * 0.52f, width: MathF.Max(3f, radius * 0.13f));
        DrawFlatHand(canvas, center, minuteAngle, length: radius * 0.82f, width: MathF.Max(2.5f, radius * 0.10f));
    }

    private void DrawFlatHand(SKCanvas canvas, SKPoint center, float angleDegrees, float length, float width)
    {
        var radians = angleDegrees * (MathF.PI / 180f);
        var dx = MathF.Cos(radians);
        var dy = MathF.Sin(radians);
        var nx = -dy;
        var ny = dx;

        var tailLength = width * 0.9f;
        var tip = new SKPoint(center.X + (dx * length), center.Y + (dy * length));
        var tail = new SKPoint(center.X - (dx * tailLength), center.Y - (dy * tailLength));

        var half = width / 2f;
        using var path = BuildClosedQuad(
            new SKPoint(tip.X + (nx * half), tip.Y + (ny * half)),
            new SKPoint(tip.X - (nx * half), tip.Y - (ny * half)),
            new SKPoint(tail.X - (nx * half), tail.Y - (ny * half)),
            new SKPoint(tail.X + (nx * half), tail.Y + (ny * half)));
        canvas.DrawPath(path, _handPaint);
    }

    private void DrawSecondHand(SKCanvas canvas, SKPoint center, float radius, DateTime timestamp)
    {
        var angle = ((timestamp.Second / 60f) * 360f) - 90f;
        var radians = angle * (MathF.PI / 180f);
        var dx = MathF.Cos(radians);
        var dy = MathF.Sin(radians);

        var tipLength = radius * 0.72f;
        var tailLength = radius * 0.18f;
        _secondHandPaint.StrokeWidth = MathF.Max(1.5f, radius * 0.045f);

        var tail = new SKPoint(center.X - (dx * tailLength), center.Y - (dy * tailLength));
        var tip = new SKPoint(center.X + (dx * tipLength), center.Y + (dy * tipLength));
        canvas.DrawLine(tail, tip, _secondHandPaint);
    }

    private void DrawCenterCap(SKCanvas canvas, SKPoint center, float radius)
    {
        canvas.DrawCircle(center, MathF.Max(2f, radius * 0.06f), _centerCapPaint);
    }

    private static SKPoint ComputeEndpoint(SKPoint center, float length, float angleDegrees)
    {
        var radians = angleDegrees * (MathF.PI / 180f);
        return new SKPoint(
            center.X + (MathF.Cos(radians) * length),
            center.Y + (MathF.Sin(radians) * length));
    }

    private static SKPath BuildClosedQuad(SKPoint p1, SKPoint p2, SKPoint p3, SKPoint p4)
    {
        var builder = new SKPathBuilder();
        builder.MoveTo(p1);
        builder.LineTo(p2);
        builder.LineTo(p3);
        builder.LineTo(p4);
        builder.Close();
        return builder.Detach();
    }
}