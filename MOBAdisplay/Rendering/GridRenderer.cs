using SkiaSharp;

namespace Moba.Display.Rendering;

public sealed class GridRenderer
{
    private readonly SKPaint _linePaint = new()
    {
        Color = new SKColor(0x66, 0x66, 0x66),
        IsAntialias = true,
        StrokeWidth = 2,
        Style = SKPaintStyle.Stroke
    };

    public SKRect[,] BuildCellRects()
    {
        var result = new SKRect[FrameDimensions.Rows, FrameDimensions.Columns];
        var cellWidth = FrameDimensions.Width / (float)FrameDimensions.Columns;
        var cellHeight = FrameDimensions.Height / (float)FrameDimensions.Rows;

        for (var row = 0; row < FrameDimensions.Rows; row++)
        {
            for (var col = 0; col < FrameDimensions.Columns; col++)
            {
                var left = col * cellWidth;
                var top = row * cellHeight;
                result[row, col] = new SKRect(left, top, left + cellWidth, top + cellHeight);
            }
        }

        return result;
    }

    public void DrawGrid(SKCanvas canvas, SKRect[,] cells)
    {
        ArgumentNullException.ThrowIfNull(canvas);
        ArgumentNullException.ThrowIfNull(cells);

        for (var row = 0; row < FrameDimensions.Rows; row++)
        {
            for (var col = 0; col < FrameDimensions.Columns; col++)
            {
                canvas.DrawRect(cells[row, col], _linePaint);
            }
        }
    }
}
