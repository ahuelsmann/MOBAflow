// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
using Moba.Display.Rendering;
using SkiaSharp;
using System.Drawing;

namespace Moba.Display;

[Obsolete("Use Rgb565Converter with SkiaFrameRenderer for runtime performance.")]
public static class BitmapToRgb565
{
    public static byte[] Convert(Bitmap bmp)
    {
        ArgumentNullException.ThrowIfNull(bmp);
        if (bmp.Width != FrameDimensions.Width || bmp.Height != FrameDimensions.Height)
        {
            throw new ArgumentException("Bitmap must be 240x280.", nameof(bmp));
        }

        using var skBitmap = new SKBitmap(FrameDimensions.Width, FrameDimensions.Height, SKColorType.Bgra8888, SKAlphaType.Opaque);
        for (var y = 0; y < FrameDimensions.Height; y++)
        {
            for (var x = 0; x < FrameDimensions.Width; x++)
            {
                var c = bmp.GetPixel(x, y);
                skBitmap.SetPixel(x, y, new SKColor(c.R, c.G, c.B));
            }
        }

        var buffer = new byte[FrameDimensions.FrameByteCount];
        Rgb565Converter.Convert(skBitmap, buffer);
        return buffer;
    }
}