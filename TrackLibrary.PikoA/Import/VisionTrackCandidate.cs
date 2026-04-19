// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.TrackLibrary.PikoA.Import;

/// <summary>
/// One PIKO A catalog entry that was unambiguously identified from an OCR'd screenshot.
/// Coordinates are in image pixels; origin is top-left.
/// </summary>
/// <param name="CatalogEntry">The matched PIKO A catalog entry.</param>
/// <param name="CenterX">X pixel coordinate of the label's bounding-box center.</param>
/// <param name="CenterY">Y pixel coordinate of the label's bounding-box center.</param>
/// <param name="RawText">The original OCR token that produced the match (useful for diagnostics).</param>
/// <param name="SourceLineIndex">Index of the OCR line this token came from (for traceability).</param>
public sealed record VisionTrackCandidate(
    TrackCatalogEntry CatalogEntry,
    double CenterX,
    double CenterY,
    string RawText,
    int SourceLineIndex);

/// <summary>
/// One OCR token that could not be mapped to a PIKO A catalog entry.
/// Kept for review UI / logging so the user can decide manually.
/// </summary>
/// <param name="RawText">The unmatched OCR token.</param>
/// <param name="CenterX">X pixel coordinate of the token's bounding-box center.</param>
/// <param name="CenterY">Y pixel coordinate of the token's bounding-box center.</param>
/// <param name="SourceLineIndex">Index of the OCR line this token came from.</param>
public sealed record VisionUnresolvedToken(
    string RawText,
    double CenterX,
    double CenterY,
    int SourceLineIndex);

/// <summary>
/// Aggregated outcome of running <see cref="PikoACodeExtractor"/> against a
/// <see cref="Moba.Vision.VisionReadResult"/>.
/// </summary>
/// <param name="Matches">Tokens that matched a PIKO A catalog entry exactly.</param>
/// <param name="Unresolved">Tokens that did not match (kept for manual review).</param>
public sealed record VisionExtractionResult(
    IReadOnlyList<VisionTrackCandidate> Matches,
    IReadOnlyList<VisionUnresolvedToken> Unresolved)
{
    /// <summary>Convenience: count of matches grouped by catalog code.</summary>
    public IReadOnlyDictionary<string, int> MatchCountsByCode =>
        Matches
            .GroupBy(m => m.CatalogEntry.Code, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.Count(), StringComparer.Ordinal);
}
