namespace Moba.Test.TrackPlanRenderer;

using Newtonsoft.Json;

using System.Diagnostics;

using TrackLibrary.PikoA;

using TrackPlan.Renderer;

[TestFixture]
public class RendererTests
{
    [Test]
    public void Render_PlacementsCount_MatchesSegmentsCount()
    {
        var plan = new TrackPlanBuilder()
            .Start(0)
            .Add<WR>().Connections(
                wr => wr.FromA.ToB<R9>().FromA.ToA<G62>(),
                wr => wr.FromB.ToA<G239>().FromB.ToA<G62>(),
                wr => wr.FromC.ToA<R9>().FromB.ToA<R9>().FromB.ToA<G62>())
            .Create();

        var renderResult = new TrackPlanSvgRenderer().Render(plan);

        Assert.That(renderResult.Placements.Count, Is.EqualTo(plan.Segments.Count),
            "Platzierungen müssen für jedes Segment erzeugt werden.");
        Assert.That(renderResult.Svg, Does.Contain("<svg"));
    }

    [Test]
    public void TrackPlan()
    {
        var plan = new TrackPlanBuilder()
            .Start(0)
            .Add<WR>().Connections(
                wr => wr.FromA.ToB<R9>().FromA.ToA<G62>(),
                wr => wr.FromB.ToA<G239>().FromB.ToA<G62>(),
                wr => wr.FromC.ToA<R9>().FromB.ToA<R9>().FromB.ToA<G62>())
            .Create();

        string p = JsonConvert.SerializeObject(plan);

        var renderer = new TrackPlanSvgRenderer();
        var renderResult = renderer.Render(plan);

        var exporter = new SvgExporter();
        var outputPath = Path.Combine(Path.GetTempPath(), "trackplan3.html");
        exporter.Export(renderResult.Svg, outputPath);

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