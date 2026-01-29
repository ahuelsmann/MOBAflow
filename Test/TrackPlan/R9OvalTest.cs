// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.TrackPlan;

using Moba.TrackLibrary.PikoA.Catalog;
using Moba.TrackPlan.Graph;
using Moba.TrackPlan.Renderer.Service;

using System.Diagnostics;
using System.Text;

/// <summary>
/// Minimal test: Focus on track topology configuration only.
/// 3× R9 curves + 1× WR switch forming a circle.
/// </summary>
[TestFixture]
public class R9OvalTest
{
    [Test]
    public void R9CircleWithWR_ShouldMaintainCircleIntegrity()
    {
        // Build complete circle: 1× WR + 23× R9 = 24 segments
        // R9 angle = 15° → 360° / 15° = 24 segments for full circle
        var topology = new TopologyGraph().Add(PikoA.WR)
            .Port("C").ConnectTo(PikoA.R9).Port("A")
            .Port("B").ConnectTo(PikoA.R9).Port("A")
            .Port("B").ConnectTo(PikoA.R9).Port("A")
            .Port("B").ConnectTo(PikoA.R9).Port("A")
            .Port("B").ConnectTo(PikoA.R9).Port("A")
            .Port("B").ConnectTo(PikoA.R9).Port("A")
            .Port("B").ConnectTo(PikoA.R9).Port("A")
            .Port("B").ConnectTo(PikoA.R9).Port("A")
            .Port("B").ConnectTo(PikoA.R9).Port("A")
            .Port("B").ConnectTo(PikoA.R9).Port("A")
            .Port("B").ConnectTo(PikoA.R9).Port("A")
            .Port("B").ConnectTo(PikoA.R9).Port("A")
            .Port("B").ConnectTo(PikoA.R9).Port("A")
            .Port("B").ConnectTo(PikoA.R9).Port("A")
            .Port("B").ConnectTo(PikoA.R9).Port("A")
            .Port("B").ConnectTo(PikoA.R9).Port("A")
            .Port("B").ConnectTo(PikoA.R9).Port("A")
            .Port("B").ConnectTo(PikoA.R9).Port("A")
            .Port("B").ConnectTo(PikoA.R9).Port("A")
            .Port("B").ConnectTo(PikoA.R9).Port("A")
            .Port("B").ConnectTo(PikoA.R9).Port("A")
            .Port("B").ConnectTo(PikoA.R9).Port("A")
            .Port("B").ConnectTo(PikoA.R9).Port("A")
            .Port("B").ConnectTo(PikoA.R9).Port("A")
            .Port("B").ConnectTo(PikoA.R9).Port("A")
            .Port("B").ConnectTo(PikoA.R9).Port("A")
            .Port("B").ConnectTo(PikoA.R9).Port("A")
            .Port("B").ConnectTo(PikoA.R9).Port("A")
            .Port("B").ConnectTo(PikoA.R9).Port("A")
            .Port("B").ConnectTo(PikoA.R9).Port("A")
            .Port("B").ConnectTo(PikoA.R9).Port("A")
            .Port("B").ConnectTo(PikoA.R9).Port("A")
            .Port("B").ConnectTo(PikoA.R9).Port("A")
            .Port("B").ConnectTo(PikoA.WR).Port("A")
            .Create();

        // Verify
        Assert.That(topology.Edges, Has.Count.EqualTo(24), "Should have exactly 24 segments (1× WR + 23× R9)");

        // Render & export SVG (Piko A catalog used implicitly via PikoA factories)
        var catalog = new PikoATrackCatalog();
        var renderer = new TopologyGraphRenderer(catalog);
        var primitives = renderer.Render(topology, 0, 0, 0).ToList();
        var svg = SvgExporter.Export(primitives, 1000, 1000, 0.35, 2.5, "#0078d4", true, true, 100);

        var htmlPath = Path.Combine(TestContext.CurrentContext.TestDirectory, "r9-circle-wr.html");
        var html = "<!DOCTYPE html><html><head><meta charset=\"UTF-8\"><title>R9 Circle with WR</title>" +
                   "<style>body{margin:0;padding:20px;background:#1e1e1e;display:flex;justify-content:center;align-items:center;min-height:100vh}" +
                   "svg{background:white;box-shadow:0 4px 6px rgba(0,0,0,0.3)}</style></head><body>" + svg + "</body></html>";

        File.WriteAllText(htmlPath, html, Encoding.UTF8);
        TestContext.WriteLine($"SVG: {htmlPath}");

        try
        {
            Process.Start(new ProcessStartInfo { FileName = htmlPath, UseShellExecute = true });
        }
        catch { }
    }
}
