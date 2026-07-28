// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Test.WinUI;

[TestFixture]
[Category("UI")]
internal sealed class SignalBoxPageSelectionTests
{
    [Test]
    public void CanvasPointerHandlers_ShouldObserveEventsHandledByScrollViewer()
    {
        // Arrange
        var codeBehind = File.ReadAllText(Path.Combine(
            FindRepositoryRoot(),
            "MOBAflow",
            "Controls",
            "SignalBox",
            "SignalBoxCanvasControl.xaml.cs"));

        // Act
        var compactCode = string.Concat(codeBehind.Where(character => !char.IsWhiteSpace(character)));

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(
                compactCode,
                Does.Contain(
                    "CanvasHost.AddHandler(UIElement.PointerPressedEvent,"
                    + "newPointerEventHandler(OnCanvasPointerPressed),true);"));
            Assert.That(compactCode, Does.Not.Contain("if(e.Handled)return;"));
        }
    }

    [Test]
    public void SelectedElementHandler_ShouldNotRebuildSignalVisualDuringPointerEvent()
    {
        // Arrange
        var codeBehind = File.ReadAllText(Path.Combine(
            FindRepositoryRoot(),
            "MOBAflow",
            "View",
            "SignalBoxPage.xaml.cs"));
        var handlerStart = codeBehind.IndexOf(
            "private void OnPlanViewModelPropertyChanged",
            StringComparison.Ordinal);
        var handlerEnd = codeBehind.IndexOf(
            "private void DetachPlanViewModel",
            handlerStart,
            StringComparison.Ordinal);

        // Act
        var handler = codeBehind[handlerStart..handlerEnd];

        // Assert
        Assert.That(
            handler,
            Does.Not.Contain("RefreshElementVisual"),
            "Selection must not mutate the ItemsSource while its pointer event is still routed.");
    }

    [TestCase("TrackPlanPage.xaml")]
    [TestCase("SignalBoxPage.xaml")]
    public void PagesUseContextWorkbenchWithoutHorizontalInterlockingRow(string pageName)
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
    public void WorkbenchExposesAccessibleVerticalSectionsAndDiagnostics()
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
    public void PagesSwitchWorkbenchToOverlayAtCompactWidth(string pageName)
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
