// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Test.WinUI;

using System.Xml.Linq;

[TestFixture]
internal sealed class XamlIconMarkupTests
{
    [TestCase("EventManagerPage.xaml", "MoveSelectedEventDownCommand")]
    [TestCase("WorkflowsPage.xaml", "MoveStepDownCommand")]
    public void MoveDownButton_ShouldUseExplicitFluentFontIcon(string pageFileName, string commandName)
    {
        // Arrange
        var repositoryRoot = FindRepositoryRoot();
        var page = XDocument.Load(Path.Combine(repositoryRoot, "MOBAflow", "View", pageFileName));

        // Act
        var moveDownButton = page
            .Descendants()
            .Single(element =>
                element.Name.LocalName == "AppBarButton"
                && element.Attribute("Command")?.Value.Contains(commandName, StringComparison.Ordinal) == true);
        var fontIcon = moveDownButton
            .Descendants()
            .Single(element => element.Name.LocalName == "FontIcon");

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(moveDownButton.Attribute("Icon"), Is.Null);
            Assert.That(
                fontIcon.Attribute("FontFamily")?.Value,
                Is.EqualTo("{StaticResource SymbolThemeFontFamily}"));
            Assert.That(fontIcon.Attribute("Glyph")?.Value, Is.EqualTo("\uE74B"));
        }
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
