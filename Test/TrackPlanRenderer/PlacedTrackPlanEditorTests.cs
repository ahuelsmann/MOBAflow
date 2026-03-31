namespace Moba.Test.TrackPlanRenderer;

using TrackLibrary.PikoA;
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
