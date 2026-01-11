// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.TrackLibrary.PikoA.Template;

using Moba.TrackLibrary.PikoA.Metadata;

/// <summary>
/// Straight track templates for Piko A-Gleis.
/// </summary>
public static class StraightTemplates
{
    public static TrackTemplate G231 => new(
        "G231",
        [new TrackEnd("A"), new TrackEnd("B")],
        new TrackGeometrySpec(TrackGeometryKind.Straight, LengthMm: PikoAConstants.G231Length),
        null);

    public static TrackTemplate G119 => new(
        "G119",
        [new TrackEnd("A"), new TrackEnd("B")],
        new TrackGeometrySpec(TrackGeometryKind.Straight, LengthMm: PikoAConstants.G119Length),
        null);

    public static TrackTemplate G62 => new(
        "G62",
        [new TrackEnd("A"), new TrackEnd("B")],
        new TrackGeometrySpec(TrackGeometryKind.Straight, LengthMm: PikoAConstants.G62Length),
        null);

    public static TrackTemplate G56 => new(
        "G56",
        [new TrackEnd("A"), new TrackEnd("B")],
        new TrackGeometrySpec(TrackGeometryKind.Straight, LengthMm: PikoAConstants.G56Length),
        null);

    public static TrackTemplate G31 => new(
        "G31",
        [new TrackEnd("A"), new TrackEnd("B")],
        new TrackGeometrySpec(TrackGeometryKind.Straight, LengthMm: PikoAConstants.G31Length),
        null);

    public static IReadOnlyList<TrackTemplate> All =>
    [
        G231,
        G119,
        G62,
        G56,
        G31
    ];
}
