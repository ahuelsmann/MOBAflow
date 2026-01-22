// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Test.TrackPlan.Renderer;

using Moba.TrackLibrary.PikoA.Catalog;
using Moba.TrackLibrary.PikoA.Metadata;
using Moba.TrackPlan.Graph;
using Moba.TrackPlan.Renderer.Geometry;
using Moba.TrackPlan.Renderer.Service;
using Moba.TrackPlan.Renderer.World;
using Moba.TrackPlan.TrackSystem;

/// <summary>
/// TEMPLATE TEST - Korrigiere die erwarteten Werte basierend auf der SVG-Ausgabe!
/// 
/// Workflow:
/// 1. Test ausführen → schlägt fehl
/// 2. SVG-Datei öffnen (Pfad in Test-Output)
/// 3. Tatsächliche Werte ablesen
/// 4. Erwartete Werte im Test korrigieren
/// 5. Test erneut ausführen → sollte grün sein
/// </summary>
[TestFixture]
public class GeometryValidationTemplate
{
    private string _outputDir = null!;
    private ITrackCatalog _catalog = null!;
    private TrackGeometryRenderer _renderer = null!;

    [SetUp]
    public void SetUp()
    {
        _outputDir = Path.Combine(Path.GetTempPath(), "TrackPlanGeometryDebug");
        Directory.CreateDirectory(_outputDir);
        _catalog = new PikoATrackCatalog();
        _renderer = new TrackGeometryRenderer();
    }

    /// <summary>
    /// Erzeugt ein Oval aus 12 Piko R1-Kurven (je 30°, Radius 358mm).
    /// Verwendet das echte Domain-Modell: TopologyGraph, TrackEdge, TrackNode.
    /// 
    /// Erwartung: Nach 12 Kurven (12 × 30° = 360°) schließt sich das Oval.
    /// </summary>
    [Test]
    public void PikoA_R1_Oval_12Curves_ShouldCloseCircle()
    {
        // ============================================================
        // ARRANGE: Graph mit 12 R1-Kurven aufbauen
        // ============================================================
        var graph = new TopologyGraph();
        var positions = new Dictionary<Guid, Point2D>();
        var rotations = new Dictionary<Guid, double>();

        var template = _catalog.GetById("R1")!;
        TestContext.WriteLine($"Template: {template.Id}");
        TestContext.WriteLine($"  Radius: {template.Geometry.RadiusMm}mm");
        TestContext.WriteLine($"  Angle: {template.Geometry.AngleDeg}°");
        TestContext.WriteLine($"  Ends: {string.Join(", ", template.Ends.Select(e => $"{e.Id}@{e.AngleDeg}°"))}");

        // Startposition und -winkel
        var currentPos = new Point2D(0, 0);
        var currentAngle = 0.0;

        var edges = new List<TrackEdge>();
        var segments = new List<SvgExporter.TrackSegment>();

        for (int i = 0; i < 12; i++)
        {
            // Neuen TrackEdge erstellen
            var edge = new TrackEdge
            {
                Id = Guid.NewGuid(),
                TemplateId = "R1"
            };

            // Nodes für die Ports erstellen
            foreach (var end in template.Ends)
            {
                var node = new TrackNode
                {
                    Id = Guid.NewGuid(),
                    Ports = [new TrackPort { Id = end.Id }]
                };
                graph.Nodes.Add(node);
                edge.Connections[end.Id] = new Endpoint(node.Id, end.Id);
            }

            graph.Edges.Add(edge);
            edges.Add(edge);

            // Position und Rotation speichern
            positions[edge.Id] = currentPos;
            rotations[edge.Id] = currentAngle;

            TestContext.WriteLine($"Kurve {i + 1}: Pos=({currentPos.X:F2}, {currentPos.Y:F2}), Rot={currentAngle:F1}°");

            // Geometrie rendern und Segment erstellen
            var primitives = _renderer.Render(template, currentPos, currentAngle).ToList();

            // Port-Positionen berechnen
            var ports = CalculatePortPositions(template, currentPos, currentAngle, primitives,
                i > 0 ? edges[i - 1].Id : null,  // Verbindung zum vorherigen
                i == 11 ? edges[0].Id : null);   // Letztes verbindet mit erstem

            segments.Add(new SvgExporter.TrackSegment(
                edge.Id,
                template.Id,
                currentPos,
                currentAngle,
                primitives,
                ports));

            // Nächste Position berechnen (Endpunkt dieser Kurve)
            if (primitives[0] is ArcPrimitive arc)
            {
                currentPos = new Point2D(
                    arc.Center.X + arc.Radius * Math.Cos(arc.StartAngleRad + arc.SweepAngleRad),
                    arc.Center.Y + arc.Radius * Math.Sin(arc.StartAngleRad + arc.SweepAngleRad));
            }

            // Nächster Winkel
            currentAngle += template.Geometry.AngleDeg!.Value;
        }

        // Verbindungen setzen (jedes Gleis verbindet Port B mit Port A des nächsten)
        for (int i = 0; i < segments.Count; i++)
        {
            var nextIdx = (i + 1) % segments.Count;
            var currentSeg = segments[i];
            var nextSeg = segments[nextIdx];

            // Aktualisiere Ports mit Verbindungsinformation
            var updatedPorts = currentSeg.Ports.Select(p =>
                p.PortId == "B"
                    ? p with { ConnectedToEdgeId = nextSeg.Id, ConnectedToPortId = "A" }
                    : p.PortId == "A" && i > 0
                        ? p with { ConnectedToEdgeId = segments[i - 1].Id, ConnectedToPortId = "B" }
                        : p.PortId == "A" && i == 0
                            ? p with { ConnectedToEdgeId = segments[^1].Id, ConnectedToPortId = "B" }
                            : p
            ).ToList();

            segments[i] = currentSeg with { Ports = updatedPorts };
        }

        // ============================================================
        // ACT: Export mit Schwellen, Ports und Verbindungen
        // ============================================================

        // Einfaches SVG (nur Geometrie)
        var allPrimitives = segments.SelectMany(s => s.Primitives).ToList();
        var svgSimplePath = Path.Combine(_outputDir, "PikoA_R1_Oval_Simple.svg");
        var svgSimple = SvgExporter.Export(allPrimitives, width: 1000, height: 1000, scale: 0.8);
        File.WriteAllText(svgSimplePath, svgSimple);
        TestContext.WriteLine($"\n📁 SVG (einfach): {svgSimplePath}");

        // Vollständiges SVG mit Schwellen, Ports, Verbindungen
        var svgPath = Path.Combine(_outputDir, "PikoA_R1_Oval_12Curves.svg");
        var svg = SvgExporter.ExportTrackPlan(segments, width: 1200, height: 1000, scale: 0.8,
            showSleepers: true, showPorts: true, showConnections: true);
        File.WriteAllText(svgPath, svg);
        TestContext.WriteLine($"📁 SVG (vollständig): {svgPath}");

        // ============================================================
        // ASSERT: Kreis sollte sich schließen
        // ============================================================
        TestContext.WriteLine($"\nEndposition nach 12 Kurven: ({currentPos.X:F2}, {currentPos.Y:F2})");
        TestContext.WriteLine($"Endwinkel: {currentAngle:F1}° (erwartet: 360°)");

        // Winkel sollte 360° sein
        Assert.That(currentAngle, Is.EqualTo(360.0).Within(0.1),
            "Nach 12 × 30° Kurven sollte der Winkel 360° sein");

        // Position sollte wieder am Start sein (0, 0)
        Assert.That(currentPos.X, Is.EqualTo(0).Within(1.0),
            $"Kreis schließt nicht! End-X sollte 0 sein, ist aber {currentPos.X:F2}");
        Assert.That(currentPos.Y, Is.EqualTo(0).Within(1.0),
            $"Kreis schließt nicht! End-Y sollte 0 sein, ist aber {currentPos.Y:F2}");

        TestContext.WriteLine("\n✅ Oval schließt korrekt!");
    }

    /// <summary>
    /// Testet eine einzelne Piko R1-Kurve und zeigt alle berechneten Werte.
    /// </summary>
    [Test]
    public void PikoA_R1_SingleCurve_DebugOutput()
    {
        var template = _catalog.GetById("R1")!;
        var start = new Point2D(0, 0);
        var startAngle = 0.0;

        var primitives = _renderer.Render(template, start, startAngle).ToList();

        // SVG mit Debug-Info
        var svgPath = Path.Combine(_outputDir, "PikoA_R1_SingleCurve_Debug.svg");
        var svg = SvgExporter.ExportDebug(primitives, start, startAngle, width: 600, height: 600, scale: 0.5);
        File.WriteAllText(svgPath, svg);
        TestContext.WriteLine($"📁 SVG: {svgPath}");

        Assert.That(primitives, Has.Count.EqualTo(1));
        var arc = primitives[0] as ArcPrimitive;
        Assert.That(arc, Is.Not.Null);

        // Alle Werte ausgeben
        TestContext.WriteLine($"\n=== Piko R1 Kurve (Radius={PikoAConstants.R1}mm, Winkel={PikoAConstants.StandardAngle}°) ===");
        TestContext.WriteLine($"Input:");
        TestContext.WriteLine($"  Start: ({start.X}, {start.Y})");
        TestContext.WriteLine($"  StartAngle: {startAngle}°");
        TestContext.WriteLine($"\nOutput (ArcPrimitive):");
        TestContext.WriteLine($"  Center: ({arc!.Center.X:F2}, {arc.Center.Y:F2})");
        TestContext.WriteLine($"  Radius: {arc.Radius:F2}mm");
        TestContext.WriteLine($"  StartAngleRad: {arc.StartAngleRad:F4} ({arc.StartAngleRad * 180 / Math.PI:F2}°)");
        TestContext.WriteLine($"  SweepAngleRad: {arc.SweepAngleRad:F4} ({arc.SweepAngleRad * 180 / Math.PI:F2}°)");

        // Berechne Start- und Endpunkt des Arcs
        var arcStart = new Point2D(
            arc.Center.X + arc.Radius * Math.Cos(arc.StartAngleRad),
            arc.Center.Y + arc.Radius * Math.Sin(arc.StartAngleRad));
        var arcEnd = new Point2D(
            arc.Center.X + arc.Radius * Math.Cos(arc.StartAngleRad + arc.SweepAngleRad),
            arc.Center.Y + arc.Radius * Math.Sin(arc.StartAngleRad + arc.SweepAngleRad));

        TestContext.WriteLine($"\nBerechnete Punkte:");
        TestContext.WriteLine($"  Arc-Startpunkt: ({arcStart.X:F2}, {arcStart.Y:F2}) - sollte Input-Start sein!");
        TestContext.WriteLine($"  Arc-Endpunkt: ({arcEnd.X:F2}, {arcEnd.Y:F2})");

        // Prüfe ob Arc am Input-Startpunkt beginnt
        Assert.That(arcStart.X, Is.EqualTo(start.X).Within(0.1),
            $"Arc startet nicht am Input! Erwartet X={start.X}, ist {arcStart.X:F2}");
        Assert.That(arcStart.Y, Is.EqualTo(start.Y).Within(0.1),
            $"Arc startet nicht am Input! Erwartet Y={start.Y}, ist {arcStart.Y:F2}");
    }

    /// <summary>
    /// Testet das Oval mit verschiedenen Radien (R1, R2, R3).
    /// </summary>
    [TestCase("R1", 358.0)]
    [TestCase("R2", 421.6)]
    [TestCase("R3", 484.5)]
    public void PikoA_Oval_DifferentRadii_ShouldCloseCircle(string templateId, double expectedRadius)
    {
        var template = _catalog.GetById(templateId)!;
        Assert.That(template.Geometry.RadiusMm, Is.EqualTo(expectedRadius).Within(0.1));

        var allPrimitives = new List<IGeometryPrimitive>();
        var currentPos = new Point2D(0, 0);
        var currentAngle = 0.0;

        for (int i = 0; i < 12; i++)
        {
            var primitives = _renderer.Render(template, currentPos, currentAngle).ToList();
            allPrimitives.AddRange(primitives);

            if (primitives[0] is ArcPrimitive arc)
            {
                currentPos = new Point2D(
                    arc.Center.X + arc.Radius * Math.Cos(arc.StartAngleRad + arc.SweepAngleRad),
                    arc.Center.Y + arc.Radius * Math.Sin(arc.StartAngleRad + arc.SweepAngleRad));
            }
            currentAngle += 30.0;
        }

        // SVG exportieren
        var svgPath = Path.Combine(_outputDir, $"PikoA_{templateId}_Oval.svg");
        var svg = SvgExporter.Export(allPrimitives, width: 1000, height: 1000, scale: 0.6);
        File.WriteAllText(svgPath, svg);
        TestContext.WriteLine($"📁 SVG: {svgPath}");

        // Kreis sollte schließen
        Assert.That(currentPos.X, Is.EqualTo(0).Within(1.0),
            $"{templateId}: End-X={currentPos.X:F2}, sollte 0 sein");
        Assert.That(currentPos.Y, Is.EqualTo(0).Within(1.0),
            $"{templateId}: End-Y={currentPos.Y:F2}, sollte 0 sein");
    }

    /// <summary>
    /// Gleisoval mit Piko A R9-Kurven, BWL, BWR und W3 (Dreiwegweiche).
    /// Am Abzweig rechts der W3 wird R1 (rechts) → R2 (links) angeschlossen.
    /// 
    /// Gleisliste:
    /// - 23× Piko 55219 (R9): Radius 907.97mm, Winkel 15°
    /// - 1× Piko 55220 (BWL): Weiche links (Abzweig = Teil des Ovals)
    /// - 1× Piko 55221 (BWR): Weiche rechts (an BWL Port B)
    /// - 1× Piko 55224 (W3): Dreiwegweiche (an BWR Port B)
    /// - 1× Piko 55211 (R1): Rechtskurve 30° (an W3 Port D)
    /// - 1× Piko 55212 (R2): Linkskurve 30° (an R1)
    /// 
    /// Layout (Vogelperspektive):
    /// 
    ///         ╭────────────────────────────╮
    ///        ╱                              ╲
    ///       │                                │
    ///       │                                │
    ///        ╲                              ╱
    ///         ╰════════════════════════════╯
    ///              ↑
    ///         BWL (Abzweig = Teil des Ovals)
    ///              ║
    ///              ╠════════════ BWR ═══════════╗
    ///              ║                            ║
    ///              ↓                            ↓
    ///         BWR Port C              ╔═════ W3 Port B (gerade)
    ///                                 ╠═════ W3 Port C (links)
    ///                                 ╚═════ W3 Port D (rechts)
    ///                                            ↓
    ///                                           R1 (Rechtskurve)
    ///                                            ↓
    ///                                           R2 (Linkskurve)
    /// 
    /// </summary>
    [Test]
    public void PikoA_R9_Oval_With_Switch_23CurvesAnd1Switch()
    {
        // Arrange
        var r9Template = _catalog.GetById("R9")!;
        var bwlTemplate = _catalog.GetById("BWL")!;
        var bwrTemplate = _catalog.GetById("BWR")!;
        var w3Template = _catalog.GetById("W3")!;
        var r1Template = _catalog.GetById("R1")!;
        var r2Template = _catalog.GetById("R2")!;

        TestContext.WriteLine("=== Piko A R9 Oval mit BWL + BWR + W3 + R1 + R2 ===\n");

        var labeledTracks = new List<SvgExporter.LabeledTrack>();
        var currentPos = new Point2D(0, 0);
        var currentAngle = 0.0;

        // ============================================================
        // 1. BWL - Links-Weiche (Position 0, Winkel 0°)
        // ============================================================
        TestContext.WriteLine("=== 1. BWL (Links-Weiche) ===");
        var bwlPrimitives = _renderer.Render(bwlTemplate, currentPos, currentAngle).ToList();

        var bwlLine = bwlPrimitives[0] as LinePrimitive;
        var bwlArc = bwlPrimitives[1] as ArcPrimitive;

        var bwlPortB = bwlLine!.To;
        var bwlPortBAngle = currentAngle;

        var bwlPortC = new Point2D(
            bwlArc!.Center.X + bwlArc.Radius * Math.Cos(bwlArc.StartAngleRad + bwlArc.SweepAngleRad),
            bwlArc.Center.Y + bwlArc.Radius * Math.Sin(bwlArc.StartAngleRad + bwlArc.SweepAngleRad));
        var bwlPortCAngle = currentAngle + bwlArc.SweepAngleRad * 180.0 / Math.PI;

        labeledTracks.Add(new SvgExporter.LabeledTrack("BWL", bwlPrimitives, currentPos, bwlPortC));
        TestContext.WriteLine($"  Port B→BWR: ({bwlPortB.X:F2}, {bwlPortB.Y:F2})");
        TestContext.WriteLine($"  Port C→Oval: ({bwlPortC.X:F2}, {bwlPortC.Y:F2}), {bwlPortCAngle:F1}°");

        // ============================================================
        // 2. BWR - Rechts-Weiche an BWL Port B
        // ============================================================
        TestContext.WriteLine("\n=== 2. BWR (Rechts-Weiche) ===");
        var bwrPrimitives = _renderer.Render(bwrTemplate, bwlPortB, bwlPortBAngle).ToList();

        var bwrLine = bwrPrimitives[0] as LinePrimitive;
        var bwrArc = bwrPrimitives[1] as ArcPrimitive;

        var bwrPortB = bwrLine!.To;
        var bwrPortC = new Point2D(
            bwrArc!.Center.X + bwrArc.Radius * Math.Cos(bwrArc.StartAngleRad + bwrArc.SweepAngleRad),
            bwrArc.Center.Y + bwrArc.Radius * Math.Sin(bwrArc.StartAngleRad + bwrArc.SweepAngleRad));

        labeledTracks.Add(new SvgExporter.LabeledTrack("BWR", bwrPrimitives, bwlPortB, bwrPortB));
        TestContext.WriteLine($"  Port B→W3: ({bwrPortB.X:F2}, {bwrPortB.Y:F2})");
        TestContext.WriteLine($"  Port C: ({bwrPortC.X:F2}, {bwrPortC.Y:F2})");

        // ============================================================
        // 3. W3 - Dreiwegweiche an BWR Port B
        // ============================================================
        TestContext.WriteLine("\n=== 3. W3 (Dreiwegweiche) ===");
        var w3Primitives = _renderer.Render(w3Template, bwrPortB, bwlPortBAngle).ToList();

        // W3 rendert: [0] = Line (gerade), [1] = Arc links, [2] = Arc rechts
        var w3Line = w3Primitives[0] as LinePrimitive;
        var w3ArcLeft = w3Primitives[1] as ArcPrimitive;
        var w3ArcRight = w3Primitives[2] as ArcPrimitive;

        var w3PortB = w3Line!.To;  // Gerade durch
        var w3PortC = new Point2D(  // Links
            w3ArcLeft!.Center.X + w3ArcLeft.Radius * Math.Cos(w3ArcLeft.StartAngleRad + w3ArcLeft.SweepAngleRad),
            w3ArcLeft.Center.Y + w3ArcLeft.Radius * Math.Sin(w3ArcLeft.StartAngleRad + w3ArcLeft.SweepAngleRad));
        var w3PortD = new Point2D(  // Rechts
            w3ArcRight!.Center.X + w3ArcRight.Radius * Math.Cos(w3ArcRight.StartAngleRad + w3ArcRight.SweepAngleRad),
            w3ArcRight.Center.Y + w3ArcRight.Radius * Math.Sin(w3ArcRight.StartAngleRad + w3ArcRight.SweepAngleRad));
        var w3PortDAngle = bwlPortBAngle + w3ArcRight.SweepAngleRad * 180.0 / Math.PI;  // -15°

        labeledTracks.Add(new SvgExporter.LabeledTrack("W3", w3Primitives, bwrPortB, w3PortB));
        TestContext.WriteLine($"  Port B (gerade): ({w3PortB.X:F2}, {w3PortB.Y:F2})");
        TestContext.WriteLine($"  Port C (links): ({w3PortC.X:F2}, {w3PortC.Y:F2})");
        TestContext.WriteLine($"  Port D (rechts): ({w3PortD.X:F2}, {w3PortD.Y:F2}), {w3PortDAngle:F1}°");

        // ============================================================
        // 4. R1 - Rechtskurve an W3 Port D
        // ============================================================
        TestContext.WriteLine("\n=== 4. R1 (Rechtskurve 30°) an W3 Port D ===");
        // R1 Template hat +30° Winkel (Linkskurve), wir brauchen negativ für Rechtskurve
        // Wir rendern R1 mit dem Winkel von W3 Port D und drehen dann weiter nach rechts
        var r1Primitives = _renderer.Render(r1Template, w3PortD, w3PortDAngle).ToList();

        var r1Arc = r1Primitives[0] as ArcPrimitive;
        var r1End = new Point2D(
            r1Arc!.Center.X + r1Arc.Radius * Math.Cos(r1Arc.StartAngleRad + r1Arc.SweepAngleRad),
            r1Arc.Center.Y + r1Arc.Radius * Math.Sin(r1Arc.StartAngleRad + r1Arc.SweepAngleRad));
        var r1EndAngle = w3PortDAngle + r1Arc.SweepAngleRad * 180.0 / Math.PI;

        labeledTracks.Add(new SvgExporter.LabeledTrack("R1", r1Primitives, w3PortD, r1End));
        TestContext.WriteLine($"  Start: ({w3PortD.X:F2}, {w3PortD.Y:F2}), {w3PortDAngle:F1}°");
        TestContext.WriteLine($"  Ende: ({r1End.X:F2}, {r1End.Y:F2}), {r1EndAngle:F1}°");

        // ============================================================
        // 5. R2 - Linkskurve an R1 Ende
        // ============================================================
        TestContext.WriteLine("\n=== 5. R2 (Linkskurve 30°) an R1 ===");
        var r2Primitives = _renderer.Render(r2Template, r1End, r1EndAngle).ToList();

        var r2Arc = r2Primitives[0] as ArcPrimitive;
        var r2End = new Point2D(
            r2Arc!.Center.X + r2Arc.Radius * Math.Cos(r2Arc.StartAngleRad + r2Arc.SweepAngleRad),
            r2Arc.Center.Y + r2Arc.Radius * Math.Sin(r2Arc.StartAngleRad + r2Arc.SweepAngleRad));
        var r2EndAngle = r1EndAngle + r2Arc.SweepAngleRad * 180.0 / Math.PI;

        labeledTracks.Add(new SvgExporter.LabeledTrack("R2", r2Primitives, r1End, r2End));
        TestContext.WriteLine($"  Start: ({r1End.X:F2}, {r1End.Y:F2}), {r1EndAngle:F1}°");
        TestContext.WriteLine($"  Ende: ({r2End.X:F2}, {r2End.Y:F2}), {r2EndAngle:F1}°");

        // ============================================================
        // 6. Oval fortsetzen: 23 R9-Kurven ab BWL Port C
        // ============================================================
        TestContext.WriteLine("\n=== 6. Oval: 23 × R9 Kurven ab BWL Port C ===");
        currentPos = bwlPortC;
        currentAngle = bwlPortCAngle;

        for (int i = 0; i < 23; i++)
        {
            var primitives = _renderer.Render(r9Template, currentPos, currentAngle).ToList();

            Point2D endPos;
            if (primitives[0] is ArcPrimitive arc)
            {
                endPos = new Point2D(
                    arc.Center.X + arc.Radius * Math.Cos(arc.StartAngleRad + arc.SweepAngleRad),
                    arc.Center.Y + arc.Radius * Math.Sin(arc.StartAngleRad + arc.SweepAngleRad));

                labeledTracks.Add(new SvgExporter.LabeledTrack("R9", primitives, currentPos, endPos));
                currentPos = endPos;
            }
            currentAngle += 15.0;
        }

        // ============================================================
        // SVG exportieren
        // ============================================================
        var svgPath = Path.Combine(_outputDir, "PikoA_R9_Oval_WithSwitch.svg");
        var svg = SvgExporter.ExportWithLabels(
            labeledTracks,
            width: 1800,
            height: 1800,
            scale: 0.30,
            showLabels: true,
            showSeparators: true,
            showSegmentNumbers: true,
            showGrid: true,
            showOrigin: true);
        File.WriteAllText(svgPath, svg);
        TestContext.WriteLine($"\n📁 SVG: {svgPath}");

        // ============================================================
        // Assert
        // ============================================================
        TestContext.WriteLine($"\nOval-Endposition: ({currentPos.X:F2}, {currentPos.Y:F2})");

        var distanceToStart = Math.Sqrt(currentPos.X * currentPos.X + currentPos.Y * currentPos.Y);
        TestContext.WriteLine($"Abstand zum Start: {distanceToStart:F2}mm");

        Assert.That(currentPos.X, Is.EqualTo(0).Within(5.0),
            $"Oval schließt nicht! End-X={currentPos.X:F2}");
        Assert.That(currentPos.Y, Is.EqualTo(0).Within(5.0),
            $"Oval schließt nicht! End-Y={currentPos.Y:F2}");
    }

    /// <summary>
    /// Einfaches R9-Oval ohne Weiche zum Vergleich (24 × R9 = 360°).
    /// </summary>
    [Test]
    public void PikoA_R9_Oval_24Curves_ShouldCloseCircle()
    {
        var template = _catalog.GetById("R9")!;

        TestContext.WriteLine($"R9 Kurve: Radius={template.Geometry.RadiusMm}mm, Winkel={template.Geometry.AngleDeg}°");
        TestContext.WriteLine($"24 × 15° = 360°\n");

        var allPrimitives = new List<IGeometryPrimitive>();
        var currentPos = new Point2D(0, 0);
        var currentAngle = 0.0;

        for (int i = 0; i < 24; i++)
        {
            var primitives = _renderer.Render(template, currentPos, currentAngle).ToList();
            allPrimitives.AddRange(primitives);

            if (primitives[0] is ArcPrimitive arc)
            {
                currentPos = new Point2D(
                    arc.Center.X + arc.Radius * Math.Cos(arc.StartAngleRad + arc.SweepAngleRad),
                    arc.Center.Y + arc.Radius * Math.Sin(arc.StartAngleRad + arc.SweepAngleRad));
            }
            currentAngle += 15.0;
        }

        var svgPath = Path.Combine(_outputDir, "PikoA_R9_Oval_24Curves.svg");
        var svg = SvgExporter.Export(allPrimitives, width: 1200, height: 1200, scale: 0.4);
        File.WriteAllText(svgPath, svg);
        TestContext.WriteLine($"📁 SVG: {svgPath}");

        TestContext.WriteLine($"\nEndposition: ({currentPos.X:F2}, {currentPos.Y:F2})");

        Assert.That(currentPos.X, Is.EqualTo(0).Within(1.0));
        Assert.That(currentPos.Y, Is.EqualTo(0).Within(1.0));
    }

    #region Helper Methods

    /// <summary>
    /// Berechnet die Weltkoordinaten der Ports eines Gleisstücks.
    /// </summary>
    private List<SvgExporter.PortInfo> CalculatePortPositions(
        TrackTemplate template,
        Point2D position,
        double rotationDeg,
        IReadOnlyList<IGeometryPrimitive> primitives,
        Guid? previousEdgeId = null,
        Guid? nextEdgeId = null)
    {
        var ports = new List<SvgExporter.PortInfo>();
        var rotRad = rotationDeg * Math.PI / 180.0;

        // Für Kurven: Port A ist am Start, Port B ist am Ende des Arcs
        if (primitives.Count > 0 && primitives[0] is ArcPrimitive arc)
        {
            // Port A: Am Startpunkt des Arcs
            var portAPos = new Point2D(
                arc.Center.X + arc.Radius * Math.Cos(arc.StartAngleRad),
                arc.Center.Y + arc.Radius * Math.Sin(arc.StartAngleRad));
            var portAAngle = rotationDeg;  // Zeigt in Richtung des Gleis-Starts

            // Port B: Am Endpunkt des Arcs  
            var portBPos = new Point2D(
                arc.Center.X + arc.Radius * Math.Cos(arc.StartAngleRad + arc.SweepAngleRad),
                arc.Center.Y + arc.Radius * Math.Sin(arc.StartAngleRad + arc.SweepAngleRad));
            var portBAngle = rotationDeg + (arc.SweepAngleRad * 180.0 / Math.PI);  // Zeigt in Richtung des Gleis-Endes

            ports.Add(new SvgExporter.PortInfo("A", portAPos, portAAngle, previousEdgeId, previousEdgeId.HasValue ? "B" : null));
            ports.Add(new SvgExporter.PortInfo("B", portBPos, portBAngle, nextEdgeId, nextEdgeId.HasValue ? "A" : null));
        }
        else if (primitives.Count > 0 && primitives[0] is LinePrimitive line)
        {
            // Für Geraden
            ports.Add(new SvgExporter.PortInfo("A", line.From, rotationDeg, previousEdgeId, previousEdgeId.HasValue ? "B" : null));
            ports.Add(new SvgExporter.PortInfo("B", line.To, rotationDeg, nextEdgeId, nextEdgeId.HasValue ? "A" : null));
        }

        return ports;
    }

    #endregion

    #region Switch Tests

    /// <summary>
    /// Einfache Links-Weiche (BWL) bei 0° Rotation.
    /// 
    /// Erwartete Darstellung (Vogelperspektive):
    /// 
    ///              C (abzweigend, 15° nach oben)
    ///             /
    ///   A ═══════╬═══════ B (gerade durch)
    /// 
    /// </summary>
    [Test]
    public void PikoA_SwitchLeft_SingleSwitch()
    {
        // Arrange
        var template = _catalog.GetById("BWL")!;
        var start = new Point2D(0, 0);
        var rotation = 0.0;

        TestContext.WriteLine($"Template: {template.Id}");
        TestContext.WriteLine($"  Länge (gerade): {template.Geometry.LengthMm}mm");
        TestContext.WriteLine($"  Radius (Abzweig): {template.Geometry.RadiusMm}mm");
        TestContext.WriteLine($"  Winkel (Abzweig): {template.Geometry.AngleDeg}°");
        TestContext.WriteLine($"  Junction-Offset: {template.Geometry.JunctionOffsetMm}mm");
        TestContext.WriteLine($"  Ports: {string.Join(", ", template.Ends.Select(e => $"{e.Id}@{e.AngleDeg}°"))}");

        // Act
        var primitives = _renderer.Render(template, start, rotation).ToList();

        // SVG exportieren
        var svgPath = Path.Combine(_outputDir, "PikoA_SwitchLeft_BWL.svg");
        var svg = SvgExporter.Export(primitives, width: 800, height: 400, scale: 1.0,
            showGrid: true, gridSize: 50, showOrigin: true);
        File.WriteAllText(svgPath, svg);
        TestContext.WriteLine($"\n📁 SVG: {svgPath}");

        // Assert
        Assert.That(primitives, Has.Count.EqualTo(2), "Weiche sollte 2 Primitives haben (Line + Arc)");

        var line = primitives[0] as LinePrimitive;
        var arc = primitives[1] as ArcPrimitive;

        Assert.That(line, Is.Not.Null, "Erstes Primitive sollte Line sein");
        Assert.That(arc, Is.Not.Null, "Zweites Primitive sollte Arc sein");

        // Line-Details ausgeben
        TestContext.WriteLine($"\nLine (gerade durch A→B):");
        TestContext.WriteLine($"  Von: ({line!.From.X:F2}, {line.From.Y:F2})");
        TestContext.WriteLine($"  Nach: ({line.To.X:F2}, {line.To.Y:F2})");
        TestContext.WriteLine($"  Länge: {Math.Sqrt(Math.Pow(line.To.X - line.From.X, 2) + Math.Pow(line.To.Y - line.From.Y, 2)):F2}mm");

        // Arc-Details ausgeben
        TestContext.WriteLine($"\nArc (Abzweig nach C):");
        TestContext.WriteLine($"  Center: ({arc!.Center.X:F2}, {arc.Center.Y:F2})");
        TestContext.WriteLine($"  Radius: {arc.Radius:F2}mm");
        TestContext.WriteLine($"  StartAngle: {arc.StartAngleRad * 180 / Math.PI:F2}°");
        TestContext.WriteLine($"  SweepAngle: {arc.SweepAngleRad * 180 / Math.PI:F2}°");

        // Berechne Arc-Endpunkt (Port C)
        var arcEndX = arc.Center.X + arc.Radius * Math.Cos(arc.StartAngleRad + arc.SweepAngleRad);
        var arcEndY = arc.Center.Y + arc.Radius * Math.Sin(arc.StartAngleRad + arc.SweepAngleRad);
        TestContext.WriteLine($"  Endpunkt (Port C): ({arcEndX:F2}, {arcEndY:F2})");

        // Prüfe dass Line bei (0,0) startet
        Assert.That(line.From.X, Is.EqualTo(0).Within(0.1), "Line sollte bei X=0 starten");
        Assert.That(line.From.Y, Is.EqualTo(0).Within(0.1), "Line sollte bei Y=0 starten");

        // Prüfe Line-Länge
        Assert.That(line.To.X, Is.EqualTo(PikoAConstants.SwitchStraightLengthMm).Within(0.1),
            $"Line sollte Länge {PikoAConstants.SwitchStraightLengthMm}mm haben");

        // Links-Weiche: Arc sollte nach oben abzweigen (positiver Sweep)
        Assert.That(arc.SweepAngleRad, Is.GreaterThan(0), "Links-Weiche sollte positiven Sweep haben (nach oben)");
    }

    /// <summary>
    /// Einfache Rechts-Weiche (BWR) bei 0° Rotation.
    /// 
    /// Erwartete Darstellung (Vogelperspektive):
    /// 
    ///   A ═══════╬═══════ B (gerade durch)
    ///             \
    ///              C (abzweigend, 15° nach unten)
    /// 
    /// </summary>
    [Test]
    public void PikoA_SwitchRight_SingleSwitch()
    {
        // Arrange
        var template = _catalog.GetById("BWR")!;
        var start = new Point2D(0, 0);
        var rotation = 0.0;

        TestContext.WriteLine($"Template: {template.Id}");
        TestContext.WriteLine($"  Ports: {string.Join(", ", template.Ends.Select(e => $"{e.Id}@{e.AngleDeg}°"))}");

        // Act
        var primitives = _renderer.Render(template, start, rotation).ToList();

        // SVG exportieren
        var svgPath = Path.Combine(_outputDir, "PikoA_SwitchRight_BWR.svg");
        var svg = SvgExporter.Export(primitives, width: 800, height: 400, scale: 1.0,
            showGrid: true, gridSize: 50, showOrigin: true);
        File.WriteAllText(svgPath, svg);
        TestContext.WriteLine($"\n📁 SVG: {svgPath}");

        // Assert
        Assert.That(primitives, Has.Count.EqualTo(2));

        var line = primitives[0] as LinePrimitive;
        var arc = primitives[1] as ArcPrimitive;

        TestContext.WriteLine($"\nLine: ({line!.From.X:F2}, {line.From.Y:F2}) → ({line.To.X:F2}, {line.To.Y:F2})");
        TestContext.WriteLine($"Arc: Center=({arc!.Center.X:F2}, {arc.Center.Y:F2}), Sweep={arc.SweepAngleRad * 180 / Math.PI:F2}°");

        // Rechts-Weiche: Arc sollte nach unten abzweigen (negativer Sweep)
        Assert.That(arc.SweepAngleRad, Is.LessThan(0), "Rechts-Weiche sollte negativen Sweep haben (nach unten)");
    }

    /// <summary>
    /// Vergleicht Links- und Rechts-Weiche nebeneinander.
    /// </summary>
    [Test]
    public void PikoA_SwitchLeftAndRight_Comparison()
    {
        var leftTemplate = _catalog.GetById("BWL")!;
        var rightTemplate = _catalog.GetById("BWR")!;

        // Links-Weiche bei (0, 100)
        var leftPrimitives = _renderer.Render(leftTemplate, new Point2D(0, 100), 0).ToList();

        // Rechts-Weiche bei (0, -100)
        var rightPrimitives = _renderer.Render(rightTemplate, new Point2D(0, -100), 0).ToList();

        // Alle Primitives kombinieren
        var allPrimitives = leftPrimitives.Concat(rightPrimitives).ToList();

        // SVG exportieren
        var svgPath = Path.Combine(_outputDir, "PikoA_Switch_LeftRight_Comparison.svg");
        var svg = SvgExporter.Export(allPrimitives, width: 800, height: 600, scale: 1.0,
            showGrid: true, gridSize: 50, showOrigin: true);
        File.WriteAllText(svgPath, svg);
        TestContext.WriteLine($"📁 SVG: {svgPath}");

        TestContext.WriteLine("\nLinks-Weiche (oben): Abzweig geht nach oben (+Y)");
        TestContext.WriteLine("Rechts-Weiche (unten): Abzweig geht nach unten (-Y)");

        Assert.That(allPrimitives, Has.Count.EqualTo(4));
    }

    #endregion

    [TearDown]
    public void TearDown()
    {
        TestContext.WriteLine($"\n📁 Alle SVG-Dateien in: {_outputDir}");
    }
}
