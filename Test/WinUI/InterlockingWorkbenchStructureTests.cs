// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Test.WinUI;

[TestFixture]
[Category("UI")]
internal sealed class InterlockingWorkbenchStructureTests
{
    [TestCase("TrackPlanPage.xaml")]
    [TestCase("SignalBoxPage.xaml")]
    public void Page_ShouldUseContextWorkbenchWithoutHorizontalInterlockingRow(string pageName)
    {
        // Arrange
        var page = File.ReadAllText(Path.Combine(
            FindRepositoryRoot(),
            "MOBAflow",
            "View",
            pageName));

        // Act
        var compactPage = string.Concat(page.Where(character => !char.IsWhiteSpace(character)));

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(compactPage, Does.Contain("Label=\"Context\""));
            Assert.That(compactPage, Does.Contain("<controls:SelectedObjectWorkbench"));
            Assert.That(compactPage, Does.Not.Contain("HorizontalScrollMode=\"Enabled\"><StackPanelOrientation=\"Horizontal\""));
            Assert.That(compactPage, Does.Not.Contain("InterlockingViewModel.Revision"));
        }
    }

    [Test]
    public void Workbench_ShouldExposeAccessibleVerticalSectionsAndDiagnostics()
    {
        // Arrange
        var workbench = File.ReadAllText(Path.Combine(
            FindRepositoryRoot(),
            "MOBAflow",
            "Controls",
            "SelectedObjectWorkbench.xaml"));

        // Act
        var compactWorkbench = string.Concat(workbench.Where(character => !char.IsWhiteSpace(character)));

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(compactWorkbench, Does.Contain("Text=\"Safety\""));
            Assert.That(compactWorkbench, Does.Contain("Text=\"Currentobject\""));
            Assert.That(compactWorkbench, Does.Contain("Text=\"Liveaction\""));
            Assert.That(compactWorkbench, Does.Contain("Text=\"Definition\""));
            Assert.That(compactWorkbench, Does.Contain("Header=\"Diagnostics\""));
            Assert.That(compactWorkbench, Does.Contain("IsExpanded=\"False\""));
            Assert.That(compactWorkbench, Does.Contain("AutomationProperties.LiveSetting=\"Polite\""));
            Assert.That(compactWorkbench, Does.Contain("HorizontalScrollMode=\"Disabled\""));
            Assert.That(compactWorkbench, Does.Not.Contain("TextTrimming=\"CharacterEllipsis\""));
        }
    }

    [TestCase("TrackPlanPage.xaml")]
    [TestCase("SignalBoxPage.xaml")]
    public void Page_ShouldSwitchWorkbenchToOverlayAtCompactWidth(string pageName)
    {
        // Arrange
        var page = File.ReadAllText(Path.Combine(
            FindRepositoryRoot(),
            "MOBAflow",
            "View",
            pageName));

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(page, Does.Contain("AdaptiveTrigger MinWindowWidth=\"1024\""));
            Assert.That(page, Does.Contain("CompactWorkbenchState"));
            Assert.That(page, Does.Contain("Panel.ZIndex"));
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
