// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.TrackLibrary.PikoA.Import;

/// <summary>
/// Imports a list of <see cref="VisionTrackCandidate"/>s as loose <see cref="PlacedSegment"/>s
/// into an <see cref="EditableTrackPlan"/>. The importer does NOT attempt to reconstruct
/// topology (port connections) — every candidate becomes a free-floating segment that the
/// user can then snap/connect in the Track Plan Editor.
/// </summary>
public static class VisionTrackPlanImporter
{
    /// <summary>
    /// Adds one <see cref="PlacedSegment"/> per candidate to <paramref name="plan"/>.
    /// Pixel coordinates from the OCR result are mapped to millimeter canvas coordinates
    /// using <paramref name="pixelsPerMillimeter"/>. Rotation is always 0°; the user re-orients
    /// in the editor.
    /// </summary>
    /// <param name="plan">Target plan; mutated in place.</param>
    /// <param name="candidates">Candidates produced by <see cref="PikoACodeExtractor"/> (or manually resolved).</param>
    /// <param name="pixelsPerMillimeter">
    /// Scale factor. Defaults to <c>1.0</c> (1 pixel = 1 mm) which is a reasonable starting point
    /// for AnyRail screenshots at typical zoom; the user can always move / rescale afterwards.
    /// Must be &gt; 0.
    /// </param>
    /// <returns>The number of segments that were actually added to the plan.</returns>
    /// <exception cref="ArgumentNullException">If <paramref name="plan"/> or <paramref name="candidates"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">If <paramref name="pixelsPerMillimeter"/> is non-positive.</exception>
    public static int Import(
        EditableTrackPlan plan,
        IReadOnlyList<VisionTrackCandidate> candidates,
        double pixelsPerMillimeter = 1.0)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(candidates);
        if (pixelsPerMillimeter <= 0 || double.IsNaN(pixelsPerMillimeter) || double.IsInfinity(pixelsPerMillimeter))
        {
            throw new ArgumentOutOfRangeException(
                nameof(pixelsPerMillimeter),
                pixelsPerMillimeter,
                "pixelsPerMillimeter must be a positive, finite number.");
        }

        var added = 0;
        foreach (var candidate in candidates)
        {
            var segment = candidate.CatalogEntry.CreateInstance();
            var x = candidate.CenterX / pixelsPerMillimeter;
            var y = candidate.CenterY / pixelsPerMillimeter;
            var placed = new PlacedSegment(segment, x, y, RotationDegrees: 0d);

            var before = plan.Segments.Count;
            plan.AddSegment(placed);
            if (plan.Segments.Count > before)
            {
                added++;
            }
        }
        return added;
    }

    /// <summary>
    /// Convenience: build a <see cref="VisionTrackCandidate"/> from a previously unresolved
    /// OCR token once the user has manually picked the correct catalog entry. Used by the
    /// import-review dialog to turn user choices into importable candidates.
    /// </summary>
    public static VisionTrackCandidate ResolveUnresolved(VisionUnresolvedToken token, TrackCatalogEntry resolvedTo)
    {
        ArgumentNullException.ThrowIfNull(token);
        ArgumentNullException.ThrowIfNull(resolvedTo);

        return new VisionTrackCandidate(
            resolvedTo,
            token.CenterX,
            token.CenterY,
            RawText: token.RawText,
            SourceLineIndex: token.SourceLineIndex);
    }
}
