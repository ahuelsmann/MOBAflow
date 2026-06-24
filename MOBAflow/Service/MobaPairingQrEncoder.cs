// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.Service;

using SkiaSharp;

using ZXing;
using ZXing.Common;

/// <summary>
/// Renders MOBAflow pairing QR codes as PNG bytes for WinUI.
/// </summary>
public static class MobaPairingQrEncoder
{
    public static byte[]? TryCreatePng(string payload, int size = 280)
    {
        if (string.IsNullOrWhiteSpace(payload) || size < 64)
        {
            return null;
        }

        var writer = new BarcodeWriterPixelData
        {
            Format = BarcodeFormat.QR_CODE,
            Options = new EncodingOptions
            {
                Height = size,
                Width = size,
                Margin = 1,
                PureBarcode = true
            }
        };

        var pixelData = writer.Write(payload);
        using var bitmap = new SKBitmap(
            pixelData.Width,
            pixelData.Height,
            SKColorType.Bgra8888,
            SKAlphaType.Premul);

        var pixelsPtr = bitmap.GetPixels();
        System.Runtime.InteropServices.Marshal.Copy(pixelData.Pixels, 0, pixelsPtr, pixelData.Pixels.Length);

        using var image = SKImage.FromBitmap(bitmap);
        using var encoded = image.Encode(SKEncodedImageFormat.Png, 100);
        return encoded?.ToArray();
    }
}
