// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.TrackLibrary.PikoA.Template;

using Moba.TrackLibrary.PikoA.Metadata;

/// <summary>
/// Curve track templates for Piko A-Gleis.
/// </summary>
public static class CurveTemplates
{
    public static TrackTemplate R1 => new(
        "R1",
        [new TrackEnd("A"), new TrackEnd("B")],
        new TrackGeometrySpec(TrackGeometryKind.Curve, RadiusMm: PikoAConstants.R1, AngleDeg: PikoAConstants.StandardAngle),
        null);

    public static TrackTemplate R2 => new(
        "R2",
        [new TrackEnd("A"), new TrackEnd("B")],
        new TrackGeometrySpec(TrackGeometryKind.Curve, RadiusMm: PikoAConstants.R2, AngleDeg: PikoAConstants.StandardAngle),
        null);

    public static TrackTemplate R3 => new(
        "R3",
        [new TrackEnd("A"), new TrackEnd("B")],
        new TrackGeometrySpec(TrackGeometryKind.Curve, RadiusMm: PikoAConstants.R3, AngleDeg: PikoAConstants.StandardAngle),
        null);

    public static TrackTemplate R9 => new(
        "R9",
        [new TrackEnd("A"), new TrackEnd("B")],
        new TrackGeometrySpec(TrackGeometryKind.Curve, RadiusMm: PikoAConstants.R9, AngleDeg: PikoAConstants.SwitchAngle),
        null);

    public static IReadOnlyList<TrackTemplate> All =>
    [
        R1,
        R2,
        R3,
        R9
    ];
}
