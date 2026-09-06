// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Test.WinUI;

using System.Xml.Linq;

[TestFixture]
[Category("UI")]
internal sealed class InfoPageMarkupTests
{
    [Test]
    public void InfoPage_ShouldUseNativeWinUiContent()
    {
        // Arrange
        var page = LoadInfoPage();

        // Act
        var elementNames = page.Descendants().Select(element => element.Name.LocalName).ToArray();
        var textValues = page
            .Descendants()
            .Select(element => element.Attribute("Text")?.Value)
            .OfType<string>()
            .ToArray();

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(elementNames, Does.Not.Contain("MarkdownTextBlock"));
            Assert.That(textValues.Any(value => value.Contains("<div", StringComparison.OrdinalIgnoreCase)), Is.False);
            Assert.That(textValues.Any(value => value.Contains("<table", StringComparison.OrdinalIgnoreCase)), Is.False);
            Assert.That(textValues.Any(value => value.Contains("README.md", StringComparison.OrdinalIgnoreCase)), Is.False);
        }
    }

    [Test]
    public void InfoPage_ShouldDescribeCurrentProductAndSafetyScope()
    {
        // Arrange
        var page = LoadInfoPage();

        // Act
        var visibleText = string.Join(
            "\n",
            page.Descendants().SelectMany(element => new[]
            {
                element.Attribute("Text")?.Value,
                element.Attribute("Content")?.Value
            }).OfType<string>());

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(visibleText.Contains("interlocking", StringComparison.OrdinalIgnoreCase), Is.True);
            Assert.That(visibleText.Contains("Recorder", StringComparison.Ordinal), Is.True);
            Assert.That(visibleText.Contains("authenticated pairing", StringComparison.OrdinalIgnoreCase), Is.True);
            Assert.That(visibleText.Contains("trusted private LAN", StringComparison.Ordinal), Is.True);
            Assert.That(visibleText.Contains("public internet", StringComparison.Ordinal), Is.True);
        }
    }

    [Test]
    public void InfoPageLinks_ShouldUseAbsoluteHttpsTargets()
    {
        // Arrange
        var page = LoadInfoPage();

        // Act
        var links = page
            .Descendants()
            .Where(element => element.Name.LocalName == "HyperlinkButton")
            .Select(element => element.Attribute("NavigateUri")?.Value)
            .OfType<string>()
            .ToArray();

        // Assert
        Assert.That(links, Has.Length.GreaterThanOrEqualTo(6));
        Assert.That(
            links,
            Has.All.Matches<string>(link =>
                Uri.TryCreate(link, UriKind.Absolute, out var uri)
                && uri.Scheme == Uri.UriSchemeHttps));
    }

    private static XDocument LoadInfoPage()
    {
        var repositoryRoot = FindRepositoryRoot();
        return XDocument.Load(Path.Combine(repositoryRoot, "MOBAflow", "View", "InfoPage.xaml"));
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "Moba.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName
            ?? throw new DirectoryNotFoundException("Could not locate the MOBAflow repository root.");
    }
}
