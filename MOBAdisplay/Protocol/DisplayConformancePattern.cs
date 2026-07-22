// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Display.Protocol;

/// <summary>
/// Creates the deterministic RGB565 big-endian test pattern defined by protocol v1.0.
/// </summary>
public static class DisplayConformancePattern
{
    private const ushort Red = 0xF800;
    private const ushort Green = 0x07E0;
    private const ushort Blue = 0x001F;
    private const ushort White = 0xFFFF;
    private const ushort Black = 0x0000;

    /// <summary>
    /// Creates a complete row-major conformance frame for the supplied dimensions.
    /// </summary>
    public static byte[] CreateRgb565(ushort width, ushort height)
    {
        if (width == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width));
        }

        if (height == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(height));
        }

        var result = new byte[checked(width * height * 2)];
        var topRowCount = (height + 1) / 2;
        for (var y = 0; y < height; y++)
        {
            var row = result.AsSpan(y * width * 2, width * 2);
            if (y < topRowCount)
            {
                WriteTopRow(row, width);
            }
            else
            {
                WriteBottomRow(row, width);
            }
        }

        return result;
    }

    private static void WriteTopRow(Span<byte> row, int width)
    {
        var baseBandWidth = width / 3;
        var remainder = width % 3;
        var redWidth = baseBandWidth + (remainder > 0 ? 1 : 0);
        var greenWidth = baseBandWidth + (remainder > 1 ? 1 : 0);
        WriteBand(row, 0, redWidth, Red);
        WriteBand(row, redWidth, greenWidth, Green);
        WriteBand(row, redWidth + greenWidth, baseBandWidth, Blue);
    }

    private static void WriteBottomRow(Span<byte> row, int width)
    {
        var whiteWidth = (width + 1) / 2;
        WriteBand(row, 0, whiteWidth, White);
        WriteBand(row, whiteWidth, width - whiteWidth, Black);
    }

    private static void WriteBand(Span<byte> row, int startPixel, int pixelCount, ushort color)
    {
        for (var pixel = startPixel; pixel < startPixel + pixelCount; pixel++)
        {
            row[pixel * 2] = (byte)(color >> 8);
            row[pixel * 2 + 1] = (byte)color;
        }
    }
}