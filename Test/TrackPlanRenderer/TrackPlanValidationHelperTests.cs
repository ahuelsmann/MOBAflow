// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.TrackPlanRenderer;

using Moba.TrackLibrary.PikoA;

[TestFixture]
internal class TrackPlanValidationHelperTests
{
    [Test]
    public void FindImplicitConnections_DetectsOverlappingOppositePorts()
    {
        // Two G62 straight segments placed end-to-end (PortB of left = PortA of right),
        // but no PortConnection has been recorded yet.
        var left = new PlacedSegment(new G62(), 0, 0, 0);
        var right = new PlacedSegment(new G62(), 61.88, 0, 0);

        var inferred = TrackPlanValidationHelper.FindImplicitConnections(
            new[] { left, right },
            Array.Empty<PortConnection>());

        Assert.That(inferred, Has.Count.EqualTo(1));
        var connection = inferred[0];
        Assert.That(
            new[] { (connection.SourceSegment, connection.SourcePort), (connection.TargetSegment, connection.TargetPort) },
            Is.EquivalentTo(new[]
            {
                (left.Segment.No, "PortB"),
                (right.Segment.No, "PortA")
            }));
    }

    [Test]
    public void FindImplicitConnections_IgnoresPortsFurtherThanThreshold()
    {
        var left = new PlacedSegment(new G62(), 0, 0, 0);
        var right = new PlacedSegment(new G62(), 61.88 + 5, 0, 0); // 5 mm gap

        var inferred = TrackPlanValidationHelper.FindImplicitConnections(
            new[] { left, right },
            Array.Empty<PortConnection>());

        Assert.That(inferred, Is.Empty);
    }

    [Test]
    public void FindImplicitConnections_IgnoresOverlappingPortsWithWrongAngle()
    {
        // Two G62 segments forming a T: `left` horizontal, `right` rotated 90° with its PortA
        // sitting exactly on `left.PortB`. Ports overlap geometrically but their outward angles
        // are perpendicular (0° vs 270°), so no implicit connection must be created.
        var left = new PlacedSegment(new G62(), 0, 0, 0);
        var right = new PlacedSegment(new G62(), 61.88, 0, 90);

        var inferred = TrackPlanValidationHelper.FindImplicitConnections(
            new[] { left, right },
            Array.Empty<PortConnection>());

        Assert.That(inferred, Is.Empty);
    }

    [Test]
    public void FindImplicitConnections_SkipsAlreadyConnectedPorts()
    {
        var left = new PlacedSegment(new G62(), 0, 0, 0);
        var right = new PlacedSegment(new G62(), 61.88, 0, 0);
        var existing = new[]
        {
            new PortConnection(left.Segment.No, "PortB", right.Segment.No, "PortA")
        };

        var inferred = TrackPlanValidationHelper.FindImplicitConnections(new[] { left, right }, existing);

        Assert.That(inferred, Is.Empty);
    }

    [Test]
    public void HealImplicitConnections_MovesOverlapFromValidationToConnections()
    {
        var plan = new EditableTrackPlan();
        plan.AddSegment(new PlacedSegment(new G62(), 0, 0, 0));
        plan.AddSegment(new PlacedSegment(new G62(), 61.88, 0, 0));

        var beforeAnalysis = TrackPlanValidationHelper.Analyze(plan.Segments, plan.Connections);
        Assert.That(beforeAnalysis.OverlappingPorts, Has.Count.EqualTo(1));

        var healed = plan.HealImplicitConnections();

        Assert.That(healed, Is.EqualTo(1));
        Assert.That(plan.Connections, Has.Count.EqualTo(1));

        var afterAnalysis = TrackPlanValidationHelper.Analyze(plan.Segments, plan.Connections);
        Assert.That(afterAnalysis.OverlappingPorts, Is.Empty);
        Assert.That(afterAnalysis.ConnectedGroups, Has.Count.EqualTo(1));
    }
}