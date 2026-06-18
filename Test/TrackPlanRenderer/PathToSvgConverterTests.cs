// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.TrackPlanRenderer;

using Moba.TrackLibrary.PikoA;

using TrackPlan.Renderer;

/// <summary>
/// Tests for <see cref="PathToSvgConverter"/> coordinate transforms used by the track-plan SVG renderer.
/// </summary>
[TestFixture]
internal sealed class PathToSvgConverterTests
{
    [Test]
    public void ToSvgPath_LineSegment_TranslatesToWorldCoordinates()
    {
        IReadOnlyList<SegmentLocalPathBuilder.PathCommand> commands =
        [
            new SegmentLocalPathBuilder.MoveTo(0, 0),
            new SegmentLocalPathBuilder.LineTo(10, 0)
        ];

        var path = PathToSvgConverter.ToSvgPath(commands, originX: 5, originY: 7, angleDegrees: 0);

        Assert.That(path, Is.EqualTo("M 5.00,7.00 L 15.00,7.00"));
    }

    [Test]
    public void ToSvgPath_RotatedLine_AppliesRotationAroundOrigin()
    {
        IReadOnlyList<SegmentLocalPathBuilder.PathCommand> commands =
        [
            new SegmentLocalPathBuilder.MoveTo(0, 0),
            new SegmentLocalPathBuilder.LineTo(10, 0)
        ];

        var path = PathToSvgConverter.ToSvgPath(commands, originX: 0, originY: 0, angleDegrees: 90);

        Assert.That(path, Is.EqualTo("M 0.00,0.00 L 0.00,10.00"));
    }

    [Test]
    public void ToSvgPath_ArcSegment_EmitsSvgArcCommand()
    {
        IReadOnlyList<SegmentLocalPathBuilder.PathCommand> commands =
        [
            new SegmentLocalPathBuilder.MoveTo(0, 0),
            new SegmentLocalPathBuilder.ArcTo(10, 0, 5, Clockwise: true, LargeArc: false)
        ];

        var path = PathToSvgConverter.ToSvgPath(commands, 0, 0, 0);

        Assert.That(path, Does.Contain(" A 5.00,5.00 0 0,1 10.00,0.00"));
    }

    [Test]
    public void ToPathDataString_AppliesScaleAndOffset()
    {
        IReadOnlyList<SegmentLocalPathBuilder.PathCommand> commands =
        [
            new SegmentLocalPathBuilder.MoveTo(0, 0),
            new SegmentLocalPathBuilder.LineTo(10, 0)
        ];

        var path = PathToSvgConverter.ToPathDataString(commands, scale: 2, offsetX: 1, offsetY: 3);

        Assert.That(path, Is.EqualTo("M 2.00,6.00 L 22.00,6.00"));
    }
}