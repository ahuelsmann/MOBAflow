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
    /// Piko 55214 - R4 Kurve mit 30° Winkel.
    /// </summary>
    public static TrackTemplate R4 => new(
        "R4",
        [
            new TrackEnd("A", 0),
            new TrackEnd("B", PikoAConstants.StandardAngle)
        ],
        new TrackGeometrySpec(
            TrackGeometryKind.Curve,
            RadiusMm: PikoAConstants.R4,
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

    /// <summary>
    /// Piko 55290 - R1 Prellbock (Endstück mit 7.5°).
    /// </summary>
    public static TrackTemplate R1X => new(
        "R1X",
        [
            new TrackEnd("A", 0)
            // Nur ein Ende - Prellbock
        ],
        new TrackGeometrySpec(
            TrackGeometryKind.Endcap,
            RadiusMm: PikoAConstants.R1X,
            AngleDeg: PikoAConstants.EndcapAngle
        ),
        null
    );

    /// <summary>
    /// Piko 55291 - R2 Prellbock (Endstück mit 7.5°).
    /// </summary>
    public static TrackTemplate R2X => new(
        "R2X",
        [
            new TrackEnd("A", 0)
            // Nur ein Ende - Prellbock
        ],
        new TrackGeometrySpec(
            TrackGeometryKind.Endcap,
            RadiusMm: PikoAConstants.R2X,
            AngleDeg: PikoAConstants.EndcapAngle
        ),
        null
    );

    public static IReadOnlyList<TrackTemplate> All =>
    [
        R1,
        R2,
        R3,
        R4,
        R9,
        R1X,
        R2X
    ];
}