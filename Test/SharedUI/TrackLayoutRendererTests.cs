// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
using Moba.Domain.TrackPlan;
using Moba.SharedUI.Renderer;
using NUnit.Framework;
using System.Linq;

namespace Moba.Test.SharedUI;

[TestFixture]
public class TopologyRendererTests
{
    [Test]
    public void Render_SimpleThreeSegmentLayout_CalculatesCorrectPositions()
    {
        // Arrange
        var library = new TrackGeometryLibrary();
        var renderer = new TopologyRenderer(library);

        var layout = new TrackLayout
        {
            Name = "Test Layout",
            TrackSystem = "Piko A-Gleis",
            Segments =
            [
                new TrackSegment { Id = "seg-1", ArticleCode = "G231", AssignedInPort = 1 },
                new TrackSegment { Id = "seg-2", ArticleCode = "R2", AssignedInPort = null },
                new TrackSegment { Id = "seg-3", ArticleCode = "G231", AssignedInPort = 2 }
            ],
            Connections =
            [
                new TrackConnection
                {
                    Segment1Id = "seg-1",
                    Segment1EndpointIndex = 1,  // Right endpoint of G231 (x=230.93)
                    Segment2Id = "seg-2",
                    Segment2EndpointIndex = 0   // Start of R2 (x=0)
                },
                new TrackConnection
                {
                    Segment1Id = "seg-2",
                    Segment1EndpointIndex = 1,  // End of R2 (x=210.94, y=56.52)
                    Segment2Id = "seg-3",
                    Segment2EndpointIndex = 0   // Start of G231 (x=0)
                }
            ]
        };

        // Act
        var rendered = renderer.Render(layout);

        // Assert
        Assert.That(rendered.Segments, Has.Count.EqualTo(3), "Should render all 3 segments");

        // Renderer offsets all coordinates by +50/+50
        const double offset = 50.0;

        // Segment 1: G231 at origin + offset
        var seg1 = rendered.Segments.First(r => r.Id == "seg-1");
        Assert.That(seg1.X, Is.EqualTo(0 + offset).Within(0.01), "First segment starts at X=50 after offset");
        Assert.That(seg1.Y, Is.EqualTo(0 + offset).Within(0.01), "First segment starts at Y=50 after offset");
        Assert.That(seg1.PathData, Is.EqualTo("M 0,0 L 230.93,0"), "G231 has correct PathData");
        Assert.That(seg1.AssignedInPort, Is.EqualTo(1), "InPort preserved");

        // Segment 2: R2 connected to seg-1
        // Position without offset = (230.93, 0) → with offset = (280.93, 50)
        var seg2 = rendered.Segments.First(r => r.Id == "seg-2");
        Assert.That(seg2.X, Is.EqualTo(230.93 + offset).Within(0.01), "R2 starts where G231 ends (plus offset)");
        Assert.That(seg2.Y, Is.EqualTo(0 + offset).Within(0.01), "R2 starts at same Y as G231 (plus offset)");
        Assert.That(seg2.PathData, Is.EqualTo("M 0,0 A 421.88,421.88 0 0 1 210.94,56.52"), "R2 has correct PathData");

        // Segment 3: G231 connected to seg-2
        // Position without offset = (441.87, 56.52) → with offset = (491.87, 106.52)
        var seg3 = rendered.Segments.First(r => r.Id == "seg-3");
        Assert.That(seg3.X, Is.EqualTo(441.87 + offset).Within(0.01), "Second G231 positioned after R2 curve (plus offset)");
        Assert.That(seg3.Y, Is.EqualTo(56.52 + offset).Within(0.01), "Second G231 Y-offset matches R2 end (plus offset)");
        Assert.That(seg3.PathData, Is.EqualTo("M 0,0 L 230.93,0"), "G231 has correct PathData");
        Assert.That(seg3.AssignedInPort, Is.EqualTo(2), "InPort preserved");
    }

    [Test]
    public void Render_EmptyLayout_ReturnsEmptyList()
    {
        // Arrange
        var library = new TrackGeometryLibrary();
        var renderer = new TopologyRenderer(library);
        var layout = new TrackLayout { Segments = [], Connections = [] };

        // Act
        var rendered = renderer.Render(layout);

        // Assert
        Assert.That(rendered.Segments, Is.Empty, "Empty layout should return no rendered segments");
    }

    [Test]
    public void Render_UnknownArticleCode_SkipsSegment()
    {
        // Arrange
        var library = new TrackGeometryLibrary();
        var renderer = new TopologyRenderer(library);
        var layout = new TrackLayout
        {
            Segments =
            [
                new TrackSegment { Id = "seg-1", ArticleCode = "UNKNOWN" }
            ],
            Connections = []
        };

        // Act
        var rendered = renderer.Render(layout);

        // Assert
        Assert.That(rendered.Segments, Is.Empty, "Unknown ArticleCode should be skipped");
    }
}
