// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.TrackPlanRenderer;

using Moba.Test.Helpers;
using Moba.TrackLibrary.PikoA;
using Moba.TrackPlan.Editor;

/// <summary>
/// World-space helpers of <see cref="SegmentPlacementGeometry"/> used by the editor, validation and the plan page.
/// </summary>
[TestFixture]
internal sealed class SegmentPlacementGeometryTests
{
    private const double StraightLengthMm = 230.93;
    private const double Tolerance = 1e-6;

    private static readonly string[] OnlyPortB = ["PortB"];

    private static SegmentPlacementGeometry Geometry => TrackPlanTestGeometry.PikoA;

    [Test]
    public void GetWorldBounds_RotatesTheDrawnPathAroundTheOrigin()
    {
        var straight = new PlacedSegment(new G231 { No = Guid.NewGuid() }, 100, 50, 90);

        var (minX, minY, maxX, maxY) = Geometry.GetWorldBounds(straight);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(minX, Is.EqualTo(100).Within(Tolerance));
            Assert.That(maxX, Is.EqualTo(100).Within(Tolerance));
            Assert.That(minY, Is.EqualTo(50).Within(Tolerance));
            Assert.That(maxY, Is.EqualTo(50 + StraightLengthMm).Within(Tolerance));
        }
    }

    [Test]
    public void GetContentBounds_IsNullWithoutPlacementsAndEnclosesAllPlacements()
    {
        var first = new PlacedSegment(new G231 { No = Guid.NewGuid() }, 0, 0, 0);
        var second = new PlacedSegment(new G231 { No = Guid.NewGuid() }, -40, 300, 90);

        var bounds = Geometry.GetContentBounds((PlacedSegment[])[first, second]);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(Geometry.GetContentBounds([]), Is.Null);
            Assert.That(bounds, Is.Not.Null);
            Assert.That(bounds!.Value.MinX, Is.EqualTo(-40).Within(Tolerance));
            Assert.That(bounds.Value.MinY, Is.EqualTo(0).Within(Tolerance));
            Assert.That(bounds.Value.MaxX, Is.EqualTo(StraightLengthMm).Within(Tolerance));
            Assert.That(bounds.Value.MaxY, Is.EqualTo(300 + StraightLengthMm).Within(Tolerance));
        }
    }

    [Test]
    public void GetPortCentroid_OfAStraightIsItsMidpoint()
    {
        var straight = new PlacedSegment(new G231 { No = Guid.NewGuid() }, 10, 20, 0);

        var (x, y) = Geometry.GetPortCentroid(straight);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(x, Is.EqualTo(10 + StraightLengthMm / 2).Within(Tolerance));
            Assert.That(y, Is.EqualTo(20).Within(Tolerance));
        }
    }

    [Test]
    public void GetOpenPortWorldPositions_SkipsConnectedPortsOfTheSegment()
    {
        var straight = new PlacedSegment(new G231 { No = Guid.NewGuid() }, 0, 0, 0);
        var other = Guid.NewGuid();
        PortConnection[] connections = [new(other, "PortB", straight.Segment.No, "PortA")];

        var open = Geometry.GetOpenPortWorldPositions(straight, connections);

        Assert.That(open.Select(port => port.PortName), Is.EqualTo(OnlyPortB));
    }

    [Test]
    public void IsConnected_MatchesSourceAndTargetEndsOnly()
    {
        var left = Guid.NewGuid();
        var right = Guid.NewGuid();
        PortConnection[] connections = [new(left, "PortB", right, "PortA")];

        using (Assert.EnterMultipleScope())
        {
            Assert.That(connections.IsConnected(left, "PortB"), Is.True);
            Assert.That(connections.IsConnected(right, "PortA"), Is.True);
            Assert.That(connections.IsConnected(left, "PortA"), Is.False);
            Assert.That(connections.IsConnected(Guid.NewGuid(), "PortB"), Is.False);
        }
    }

    [TestCase(0, 0)]
    [TestCase(360, 0)]
    [TestCase(-90, 270)]
    [TestCase(725, 5)]
    public void NormalizeAngle_MapsIntoZeroToThreeSixty(double degrees, double expected)
    {
        Assert.That(SegmentPlacementGeometry.NormalizeAngle(degrees), Is.EqualTo(expected).Within(Tolerance));
    }

    [TestCase(10, 350, 20)]
    [TestCase(0, 180, 180)]
    [TestCase(90, 90, 0)]
    public void AngleDeltaDegrees_ReturnsTheShortestDifference(double left, double right, double expected)
    {
        Assert.That(SegmentPlacementGeometry.AngleDeltaDegrees(left, right), Is.EqualTo(expected).Within(Tolerance));
    }
}
