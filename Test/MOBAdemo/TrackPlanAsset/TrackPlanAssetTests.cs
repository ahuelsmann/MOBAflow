// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.MOBAdemo.TrackPlanAsset;

using System.Globalization;
using System.Xml.Linq;

[TestFixture]
internal sealed class TrackPlanAssetTests
{
    private const int MaximumAssetBytes = 30_000;
    private static readonly string[] SurfaceBounds = ["x", "y", "width", "height"];
    private static readonly XNamespace Svg = "http://www.w3.org/2000/svg";
    private static readonly string RepoRoot = FindRepoRoot();
    private static readonly string AssetPath = Path.Combine(RepoRoot, "MOBAdemo", "app", "public", "assets", "track-plan.svg");

    [Test]
    public void CommittedLayout_IsStaticGeometryWithinItsBudget()
    {
        Assert.That(File.Exists(AssetPath), Is.True, "The demo layout SVG must be committed.");

        var document = XDocument.Load(AssetPath);
        var root = document.Root!;
        var viewBox = (root.Attribute("viewBox")?.Value ?? string.Empty)
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(part => double.Parse(part, CultureInfo.InvariantCulture)).ToArray();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(root.Name, Is.EqualTo(Svg + "svg"));
            Assert.That(root.Descendants(Svg + "path"), Is.Not.Empty);
            Assert.That(root.DescendantsAndSelf().Attributes().Where(attribute =>
                attribute.Name.LocalName is "style" or "href"
                || attribute.Name.LocalName.StartsWith("on", StringComparison.OrdinalIgnoreCase)
                || attribute.Value.Contains("url(", StringComparison.OrdinalIgnoreCase)), Is.Empty);
            Assert.That(root.Descendants().Where(element => element.Name.LocalName is "script" or "foreignObject" or "style" or "image"), Is.Empty);
            Assert.That(viewBox, Has.Length.EqualTo(4));
            Assert.That(viewBox.Skip(2), Is.All.Positive);
            Assert.That(new FileInfo(AssetPath).Length, Is.LessThanOrEqualTo(MaximumAssetBytes));
        }
    }

    [Test]
    public void CommittedLayout_EqualsTheRenderingOfTheShippedSample()
    {
        var samplePath = Path.Combine(RepoRoot, "MOBAflow", "track-plan.json");
        var rendering = DemoTrackPlanAsset.Render(samplePath);

        if (Environment.GetEnvironmentVariable("MOBADEMO_UPDATE_TRACK_PLAN_ASSET") == "1")
        {
            Directory.CreateDirectory(Path.GetDirectoryName(AssetPath)!);
            File.WriteAllText(AssetPath, rendering);
        }

        Assert.That(File.ReadAllText(AssetPath).ReplaceLineEndings("\n"), Is.EqualTo(rendering), "See MOBAdemo/README.md to regenerate the committed SVG.");
    }

    [Test]
    public void CommittedLayout_MeetsContrastOnItsOwnSurface()
    {
        var root = XDocument.Load(AssetPath).Root!;
        var surface = root.Elements(Svg + "rect").Single();
        var background = surface.Attribute("fill")!.Value;
        var viewBox = root.Attribute("viewBox")!.Value.Split(' ');

        using (Assert.EnterMultipleScope())
        {
            Assert.That(surface, Is.SameAs(root.Elements().First()));
            Assert.That(SurfaceBounds.Select(name => surface.Attribute(name)?.Value), Is.EqualTo(viewBox));
            Assert.That(root.DescendantsAndSelf().Attributes()
                .Where(attribute => attribute.Name.LocalName.EndsWith("opacity", StringComparison.Ordinal)), Is.Empty);
            Assert.That(root.Elements(Svg + "path").Select(path => ContrastRatio(path.Attribute("stroke")!.Value, background)),
                Is.All.GreaterThanOrEqualTo(3.0));
            Assert.That(root.Elements(Svg + "text"), Is.Not.Empty);
            Assert.That(root.Elements(Svg + "text").Select(label => ContrastRatio(label.Attribute("fill")!.Value, background)),
                Is.All.GreaterThanOrEqualTo(4.5));
        }
    }

    private static double ContrastRatio(string foreground, string background)
    {
        var foregroundLuminance = Luminance(foreground);
        var backgroundLuminance = Luminance(background);
        return (Math.Max(foregroundLuminance, backgroundLuminance) + 0.05)
            / (Math.Min(foregroundLuminance, backgroundLuminance) + 0.05);
    }

    private static double Luminance(string color)
    {
        static double Channel(string color, int start)
        {
            var value = int.Parse(color.AsSpan(start, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture) / 255.0;
            return value <= 0.04045 ? value / 12.92 : Math.Pow((value + 0.055) / 1.055, 2.4);
        }

        return 0.2126 * Channel(color, 1) + 0.7152 * Channel(color, 3) + 0.0722 * Channel(color, 5);
    }

    private static string FindRepoRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Moba.slnx")))
                return directory.FullName;
        }

        throw new DirectoryNotFoundException("Could not locate the MOBAflow checkout.");
    }
}
