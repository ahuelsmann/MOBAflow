// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.TrackLibrary.PikoA;

using Moba.TrackLibrary.PikoA;

/// <summary>
/// Tests for <see cref="EditableTrackPlan"/> graph operations: grouping, feedback lookup and segment lifecycle.
/// </summary>
[TestFixture]
internal sealed class EditableTrackPlanOperationsTests
{
    [Test]
    public void AddSegment_DuplicateSegmentNo_IsIgnored()
    {
        var plan = new EditableTrackPlan();
        var segment = new G62();
        plan.AddSegment(new PlacedSegment(segment, 0, 0, 0));
        plan.AddSegment(new PlacedSegment(segment, 50, 0, 0));

        Assert.That(plan.Segments, Has.Count.EqualTo(1));
    }

    [Test]
    public void RemoveSegment_RemovesConnectionsAndSegment()
    {
        var plan = new EditableTrackPlan();
        var first = new PlacedSegment(new G62(), 0, 0, 0);
        var second = new PlacedSegment(new R9(), 70, 0, 0);
        plan.AddSegment(first);
        plan.AddSegment(second);
        plan.AddConnection(first.Segment.No, "PortB", second.Segment.No, "PortA");

        plan.RemoveSegment(first.Segment.No);

        Assert.Multiple(() =>
        {
            Assert.That(plan.Segments, Has.Count.EqualTo(1));
            Assert.That(plan.Connections, Is.Empty);
        });
    }

    [Test]
    public void GetSegmentIdsByInPort_ReturnsMatchingPlacements()
    {
        var plan = new EditableTrackPlan();
        var withPort = new PlacedSegment(new G62(), 0, 0, 0, InPort: 12);
        var withoutPort = new PlacedSegment(new R9(), 70, 0, 0);
        plan.AddSegment(withPort);
        plan.AddSegment(withoutPort);

        var ids = plan.GetSegmentIdsByInPort(12).ToList();

        Assert.That(ids, Is.EqualTo(new[] { withPort.Segment.No }));
    }

    [Test]
    public void GetConnectedGroup_ReturnsTransitiveSegmentSet()
    {
        var plan = new EditableTrackPlan();
        var a = new PlacedSegment(new G62(), 0, 0, 0);
        var b = new PlacedSegment(new G62(), 70, 0, 0);
        var c = new PlacedSegment(new R9(), 140, 0, 0);
        plan.AddSegment(a);
        plan.AddSegment(b);
        plan.AddSegment(c);
        plan.AddConnection(a.Segment.No, "PortB", b.Segment.No, "PortA");
        plan.AddConnection(b.Segment.No, "PortB", c.Segment.No, "PortA");

        var group = plan.GetConnectedGroup(a.Segment.No);

        Assert.That(group, Is.EquivalentTo(new[] { a.Segment.No, b.Segment.No, c.Segment.No }));
    }

    [Test]
    public void MoveGroup_TranslatesOnlySelectedSegments()
    {
        var plan = new EditableTrackPlan();
        var moved = new PlacedSegment(new G62(), 0, 0, 0);
        var fixedSegment = new PlacedSegment(new R9(), 100, 0, 0);
        plan.AddSegment(moved);
        plan.AddSegment(fixedSegment);

        plan.MoveGroup(new HashSet<Guid> { moved.Segment.No }, deltaX: 5, deltaY: -2);

        Assert.Multiple(() =>
        {
            Assert.That(plan.Segments.First(s => s.Segment.No == moved.Segment.No).X, Is.EqualTo(5));
            Assert.That(plan.Segments.First(s => s.Segment.No == moved.Segment.No).Y, Is.EqualTo(-2));
            Assert.That(plan.Segments.First(s => s.Segment.No == fixedSegment.Segment.No).X, Is.EqualTo(100));
        });
    }
}
