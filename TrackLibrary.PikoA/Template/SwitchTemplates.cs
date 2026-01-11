// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.TrackLibrary.PikoA.Template;

using Moba.TrackLibrary.PikoA.Metadata;

/// <summary>
/// Switch (Weiche) track templates for Piko A-Gleis.
/// </summary>
public static class SwitchTemplates
{
    /// <summary>Left switch (Weiche links)</summary>
    public static TrackTemplate BWL => new(
        "BWL",
        [new TrackEnd("A"), new TrackEnd("B"), new TrackEnd("C")],
        new TrackGeometrySpec(TrackGeometryKind.Switch, RadiusMm: PikoAConstants.R9, AngleDeg: PikoAConstants.SwitchAngle),
        new SwitchRoutingModel { InEndId = "A", StraightEndId = "B", DivergingEndId = "C" });

    /// <summary>Right switch (Weiche rechts)</summary>
    public static TrackTemplate BWR => new(
        "BWR",
        [new TrackEnd("A"), new TrackEnd("B"), new TrackEnd("C")],
        new TrackGeometrySpec(TrackGeometryKind.Switch, RadiusMm: PikoAConstants.R9, AngleDeg: PikoAConstants.SwitchAngle),
        new SwitchRoutingModel { InEndId = "A", StraightEndId = "B", DivergingEndId = "C" });

    /// <summary>Crossing 30 degrees (Kreuzung)</summary>
    public static TrackTemplate K30 => new(
        "K30",
        [new TrackEnd("A"), new TrackEnd("B"), new TrackEnd("C"), new TrackEnd("D")],
        new TrackGeometrySpec(TrackGeometryKind.Straight, AngleDeg: PikoAConstants.CrossingAngle),
        null);

    public static IReadOnlyList<TrackTemplate> All =>
    [
        BWL,
        BWR,
        K30
    ];
}
