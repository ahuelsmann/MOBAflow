// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.TrackPlanRenderer;

using TrackPlan.Renderer;

[TestFixture]
internal sealed class SvgExporterTests
{
    [Test]
    public void Export_WritesHtmlPageThatEmbedsTheSvg()
    {
        const string svg = "<svg xmlns=\"http://www.w3.org/2000/svg\"><path d=\"M 0 0 L 10 0\" /></svg>";
        var outputPath = Path.Combine(Path.GetTempPath(), $"moba-trackplan-{Guid.NewGuid():N}.html");

        try
        {
            new SvgExporter().Export(svg, outputPath);

            var html = File.ReadAllText(outputPath);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(html, Does.StartWith("<!DOCTYPE html>"));
                Assert.That(html, Does.Contain("<title>Track Plan</title>"));
                Assert.That(html, Does.Contain(svg));
            }
        }
        finally
        {
            File.Delete(outputPath);
        }
    }
}
