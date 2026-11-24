// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.MOBAdemo.TrackPlanAsset;

using System.Globalization;
using System.Xml.Linq;

[TestFixture]
internal sealed class TrackPlanAssetTests
{
    private const int MaximumAssetBytes = 200_000;
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
            Assert.That(root.DescendantsAndSelf().Attributes().Where(attribute => attribute.Name.LocalName is "style" or "href"), Is.Empty);
            Assert.That(root.Descendants().Where(element => element.Name.LocalName is "script" or "foreignObject"), Is.Empty);
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

        Assert.That(File.ReadAllText(AssetPath).ReplaceLineEndings("\n"), Is.EqualTo(rendering));
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
