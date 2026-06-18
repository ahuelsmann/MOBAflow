// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
using Moba.Display.Rendering;

using SkiaSharp;

namespace Moba.Test.MOBAdisplay;

[TestFixture]
internal sealed class Rgb565ConverterTests
{
    [Test]
    public void Convert_EncodesPrimaryColors()
    {
        using var bitmap = new SKBitmap(FrameDimensions.Width, FrameDimensions.Height, SKColorType.Bgra8888, SKAlphaType.Opaque);
        bitmap.Erase(SKColors.Red);

        var rgb565 = new byte[FrameDimensions.FrameByteCount];
        Rgb565Converter.Convert(bitmap, rgb565);

        Assert.That(rgb565[0], Is.EqualTo(0xF8));
        Assert.That(rgb565[1], Is.EqualTo(0x00));
    }

    [Test]
    public void DecodeToBgra8888_ProducesOpaqueOutput()
    {
        var rgb565 = new byte[FrameDimensions.FrameByteCount];
        for (var i = 0; i < rgb565.Length; i += 2)
        {
            rgb565[i] = 0x07;
            rgb565[i + 1] = 0xE0;
        }

        var bgra = new byte[FrameDimensions.Width * FrameDimensions.Height * 4];
        Rgb565Converter.DecodeToBgra8888(rgb565, bgra);

        Assert.That(bgra[3], Is.EqualTo(255));
    }
}