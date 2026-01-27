namespace Moba.TrackLibrary.PikoA.Template;

using Moba.TrackLibrary.PikoA.Metadata;
using Moba.TrackPlan.TrackSystem;

public static class StraightTemplates
{
    public static TrackTemplate G940 => new(
        "G940",
        [
            new TrackEnd("A", 0),
            new TrackEnd("B", 0)
        ],
        new TrackGeometrySpec(
            TrackGeometryKind.Straight,
            LengthMm: PikoAConstants.G940Length
        ),
        null
    );

    public static TrackTemplate G239 => new(
        "G239",
        [
            new TrackEnd("A", 0),
            new TrackEnd("B", 0)
        ],
        new TrackGeometrySpec(
            TrackGeometryKind.Straight,
            LengthMm: PikoAConstants.G239Length
        ),
        null
    );

    public static TrackTemplate G231 => new(
        "G231",
        [
            new TrackEnd("A", 0),
            new TrackEnd("B", 0)
        ],
        new TrackGeometrySpec(
            TrackGeometryKind.Straight,
            LengthMm: PikoAConstants.G231Length
        ),
        null
    );

    public static TrackTemplate G119 => new(
        "G119",
        [
            new TrackEnd("A", 0),
            new TrackEnd("B", 0)
        ],
        new TrackGeometrySpec(
            TrackGeometryKind.Straight,
            LengthMm: PikoAConstants.G119Length
        ),
        null
    );

    public static TrackTemplate G115 => new(
        "G115",
        [
            new TrackEnd("A", 0),
            new TrackEnd("B", 0)
        ],
        new TrackGeometrySpec(
            TrackGeometryKind.Straight,
            LengthMm: PikoAConstants.G115Length
        ),
        null
    );

    public static TrackTemplate G107 => new(
        "G107",
        [
            new TrackEnd("A", 0),
            new TrackEnd("B", 0)
        ],
        new TrackGeometrySpec(
            TrackGeometryKind.Straight,
            LengthMm: PikoAConstants.G107Length
        ),
        null
    );

    public static TrackTemplate G62 => new(
        "G62",
        [
            new TrackEnd("A", 0),
            new TrackEnd("B", 0)
        ],
        new TrackGeometrySpec(
            TrackGeometryKind.Straight,
            LengthMm: PikoAConstants.G62Length
        ),
        null
    );

    public static TrackTemplate G56 => new(
        "G56",
        [
            new TrackEnd("A", 0),
            new TrackEnd("B", 0)
        ],
        new TrackGeometrySpec(
            TrackGeometryKind.Straight,
            LengthMm: PikoAConstants.G56Length
        ),
        null
    );

    public static TrackTemplate G31 => new(
        "G31",
        [
            new TrackEnd("A", 0),
            new TrackEnd("B", 0)
        ],
        new TrackGeometrySpec(
            TrackGeometryKind.Straight,
            LengthMm: PikoAConstants.G31Length
        ),
        null
    );

    /// <summary>
    /// Piko 55620 - Anschlussgleis 37.5mm
    /// </summary>
    public static TrackTemplate G55620 => new(
        "55620",
        [
            new TrackEnd("A", 0),
            new TrackEnd("B", 0)
        ],
        new TrackGeometrySpec(
            TrackGeometryKind.Straight,
            LengthMm: PikoAConstants.G55620Length
        ),
        null
    );

    /// <summary>
    /// Piko 55621 - Anschlussgleis 77.5mm
    /// </summary>
    public static TrackTemplate G55621 => new(
        "55621",
        [
            new TrackEnd("A", 0),
            new TrackEnd("B", 0)
        ],
        new TrackGeometrySpec(
            TrackGeometryKind.Straight,
            LengthMm: PikoAConstants.G55621Length
        ),
        null
    );

    public static IReadOnlyList<TrackTemplate> All =>
    [
        G940,
        G239,
        G231,
        G119,
        G115,
        G107,
        G62,
        G56,
        G31,
        G55620,
        G55621
    ];
}