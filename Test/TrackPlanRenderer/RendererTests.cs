// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.TrackPlanRenderer;

using Moba.TrackLibrary.PikoA;
using System.Text.Json;
using TrackPlan.Renderer;

[TestFixture]
internal class RendererTests
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

    [TestCase(typeof(WL))]
    [TestCase(typeof(WY))]
    [TestCase(typeof(W3))]
    [TestCase(typeof(BWL))]
    [TestCase(typeof(BWR))]
    [TestCase(typeof(DKW))]
    [TestCase(typeof(K15))]
    [TestCase(typeof(K30))]
    public void Render_WithPreviouslySkippedSegmentType_DrawsSegmentPath(Type segmentType)
    {
        var segment = (Moba.TrackLibrary.Base.Segment)Activator.CreateInstance(segmentType)!;
        segment.No = Guid.NewGuid();
        var plan = new TrackPlanResult
        {
            Segments = [segment],
            Connections = [],
            StartAngleDegrees = 0
        };

        var renderResult = new TrackPlanSvgRenderer().Render(plan);

        Assert.That(renderResult.Placements, Has.Count.EqualTo(1));
        Assert.That(renderResult.Svg, Does.Contain("<path"));
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

        _ = JsonSerializer.Serialize(plan);

        var renderer = new TrackPlanSvgRenderer();
        var renderResult = renderer.Render(plan);

        var exporter = new SvgExporter();
        var outputPath = Path.Combine(Path.GetTempPath(), "trackplan3.html");
        exporter.Export(renderResult.Svg, outputPath);

        Assert.Multiple(() =>
        {
            Assert.That(renderResult.Svg, Does.Contain("<svg"));
            Assert.That(File.Exists(outputPath), Is.True);
        });

        Console.WriteLine($"Track plan exported to: {outputPath}");
    }
}
