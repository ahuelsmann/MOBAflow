// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.WinUI.Service;

using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;

using System.Runtime.InteropServices.WindowsRuntime;

using ZXing;
using ZXing.QrCode;

/// <summary>
/// Renders a secret-bearing QR payload without writing it to disk.
/// </summary>
internal interface IRestApiQrCodeImageFactory
{
    Task<ImageSource> CreateAsync(string payload, CancellationToken cancellationToken = default);
}

/// <summary>
/// Creates an in-memory QR image for the MOBAflow Settings page.
/// </summary>
internal sealed class RestApiQrCodeImageFactory : IRestApiQrCodeImageFactory
{
    private const int ImageSize = 320;

    public async Task<ImageSource> CreateAsync(
        string payload,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(payload);
        cancellationToken.ThrowIfCancellationRequested();

        var writer = new BarcodeWriterPixelData
        {
            Format = BarcodeFormat.QR_CODE,
            Options = new QrCodeEncodingOptions
            {
                Height = ImageSize,
                Width = ImageSize,
                Margin = 2,
                CharacterSet = "UTF-8"
            }
        };
        var pixelData = writer.Write(payload);
        var bitmap = new WriteableBitmap(pixelData.Width, pixelData.Height);
        using var stream = bitmap.PixelBuffer.AsStream();
        await stream.WriteAsync(pixelData.Pixels, cancellationToken).ConfigureAwait(true);
        bitmap.Invalidate();
        return bitmap;
    }
}
