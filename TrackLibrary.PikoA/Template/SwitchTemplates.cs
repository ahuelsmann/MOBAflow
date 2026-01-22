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

    public static TrackTemplate K30 => new(
        "K30",
        [
            new TrackEnd("A", 0),
            new TrackEnd("B", 180),
            new TrackEnd("C", 30),
            new TrackEnd("D", 210)
        ],
        new TrackGeometrySpec(
            TrackGeometryKind.Straight,
            AngleDeg: PikoAConstants.StandardAngle
        ),
        null
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
        null  // W3 hat kein einfaches Routing-Modell
    );

    public static IReadOnlyList<TrackTemplate> All =>
    [
        WL,
        WR,
        BWL,
        BWR,
        K30,
        W3
    ];
}
