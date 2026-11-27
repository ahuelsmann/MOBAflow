// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.SharedUI;

using Moba.SharedUI.Configuration;
using Moba.SharedUI.Desktop.ViewModel;

[TestFixture]
internal sealed class LayoutColumnWidthsViewModelTests
{
    [Test]
    public void ClearColumnWidth_RemovesObservableAndPersistedPixelWidth()
    {
        var layout = new LayoutSettings();
        layout.ColumnWidths["LocomotivesPage:2"] = 420;
        var viewModel = new LayoutColumnWidthsViewModel();
        viewModel.LoadFrom(layout);

        Assert.That(viewModel.GetColumnWidth("LocomotivesPage", 2), Is.EqualTo(420));

        viewModel.ClearColumnWidth("LocomotivesPage", 2, layout);

        Assert.That(viewModel.GetColumnWidth("LocomotivesPage", 2), Is.EqualTo(0));
        Assert.That(layout.ColumnWidths.ContainsKey("LocomotivesPage:2"), Is.False);
    }

    [Test]
    public void LoadFromAfterResizingResetsDefaultsAndClearsZeroDefaultColumns()
    {
        var viewModel = new LayoutColumnWidthsViewModel();
        viewModel.LoadFrom(new LayoutSettings());

        viewModel.SetColumnWidth("JourneysPage", 0, 410);
        viewModel.SetColumnWidth("JourneysPage", 1, 160);
        viewModel.SetColumnWidth("JourneysPage", 2, 330);
        viewModel.SetColumnWidth("JourneysPage", 8, 520);
        viewModel.SetColumnWidth("LocomotivesPage", 0, 380);
        viewModel.SetColumnWidth("LocomotivesPage", 2, 420);
        viewModel.SetColumnWidth("GoodsWagonPage", 0, 360);
        viewModel.SetColumnWidth("GoodsWagonPage", 1, 120);
        viewModel.SetColumnWidth("MonitorPage", 15, 220);

        var locomotivesChanged = new List<string?>();
        var journeysChanged = new List<string?>();
        viewModel.LocomotivesPage.PropertyChanged += (_, e) => locomotivesChanged.Add(e.PropertyName);
        viewModel.JourneysPage.PropertyChanged += (_, e) => journeysChanged.Add(e.PropertyName);

        viewModel.LoadFrom(new LayoutSettings());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(viewModel.GetColumnWidth("JourneysPage", 0), Is.EqualTo(250));
            Assert.That(viewModel.GetColumnWidth("JourneysPage", 1), Is.Zero);
            Assert.That(viewModel.GetColumnWidth("JourneysPage", 2), Is.EqualTo(250));
            Assert.That(viewModel.GetColumnWidth("JourneysPage", 8), Is.EqualTo(400));
            Assert.That(viewModel.GetColumnWidth("LocomotivesPage", 0), Is.EqualTo(250));
            Assert.That(viewModel.GetColumnWidth("LocomotivesPage", 2), Is.Zero);
            Assert.That(viewModel.GetColumnWidth("GoodsWagonPage", 0), Is.EqualTo(250));
            Assert.That(viewModel.GetColumnWidth("GoodsWagonPage", 1), Is.Zero);
            Assert.That(viewModel.GetColumnWidth("MonitorPage", 15), Is.Zero);
            Assert.That(locomotivesChanged, Does.Contain(nameof(PageColumnWidthsViewModel.Col0)));
            Assert.That(locomotivesChanged, Does.Contain(nameof(PageColumnWidthsViewModel.Col2)));
            Assert.That(journeysChanged, Does.Contain(nameof(PageColumnWidthsViewModel.Col1)));
        }
    }
}
