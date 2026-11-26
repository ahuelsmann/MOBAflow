// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.TrackPlanRenderer;

using System.Globalization;
using System.Xml.Linq;

using Moba.Domain;

using Moba.TrackLibrary.PikoA;
using Moba.TrackPlan.Editor;
using Moba.Test.Helpers;

using TrackPlan.Renderer;

[TestFixture]
internal class PlacedTrackPlanEditorTests
{
    private static readonly XNamespace SvgNs = "http://www.w3.org/2000/svg";
    private static readonly string[] ExpectedVerticalGridX = ["-100.00", "0.00", "100.00", "200.00", "300.00", "400.00", "500.00"];
    private static readonly string[] ExpectedHorizontalGridY = ["-100.00", "0.00"];
    private static readonly string[] ExpectedPortColors = ["#000000", "#FF0000", "#00AA00", "#0000FF", "#666666"];
    private static readonly string[] ExpectedPortShortNames = ["A", "B", "C", "D", "X"];
    private static readonly string[] ExpectedEscapedTexts = ["Track <1> & \"Main\" 'A'", "&", "<", "Gap > 2mm & <open> \"Port\" 'B'"];
    private static readonly SegmentLocalPathBuilder.PathCommand[] HorizontalHundredMmPath =
    [
        new SegmentLocalPathBuilder.MoveTo(0, 0),
        new SegmentLocalPathBuilder.LineTo(100, 0)
    ];
    [Test]
    public void TrackPlanEditorDocument_RoundTrips_Placements_Connections_AndViewport()
    {
        var plan = new EditableTrackPlan();
        var first = new PlacedSegment(new G62(), 10, 20, 0);
        var second = new PlacedSegment(new R9(), 71.88, 20, 15);

        plan.AddSegment(first);
        plan.AddSegment(second);
        plan.AddConnection(first.Segment.No, "PortB", second.Segment.No, "PortA");

        var document = TrackPlanEditorDocument.FromEditableTrackPlan(TrackPlanTestGeometry.Catalog, plan, 125, 75, 1.5);
        var (placements, connections) = document.ToEditableTrackPlanData(TrackPlanTestGeometry.Catalog);

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

        var document = TrackPlanEditorDocument.FromEditableTrackPlan(TrackPlanTestGeometry.Catalog, plan);
        var (placements, _) = document.ToEditableTrackPlanData(TrackPlanTestGeometry.Catalog);

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

        var editorDoc = TrackPlanEditorDocument.FromEditableTrackPlan(TrackPlanTestGeometry.Catalog, plan, 125, 75, 1.5);
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

        var scene = TrackPlanRenderSceneBuilder.Build(TrackPlanTestGeometry.PikoA, placements, includePorts: true);

        var svg = new PlacedTrackPlanSvgRenderer().Render(scene, trackOpacity: 0.65, showGrid: true);

        Assert.That(svg, Does.Contain("<svg"));
        Assert.That(svg, Does.Contain("stroke-opacity=\"0.65\""));
        Assert.That(svg, Does.Contain("<line"));
        Assert.That(svg, Does.Contain("<circle"));
        Assert.That(svg, Does.Contain("<text"));
    }

    [Test]
    public void PlacedTrackPlanSvgRenderer_Renders_RenderSceneLabelsFeedbackAndValidationMarkers()
    {
        var placement = new PlacedSegment(new G62(), 0, 0, 0);
        var scene = TrackPlanRenderSceneBuilder.Build(
            TrackPlanTestGeometry.PikoA,
            (PlacedSegment[])[placement],
            new HashSet<Guid> { placement.Segment.No },
            new Dictionary<Guid, double> { [placement.Segment.No] = 0.5 },
            (TrackPlanValidationMarker[])[new TrackPlanValidationMarker(placement.Segment.No, 20, 10, "Open port")]);

        var svg = new PlacedTrackPlanSvgRenderer().Render(scene);

        Assert.Multiple(() =>
        {
            Assert.That(svg, Does.Contain("#0078D4"));
            Assert.That(svg, Does.Contain("#FFB400"));
            Assert.That(svg, Does.Contain("Open port"));
        });
    }

    [Test]
    public void PlacedTrackPlanSvgRenderer_Render_ThrowsOnNullScene()
    {
        var renderer = new PlacedTrackPlanSvgRenderer();

        var ex = Assert.Throws<ArgumentNullException>(() => renderer.Render(null!));

        Assert.That(ex!.ParamName, Is.EqualTo("scene"));
    }

    [Test]
    public void PlacedTrackPlanSvgRenderer_Render_EmptyScene_ReturnsMinimalSvgEvenWithGridAndMarkers()
    {
        var scene = new TrackPlanRenderScene(
            [],
            [new TrackPlanValidationMarker(null, 10, 20, "Orphan")],
            [new TrackPlanPortMarker(30, 40, "PortA")]);

        var svg = new PlacedTrackPlanSvgRenderer().Render(scene, showGrid: true);

        Assert.That(svg, Is.EqualTo("<svg></svg>"));
    }

    [Test]
    public void PlacedTrackPlanSvgRenderer_Render_ComputesViewBoxAndDimensionsWithFiftyMmMargin()
    {
        var scene = new TrackPlanRenderScene(
        [
            new TrackPlanRenderItem(Guid.NewGuid(), "G100", 10, 20, 0, HorizontalHundredMmPath)
        ]);

        var svg = new PlacedTrackPlanSvgRenderer().Render(scene);
        var root = XDocument.Parse(svg).Root!;
        var paths = root.Elements(SvgNs + "path").ToList();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(root.Name, Is.EqualTo(SvgNs + "svg"));
            Assert.That(root.Attribute("width")?.Value, Is.EqualTo("200.00"));
            Assert.That(root.Attribute("height")?.Value, Is.EqualTo("100.00"));
            Assert.That(root.Attribute("viewBox")?.Value, Is.EqualTo("-40.00 -30.00 200.00 100.00"));
            Assert.That(root.Elements(SvgNs + "line"), Is.Empty);
            Assert.That(root.Elements(SvgNs + "circle"), Is.Empty);
            Assert.That(root.Elements(SvgNs + "text"), Is.Empty);
            Assert.That(paths, Has.Count.EqualTo(1));
            Assert.That(paths[0].Attribute("d")?.Value, Is.EqualTo("M 10.00,20.00 L 110.00,20.00"));
            Assert.That(paths[0].Attribute("stroke")?.Value, Is.EqualTo("#333333"));
            Assert.That(paths[0].Attribute("stroke-width")?.Value, Is.EqualTo("4"));
            Assert.That(paths[0].Attribute("stroke-opacity")?.Value, Is.EqualTo("0.80"));
            Assert.That(paths[0].Attribute("fill")?.Value, Is.EqualTo("none"));
        }
    }

    [Test]
    public void PlacedTrackPlanSvgRenderer_Render_RotatedAndMultiItemScene_ExpandsViewBoxAroundTransformedBounds()
    {
        SegmentLocalPathBuilder.PathCommand[] asymmetricPath =
        [
            new SegmentLocalPathBuilder.MoveTo(0, 0),
            new SegmentLocalPathBuilder.LineTo(100, 0),
            new SegmentLocalPathBuilder.MoveTo(0, -20),
            new SegmentLocalPathBuilder.LineTo(50, 30)
        ];
        SegmentLocalPathBuilder.PathCommand[] shortPath =
        [
            new SegmentLocalPathBuilder.MoveTo(0, 0),
            new SegmentLocalPathBuilder.LineTo(40, 0)
        ];

        // Rotated 90 deg around (100, 200): local [0..100, -20..30] maps to world [70..120, 200..300].
        // Combined with second segment at [-10..30, 50..50] -> world bounds [-10..120, 50..300].
        var scene = new TrackPlanRenderScene(
        [
            new TrackPlanRenderItem(Guid.NewGuid(), "Turnout", 100, 200, 90, asymmetricPath),
            new TrackPlanRenderItem(Guid.NewGuid(), "G40", -10, 50, 0, shortPath)
        ]);

        var svg = new PlacedTrackPlanSvgRenderer().Render(scene);
        var root = XDocument.Parse(svg).Root!;
        var paths = root.Elements(SvgNs + "path").ToList();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(root.Attribute("width")?.Value, Is.EqualTo("230.00"));
            Assert.That(root.Attribute("height")?.Value, Is.EqualTo("350.00"));
            Assert.That(root.Attribute("viewBox")?.Value, Is.EqualTo("-60.00 0.00 230.00 350.00"));
            Assert.That(paths, Has.Count.EqualTo(2));
            Assert.That(paths[0].Attribute("d")?.Value, Is.EqualTo("M 100.00,200.00 L 100.00,300.00 M 120.00,200.00 L 70.00,250.00"));
            Assert.That(paths[1].Attribute("d")?.Value, Is.EqualTo("M -10.00,50.00 L 30.00,50.00"));
        }
    }

    [Test]
    public void PlacedTrackPlanSvgRenderer_Render_PortsAndValidationMarkersOutsideTrackBounds_ExpandViewBox()
    {
        var scene = new TrackPlanRenderScene(
            [new TrackPlanRenderItem(Guid.NewGuid(), "G100", 0, 0, 0, HorizontalHundredMmPath)],
            [new TrackPlanValidationMarker(null, -30, -40, "Warning")],
            [new TrackPlanPortMarker(150, 80, "PortB")]);

        var svg = new PlacedTrackPlanSvgRenderer().Render(scene);
        var root = XDocument.Parse(svg).Root!;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(root.Attribute("width")?.Value, Is.EqualTo("280.00"));
            Assert.That(root.Attribute("height")?.Value, Is.EqualTo("220.00"));
            Assert.That(root.Attribute("viewBox")?.Value, Is.EqualTo("-80.00 -90.00 280.00 220.00"));
        }
    }

    [Test]
    public void PlacedTrackPlanSvgRenderer_Render_GridLines_CoverViewBoxWithMajorAndMinorIntervals()
    {
        SegmentLocalPathBuilder.PathCommand[] fiveHundredPath =
        [
            new SegmentLocalPathBuilder.MoveTo(0, 0),
            new SegmentLocalPathBuilder.LineTo(500, 0)
        ];
        var scene = new TrackPlanRenderScene(
        [
            new TrackPlanRenderItem(Guid.NewGuid(), "G500", 0, 0, 0, fiveHundredPath)
        ]);

        var svg = new PlacedTrackPlanSvgRenderer().Render(scene, showGrid: true);
        var root = XDocument.Parse(svg).Root!;
        var lines = root.Elements(SvgNs + "line").ToList();
        var verticalLines = lines.Where(line => line.Attribute("x1")?.Value == line.Attribute("x2")?.Value).ToList();
        var horizontalLines = lines.Where(line => line.Attribute("y1")?.Value == line.Attribute("y2")?.Value).ToList();

        using (Assert.EnterMultipleScope())
        {
            // ViewBox is [-50..550, -50..50], so X grid lines are -100..500 (7) and Y grid lines are -100, 0 (2).
            Assert.That(lines, Has.Count.EqualTo(9));
            Assert.That(verticalLines, Has.Count.EqualTo(7));
            Assert.That(horizontalLines, Has.Count.EqualTo(2));

            Assert.That(
                verticalLines.Select(line => line.Attribute("x1")?.Value),
                Is.EqualTo(ExpectedVerticalGridX));
            Assert.That(verticalLines.Select(line => line.Attribute("y1")?.Value), Is.All.EqualTo("-50.00"));
            Assert.That(verticalLines.Select(line => line.Attribute("y2")?.Value), Is.All.EqualTo("50.00"));

            Assert.That(
                horizontalLines.Select(line => line.Attribute("y1")?.Value),
                Is.EqualTo(ExpectedHorizontalGridY));
            Assert.That(horizontalLines.Select(line => line.Attribute("x1")?.Value), Is.All.EqualTo("-50.00"));
            Assert.That(horizontalLines.Select(line => line.Attribute("x2")?.Value), Is.All.EqualTo("550.00"));

            var zeroVertical = verticalLines.Single(line => line.Attribute("x1")?.Value == "0.00");
            var fiveHundredVertical = verticalLines.Single(line => line.Attribute("x1")?.Value == "500.00");
            var hundredVertical = verticalLines.Single(line => line.Attribute("x1")?.Value == "100.00");
            var zeroHorizontal = horizontalLines.Single(line => line.Attribute("y1")?.Value == "0.00");
            var minusHundredHorizontal = horizontalLines.Single(line => line.Attribute("y1")?.Value == "-100.00");

            Assert.That(zeroVertical.Attribute("stroke")?.Value, Is.EqualTo("#D0D0D0"));
            Assert.That(zeroVertical.Attribute("stroke-width")?.Value, Is.EqualTo("1.5"));
            Assert.That(fiveHundredVertical.Attribute("stroke")?.Value, Is.EqualTo("#D0D0D0"));
            Assert.That(fiveHundredVertical.Attribute("stroke-width")?.Value, Is.EqualTo("1.5"));
            Assert.That(hundredVertical.Attribute("stroke")?.Value, Is.EqualTo("#ECECEC"));
            Assert.That(hundredVertical.Attribute("stroke-width")?.Value, Is.EqualTo("1"));
            Assert.That(zeroHorizontal.Attribute("stroke")?.Value, Is.EqualTo("#D0D0D0"));
            Assert.That(zeroHorizontal.Attribute("stroke-width")?.Value, Is.EqualTo("1.5"));
            Assert.That(minusHundredHorizontal.Attribute("stroke")?.Value, Is.EqualTo("#ECECEC"));
            Assert.That(minusHundredHorizontal.Attribute("stroke-width")?.Value, Is.EqualTo("1"));
        }
    }

    [Test]
    public void PlacedTrackPlanSvgRenderer_Render_SelectionFeedbackAndOpacity_EmitExpectedPathsAndStrokes()
    {
        var scene = new TrackPlanRenderScene(
        [
            new TrackPlanRenderItem(Guid.NewGuid(), "Plain", 0, 0, 0, HorizontalHundredMmPath, IsSelected: false, FeedbackIntensity: 0),
            new TrackPlanRenderItem(Guid.NewGuid(), "Selected", 0, 20, 0, HorizontalHundredMmPath, IsSelected: true, FeedbackIntensity: 0),
            new TrackPlanRenderItem(Guid.NewGuid(), "SelectedWithFeedback", 0, 40, 0, HorizontalHundredMmPath, IsSelected: true, FeedbackIntensity: 0.75),
            new TrackPlanRenderItem(Guid.NewGuid(), "NegativeFeedback", 0, 60, 0, HorizontalHundredMmPath, IsSelected: false, FeedbackIntensity: -0.25)
        ]);

        var svg = new PlacedTrackPlanSvgRenderer().Render(scene, trackOpacity: 0.456);
        var paths = XDocument.Parse(svg).Root!.Elements(SvgNs + "path").ToList();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(paths, Has.Count.EqualTo(5));

            // Plain unselected item
            Assert.That(paths[0].Attribute("stroke")?.Value, Is.EqualTo("#333333"));
            Assert.That(paths[0].Attribute("stroke-width")?.Value, Is.EqualTo("4"));
            Assert.That(paths[0].Attribute("stroke-opacity")?.Value, Is.EqualTo("0.46"));

            // Selected item without feedback
            Assert.That(paths[1].Attribute("stroke")?.Value, Is.EqualTo("#0078D4"));
            Assert.That(paths[1].Attribute("stroke-width")?.Value, Is.EqualTo("10"));
            Assert.That(paths[1].Attribute("stroke-opacity")?.Value, Is.EqualTo("0.46"));

            // Selected item with feedback: amber glow first, selection stroke second, identical geometry
            Assert.That(paths[2].Attribute("d")?.Value, Is.EqualTo("M 0.00,40.00 L 100.00,40.00"));
            Assert.That(paths[2].Attribute("stroke")?.Value, Is.EqualTo("#FFB400"));
            Assert.That(paths[2].Attribute("stroke-width")?.Value, Is.EqualTo("14"));
            Assert.That(paths[2].Attribute("stroke-opacity")?.Value, Is.EqualTo("0.75"));
            Assert.That(paths[2].Attribute("fill")?.Value, Is.EqualTo("none"));

            Assert.That(paths[3].Attribute("d")?.Value, Is.EqualTo("M 0.00,40.00 L 100.00,40.00"));
            Assert.That(paths[3].Attribute("stroke")?.Value, Is.EqualTo("#0078D4"));
            Assert.That(paths[3].Attribute("stroke-width")?.Value, Is.EqualTo("10"));
            Assert.That(paths[3].Attribute("stroke-opacity")?.Value, Is.EqualTo("0.46"));

            // Negative feedback emits no glow path
            Assert.That(paths[4].Attribute("stroke")?.Value, Is.EqualTo("#333333"));
            Assert.That(paths[4].Attribute("stroke-width")?.Value, Is.EqualTo("4"));
        }
    }

    [Test]
    public void PlacedTrackPlanSvgRenderer_Render_Labels_EmitPositionedTextOnlyForNonWhitespace()
    {
        var scene = new TrackPlanRenderScene(
        [
            new TrackPlanRenderItem(Guid.NewGuid(), "Station", 10, 40, 0, HorizontalHundredMmPath, Label: "Platform 1"),
            new TrackPlanRenderItem(Guid.NewGuid(), "Blank", 20, 60, 0, HorizontalHundredMmPath, Label: "   "),
            new TrackPlanRenderItem(Guid.NewGuid(), "Empty", 30, 80, 0, HorizontalHundredMmPath, Label: string.Empty),
            new TrackPlanRenderItem(Guid.NewGuid(), "NullLabel", 40, 100, 0, HorizontalHundredMmPath, Label: null)
        ]);

        var svg = new PlacedTrackPlanSvgRenderer().Render(scene);
        var texts = XDocument.Parse(svg).Root!.Elements(SvgNs + "text").ToList();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(texts, Has.Count.EqualTo(1));
            Assert.That(texts[0].Attribute("x")?.Value, Is.EqualTo("10.00"));
            Assert.That(texts[0].Attribute("y")?.Value, Is.EqualTo("28.00"));
            Assert.That(texts[0].Attribute("font-size")?.Value, Is.EqualTo("12"));
            Assert.That(texts[0].Attribute("fill")?.Value, Is.EqualTo("#333333"));
            Assert.That(texts[0].Value, Is.EqualTo("Platform 1"));
        }
    }

    [Test]
    public void PlacedTrackPlanSvgRenderer_Render_PortColorsAndValidationMarkers_EmitExactGeometryAndStyles()
    {
        var scene = new TrackPlanRenderScene(
            [new TrackPlanRenderItem(Guid.NewGuid(), "G100", 0, 0, 0, HorizontalHundredMmPath)],
            [new TrackPlanValidationMarker(null, 50, 60, "Loose end")],
            [
                new TrackPlanPortMarker(10, 20, "PortA"),
                new TrackPlanPortMarker(30, 20, "PortB"),
                new TrackPlanPortMarker(50, 20, "PortC"),
                new TrackPlanPortMarker(70, 20, "PortD"),
                new TrackPlanPortMarker(90, 20, "PortX")
            ]);

        var svg = new PlacedTrackPlanSvgRenderer().Render(scene);
        var root = XDocument.Parse(svg).Root!;
        var circles = root.Elements(SvgNs + "circle").ToList();
        var texts = root.Elements(SvgNs + "text").ToList();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(circles, Has.Count.EqualTo(6));
            Assert.That(texts, Has.Count.EqualTo(6));

            Assert.That(
                circles.Take(5).Select(circle => circle.Attribute("fill")?.Value),
                Is.EqualTo(ExpectedPortColors));
            Assert.That(circles.Take(5).Select(circle => circle.Attribute("r")?.Value), Is.All.EqualTo("6"));
            Assert.That(circles.Take(5).Select(circle => circle.Attribute("fill-opacity")?.Value), Is.All.EqualTo("0.9"));
            Assert.That(circles[0].Attribute("cx")?.Value, Is.EqualTo("10.00"));
            Assert.That(circles[0].Attribute("cy")?.Value, Is.EqualTo("20.00"));

            Assert.That(texts.Take(5).Select(text => text.Value), Is.EqualTo(ExpectedPortShortNames));
            Assert.That(
                texts.Take(5).Select(text => text.Attribute("fill")?.Value),
                Is.EqualTo(ExpectedPortColors));
            Assert.That(texts.Take(5).Select(text => text.Attribute("font-size")?.Value), Is.All.EqualTo("12"));
            Assert.That(texts.Take(5).Select(text => text.Attribute("font-weight")?.Value), Is.All.EqualTo("bold"));
            Assert.That(texts[0].Attribute("x")?.Value, Is.EqualTo("20.00"));
            Assert.That(texts[0].Attribute("y")?.Value, Is.EqualTo("10.00"));

            // Validation marker circle + text
            Assert.That(circles[5].Attribute("cx")?.Value, Is.EqualTo("50.00"));
            Assert.That(circles[5].Attribute("cy")?.Value, Is.EqualTo("60.00"));
            Assert.That(circles[5].Attribute("r")?.Value, Is.EqualTo("8"));
            Assert.That(circles[5].Attribute("fill")?.Value, Is.EqualTo("#E81123"));
            Assert.That(circles[5].Attribute("fill-opacity")?.Value, Is.EqualTo("0.7"));

            Assert.That(texts[5].Attribute("x")?.Value, Is.EqualTo("60.00"));
            Assert.That(texts[5].Attribute("y")?.Value, Is.EqualTo("50.00"));
            Assert.That(texts[5].Attribute("font-size")?.Value, Is.EqualTo("12"));
            Assert.That(texts[5].Attribute("fill")?.Value, Is.EqualTo("#A80000"));
            Assert.That(texts[5].Value, Is.EqualTo("Loose end"));
        }
    }

    [Test]
    public void PlacedTrackPlanSvgRenderer_Render_EnforcesLayerOrder_GridThenTracksThenPortsThenMarkers()
    {
        var scene = new TrackPlanRenderScene(
            [new TrackPlanRenderItem(Guid.NewGuid(), "G100", 0, 0, 0, HorizontalHundredMmPath, Label: "Track 1", FeedbackIntensity: 0.8)],
            [new TrackPlanValidationMarker(null, 50, 0, "Open")],
            [new TrackPlanPortMarker(0, 0, "PortA")]);

        var svg = new PlacedTrackPlanSvgRenderer().Render(scene, showGrid: true);
        var elements = XDocument.Parse(svg).Root!.Elements().ToList();

        var firstNonLineIndex = elements.FindIndex(element => element.Name.LocalName != "line");
        var lastLineIndex = elements.FindLastIndex(element => element.Name.LocalName == "line");
        var pathIndices = elements
            .Select((element, index) => (element, index))
            .Where(entry => entry.element.Name.LocalName == "path")
            .Select(entry => entry.index)
            .ToList();
        var trackLabelIndex = elements.FindIndex(element => element.Name.LocalName == "text" && element.Value == "Track 1");
        var portCircleIndex = elements.FindIndex(element => element.Name.LocalName == "circle" && element.Attribute("r")?.Value == "6");
        var portTextIndex = elements.FindIndex(element => element.Name.LocalName == "text" && element.Value == "A");
        var markerCircleIndex = elements.FindIndex(element => element.Name.LocalName == "circle" && element.Attribute("r")?.Value == "8");
        var markerTextIndex = elements.FindIndex(element => element.Name.LocalName == "text" && element.Value == "Open");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(lastLineIndex, Is.GreaterThanOrEqualTo(0));
            Assert.That(lastLineIndex, Is.LessThan(firstNonLineIndex));
            Assert.That(pathIndices, Has.Count.EqualTo(2));
            Assert.That(pathIndices[0], Is.GreaterThan(lastLineIndex));
            Assert.That(elements[pathIndices[0]].Attribute("stroke")?.Value, Is.EqualTo("#FFB400"));
            Assert.That(elements[pathIndices[1]].Attribute("stroke")?.Value, Is.EqualTo("#333333"));
            Assert.That(pathIndices[1], Is.GreaterThan(pathIndices[0]));
            Assert.That(trackLabelIndex, Is.GreaterThan(pathIndices[1]));
            Assert.That(portCircleIndex, Is.GreaterThan(trackLabelIndex));
            Assert.That(portTextIndex, Is.GreaterThan(portCircleIndex));
            Assert.That(markerCircleIndex, Is.GreaterThan(portTextIndex));
            Assert.That(markerTextIndex, Is.GreaterThan(markerCircleIndex));
        }
    }

    [Test]
    public void PlacedTrackPlanSvgRenderer_Render_EscapesXmlSpecialCharactersInLabelsPortsAndMarkers()
    {
        const string specialLabel = "Track <1> & \"Main\" 'A'";
        const string specialMessage = "Gap > 2mm & <open> \"Port\" 'B'";
        var scene = new TrackPlanRenderScene(
            [new TrackPlanRenderItem(Guid.NewGuid(), "G100", 0, 0, 0, HorizontalHundredMmPath, Label: specialLabel)],
            [new TrackPlanValidationMarker(null, 50, 0, specialMessage)],
            [
                new TrackPlanPortMarker(0, 0, "Port&"),
                new TrackPlanPortMarker(100, 0, "Port<")
            ]);

        var svg = new PlacedTrackPlanSvgRenderer().Render(scene);
        var texts = XDocument.Parse(svg).Root!.Elements(SvgNs + "text").ToList();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(svg, Does.Contain("&lt;1&gt; &amp; &quot;Main&quot; &apos;A&apos;"));
            Assert.That(svg, Does.Contain("&gt; 2mm &amp; &lt;open&gt; &quot;Port&quot; &apos;B&apos;"));
            Assert.That(texts.Select(text => text.Value), Is.EqualTo(ExpectedEscapedTexts));
        }
    }

    [Test]
    public void PlacedTrackPlanSvgRenderer_Render_CurveAndTurnoutSegments_EmitArcAndBranchPaths()
    {
        var placements = new List<PlacedSegment>
        {
            new(new R9(), 0, 0, 0),
            new(new WR(), 300, 100, 30)
        };
        var scene = TrackPlanRenderSceneBuilder.Build(TrackPlanTestGeometry.PikoA, placements, includePorts: true);

        var svg = new PlacedTrackPlanSvgRenderer().Render(scene);
        var root = XDocument.Parse(svg).Root!;
        var paths = root.Elements(SvgNs + "path").ToList();
        var portCircles = root.Elements(SvgNs + "circle").ToList();
        var viewBox = root.Attribute("viewBox")!.Value
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(part => double.Parse(part, CultureInfo.InvariantCulture))
            .ToArray();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(paths, Has.Count.EqualTo(2));
            Assert.That(paths[0].Attribute("d")?.Value, Does.Contain(" A "));
            Assert.That(paths[1].Attribute("d")?.Value, Does.Contain(" L "));
            Assert.That(paths[1].Attribute("d")?.Value, Does.Contain(" A "));
            Assert.That(portCircles, Has.Count.EqualTo(5));
            Assert.That(portCircles.Select(circle => circle.Attribute("fill")?.Value), Does.Contain("#00AA00"));
            Assert.That(viewBox, Has.Length.EqualTo(4));
            Assert.That(viewBox[2], Is.GreaterThan(100));
            Assert.That(viewBox[3], Is.GreaterThan(100));
        }
    }
}
