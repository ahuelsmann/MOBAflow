namespace Moba.Test.TrackPlanRenderer;

using System.Diagnostics;

using TrackLibrary.PikoA;

using TrackPlan.Renderer;

[TestFixture]
public class RendererTests
{
    [Test]
    public void TrackPlan1()
    {
        var plan = new TrackPlanBuilder()
            .Add<WR>().FromC
            .ToA<R9>().FromB
            .ToA<R9>()
            .Create();

        Assert.That(plan.Segments, Has.Count.EqualTo(3));

        var wr = plan.Segments.OfType<WR>().Single();
        var r9List = plan.Segments.OfType<R9>().ToList();

        Assert.That(r9List, Has.Count.EqualTo(2));

        // WR.PortC -> R9(1).PortA
        Assert.That(wr.PortC, Is.EqualTo(r9List[0].No));
        Assert.That(r9List[0].PortA, Is.EqualTo(wr.No));

        // R9(1).PortB -> R9(2).PortA
        Assert.That(r9List[0].PortB, Is.EqualTo(r9List[1].No));
        Assert.That(r9List[1].PortA, Is.EqualTo(r9List[0].No));
    }

    [Test]
    public void TrackPlan2()
    {
        var plan = new TrackPlanBuilder()
            .Add<WR>().Connections(
                wr => wr.FromA
                    .ToB<R9>(),
                wr => wr.FromC
                    .ToA<R9>().FromB
                    .ToA<R9>())
            .Create();

        Assert.That(plan.Segments, Has.Count.EqualTo(4)); // WR + 3x R9

        var wr = plan.Segments.OfType<WR>().Single();
        var r9List = plan.Segments.OfType<R9>().ToList();

        Assert.That(r9List, Has.Count.EqualTo(3));

        // WR.PortA -> R9(0).PortB
        Assert.That(wr.PortA, Is.EqualTo(r9List[0].No));
        Assert.That(r9List[0].PortB, Is.EqualTo(wr.No));

        // WR.PortC -> R9(1).PortA
        Assert.That(wr.PortC, Is.EqualTo(r9List[1].No));
        Assert.That(r9List[1].PortA, Is.EqualTo(wr.No));

        // R9(1).PortB -> R9(2).PortA
        Assert.That(r9List[1].PortB, Is.EqualTo(r9List[2].No));
        Assert.That(r9List[2].PortA, Is.EqualTo(r9List[1].No));
    }

    [Test]
    public void TrackPlan3()
    {
        var plan = new TrackPlanBuilder()
            .Start(0)
            .Add<WR>().Connections(
                wr => wr.FromA
                    .ToB<R9>(),
                wr => wr.FromC
                    .ToA<R9>().FromB
                    .ToA<R9>()).Create();

        var renderer = new TrackPlanSvgRenderer();
        var svg = renderer.Render(plan);

        var exporter = new SvgExporter();
        var outputPath = Path.Combine(Path.GetTempPath(), "trackplan3.html");
        exporter.Export(svg, outputPath);

        Console.WriteLine($"Track plan exported to: {outputPath}");

        if (OperatingSystem.IsWindows())
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = outputPath,
                UseShellExecute = true
            });
        }
    }
}