// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.TrackLibrary.PikoA;

using Base;

using System.Diagnostics.CodeAnalysis;

/// <summary>
/// Piko A implementation of the catalogue-neutral <see cref="ITrackCatalog"/> used by the track-plan editor.
/// </summary>
public sealed class PikoATrackCatalog : ITrackCatalog
{
    /// <summary>Persisted library identifier of the Piko A catalogue.</summary>
    public const string Id = "piko-a";

    /// <inheritdoc />
    public string LibraryId => Id;

    /// <inheritdoc />
    public IReadOnlyList<SegmentPort> GetPorts(Segment segment)
    {
        ArgumentNullException.ThrowIfNull(segment);
        return SegmentPortGeometry.GetPorts(segment);
    }

    /// <inheritdoc />
    public IReadOnlyList<ITrackPathCommand> GetPath(Segment segment)
    {
        ArgumentNullException.ThrowIfNull(segment);
        return SegmentLocalPathBuilder.GetPath(segment);
    }

    /// <inheritdoc />
    public bool TryCreateSegment(string code, [NotNullWhen(true)] out Segment? segment)
    {
        var entry = PikoACatalog.All.FirstOrDefault(item => string.Equals(item.Code, code, StringComparison.OrdinalIgnoreCase));
        segment = entry?.CreateInstance();
        return segment != null;
    }

    /// <inheritdoc />
    public string GetCode(Segment segment) => GetEntry(segment).Code;

    /// <inheritdoc />
    public string GetDisplayName(Segment segment) => GetEntry(segment).DisplayName;

    private static TrackCatalogEntry GetEntry(Segment segment)
    {
        ArgumentNullException.ThrowIfNull(segment);
        return PikoACatalog.All.FirstOrDefault(item => item.SegmentType == segment.GetType())
            ?? throw new InvalidOperationException($"No catalog entry found for segment type {segment.GetType().Name}.");
    }
}
