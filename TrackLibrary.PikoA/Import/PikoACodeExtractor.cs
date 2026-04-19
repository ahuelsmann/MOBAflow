// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.TrackLibrary.PikoA.Import;

using System.Text.RegularExpressions;

using Moba.Vision;

/// <summary>
/// Extracts PIKO A catalog codes (G231, R2, WL, DKW, ...) from the text lines produced by
/// <see cref="IVisionService.ReadTextAsync"/>. Uses strict, case-sensitive equality against
/// <see cref="PikoACatalog.All"/> — no fuzzy correction, no neighbor guessing.
/// Tokens that cannot be mapped are returned as <see cref="VisionUnresolvedToken"/> so the
/// caller can present them for manual review.
/// </summary>
public static class PikoACodeExtractor
{
    /// <summary>
    /// A single whitespace-delimited token may still have decorative punctuation around it
    /// (e.g. trailing "." or leading quote). We only strip characters that clearly cannot be
    /// part of a PIKO A code: anything that is neither a letter nor a digit.
    /// </summary>
    private static readonly Regex s_tokenTrim = new(@"^[^A-Za-z0-9]+|[^A-Za-z0-9]+$", RegexOptions.Compiled);

    /// <summary>All valid catalog codes, keyed for O(1) strict lookup.</summary>
    private static readonly IReadOnlyDictionary<string, TrackCatalogEntry> s_catalogByCode =
        PikoACatalog.All.ToDictionary(e => e.Code, StringComparer.Ordinal);

    /// <summary>
    /// Extract PIKO A matches from a Vision OCR result.
    /// </summary>
    /// <param name="readResult">The OCR result returned by <see cref="IVisionService.ReadTextAsync"/>.</param>
    /// <returns>
    /// Matches + unresolved tokens. Never returns <c>null</c>. If <paramref name="readResult"/> is
    /// empty the returned collections are empty too.
    /// </returns>
    public static VisionExtractionResult Extract(VisionReadResult readResult)
    {
        ArgumentNullException.ThrowIfNull(readResult);

        var matches = new List<VisionTrackCandidate>();
        var unresolved = new List<VisionUnresolvedToken>();

        for (var lineIdx = 0; lineIdx < readResult.Lines.Count; lineIdx++)
        {
            var line = readResult.Lines[lineIdx];
            // Use per-word bounding polygons when available so compound lines like
            // "G62 G119" produce two separate candidates with their own positions.
            if (line.Words.Count > 0)
            {
                foreach (var word in line.Words)
                {
                    var (cx, cy) = PolygonCenter(word.BoundingPolygon);
                    ClassifyToken(word.Text, cx, cy, lineIdx, matches, unresolved);
                }
            }
            else
            {
                // Fallback: split the whole line on whitespace and share the line's center.
                var (cx, cy) = PolygonCenter(line.BoundingPolygon);
                foreach (var rawToken in line.Text.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                {
                    ClassifyToken(rawToken, cx, cy, lineIdx, matches, unresolved);
                }
            }
        }

        return new VisionExtractionResult(matches, unresolved);
    }

    private static void ClassifyToken(
        string rawToken,
        double cx,
        double cy,
        int sourceLineIndex,
        List<VisionTrackCandidate> matches,
        List<VisionUnresolvedToken> unresolved)
    {
        if (string.IsNullOrWhiteSpace(rawToken))
        {
            return;
        }

        var cleaned = s_tokenTrim.Replace(rawToken, string.Empty);
        if (cleaned.Length == 0)
        {
            unresolved.Add(new VisionUnresolvedToken(rawToken, cx, cy, sourceLineIndex));
            return;
        }

        if (s_catalogByCode.TryGetValue(cleaned, out var entry))
        {
            matches.Add(new VisionTrackCandidate(entry, cx, cy, rawToken, sourceLineIndex));
            return;
        }

        unresolved.Add(new VisionUnresolvedToken(rawToken, cx, cy, sourceLineIndex));
    }

    private static (double X, double Y) PolygonCenter(IReadOnlyList<VisionPoint> polygon)
    {
        if (polygon is null || polygon.Count == 0)
        {
            return (0d, 0d);
        }

        var sumX = 0L;
        var sumY = 0L;
        for (var i = 0; i < polygon.Count; i++)
        {
            sumX += polygon[i].X;
            sumY += polygon[i].Y;
        }
        return ((double)sumX / polygon.Count, (double)sumY / polygon.Count);
    }
}
