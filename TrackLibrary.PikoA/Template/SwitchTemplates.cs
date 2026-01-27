namespace Moba.TrackLibrary.PikoA.Template;

using Moba.TrackLibrary.PikoA.Metadata;
using Moba.TrackPlan.TrackSystem;

public static class SwitchTemplates
{
    /// <summary>
    /// Piko A - Einfache Weiche Links (WL).
    /// 3 Ports: A (Eingang), B (gerade), C (links +15°)
    /// </summary>
    public static TrackTemplate WL => new(
        "WL",
        [
            new TrackEnd("A", 0),
            new TrackEnd("B", 0),
            new TrackEnd("C", +PikoAConstants.SwitchAngle)
        ],
        new TrackGeometrySpec(
            TrackGeometryKind.Switch,
            LengthMm: PikoAConstants.SwitchStraightLengthMm,
            RadiusMm: PikoAConstants.SwitchRadiusMm,
            AngleDeg: PikoAConstants.SwitchAngle,
            JunctionOffsetMm: PikoAConstants.SwitchJunctionOffsetMm
        ),
        new SwitchRoutingModel
        {
            InEndId = "A",
            StraightEndId = "B",
            DivergingEndId = "C"
        }
    );

    /// <summary>
    /// Piko A - Einfache Weiche Rechts (WR).
    /// 3 Ports: A (Eingang), B (gerade), C (rechts -15°)
    /// </summary>
    public static TrackTemplate WR => new(
        "WR",
        [
            new TrackEnd("A", 0),
            new TrackEnd("B", 0),
            new TrackEnd("C", -PikoAConstants.SwitchAngle)
        ],
        new TrackGeometrySpec(
            TrackGeometryKind.Switch,
            LengthMm: PikoAConstants.SwitchStraightLengthMm,
            RadiusMm: PikoAConstants.SwitchRadiusMm,
            AngleDeg: PikoAConstants.SwitchAngle,
            JunctionOffsetMm: PikoAConstants.SwitchJunctionOffsetMm
        ),
        new SwitchRoutingModel
        {
            InEndId = "A",
            StraightEndId = "B",
            DivergingEndId = "C"
        }
    );

    /// <summary>
    /// Piko A - Bogenweiche Links (BWL).
    /// 3 Ports: A (Eingang), B (gerade), C (links +15°)
    /// </summary>
    public static TrackTemplate BWL => new(
        "BWL",
        [
            new TrackEnd("A", 0),
            new TrackEnd("B", 0),
            new TrackEnd("C", +PikoAConstants.SwitchAngle)
        ],
        new TrackGeometrySpec(
            TrackGeometryKind.Switch,
            LengthMm: PikoAConstants.SwitchStraightLengthMm,
            RadiusMm: PikoAConstants.SwitchRadiusMm,
            AngleDeg: PikoAConstants.SwitchAngle,
            JunctionOffsetMm: PikoAConstants.SwitchJunctionOffsetMm
        ),
        new SwitchRoutingModel
        {
            InEndId = "A",
            StraightEndId = "B",
            DivergingEndId = "C"
        }
    );

    /// <summary>
    /// Piko A - Bogenweiche Rechts (BWR).
    /// 3 Ports: A (Eingang), B (gerade), C (rechts -15°)
    /// </summary>
    public static TrackTemplate BWR => new(
        "BWR",
        [
            new TrackEnd("A", 0),
            new TrackEnd("B", 0),
            new TrackEnd("C", -PikoAConstants.SwitchAngle)
        ],
        new TrackGeometrySpec(
            TrackGeometryKind.Switch,
            LengthMm: PikoAConstants.SwitchStraightLengthMm,
            RadiusMm: PikoAConstants.SwitchRadiusMm,
            AngleDeg: PikoAConstants.SwitchAngle,
            JunctionOffsetMm: PikoAConstants.SwitchJunctionOffsetMm
        ),
        new SwitchRoutingModel
        {
            InEndId = "A",
            StraightEndId = "B",
            DivergingEndId = "C"
        }
    );

    /// <summary>
    /// Piko 55222 - Bogenweiche Links R3 (BWL R3).
    /// 3 Ports: A (Eingang), B (gerade), C (links +15°)
    /// </summary>
    public static TrackTemplate BWL_R3 => new(
        "BWL R3",
        [
            new TrackEnd("A", 0),
            new TrackEnd("B", 0),
            new TrackEnd("C", +PikoAConstants.SwitchAngle)
        ],
        new TrackGeometrySpec(
            TrackGeometryKind.Switch,
            LengthMm: PikoAConstants.SwitchStraightLengthMm,
            RadiusMm: PikoAConstants.SwitchR3RadiusMm,
            AngleDeg: PikoAConstants.SwitchAngle,
            JunctionOffsetMm: PikoAConstants.SwitchJunctionOffsetMm
        ),
        new SwitchRoutingModel
        {
            InEndId = "A",
            StraightEndId = "B",
            DivergingEndId = "C"
        }
    );

    /// <summary>
    /// Piko 55223 - Bogenweiche Rechts R3 (BWR R3).
    /// 3 Ports: A (Eingang), B (gerade), C (rechts -15°)
    /// </summary>
    public static TrackTemplate BWR_R3 => new(
        "BWR R3",
        [
            new TrackEnd("A", 0),
            new TrackEnd("B", 0),
            new TrackEnd("C", -PikoAConstants.SwitchAngle)
        ],
        new TrackGeometrySpec(
            TrackGeometryKind.Switch,
            LengthMm: PikoAConstants.SwitchStraightLengthMm,
            RadiusMm: PikoAConstants.SwitchR3RadiusMm,
            AngleDeg: PikoAConstants.SwitchAngle,
            JunctionOffsetMm: PikoAConstants.SwitchJunctionOffsetMm
        ),
        new SwitchRoutingModel
        {
            InEndId = "A",
            StraightEndId = "B",
            DivergingEndId = "C"
        }
    );

    /// <summary>
    /// Piko 55224 - Dreiwegweiche (W3).
    /// 4 Ports: A (Eingang), B (gerade), C (links +15°), D (rechts -15°)
    /// </summary>
    public static TrackTemplate W3 => new(
        "W3",
        [
            new TrackEnd("A", 0),
            new TrackEnd("B", 0),
            new TrackEnd("C", +PikoAConstants.ThreeWaySwitchAngle),
            new TrackEnd("D", -PikoAConstants.ThreeWaySwitchAngle)
        ],
        new TrackGeometrySpec(
            TrackGeometryKind.ThreeWaySwitch,
            LengthMm: PikoAConstants.ThreeWaySwitchLengthMm,
            RadiusMm: PikoAConstants.ThreeWaySwitchRadiusMm,
            AngleDeg: PikoAConstants.ThreeWaySwitchAngle,
            JunctionOffsetMm: 0
        ),
        null
    );

    /// <summary>
    /// Piko 55240 - Kreuzung 30° (K30).
    /// 4 Ports: A (0°), B (180°), C (30°), D (210°)
    /// </summary>
    public static TrackTemplate K30 => new(
        "K30",
        [
            new TrackEnd("A", 0),
            new TrackEnd("B", 180),
            new TrackEnd("C", PikoAConstants.K30Angle),
            new TrackEnd("D", 180 + PikoAConstants.K30Angle)
        ],
        new TrackGeometrySpec(
            TrackGeometryKind.Crossing,
            LengthMm: PikoAConstants.K30LengthMm,
            AngleDeg: PikoAConstants.K30Angle
        ),
        null
    );

    /// <summary>
    /// Piko 55245 - Kreuzung 10° (K10).
    /// 4 Ports: A (0°), B (180°), C (10°), D (190°)
    /// </summary>
    public static TrackTemplate K10 => new(
        "K10",
        [
            new TrackEnd("A", 0),
            new TrackEnd("B", 180),
            new TrackEnd("C", PikoAConstants.K10Angle),
            new TrackEnd("D", 180 + PikoAConstants.K10Angle)
        ],
        new TrackGeometrySpec(
            TrackGeometryKind.Crossing,
            LengthMm: PikoAConstants.K10LengthMm,
            AngleDeg: PikoAConstants.K10Angle
        ),
        null
    );

    /// <summary>
    /// Piko 55241 - Doppelkreuzungsweiche (DKW).
    /// 4 Ports: A, B, C, D
    /// </summary>
    public static TrackTemplate DKW => new(
        "DKW",
        [
            new TrackEnd("A", 0),
            new TrackEnd("B", 180),
            new TrackEnd("C", PikoAConstants.DKWAngle),
            new TrackEnd("D", 180 + PikoAConstants.DKWAngle)
        ],
        new TrackGeometrySpec(
            TrackGeometryKind.DoubleCrossover,
            LengthMm: PikoAConstants.DKWLengthMm,
            AngleDeg: PikoAConstants.DKWAngle
        ),
        null
    );

    /// <summary>
    /// Piko 55242 - Doppelkreuzungsweiche 3 (DKW3).
    /// 4 Ports: A, B, C, D
    /// </summary>
    public static TrackTemplate DKW3 => new(
        "DKW3",
        [
            new TrackEnd("A", 0),
            new TrackEnd("B", 180),
            new TrackEnd("C", PikoAConstants.DKW3Angle),
            new TrackEnd("D", 180 + PikoAConstants.DKW3Angle)
        ],
        new TrackGeometrySpec(
            TrackGeometryKind.DoubleCrossover,
            LengthMm: PikoAConstants.DKW3LengthMm,
            AngleDeg: PikoAConstants.DKW3Angle
        ),
        null
    );

    public static IReadOnlyList<TrackTemplate> All =>
    [
        WL,
        WR,
        BWL,
        BWR,
        BWL_R3,
        BWR_R3,
        W3,
        K30,
        K10,
        DKW,
        DKW3
    ];
}
