namespace Moba.TrackLibrary.PikoA.Template;

using Moba.TrackLibrary.PikoA.Metadata;
using Moba.TrackPlan.TrackSystem;

public static class CurveTemplates
{
    public static TrackTemplate R1 => new(
        "R1",
        [
            new TrackEnd("A", 0),
            new TrackEnd("B", PikoAConstants.StandardAngle)
        ],
        new TrackGeometrySpec(
            TrackGeometryKind.Curve,
            RadiusMm: PikoAConstants.R1,
            AngleDeg: PikoAConstants.StandardAngle
        ),
        null
    );

    public static TrackTemplate R2 => new(
        "R2",
        [
            new TrackEnd("A", 0),
            new TrackEnd("B", PikoAConstants.StandardAngle)
        ],
        new TrackGeometrySpec(
            TrackGeometryKind.Curve,
            RadiusMm: PikoAConstants.R2,
            AngleDeg: PikoAConstants.StandardAngle
        ),
        null
    );

    public static TrackTemplate R3 => new(
        "R3",
        [
            new TrackEnd("A", 0),
            new TrackEnd("B", PikoAConstants.StandardAngle)
        ],
        new TrackGeometrySpec(
            TrackGeometryKind.Curve,
            RadiusMm: PikoAConstants.R3,
            AngleDeg: PikoAConstants.StandardAngle
        ),
        null
    );

    /// <summary>
    /// Piko 55219 - R9 Kurve mit 15° Winkel.
    /// 24 Stück ergeben einen vollen Kreis (24 × 15° = 360°).
    /// </summary>
    public static TrackTemplate R9 => new(
        "R9",
        [
            new TrackEnd("A", 0),
            new TrackEnd("B", PikoAConstants.R9Angle)
        ],
        new TrackGeometrySpec(
            TrackGeometryKind.Curve,
            RadiusMm: PikoAConstants.R9,
            AngleDeg: PikoAConstants.R9Angle
        ),
        null
    );

    public static IReadOnlyList<TrackTemplate> All =>
    [
        R1,
        R2,
        R3,
        R9
    ];
}