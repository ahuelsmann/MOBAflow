// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.TrackPlanRenderer;

using Moba.Domain;

using Moba.TrackLibrary.PikoA;

using TrackPlan.Renderer;

[TestFixture]
internal class PlacedTrackPlanEditorTests
{
    [Test]
    public void TrackPlanEditorDocument_RoundTrips_Placements_Connections_AndViewport()
    {
        var plan = new EditableTrackPlan();
        var first = new PlacedSegment(new G62(), 10, 20, 0);
        var second = new PlacedSegment(new R9(), 71.88, 20, 15);

        plan.AddSegment(first);
        plan.AddSegment(second);
        plan.AddConnection(first.Segment.No, "PortB", second.Segment.No, "PortA");

        var document = TrackPlanEditorDocument.FromEditableTrackPlan(plan, 125, 75, 1.5);
        var (placements, connections) = document.ToEditableTrackPlanData();

        Assert.That(document.OffsetX, Is.EqualTo(125));
        Assert.That(document.OffsetY, Is.EqualTo(75));
        Assert.That(document.ZoomFactor, Is.EqualTo(1.5));
        Assert.That(placements, Has.Count.EqualTo(2));
        Assert.That(connections, Has.Count.EqualTo(1));
        Assert.That(placements.Select(p => p.Segment.No), Is.EquivalentTo(new[] { first.Segment.No, second.Segment.No }));
        Assert.That(placements.Select(p => p.Segment.GetType()), Is.EquivalentTo(new[] { typeof(G62), typeof(R9) }));
        Assert.That(placements.First(p => p.Segment.No == first.Segment.No).X, Is.EqualTo(10).Within(0.001));
        Assert.That(placements.First(p => p.Segment.No == second.Segment.No).RotationDegrees, Is.EqualTo(15).Within(0.001));
        Assert.That(connections[0].SourceSegment, Is.EqualTo(first.Segment.No));
        Assert.That(connections[0].TargetSegment, Is.EqualTo(second.Segment.No));
    }

    [Test]
    public void PlacedSegment_WithInPort_PreservesPositionAndRotation()
    {
        var original = new PlacedSegment(new G62(), 10, 20, 30);

        var updated = original.WithInPort(42);

        Assert.That(updated.Segment, Is.SameAs(original.Segment));
        Assert.That(updated.X, Is.EqualTo(10));
        Assert.That(updated.Y, Is.EqualTo(20));
        Assert.That(updated.RotationDegrees, Is.EqualTo(30));
        Assert.That(updated.InPort, Is.EqualTo(42));
        Assert.That(original.InPort, Is.Null);
    }

    [Test]
    public void EditableTrackPlan_UpdateSegmentInPort_ChangesValueAndRaisesPlanChanged()
    {
        var plan = new EditableTrackPlan();
        var placed = new PlacedSegment(new G62(), 0, 0, 0);
        plan.AddSegment(placed);

        var planChangedCount = 0;
        plan.PlanChanged += (_, _) => planChangedCount++;

        plan.UpdateSegmentInPort(placed.Segment.No, 17);

        Assert.That(plan.Segments.Single().InPort, Is.EqualTo(17));
        Assert.That(planChangedCount, Is.EqualTo(1));

        // Setting the same value must not re-fire the event
        plan.UpdateSegmentInPort(placed.Segment.No, 17);
        Assert.That(planChangedCount, Is.EqualTo(1));

        // Clearing the InPort
        plan.UpdateSegmentInPort(placed.Segment.No, null);
        Assert.That(plan.Segments.Single().InPort, Is.Null);
        Assert.That(planChangedCount, Is.EqualTo(2));
    }

    [Test]
    public void TrackPlanEditorDocument_RoundTrips_InPort()
    {
        var plan = new EditableTrackPlan();
        var first = new PlacedSegment(new G62(), 10, 20, 0, InPort: 12);
        var second = new PlacedSegment(new R9(), 71.88, 20, 15);

        plan.AddSegment(first);
        plan.AddSegment(second);

        var document = TrackPlanEditorDocument.FromEditableTrackPlan(plan);
        var (placements, _) = document.ToEditableTrackPlanData();

        Assert.That(placements.First(p => p.Segment.No == first.Segment.No).InPort, Is.EqualTo(12));
        Assert.That(placements.First(p => p.Segment.No == second.Segment.No).InPort, Is.Null);
    }

    [Test]
    public void TrackPlanEditorDocument_RoundTrips_Through_DomainDocument()
    {
        var plan = new EditableTrackPlan();
        var first = new PlacedSegment(new G62(), 10, 20, 0, InPort: 12);
        var second = new PlacedSegment(new R9(), 71.88, 20, 15);
        plan.AddSegment(first);
        plan.AddSegment(second);
        plan.AddConnection(first.Segment.No, "PortB", second.Segment.No, "PortA");

        var editorDoc = TrackPlanEditorDocument.FromEditableTrackPlan(plan, 125, 75, 1.5);
        TrackPlanDocument domainDoc = editorDoc.ToDomainDocument();
        TrackPlanEditorDocument restored = TrackPlanEditorDocument.FromDomainDocument(domainDoc);

        Assert.That(restored.OffsetX, Is.EqualTo(125));
        Assert.That(restored.OffsetY, Is.EqualTo(75));
        Assert.That(restored.ZoomFactor, Is.EqualTo(1.5));
        Assert.That(restored.Segments, Has.Count.EqualTo(2));
        Assert.That(restored.Connections, Has.Count.EqualTo(1));
        Assert.That(restored.Segments.First(s => s.Id == first.Segment.No).InPort, Is.EqualTo(12));
        Assert.That(restored.Segments.First(s => s.Id == second.Segment.No).InPort, Is.Null);
        Assert.That(restored.Connections[0].SourceSegment, Is.EqualTo(first.Segment.No));
        Assert.That(restored.Connections[0].TargetPort, Is.EqualTo("PortA"));
    }

    [Test]
    public void PlacedTrackPlanSvgRenderer_Renders_CurrentPlacement_WithGrid_AndPorts()
    {
        var placements = new List<PlacedSegment>
        {
            new(new G62(), 0, 0, 0),
            new(new WR(), 100, 40, 15)
        };

        var svg = new PlacedTrackPlanSvgRenderer().Render(placements, trackOpacity: 0.65, showGrid: true, showPorts: true);

        Assert.That(svg, Does.Contain("<svg"));
        Assert.That(svg, Does.Contain("stroke-opacity=\"0.65\""));
        Assert.That(svg, Does.Contain("<line"));
        Assert.That(svg, Does.Contain("<circle"));
        Assert.That(svg, Does.Contain("<text"));
    }
}