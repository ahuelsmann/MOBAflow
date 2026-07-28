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