// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.SharedUI;

using Moba.Common.Configuration;
using Moba.SharedUI.ViewModel;

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
}
