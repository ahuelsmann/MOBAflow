// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Vision;

/// <summary>
/// Wrapper around the Azure AI Vision Image Analysis service, focused on the Read (OCR) feature.
/// </summary>
/// <remarks>
/// Implementations must not expose Azure SDK types across this interface so that the rest of the
/// application stays decoupled from the vendor SDK.
/// </remarks>
public interface IVisionService
{
    /// <summary>
    /// Display name of this vision engine, primarily used for diagnostics.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets whether the service is configured with key and endpoint.
    /// </summary>
    bool IsConfigured { get; }

    /// <summary>
    /// Runs the Read (OCR) visual feature on the given image.
    /// </summary>
    /// <param name="imageStream">Seekable stream containing image bytes (PNG/JPEG/BMP/TIFF).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Lines and words detected in the image.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when credentials are missing or the service call fails.
    /// </exception>
    Task<VisionReadResult> ReadTextAsync(Stream imageStream, CancellationToken cancellationToken = default);

    /// <summary>
    /// Convenience overload that reads an image from disk before calling <see cref="ReadTextAsync(Stream, CancellationToken)"/>.
    /// </summary>
    /// <param name="imagePath">Absolute path to an image file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<VisionReadResult> ReadTextAsync(string imagePath, CancellationToken cancellationToken = default);
}
