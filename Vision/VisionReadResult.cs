// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Vision;

/// <summary>
/// Simple OCR result returned by <see cref="IVisionService.ReadTextAsync"/>.
/// Intentionally free of any SDK types so callers never take a hard dependency on the
/// underlying Azure SDK.
/// </summary>
/// <param name="Lines">All text lines detected in the image, in top-to-bottom reading order.</param>
/// <param name="ProcessingMilliseconds">Time the Azure service reported for the analysis run.</param>
/// <param name="ImageWidth">Pixel width of the analyzed image as reported by the service.</param>
/// <param name="ImageHeight">Pixel height of the analyzed image as reported by the service.</param>
public sealed record VisionReadResult(
    IReadOnlyList<VisionReadLine> Lines,
    long ProcessingMilliseconds,
    int ImageWidth,
    int ImageHeight)
{
    /// <summary>Empty result, used as a safe default.</summary>
    public static VisionReadResult Empty { get; } = new(Array.Empty<VisionReadLine>(), 0, 0, 0);

    /// <summary>Total number of words across all lines.</summary>
    public int WordCount => Lines.Sum(l => l.Words.Count);
}

/// <summary>
/// One line of text recognized in an image.
/// </summary>
/// <param name="Text">Full line text.</param>
/// <param name="Words">Individual words with bounding boxes and confidence.</param>
/// <param name="BoundingPolygon">
/// Polygon (usually 4 points) that encloses the line, in image pixel coordinates.
/// Top-left origin. May be empty if the SDK did not return one.
/// </param>
public sealed record VisionReadLine(
    string Text,
    IReadOnlyList<VisionReadWord> Words,
    IReadOnlyList<VisionPoint> BoundingPolygon);

/// <summary>
/// One recognized word inside a line.
/// </summary>
/// <param name="Text">Word text.</param>
/// <param name="Confidence">Confidence in the range 0..1.</param>
/// <param name="BoundingPolygon">Polygon enclosing the word in image pixel coordinates.</param>
public sealed record VisionReadWord(
    string Text,
    float Confidence,
    IReadOnlyList<VisionPoint> BoundingPolygon);

/// <summary>Two-dimensional pixel coordinate.</summary>
public readonly record struct VisionPoint(int X, int Y);
