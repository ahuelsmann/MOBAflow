// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.TrackLibrary.Base;

using System.Diagnostics.CodeAnalysis;

/// <summary>
/// One connector of a segment in the segment's local coordinates (mm, degrees).
/// </summary>
/// <param name="Name">Port name such as <c>PortA</c>.</param>
/// <param name="LocalX">X position relative to the segment origin.</param>
/// <param name="LocalY">Y position relative to the segment origin.</param>
/// <param name="LocalAngleDegrees">Track direction at the port, relative to the segment rotation.</param>
/// <param name="IsStart">
/// <c>true</c> when the track leaves the segment in the opposite direction of <paramref name="LocalAngleDegrees"/>
/// at this port (for example the entry port of a straight).
/// </param>
public sealed record SegmentPort(string Name, double LocalX, double LocalY, double LocalAngleDegrees, bool IsStart);

/// <summary>
/// Catalogue-specific track geometry and identity. The track-plan editor works only through this
/// interface, so a catalogue (for example Piko A) can be replaced without touching editor code.
/// </summary>
public interface ITrackCatalog
{
    /// <summary>Persisted identifier of the catalogue, stored with each track plan.</summary>
    string LibraryId { get; }

    /// <summary>Connectors of the segment in local coordinates.</summary>
    IReadOnlyList<SegmentPort> GetPorts(Segment segment);

    /// <summary>Drawing path of the segment in local coordinates, starting at the origin.</summary>
    IReadOnlyList<ITrackPathCommand> GetPath(Segment segment);

    /// <summary>Creates a new segment for a persisted catalogue code.</summary>
    bool TryCreateSegment(string code, [NotNullWhen(true)] out Segment? segment);

    /// <summary>Returns the persisted catalogue code of a segment.</summary>
    string GetCode(Segment segment);

    /// <summary>Returns the human-readable catalogue name of a segment.</summary>
    string GetDisplayName(Segment segment);
}
