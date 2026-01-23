// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

using NUnit.Framework;
using Moba.TrackLibrary.PikoA.Catalog;
using Moba.TrackPlan.Graph;
using Moba.TrackPlan.Renderer.Geometry;
using Moba.TrackPlan.Renderer.Service;
using Moba.TrackPlan.Renderer.World;
using System.Diagnostics;

namespace Test.TrackPlan.Renderer;

/// <summary>
/// R9 Oval mit WR-Weiche (Option B): 23×R9 + 1×WR = 360° geschlossener Kreis
/// TOPOLOGY FIRST: Domain Model (TopologyGraph) ist die Wahrheit.
/// Renderer liest die Topologie und zeichnet basierend auf Piko A Geometrie.
/// </summary>
[TestFixture]
public sealed class Complete_R9_Oval_With_WR
{
    [Test]
    public void Build_R9_Oval_With_WR_Option_B_Topology_First()
    {
        // ============================================================================
        // SCHRITT 1: TOPOLOGIE aufbauen (Domain Model)
        // ============================================================================
        // Geschlossenes Oval - Option B korrekt:
        // Node[0] --[12×R9, 180°]--> Node[12]
        // Node[12] --[1×WR, 15°]--> Node[13]
        // Node[13] --[11×R9, 165°]--> Node[0]
        //
        // Total: 24 Nodes (0-23 für alle intermediate Nodes)
        // Total: 24 Edges (12 + 1 + 11)

        var nodes = new List<TrackNode>();
        for (int i = 0; i < 24; i++)  // Nodes 0-23
            nodes.Add(new() { Id = Guid.NewGuid() });

        var edges = new List<TrackEdge>();

        // Segment 1: 12×R9 (180°): Node[0] → Node[12]
        for (int i = 0; i < 12; i++)
        {
            edges.Add(new TrackEdge
            {
                Id = Guid.NewGuid(),
                TemplateId = "R9",
                Connections = new Dictionary<string, Endpoint>
                {
                    { "A", new Endpoint(nodes[i].Id, "End") },
                    { "B", new Endpoint(nodes[i + 1].Id, "Start") }
                }
            });
        }

        // Segment 2: 1×WR (15°): Node[12] → Node[13] via Port B (Hauptweg)
        edges.Add(new TrackEdge
        {
            Id = Guid.NewGuid(),
            TemplateId = "WR",
            Connections = new Dictionary<string, Endpoint>
            {
                { "A", new Endpoint(nodes[12].Id, "End") },
                { "B", new Endpoint(nodes[13].Id, "Start") }  // Port B ist der Hauptweg!
            }
        });

        // Segment 3: 11×R9 (165°): Node[13] → Node[0]
        // Mit intermediate Nodes [14-23] für die 11 R9 Kurven
        for (int i = 0; i < 11; i++)
        {
            int fromNodeIdx = 13 + i;           // 13, 14, 15, ..., 23
            int toNodeIdx = (13 + i + 1) % 24;  // 14, 15, ..., 23, 0 (wrap!)
            
            edges.Add(new TrackEdge
            {
                Id = Guid.NewGuid(),
                TemplateId = "R9",
                Connections = new Dictionary<string, Endpoint>
                {
                    { "A", new Endpoint(nodes[fromNodeIdx].Id, "End") },
                    { "B", new Endpoint(nodes[toNodeIdx].Id, "Start") }
                }
            });
        }

        var graph = new TopologyGraph
        {
            Nodes = nodes,
            Edges = edges,
            Endcaps = [],
            Sections = [],
            Isolators = []
        };

        TestContext.WriteLine($"✅ Topologie aufgebaut:");
        TestContext.WriteLine($"   Nodes: {nodes.Count}");
        TestContext.WriteLine($"   Edges: {edges.Count}");
        TestContext.WriteLine($"   Segment 1: 12×R9 (180°) - Node[0] → Node[12]");
        TestContext.WriteLine($"   Segment 2: 1×WR (15°) - Node[12] → Node[13]");
        TestContext.WriteLine($"   Segment 3: 11×R9 (165°) - Node[13] → Node[0] (geschlossen!)");

        // ============================================================================
        // SCHRITT 2: RENDERER - Topologie auslesen und rendern
        // ============================================================================

        var primitives = RenderTopology(graph);

        TestContext.WriteLine($"✅ Rendering abgeschlossen: {primitives.Count} Primitive");

        // ============================================================================
        // SCHRITT 2b: Bounding-Box berechnen und neu skalieren
        // ============================================================================

        var (minX, maxX, minY, maxY) = CalculateBoundingBox(primitives);
        var width = maxX - minX;
        var height = maxY - minY;

        TestContext.WriteLine($"📏 Bounding-Box:");
        TestContext.WriteLine($"   X: {minX:F1} ... {maxX:F1} (Breite: {width:F1}mm)");
        TestContext.WriteLine($"   Y: {minY:F1} ... {maxY:F1} (Höhe: {height:F1}mm)");

        // ============================================================================
        // SCHRITT 3: SVG exportieren - EINFACHER ANSATZ
        // ============================================================================
        // Strategie: Canvas so groß machen, dass die komplette Geometrie + Padding passt
        // KEIN Offset-Rumrechnen - einfach groß genug Canvas!

        var paddingMM = 200;
        
        // Berechne echte Größe mit Padding
        var totalWidth = (maxX - minX) + (2 * paddingMM);
        var totalHeight = (maxY - minY) + (2 * paddingMM);
        
        // Skalierung: Zielgröße / echte Größe
        var targetSize = 2400;
        var scale = targetSize / Math.Max(totalWidth, totalHeight);
        
        // Canvas ist so groß wie nötig (in Pixeln)
        var svgWidth = (int)(totalWidth * scale);
        var svgHeight = (int)(totalHeight * scale);

        // Offset: Verschiebe um Minimum + Padding
        var offsetX = -(minX - paddingMM);
        var offsetY = -(minY - paddingMM);

        var svg = SvgExporter.Export(
            primitives,
            width: svgWidth,
            height: svgHeight,
            scale: scale,
            strokeWidth: 3,
            showGrid: true,
            showOrigin: true,
            gridSize: 100,
            centerOffsetX: offsetX,
            centerOffsetY: offsetY
        );

        TestContext.WriteLine($"📐 SVG Canvas: {svgWidth}×{svgHeight}px");
        TestContext.WriteLine($"   Geometry: X[{minX:F0}..{maxX:F0}], Y[{minY:F0}..{maxY:F0}]");
        TestContext.WriteLine($"   Total Size: {totalWidth:F0}×{totalHeight:F0}mm");
        TestContext.WriteLine($"   Scale: {scale:F4} px/mm ({targetSize}/{Math.Max(totalWidth, totalHeight):F0})");
        TestContext.WriteLine($"   Offset: ({offsetX:F0}, {offsetY:F0})");

        OpenSvgInBrowser(svg);
        TestContext.WriteLine($"🌐 Browser: SVG im Browser angezeigt");
    }

    /// <summary>
    /// Renderer: Liest TopologyGraph und rendert Geometrie-Primitive
    /// </summary>
    private static List<IGeometryPrimitive> RenderTopology(TopologyGraph graph)
    {
        var primitives = new List<IGeometryPrimitive>();
        var catalog = new PikoATrackCatalog();
        var renderer = new TrackGeometryRenderer();
        
        // Mapping: NodeId → (Position, Angle)
        var nodePositions = new Dictionary<Guid, (Point2D Pos, double Angle)>();
        
        // Start bei Node[0]
        var startNodeId = graph.Nodes[0].Id;
        nodePositions[startNodeId] = (new Point2D(0, 0), 0.0);

        TestContext.WriteLine($"\n🔍 Rendering Topologie:");
        TestContext.WriteLine($"   Start: (0, 0) @ 0°");

        var edgesProcessed = 0;

        // Rendern: Für jede Edge, hole Start-Node Position, rendern, speichere End-Node Position
        for (int edgeIdx = 0; edgeIdx < graph.Edges.Count; edgeIdx++)
        {
            var edge = graph.Edges[edgeIdx];
            var template = catalog.GetById(edge.TemplateId);
            if (template == null)
            {
                TestContext.WriteLine($"   ❌ Edge {edgeIdx}: Template '{edge.TemplateId}' nicht gefunden");
                continue;
            }

            // Port "A" ist IMMER der Eingang (Start)
            if (!edge.Connections.TryGetValue("A", out var startConnectionEndpoint))
            {
                TestContext.WriteLine($"   ❌ Edge {edgeIdx}: Keine Port-A");
                continue;
            }

            var startNodeId_Edge = startConnectionEndpoint.NodeId;
            if (!nodePositions.TryGetValue(startNodeId_Edge, out var startPosAngle))
            {
                TestContext.WriteLine($"   ⚠️ Edge {edgeIdx}: Start-Position unbekannt");
                continue;
            }

            // Rendern
            var edgePrimitives = renderer.Render(template, startPosAngle.Pos, startPosAngle.Angle).ToList();
            primitives.AddRange(edgePrimitives);

            // Berechne End-Position
            Point2D endPos = startPosAngle.Pos;
            double endAngle = startPosAngle.Angle;

            if (edgePrimitives.Count > 0 && edgePrimitives[0] is ArcPrimitive arc)
            {
                endPos = new Point2D(
                    arc.Center.X + arc.Radius * Math.Cos(arc.StartAngleRad + arc.SweepAngleRad),
                    arc.Center.Y + arc.Radius * Math.Sin(arc.StartAngleRad + arc.SweepAngleRad));
                endAngle = startPosAngle.Angle + (arc.SweepAngleRad * 180 / Math.PI);
            }
            else if (edgePrimitives.Count > 0 && edgePrimitives[0] is LinePrimitive line)
            {
                endPos = line.To;
            }

            // Port "B" ist der Hauptausgang (follow main path)
            Guid? endNodeId = null;
            if (edge.Connections.TryGetValue("B", out var endConnectionB))
            {
                endNodeId = endConnectionB.NodeId;
            }
            else if (edge.Connections.TryGetValue("C", out var endConnectionC))
            {
                // Fallback auf Port C wenn Port B nicht existiert
                endNodeId = endConnectionC.NodeId;
            }

            if (endNodeId.HasValue)
            {
                nodePositions[endNodeId.Value] = (endPos, endAngle);
                edgesProcessed++;
                
                TestContext.WriteLine($"   Edge {edgeIdx:D2} ({edge.TemplateId}): " +
                    $"({startPosAngle.Pos.X:F1},{startPosAngle.Pos.Y:F1}) @{startPosAngle.Angle:F1}° → " +
                    $"({endPos.X:F1},{endPos.Y:F1}) @{endAngle:F1}°");
            }
        }

        TestContext.WriteLine($"   Total: {edgesProcessed} Edges verarbeitet");

        return primitives;
    }

    private static void OpenSvgInBrowser(string svgContent)
    {
        var fileName = $"R9_Oval_Option_B_{DateTime.Now:yyyyMMdd_HHmmss}.html";
        var filePath = Path.Combine(Path.GetTempPath(), fileName);

        var htmlContent = $@"<!DOCTYPE html>
<html lang=""de"">
<head>
    <meta charset=""utf-8"">
    <title>🚂 R9 Oval mit WR (Option B)</title>
    <style>
        * {{ margin: 0; padding: 0; box-sizing: border-box; }}
        body {{
            font-family: 'Segoe UI', sans-serif;
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            min-height: 100vh;
            padding: 20px;
            display: flex;
            justify-content: center;
            align-items: center;
        }}
        .container {{
            background: white;
            border-radius: 12px;
            box-shadow: 0 20px 60px rgba(0,0,0,0.3);
            padding: 30px;
            max-width: 2200px;
        }}
        h1 {{ color: #333; margin-bottom: 10px; }}
        .info {{ background: #f5f5f5; padding: 15px; border-left: 4px solid #667eea; margin-bottom: 20px; }}
        .svg-wrapper {{ background: #fafafa; padding: 20px; border-radius: 8px; }}
        svg {{ border: 2px solid #ddd; border-radius: 8px; }}
    </style>
</head>
<body>
    <div class=""container"">
        <h1>🚂 R9 Oval mit WR - Option B</h1>
        <div class=""info"">
            <strong>Topology First:</strong> 11×R9 (165°) + 1×WR (15°) + 12×R9 (180°) = 360°
        </div>
        <div class=""svg-wrapper"">
            {svgContent}
        </div>
    </div>
</body>
</html>";

        File.WriteAllText(filePath, htmlContent);
        
        try
        {
            Process.Start(new ProcessStartInfo { FileName = filePath, UseShellExecute = true });
        }
        catch { }
        
        TestContext.WriteLine($"📂 {filePath}");
    }

    /// <summary>
    /// Berechnet Bounding-Box aller Primitive
    /// </summary>
    private static (double minX, double maxX, double minY, double maxY) CalculateBoundingBox(List<IGeometryPrimitive> primitives)
    {
        double minX = double.MaxValue, maxX = double.MinValue;
        double minY = double.MaxValue, maxY = double.MinValue;

        foreach (var prim in primitives)
        {
            if (prim is ArcPrimitive arc)
            {
                // Für Arc: Bounds sind Center ± Radius (vereinfacht)
                minX = Math.Min(minX, arc.Center.X - arc.Radius);
                maxX = Math.Max(maxX, arc.Center.X + arc.Radius);
                minY = Math.Min(minY, arc.Center.Y - arc.Radius);
                maxY = Math.Max(maxY, arc.Center.Y + arc.Radius);
            }
            else if (prim is LinePrimitive line)
            {
                minX = Math.Min(minX, Math.Min(line.From.X, line.To.X));
                maxX = Math.Max(maxX, Math.Max(line.From.X, line.To.X));
                minY = Math.Min(minY, Math.Min(line.From.Y, line.To.Y));
                maxY = Math.Max(maxY, Math.Max(line.From.Y, line.To.Y));
            }
        }

        return (minX, maxX, minY, maxY);
    }
}
