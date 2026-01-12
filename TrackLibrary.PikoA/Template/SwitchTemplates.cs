namespace Moba.TrackLibrary.PikoA.Template;

using Moba.TrackLibrary.PikoA.Metadata;
using Moba.TrackPlan.TrackSystem;

public static class SwitchTemplates
{
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

    public static IReadOnlyList<TrackTemplate> All =>
    [
        BWL,
        BWR,
        K30
    ];
}
