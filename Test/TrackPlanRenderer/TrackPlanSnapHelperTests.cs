// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.TrackPlanRenderer;

using Moba.TrackLibrary.PikoA;

[TestFixture]
internal class TrackPlanSnapHelperTests
{
    [Test]
    public void FindBestSnap_AlignsIsolatedSegmentToFreeTargetPort()
    {
        var target = new PlacedSegment(new G62(), 0, 0, 0);
        var moving = new PlacedSegment(new G62(), 61.88, 5, 37);

        var snap = TrackPlanSnapHelper.FindBestSnap(moving, new[] { target }, Array.Empty<PortConnection>());

        Assert.That(snap, Is.Not.Null);
        Assert.That(snap!.SourcePort, Is.EqualTo("PortA"));
        Assert.That(snap.TargetPort, Is.EqualTo("PortB"));

        var sourcePort = SegmentPortGeometry.GetPortWorldPosition(snap.Placed, snap.SourcePort);
        var targetPort = SegmentPortGeometry.GetPortWorldPosition(target, snap.TargetPort);
        Assert.That(sourcePort.X, Is.EqualTo(targetPort.X).Within(0.001));
        Assert.That(sourcePort.Y, Is.EqualTo(targetPort.Y).Within(0.001));

        var sourceOutwardAngle = SegmentPortGeometry.GetPortOutwardWorldAngleDegrees(snap.Placed, snap.SourcePort);
        var targetOutwardAngle = SegmentPortGeometry.GetPortOutwardWorldAngleDegrees(target, snap.TargetPort);
        Assert.That(GetAngleDeltaDegrees(sourceOutwardAngle, targetOutwardAngle), Is.EqualTo(180).Within(0.001));
    }

    [Test]
    public void FindBestSnap_IgnoresOccupiedTargetPorts()
    {
        var target = new PlacedSegment(new G62(), 0, 0, 0);
        var moving = new PlacedSegment(new G62(), 61.88, 5, 0);
        var connections = new[]
        {
            new PortConnection(target.Segment.No, "PortB", Guid.NewGuid(), "PortA")
        };

        var snap = TrackPlanSnapHelper.FindBestSnap(moving, new[] { target }, connections);

        Assert.That(snap, Is.Null);
    }

    [Test]
    public void FindBestSnap_IgnoresOccupiedSourcePorts()
    {
        var target = new PlacedSegment(new G62(), 0, 0, 0);
        var moving = new PlacedSegment(new G62(), 61.88, 5, 0);
        var connections = new[]
        {
            new PortConnection(moving.Segment.No, "PortA", Guid.NewGuid(), "PortA")
        };

        var snap = TrackPlanSnapHelper.FindBestSnap(moving, new[] { target }, connections);

        Assert.That(snap, Is.Null);
    }

    [Test]
    public void FindBestSnap_KeepsRotationForRigidDraggingGroups()
    {
        var target = new PlacedSegment(new G62(), 0, 0, 0);
        var moving = new PlacedSegment(new G62(), 61.88, 4, 0);
        var partner = new PlacedSegment(new G62(), 123.76, 4, 0);
        var connections = new[]
        {
            new PortConnection(moving.Segment.No, "PortB", partner.Segment.No, "PortA")
        };
        var movingGroup = new HashSet<Guid> { moving.Segment.No, partner.Segment.No };

        var snap = TrackPlanSnapHelper.FindBestSnap(moving, new[] { target, partner }, connections, movingGroup: movingGroup);

        Assert.That(snap, Is.Not.Null);
        Assert.That(snap!.SourcePort, Is.EqualTo("PortA"));
        Assert.That(snap.TargetPort, Is.EqualTo("PortB"));
        Assert.That(snap.Placed.RotationDegrees, Is.EqualTo(moving.RotationDegrees).Within(0.001));
        Assert.That(snap.Placed.X, Is.EqualTo(61.88).Within(0.001));
        Assert.That(snap.Placed.Y, Is.EqualTo(0).Within(0.001));
    }

    [Test]
    public void FindBestSnap_RejectsRigidDraggingGroup_WhenRotationWouldBeRequired()
    {
        var target = new PlacedSegment(new G62(), 61.88, 0, 90);
        var moving = new PlacedSegment(new G62(), 61.88, 4, 0);
        var partner = new PlacedSegment(new G62(), 123.76, 4, 0);
        var connections = new[]
        {
            new PortConnection(moving.Segment.No, "PortB", partner.Segment.No, "PortA")
        };
        var movingGroup = new HashSet<Guid> { moving.Segment.No, partner.Segment.No };

        var snap = TrackPlanSnapHelper.FindBestSnap(moving, new[] { target, partner }, connections, movingGroup: movingGroup);

        Assert.That(snap, Is.Null);
    }

    private static double GetAngleDeltaDegrees(double leftDegrees, double rightDegrees)
    {
        var delta = NormalizeAngle(leftDegrees - rightDegrees);
        if (delta > 180)
            delta = 360 - delta;
        return Math.Abs(delta);
    }

    private static double NormalizeAngle(double degrees)
    {
        while (degrees >= 360)
            degrees -= 360;
        while (degrees < 0)
            degrees += 360;
        return degrees;
    }
}
