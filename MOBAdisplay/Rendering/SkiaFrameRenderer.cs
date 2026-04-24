using SkiaSharp;

namespace Moba.Display.Rendering;

public sealed class SkiaFrameRenderer : IFrameRenderer, IDisposable
{
    private readonly SKBitmap _bitmap;
    private readonly SKCanvas _canvas;
    private readonly GridRenderer _gridRenderer;
    private readonly ClockRenderer _clockRenderer;
    private readonly SKPaint _backgroundPaint;
    private readonly SKPaint _trackNumberPaint;
    private readonly SKFont _trackNumberFont;
    private readonly SKRect[,] _cells;
    private bool _disposed;

    public SkiaFrameRenderer()
    {
        _bitmap = new SKBitmap(FrameDimensions.Width, FrameDimensions.Height, SKColorType.Bgra8888, SKAlphaType.Opaque);
        _canvas = new SKCanvas(_bitmap);
        _gridRenderer = new GridRenderer();
        _clockRenderer = new ClockRenderer();
        _cells = _gridRenderer.BuildCellRects();

        _backgroundPaint = new SKPaint
        {
            Color = new SKColor(0x10, 0x14, 0x1F),
            IsAntialias = false,
            Style = SKPaintStyle.Fill
        };

        _trackNumberPaint = new SKPaint
        {
            Color = SKColors.White,
            IsAntialias = true,
            Style = SKPaintStyle.Fill
        };

        // Large bold numeric font so the two-digit track number fills the top-left cell and stays readable on the 1.69" display.
        var typeface = SKTypeface.FromFamilyName(
            "Segoe UI",
            SKFontStyleWeight.Bold,
            SKFontStyleWidth.Normal,
            SKFontStyleSlant.Upright) ?? SKTypeface.Default;
        _trackNumberFont = new SKFont(typeface, 68f);
    }

    public void Render(FrameContext context, Span<byte> destinationRgb565)
    {
        ThrowIfDisposed();
        _canvas.Clear(_backgroundPaint.Color);

        DrawTrackNumber(_cells[0, 0], context.TrackNumber);
        _clockRenderer.DrawClock(_canvas, _cells[0, 1], context.Timestamp);

        Rgb565Converter.Convert(_bitmap, destinationRgb565);
    }

    private void DrawTrackNumber(SKRect cellRect, int trackNumber)
    {
        var clamped = Math.Clamp(trackNumber, 0, 99);
        var text = clamped.ToString("D2");

        _trackNumberFont.MeasureText(text, out var textBounds, _trackNumberPaint);
        var x = cellRect.MidX - textBounds.MidX;
        var y = cellRect.MidY - textBounds.MidY;
        _canvas.DrawText(text, x, y, SKTextAlign.Left, _trackNumberFont, _trackNumberPaint);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _trackNumberFont.Dispose();
        _trackNumberPaint.Dispose();
        _backgroundPaint.Dispose();
        _canvas.Dispose();
        _bitmap.Dispose();
        _disposed = true;
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }
}
