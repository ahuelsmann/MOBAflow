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
            Assert.That(compactPage, Does.Contain("Context=\"{x:BindInterlockingViewModel}\""));
            Assert.That(compactPage, Does.Contain("controls:FocusTargetBehavior.Target=\"{x:BindSelectedObjectWorkbench,Mode=OneTime}\""));
            Assert.That(compactPage, Does.Not.Contain("Click=\"OnContextButtonClick\""));
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
            Assert.That(compactWorkbench, Does.Contain("SelectedItem=\"{x:BindViewModel.SelectedDraftOperationalElement,Mode=TwoWay}\""));
            Assert.That(compactWorkbench, Does.Contain("SelectedItem=\"{x:BindViewModel.SelectedDraftTurnout,Mode=TwoWay}\""));
            Assert.That(compactWorkbench, Does.Contain("SelectedItem=\"{x:BindViewModel.SelectedDraftBlock,Mode=TwoWay}\""));
            Assert.That(compactWorkbench, Does.Contain("SelectedItem=\"{x:BindViewModel.SelectedDraftSignal,Mode=TwoWay}\""));
            Assert.That(compactWorkbench, Does.Contain("AutomationProperties.Name=\"Turnoutposition\""));
            Assert.That(compactWorkbench, Does.Contain("AutomationProperties.Name=\"Proceedaspect\""));
            Assert.That(compactWorkbench, Does.Contain("ViewModel.ShowNoAuthorizedLiveActionMessage"));
            Assert.That(compactWorkbench, Does.Contain("ViewModel.IsRoutineCancelRouteVisible"));
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

        // Act
        var compactPage = string.Concat(page.Where(character => !char.IsWhiteSpace(character)));

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(compactPage, Does.Contain("AdaptiveTriggerMinWindowWidth=\"1024\""));
            Assert.That(compactPage, Does.Contain("CompactWorkbenchState"));
            Assert.That(compactPage, Does.Contain("Panel.ZIndex"));
            Assert.That(compactPage, Does.Contain("MaxWidth=\"420\""));
            Assert.That(compactPage, Does.Not.Contain("Target=\"WorkbenchHost.Width\""));
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
