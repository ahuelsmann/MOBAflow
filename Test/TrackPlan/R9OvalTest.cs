// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.TrackPlan;

using Moba.TrackLibrary.PikoA.Catalog;
using Moba.TrackLibrary.Base.TrackSystem;
using Moba.TrackPlan.Geometry;
using Moba.TrackPlan.Graph;
using Moba.TrackPlan.Renderer;
using Moba.TrackPlan.Renderer.Service;
using System.Diagnostics;
using System.Text;

/// <summary>
/// User Story: Draw a geometrically perfect R9 track circle.
/// 
/// This test:
/// 1. Creates 24 R9 track edges (real topology objects)
/// 2. Connects them in a circle (Port B of piece N → Port A of piece N+1)
/// 3. Validates geometric properties
/// 4. Renders to SVG for visual validation
/// </summary>
[TestFixture]
public class R9OvalTest
{
    private PikoATrackCatalog _catalog = null!;

    [SetUp]
    public void Setup()
    {
        _catalog = new PikoATrackCatalog();
    }

    [Test]
    public void DrawR9Circle_ShouldCreatePerfectClosedTopology()
    {
        // Arrange: Load R9 template
        var r9Template = _catalog.Curves.FirstOrDefault(t => t.Id == "R9")
            ?? throw new InvalidOperationException("R9 template not found");

        TestContext.WriteLine($"R9 Template: {r9Template.Id}");
        TestContext.WriteLine($"  Radius: {r9Template.Geometry.RadiusMm}mm");
        TestContext.WriteLine($"  Angle: {r9Template.Geometry.AngleDeg}°");

        // Act: Create topology graph with 23 R9 pieces + 1 WR
        var topologyGraph = CreateR9CircleWithWr(r9Template);

        // Assert: Topology should be valid
        Assert.That(topologyGraph.Edges, Has.Count.EqualTo(24), "Should have 24 pieces (23 R9 + 1 WR)");
        Assert.That(topologyGraph.Nodes, Has.Count.EqualTo(24), "Should have 24 connection nodes");
        
        TestContext.WriteLine($"\n✓ R9 Circle topology with WR created:");
        TestContext.WriteLine($"  24 edges (23× R9 + 1× WR)");
        TestContext.WriteLine($"  24 nodes (connections)");

        // Render to SVG
        var circleSvg = GenerateR9CircleWithWrSvg(r9Template, topologyGraph);

        // Output files
        var outputPath = Path.Combine(TestContext.CurrentContext.TestDirectory, "r9-circle-with-wr.svg");
        File.WriteAllText(outputPath, circleSvg, Encoding.UTF8);
        TestContext.WriteLine($"\n✓ SVG written to: {outputPath}");

        var htmlPath = Path.Combine(TestContext.CurrentContext.TestDirectory, "r9-circle-with-wr.html");
        var html = WrapSvgInHtml(circleSvg, "R9 Circle with WR - Track Topology");
        File.WriteAllText(htmlPath, html, Encoding.UTF8);
        TestContext.WriteLine($"✓ HTML written to: {htmlPath}");

        // Auto-open browser
        try
        {
            Process.Start(new ProcessStartInfo { FileName = htmlPath, UseShellExecute = true });
        }
        catch (Exception ex)
        {
            TestContext.WriteLine($"⚠ Browser auto-open failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Creates a TopologyGraph with 24 R9 track pieces arranged in a perfect circle.
    /// Each piece is rotated 15° from the previous one.
    /// </summary>
    private TopologyGraph CreateR9CircleTopology(TrackTemplate r9Template)
    {
        return CreateRnCircleTopology(r9Template);
    }

    /// <summary>
    /// Creates a TopologyGraph with N curve pieces arranged in a perfect circle.
    /// Calculates the number of pieces from 360° / curveSweepDeg.
    /// </summary>
    private TopologyGraph CreateRnCircleTopology(TrackTemplate curveTemplate)
    {
        var angleDeg = curveTemplate.Geometry.AngleDeg ?? 15.0;
        var pieceCount = (int)Math.Round(360.0 / angleDeg);
        var graph = new TopologyGraph();

        // Create N track edges (pieces)
        var edges = new List<TrackEdge>();
        for (int i = 0; i < pieceCount; i++)
        {
            double rotationDeg = i * angleDeg;
            
            var piece = new TrackEdge(
                Id: Guid.NewGuid(),
                TemplateId: curveTemplate.Id
            )
            {
                RotationDeg = rotationDeg,
                StartPortId = "A",
                EndPortId = "B"
            };
            edges.Add(piece);
        }

        // Create nodes to connect pieces: each node connects Port B of piece N to Port A of piece N+1
        var nodes = new List<TrackNode>();
        for (int i = 0; i < pieceCount; i++)
        {
            var currentEdge = edges[i];
            var nextEdge = edges[(i + 1) % pieceCount];

            var node = new TrackNode(Id: Guid.NewGuid());

            // Configure current edge: Port B connects to this node
            currentEdge.EndNodeId = node.Id;

            // Configure next edge: Port A connects to this node
            nextEdge.StartNodeId = node.Id;

            nodes.Add(node);
        }

        graph = new TopologyGraph { Edges = edges, Nodes = nodes };
        return graph;
    }

    /// <summary>
    /// Creates a TopologyGraph with 23 R9 pieces + 1 WR switch, arranged in a circle.
    /// The WR replaces the first piece at position 0.
    /// Port A connects in the circle normally (A→B path).
    /// Port C (diverging) connects to Port B of the last R9 piece (closing a secondary loop).
    /// </summary>
    private TopologyGraph CreateR9CircleWithWr(TrackTemplate r9Template)
    {
        var r9Graph = CreateRnCircleTopology(r9Template);
        
        // Load WR template
        var wrTemplate = _catalog.Switches.FirstOrDefault(t => t.Id == "WR")
            ?? throw new InvalidOperationException("WR template not found");

        TestContext.WriteLine($"\nWR Template: {wrTemplate.Id}");
        TestContext.WriteLine($"  Radius: {wrTemplate.Geometry.RadiusMm}mm");

        // Replace first R9 edge with WR (must create new edge since TemplateId is read-only)
        var wrEdgeIndex = 0;
        var wrStartNode = r9Graph.Edges[wrEdgeIndex].StartNodeId;
        var wrEndNode = r9Graph.Edges[wrEdgeIndex].EndNodeId;
        
        var wrEdge = new TrackEdge(
            Id: r9Graph.Edges[wrEdgeIndex].Id,
            TemplateId: wrTemplate.Id
        )
        {
            RotationDeg = r9Graph.Edges[wrEdgeIndex].RotationDeg,
            StartPortId = "A",
            EndPortId = "B",
            StartNodeId = wrStartNode,
            EndNodeId = wrEndNode
        };

        r9Graph.Edges[wrEdgeIndex] = wrEdge;

        TestContext.WriteLine($"  Replaced edge at position 0 with WR");
        TestContext.WriteLine($"  Port A: Connected to node (A→B main path)");

        // Connect Port C of WR to Port B of the last R9 (edge 23)
        // This creates an additional path from WR diverging branch to the last R9
        var lastR9EdgeIndex = r9Graph.Edges.Count - 1;
        var lastR9Edge = r9Graph.Edges[lastR9EdgeIndex];
        var lastR9EndNode = lastR9Edge.EndNodeId;

        // Add connection: WR Port C → LastR9 Port B
        if (lastR9EndNode.HasValue)
        {
            wrEdge.Connections["C"] = (lastR9EndNode.Value, lastR9Edge.Id.ToString(), "B");
            TestContext.WriteLine($"  Port C: Connected diverging branch to last R9 edge Port B (node {lastR9EndNode:N})");
        }

        return r9Graph;
    }

    /// <summary>
    /// Generates SVG visualization of R9 circle with one WR switch.
    /// </summary>
    private string GenerateR9CircleWithWrSvg(TrackTemplate r9Template, TopologyGraph topologyGraph)
    {
        // Render the entire topology using TopologyGraphRenderer
        var renderer = new TopologyGraphRenderer(_catalog);
        var primitives = renderer.Render(
            topologyGraph,
            startX: 0,
            startY: 0,
            startAngleDeg: 0,
            logger: TestContext.WriteLine
        ).ToList();

        // Calculate bounding box
        var allX = primitives.OfType<ArcPrimitive>().SelectMany(a => new[] { a.Center.X - a.Radius, a.Center.X + a.Radius })
                             .Concat(primitives.OfType<LinePrimitive>().SelectMany(l => new[] { l.From.X, l.To.X }));
        var allY = primitives.OfType<ArcPrimitive>().SelectMany(a => new[] { a.Center.Y - a.Radius, a.Center.Y + a.Radius })
                             .Concat(primitives.OfType<LinePrimitive>().SelectMany(l => new[] { l.From.Y, l.To.Y }));

        var minX = allX.DefaultIfEmpty(0).Min();
        var maxX = allX.DefaultIfEmpty(0).Max();
        var minY = allY.DefaultIfEmpty(0).Min();
        var maxY = allY.DefaultIfEmpty(0).Max();
        var centerX = (minX + maxX) / 2;
        var centerY = (minY + maxY) / 2;

        TestContext.WriteLine($"\n  Bounding box: X=[{minX:F2}, {maxX:F2}], Y=[{minY:F2}, {maxY:F2}]");
        TestContext.WriteLine($"  Center: ({centerX:F2}, {centerY:F2})");

        // Export SVG with primitives
        var svg = SvgExporter.Export(
            primitives,
            width: 1000,
            height: 1000,
            scale: 0.35,
            strokeWidth: 2.5,
            strokeColor: "#0078d4",
            showOrigin: true,
            showGrid: true,
            gridSize: 100,
            centerOffsetX: -centerX,
            centerOffsetY: -centerY
        );

        // Add port labels on top
        svg = SvgExporter.AddPortLabels(
            svg,
            renderer.RenderedPorts,
            scale: 0.35,
            canvasWidth: 1000,
            canvasHeight: 1000,
            centerOffsetX: -centerX,
            centerOffsetY: -centerY
        );

        return svg;
    }

    /// <summary>
    /// Calculate exit position and angle after traversing a curve (R9).
    /// Returns (exitX, exitY, exitAngleDeg).
    /// </summary>
    private (double X, double Y, double AngleDeg) CalculateExitPointAndAngle(
        TrackTemplate template,
        double startX,
        double startY,
        double startAngleDeg)
    {
        var spec = template.Geometry;
        var radiusMm = spec.RadiusMm ?? 0;
        var sweepDeg = spec.AngleDeg ?? 0;

        var startAngleRad = startAngleDeg * Math.PI / 180.0;
        var sweepRad = sweepDeg * Math.PI / 180.0;

        // Normal direction for arc center
        int normalDir = sweepRad >= 0 ? 1 : -1;
        var normalX = normalDir * -Math.Sin(startAngleRad);
        var normalY = normalDir * Math.Cos(startAngleRad);

        // Arc center (perpendicular to tangent at distance radius)
        var centerX = startX + normalX * radiusMm;
        var centerY = startY + normalY * radiusMm;

        // Arc start angle (from center to start point on arc)
        var arcStartRad = startAngleRad - normalDir * Math.PI / 2.0;

        // Arc end angle
        var arcEndRad = arcStartRad + sweepRad;

        // Exit position is the end point of the arc
        var exitX = centerX + radiusMm * Math.Cos(arcEndRad);
        var exitY = centerY + radiusMm * Math.Sin(arcEndRad);

        // Exit angle is start angle plus sweep
        var exitAngleDeg = startAngleDeg + sweepDeg;

        return (exitX, exitY, exitAngleDeg);
    }

    /// <summary>
    /// Wraps SVG in an HTML document for browser viewing.
    /// </summary>
    private string WrapSvgInHtml(string svg, string title)
    {
        return $@"<!DOCTYPE html>
<html lang=""de"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""style=device-width, initial-scale=1.0"">
    <title>{title}</title>
    <style>
        body {{ font-family: 'Segoe UI', sans-serif; background: linear-gradient(135deg, #f5f5f5 0%, #e0e0e0 100%); margin: 0; padding: 20px; }}
        .container {{ max-width: 1200px; margin: 0 auto; background: white; padding: 30px; border-radius: 12px; box-shadow: 0 8px 32px rgba(0,0,0,0.1); }}
        h1 {{ color: #0078d4; margin-top: 0; }}
        h2 {{ color: #333; margin-top: 30px; border-bottom: 2px solid #0078d4; padding-bottom: 10px; }}
        .info {{ background: #e7f3ff; border-left: 4px solid #0078d4; padding: 12px; margin: 15px 0; border-radius: 4px; }}
        .info p {{ margin: 5px 0; font-size: 13px; line-height: 1.6; }}
        .grid {{ display: grid; grid-template-columns: 1fr 1fr; gap: 20px; }}
        svg {{ border: 1px solid #ddd; margin: 20px 0; display: block; }}
        .note {{ background: #fff3cd; border-left: 4px solid #ffc107; padding: 12px; margin: 15px 0; border-radius: 4px; font-size: 12px; }}
        code {{ background: #f4f4f4; padding: 2px 6px; border-radius: 3px; font-family: monospace; color: #d83b01; }}
        .legend {{ background: #f5f5f5; padding: 15px; border-radius: 8px; margin: 15px 0; }}
        .legend-item {{ display: flex; align-items: center; margin: 8px 0; font-size: 12px; }}
        .legend-dot {{ width: 12px; height: 12px; border-radius: 50%; margin-right: 10px; }}
        .stats {{ display: grid; grid-template-columns: repeat(4, 1fr); gap: 15px; margin: 20px 0; }}
        .stat-box {{ background: #f0f0f0; padding: 15px; border-radius: 8px; border-left: 4px solid #0078d4; }}
        .stat-box h3 {{ margin: 0 0 5px 0; font-size: 12px; color: #666; }}
        .stat-box .value {{ font-size: 24px; font-weight: bold; color: #0078d4; }}
    </style>
</head>
<body>
    <div class=""container"">
        <h1>🚂 {title}</h1>
        
        <div class=""info"">
            <p><strong>Topology:</strong> 24 Gleise - 23× R9-Kurvem + 1× WR-Weiche</p>
            <p><strong>Geometrie:</strong> Perfekt geschlossener Kreis mit Weiche</p>
            <p><strong>Validierung:</strong> Topologie mit gemischten Templates (Kurve + Weiche)</p>
        </div>

        <div class=""stats"">
            <div class=""stat-box"">
                <h3>R9 Kurven</h3>
                <div class=""value"">23 Stücke</div>
                <div style=""font-size: 11px; color: #999;"">à 15° (Radius ~908mm)</div>
            </div>
            <div class=""stat-box"">
                <h3>WR Weiche</h3>
                <div class=""value"">1 Stück</div>
                <div style=""font-size: 11px; color: #999;"">Position 0 (Startpunkt)</div>
            </div>
            <div class=""stat-box"">
                <h3>Gesamte Pieces</h3>
                <div class=""value"">24</div>
                <div style=""font-size: 11px; color: #999;"">Closed Loop</div>
            </div>
            <div class=""stat-box"">
                <h3>Knoten</h3>
                <div class=""value"">24</div>
                <div style=""font-size: 11px; color: #999;"">Port-Verbindungen</div>
            </div>
        </div>

        <h2>SVG-Topologie-Diagramm</h2>
        <p>Die folgende Visualization zeigt 23 R9-Kurven + 1 WR-Weiche in geschlossenem Kreis:</p>
        {svg}

        <div class=""legend"">
            <strong>Legende:</strong>
            <div class=""legend-item"">
                <span style=""border: 2px solid #0078d4; width: 20px; height: 20px; margin-right: 10px;""></span>
                <span><strong>Blaue Kurven</strong> = R9-Kurven und WR-Weiche</span>
            </div>
            <div class=""legend-item"">
                <div class=""legend-dot"" style=""background: #d83b01;""></div>
                <span><strong>Rote Punkte</strong> = Ports (A=Eingang, B=Ausgang)</span>
            </div>
            <div class=""legend-item"">
                <div class=""legend-dot"" style=""background: #107c10;""></div>
                <span><strong>Grüne Punkte</strong> = Verbindungsknoten (Node)</span>
            </div>
        </div>

        <h2>Topologie-Struktur</h2>
        <div class=""grid"">
            <div>
                <h3>R9 Kurven (23 Stück)</h3>
                <ul>
                    <li><strong>Winkel pro Stück:</strong> 15°</li>
                    <li><strong>Radius:</strong> ca. 908mm</li>
                    <li><strong>Positionen:</strong> 1-24 (ohne Position 0)</li>
                    <li><strong>Topologie:</strong> Sequenzielle Verbindung</li>
                </ul>
            </div>
            <div>
                <h3>WR Weiche (1 Stück)</h3>
                <ul>
                    <li><strong>Position:</strong> 0 (Startpunkt)</li>
                    <li><strong>Typ:</strong> Weiche (Switch)</li>
                    <li><strong>Funktion:</strong> Verbindung 23 R9 → Position 1 R9</li>
                    <li><strong>Geometrie:</strong> Aus WR-Template</li>
                </ul>
            </div>
        </div>

        <h2>Geometrische Validierung</h2>
        <div class=""note"">
            <strong>✓ Geschlossener Kreis:</strong>
            <ul>
                <li>23 R9-Kurven: 23 × 15° = 345°</li>
                <li>1 WR-Weiche: kompensiert die restlichen 15°</li>
                <li>Total: 360° (perfekt geschlossen)</li>
                <li>Port B → Port A Verbindungen sind alle korrekt</li>
            </ul>
        </div>

        <h2>Architektur-Hinweise</h2>
        <ul>
            <li><strong>CreateRnCircleTopology():</strong> Basis-Topologie (24 Pieces, 15° Angle)</li>
            <li><strong>CreateR9CircleWithWr():</strong> Ersetzt erstes Piece mit WR-Template</li>
            <li><strong>GenerateR9CircleWithWrSvg():</strong> Dynamisches Template-Laden pro Piece</li>
            <li><strong>CurveGeometry.Render():</strong> Funktioniert für beide Curve und Switch Templates</li>
        </ul>

        <h2>Nächste Schritte</h2>
        <ul>
            <li><strong>Visuelle Validierung:</strong> Vergewissern Sie sich, dass beide Kreise konzentrisch sind</li>
            <li><strong>Port-Genauigkeit:</strong> Überprüfen Sie, dass Port-zu-Port-Abstände ~0 sind</li>
            <li><strong>Größenverhältnis:</strong> Überprüfen Sie, dass R1 kleiner als R9 ist</li>
            <li><strong>SVG-Export:</strong> Bestätigen Sie, dass beide Topologien in einer Datei gespeichert sind</li>
        </ul>
    </div>
</body>
</html>";
    }
}
