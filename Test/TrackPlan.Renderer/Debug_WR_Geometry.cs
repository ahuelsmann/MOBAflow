// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Test.TrackPlan.Renderer;

using Moba.TrackLibrary.PikoA.Catalog;
using Moba.TrackPlan.Renderer.Geometry;
using Moba.TrackPlan.Renderer.Service;
using Moba.TrackPlan.Renderer.World;
using Moba.TrackPlan.TrackSystem;

/// <summary>
/// Testet WR als R9-Ersatz im Oval.
/// Der WR ARC (nicht Port C!) sollte ein R9-Gleis ersetzen.
/// </summary>
[TestFixture]
public class Debug_WR_Geometry
{
    private string _outputDir = null!;
    private PikoATrackCatalog _catalog = null!;
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
    /// Baseline: 24×R9 Oval (sollte perfekt schließen)
    /// </summary>
    [Test]
    public void Baseline_24x_R9()
    {
        var r9 = _catalog.GetById("R9")!;
        
        TestContext.WriteLine("=== Baseline: 24×R9 Oval ===\n");
        
        var tracks = new List<SvgExporter.LabeledTrack>();
        var pos = new Point2D(0, 0);
        var angle = 0.0;

        for (int i = 0; i < 24; i++)
        {
            var prims = _renderer.Render(r9, pos, angle).ToList();
            var arc = prims[0] as ArcPrimitive;
            
            var end = new Point2D(
                arc!.Center.X + arc.Radius * Math.Cos(arc.StartAngleRad + arc.SweepAngleRad),
                arc.Center.Y + arc.Radius * Math.Sin(arc.StartAngleRad + arc.SweepAngleRad));
            
            tracks.Add(new SvgExporter.LabeledTrack($"R9-{i+1}", prims, pos, end));
            
            pos = end;
            angle += 15.0;
        }

        var error = Math.Sqrt(pos.X * pos.X + pos.Y * pos.Y);
        TestContext.WriteLine($"Endposition: ({pos.X:F2}, {pos.Y:F2}), Winkel: {angle:F0}°");
        TestContext.WriteLine($"Schließungsfehler: {error:F3} mm\n");

        SaveAndOpen("Baseline_24xR9", tracks, error < 1.0 ? "✅ PERFEKT" : "❌ Fehler");
    }

    /// <summary>
    /// Test: R9-Oval wo ein beliebiges R9 durch WR ersetzt wird.
    /// Die WR wird so gedreht, dass ihr ARC ins Oval passt.
    /// </summary>
    [TestCase(0, 0, Description = "WR ersetzt R9#1 bei 0°")]
    [TestCase(0, 180, Description = "WR ersetzt R9#1 bei 180°")]
    [TestCase(12, 0, Description = "WR ersetzt R9#13 bei 0°")]
    [TestCase(12, 180, Description = "WR ersetzt R9#13 bei 180°")]
    public void WR_Replaces_R9_At_Position(int wrIndex, int wrRotationOffset)
    {
        var r9 = _catalog.GetById("R9")!;
        var wr = _catalog.GetById("WR")!;
        
        TestContext.WriteLine($"=== WR ersetzt R9#{wrIndex+1} (Rotation +{wrRotationOffset}°) ===\n");
        
        var tracks = new List<SvgExporter.LabeledTrack>();
        var pos = new Point2D(0, 0);
        var angle = 0.0;

        for (int i = 0; i < 24; i++)
        {
            if (i == wrIndex)
            {
                // WR mit Offset rendern
                var wrAngle = angle + wrRotationOffset;
                var wrPrims = _renderer.Render(wr, pos, wrAngle).ToList();
                
                // WR hat 2 Primitives: Line + Arc
                // Wir wollen nur den ARC-Endpunkt für das Oval
                var wrArc = wrPrims[1] as ArcPrimitive;
                var wrEnd = new Point2D(
                    wrArc!.Center.X + wrArc.Radius * Math.Cos(wrArc.StartAngleRad + wrArc.SweepAngleRad),
                    wrArc.Center.Y + wrArc.Radius * Math.Sin(wrArc.StartAngleRad + wrArc.SweepAngleRad));
                
                tracks.Add(new SvgExporter.LabeledTrack("WR", wrPrims, pos, wrEnd));
                
                TestContext.WriteLine($"WR bei Index {i}: Rotation={wrAngle:F0}°");
                TestContext.WriteLine($"  Start: ({pos.X:F2}, {pos.Y:F2})");
                TestContext.WriteLine($"  Arc Ende: ({wrEnd.X:F2}, {wrEnd.Y:F2})");
                
                pos = wrEnd;
                angle = wrAngle + wrArc.SweepAngleRad * 180.0 / Math.PI;
            }
            else
            {
                // Normales R9
                var r9Prims = _renderer.Render(r9, pos, angle).ToList();
                var r9Arc = r9Prims[0] as ArcPrimitive;
                
                var r9End = new Point2D(
                    r9Arc!.Center.X + r9Arc.Radius * Math.Cos(r9Arc.StartAngleRad + r9Arc.SweepAngleRad),
                    r9Arc.Center.Y + r9Arc.Radius * Math.Sin(r9Arc.StartAngleRad + r9Arc.SweepAngleRad));
                
                tracks.Add(new SvgExporter.LabeledTrack($"R9-{i+1}", r9Prims, pos, r9End));
                
                pos = r9End;
                angle += 15.0;
            }
        }

        var error = Math.Sqrt(pos.X * pos.X + pos.Y * pos.Y);
        var angleNorm = angle % 360;
        
        TestContext.WriteLine($"\nEndposition: ({pos.X:F2}, {pos.Y:F2}), Winkel: {angle:F0}° (={angleNorm:F0}°)");
        TestContext.WriteLine($"Schließungsfehler: {error:F3} mm\n");

        var name = $"WR_at_R9_{wrIndex+1}_rot{wrRotationOffset}";
        var result = error < 5.0 ? "✅ PASST!" : "❌ Nicht geschlossen";
        SaveAndOpen(name, tracks, result);
    }

    /// <summary>
    /// NEUE IDEE: WR Arc RÜCKWÄRTS durchlaufen (Port C → Port A statt Port A → Port C)
    /// Dann sollte der Arc +15° ergeben wie R9!
    /// </summary>
    [Test]
    public void WR_Arc_Backwards_As_R9()
    {
        var r9 = _catalog.GetById("R9")!;
        var wr = _catalog.GetById("WR")!;
        
        TestContext.WriteLine("=== WR Arc RÜCKWÄRTS als R9-Ersatz ===\n");
        
        // WR bei 0° rendern
        var wrPrims = _renderer.Render(wr, new Point2D(0, 0), 0).ToList();
        var wrArc = wrPrims[1] as ArcPrimitive;
        
        TestContext.WriteLine($"WR Arc bei 0°:");
        TestContext.WriteLine($"  Center: ({wrArc!.Center.X:F2}, {wrArc.Center.Y:F2})");
        TestContext.WriteLine($"  Radius: {wrArc.Radius:F2}mm");
        TestContext.WriteLine($"  StartAngle: {wrArc.StartAngleRad * 180 / Math.PI:F2}°");
        TestContext.WriteLine($"  Sweep: {wrArc.SweepAngleRad * 180 / Math.PI:F2}°");
        
        // Port A (Start des WR)
        var portA = new Point2D(0, 0);
        
        // Port C (Ende des WR Arc)
        var portC = new Point2D(
            wrArc.Center.X + wrArc.Radius * Math.Cos(wrArc.StartAngleRad + wrArc.SweepAngleRad),
            wrArc.Center.Y + wrArc.Radius * Math.Sin(wrArc.StartAngleRad + wrArc.SweepAngleRad));
        
        TestContext.WriteLine($"\nPort A: ({portA.X:F2}, {portA.Y:F2})");
        TestContext.WriteLine($"Port C: ({portC.X:F2}, {portC.Y:F2})");
        
        // RÜCKWÄRTS: Start bei Port C, ende bei Port A
        // Das bedeutet: Sweep = -(-15°) = +15° !
        TestContext.WriteLine($"\n🔄 RÜCKWÄRTS (Port C → Port A):");
        TestContext.WriteLine($"  Start: Port C ({portC.X:F2}, {portC.Y:F2})");
        TestContext.WriteLine($"  Ende: Port A ({portA.X:F2}, {portA.Y:F2})");
        TestContext.WriteLine($"  Sweep (rückwärts): +{-wrArc.SweepAngleRad * 180 / Math.PI:F2}° = +15°!");
        
        // Jetzt das Oval testen: Port C → Port A (WR rückwärts) → 23×R9 → Port C
        TestContext.WriteLine($"\n=== Oval: WR(rückwärts) + 23×R9 ===");
        
        var tracks = new List<SvgExporter.LabeledTrack>();
        
        // 1. WR rückwärts (Start bei Port C, Ende bei Port A)
        tracks.Add(new SvgExporter.LabeledTrack("WR", wrPrims, portC, portA));
        
        var pos = portA;
        var angle = 0.0 + 15.0; // Nach WR rückwärts sind wir bei +15°
        
        // 2. 23×R9
        for (int i = 0; i < 23; i++)
        {
            var r9Prims = _renderer.Render(r9, pos, angle).ToList();
            var r9Arc = r9Prims[0] as ArcPrimitive;
            
            var r9End = new Point2D(
                r9Arc!.Center.X + r9Arc.Radius * Math.Cos(r9Arc.StartAngleRad + r9Arc.SweepAngleRad),
                r9Arc.Center.Y + r9Arc.Radius * Math.Sin(r9Arc.StartAngleRad + r9Arc.SweepAngleRad));
            
            tracks.Add(new SvgExporter.LabeledTrack($"R9-{i+1}", r9Prims, pos, r9End));
            
            pos = r9End;
            angle += 15.0;
        }
        
        var error = Math.Sqrt(Math.Pow(pos.X - portC.X, 2) + Math.Pow(pos.Y - portC.Y, 2));
        
        TestContext.WriteLine($"\nEndposition: ({pos.X:F2}, {pos.Y:F2}), Winkel: {angle:F0}°");
        TestContext.WriteLine($"Soll (Port C): ({portC.X:F2}, {portC.Y:F2})");
        TestContext.WriteLine($"Schließungsfehler: {error:F3} mm\n");
        
        var result = error < 5.0 ? "✅ PERFEKT!!!" : "❌ Nicht geschlossen";
        SaveAndOpen("WR_Arc_Backwards", tracks, result);
    }

    private void SaveAndOpen(string name, List<SvgExporter.LabeledTrack> tracks, string result)
    {
        var svgPath = Path.Combine(_outputDir, $"{name}.svg");
        var htmlPath = Path.Combine(_outputDir, $"{name}.html");

        var svg = SvgExporter.Export(
            tracks,
            width: 1600,
            height: 1600,
            scale: 0.35,
            showLabels: true,
            showSegmentNumbers: true,
            showGrid: true,
            gridSize: 500,
            showOrigin: true);

        File.WriteAllText(svgPath, svg);

        var html = $@"<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <title>{name}</title>
    <style>
        body {{ margin: 0; padding: 20px; background: #f5f5f5; font-family: Arial, sans-serif; }}
        h1 {{ color: #333; }}
        .container {{ background: white; padding: 20px; border-radius: 8px; max-width: 1800px; margin: 0 auto; }}
        svg {{ border: 1px solid #ddd; display: block; margin: 20px auto; background: white; }}
        .info {{ background: #f0f0f0; padding: 10px; border-radius: 4px; margin: 10px 0; }}
    </style>
</head>
<body>
    <div class=""container"">
        <h1>🚂 {name}</h1>
        <div class=""info"">
            <strong>Ergebnis:</strong> {result}
        </div>
        {svg}
    </div>
</body>
</html>";
        File.WriteAllText(htmlPath, html);

        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = htmlPath,
            UseShellExecute = true
        });

        TestContext.WriteLine($"📁 {htmlPath}");
        TestContext.WriteLine($"✅ Browser: {result}\n");
    }

    [TearDown]
    public void TearDown()
    {
        TestContext.WriteLine($"📁 Alle Dateien: {_outputDir}");
    }
}
