using SkiaSharp;

namespace Moba.Display.Rendering;

public static class Rgb565Converter
{
    public static unsafe void Convert(SKBitmap bitmap, Span<byte> destinationRgb565)
    {
        ArgumentNullException.ThrowIfNull(bitmap);

        if (destinationRgb565.Length < FrameDimensions.FrameByteCount)
        {
            throw new ArgumentException("Destination buffer is too small.", nameof(destinationRgb565));
        }

        var src = (uint*)bitmap.GetPixels().ToPointer();
        var pos = 0;
        var pixelCount = FrameDimensions.Width * FrameDimensions.Height;
        for (var i = 0; i < pixelCount; i++)
        {
            var pixel = src[i];
            var b = (byte)(pixel & 0xFF);
            var g = (byte)((pixel >> 8) & 0xFF);
            var r = (byte)((pixel >> 16) & 0xFF);

            var rgb = (ushort)(((r & 0xF8) << 8) | ((g & 0xFC) << 3) | (b >> 3));
            destinationRgb565[pos++] = (byte)(rgb >> 8);
            destinationRgb565[pos++] = (byte)(rgb & 0xFF);
        }
    }

    public static void DecodeToBgra8888(ReadOnlySpan<byte> rgb565, Span<byte> destinationBgra8888)
    {
        if (rgb565.Length < FrameDimensions.FrameByteCount)
        {
            throw new ArgumentException("RGB565 source buffer is too small.", nameof(rgb565));
        }

        var required = FrameDimensions.Width * FrameDimensions.Height * 4;
        if (destinationBgra8888.Length < required)
        {
            throw new ArgumentException("BGRA destination buffer is too small.", nameof(destinationBgra8888));
        }

        var src = 0;
        var dst = 0;
        for (var i = 0; i < FrameDimensions.Width * FrameDimensions.Height; i++)
        {
            var hi = rgb565[src++];
            var lo = rgb565[src++];
            var packed = (ushort)((hi << 8) | lo);

            var r5 = (packed >> 11) & 0x1F;
            var g6 = (packed >> 5) & 0x3F;
            var b5 = packed & 0x1F;

            var r = (byte)((r5 * 255) / 31);
            var g = (byte)((g6 * 255) / 63);
            var b = (byte)((b5 * 255) / 31);

            destinationBgra8888[dst++] = b;
            destinationBgra8888[dst++] = g;
            destinationBgra8888[dst++] = r;
            destinationBgra8888[dst++] = 255;
        }
    }
}
