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
        Assert.That(rendered, Has.Count.EqualTo(3), "Should render all 3 segments");

        // Segment 1: G231 at origin
        var seg1 = rendered.First(r => r.Id == "seg-1");
        Assert.That(seg1.X, Is.EqualTo(0), "First segment starts at X=0");
        Assert.That(seg1.Y, Is.EqualTo(0), "First segment starts at Y=0");
        Assert.That(seg1.PathData, Is.EqualTo("M 0,0 L 230.93,0"), "G231 has correct PathData");
        Assert.That(seg1.AssignedInPort, Is.EqualTo(1), "InPort preserved");

        // Segment 2: R2 connected to seg-1
        // Position = seg1.Pos + seg1.Endpoint1 - seg2.Endpoint0
        //          = (0,0) + (230.93,0) - (0,0) = (230.93, 0)
        var seg2 = rendered.First(r => r.Id == "seg-2");
        Assert.That(seg2.X, Is.EqualTo(230.93).Within(0.01), "R2 starts where G231 ends (X=230.93)");
        Assert.That(seg2.Y, Is.EqualTo(0), "R2 starts at same Y as G231");
        Assert.That(seg2.PathData, Is.EqualTo("M 0,0 A 421.88,421.88 0 0 1 210.94,56.52"), "R2 has correct PathData");

        // Segment 3: G231 connected to seg-2
        // Position = seg2.Pos + seg2.Endpoint1 - seg3.Endpoint0
        //          = (230.93,0) + (210.94,56.52) - (0,0) = (441.87, 56.52)
        var seg3 = rendered.First(r => r.Id == "seg-3");
        Assert.That(seg3.X, Is.EqualTo(441.87).Within(0.01), "Second G231 positioned after R2 curve");
        Assert.That(seg3.Y, Is.EqualTo(56.52).Within(0.01), "Second G231 Y-offset matches R2 end");
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
        Assert.That(rendered, Is.Empty, "Empty layout should return no rendered segments");
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
        Assert.That(rendered, Is.Empty, "Unknown ArticleCode should be skipped");
    }
}
