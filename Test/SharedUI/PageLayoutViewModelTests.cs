// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.SharedUI;

using Moba.SharedUI.Configuration;
using Moba.SharedUI.Desktop.ViewModel;

using System.Collections;

[TestFixture]
internal sealed partial class PageLayoutViewModelTests
{
    [Test]
    public void ApplyToGrid_MapsColumnsByNameOrder()
    {
        var viewModel = CreateViewModel();
        var grid = new FakeGrid(new FakeColumnDefinition(), new FakeColumnDefinition());

        viewModel.ApplyToGrid(grid);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(grid.Column(0).Width.Value, Is.EqualTo(300), "Column 0 is 'Left' (alphabetical order).");
            Assert.That(grid.Column(1).Width.Value, Is.EqualTo(500), "Column 1 is 'Right'.");
        }
    }

    [Test]
    public void ApplyToGrid_CollapsedColumnGetsZeroWidth()
    {
        var viewModel = CreateViewModel();
        viewModel.SetColumnExpanded("Right", false);
        var grid = new FakeGrid(new FakeColumnDefinition(), new FakeColumnDefinition());

        viewModel.ApplyToGrid(grid);

        Assert.That(grid.Column(1).Width.Value, Is.Zero);
    }

    [Test]
    public void ApplyToGrid_SkipsNullDefinitionsAndColumnsBeyondTheLayout()
    {
        var viewModel = CreateViewModel();
        var extra = new FakeColumnDefinition { Width = new FakeGridLength(42) };
        var grid = new FakeGrid(null, new FakeColumnDefinition(), extra);

        viewModel.ApplyToGrid(grid);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(grid.Column(1).Width.Value, Is.EqualTo(500));
            Assert.That(extra.Width.Value, Is.EqualTo(42), "A grid column without a layout column stays unchanged.");
        }
    }

    [Test]
    public void ApplyToGrid_IgnoresNullAndGridWithoutColumnDefinitions()
    {
        var viewModel = CreateViewModel();

        using (Assert.EnterMultipleScope())
        {
            Assert.DoesNotThrow(() => viewModel.ApplyToGrid(null));
            Assert.DoesNotThrow(() => viewModel.ApplyToGrid(new object()));
        }
    }

    [Test]
    public void ApplyToGrid_SkipsIndexWithoutColumnName()
    {
        var viewModel = new NamelessSecondColumnViewModel();
        var second = new FakeColumnDefinition { Width = new FakeGridLength(42) };
        var grid = new FakeGrid(new FakeColumnDefinition(), second);

        viewModel.ApplyToGrid(grid);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(grid.Column(0).Width.Value, Is.EqualTo(300));
            Assert.That(second.Width.Value, Is.EqualTo(42));
        }
    }

    [Test]
    public void UpdateFromGrid_ReadsWidthsAndCollapsedState()
    {
        var viewModel = CreateViewModel();
        var grid = new FakeGrid(
            new FakeColumnDefinition { Width = new FakeGridLength(320) },
            new FakeColumnDefinition { Width = new FakeGridLength(0) });

        viewModel.UpdateFromGrid(grid);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(viewModel.Columns["Left"].Width, Is.EqualTo(320));
            Assert.That(viewModel.Columns["Left"].IsExpanded, Is.True);
            Assert.That(viewModel.Columns["Right"].IsExpanded, Is.False);
            Assert.That(viewModel.Columns["Right"].Width, Is.Zero, "A collapsed column reports no width.");
        }
    }

    [Test]
    public void UpdateFromGrid_SkipsNullDefinitionsAndIgnoresMissingGrid()
    {
        var viewModel = CreateViewModel();
        var grid = new FakeGrid(null, new FakeColumnDefinition { Width = new FakeGridLength(450) });

        viewModel.UpdateFromGrid(grid);
        viewModel.UpdateFromGrid(null);
        viewModel.UpdateFromGrid(new object());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(viewModel.Columns["Left"].Width, Is.EqualTo(300));
            Assert.That(viewModel.Columns["Right"].Width, Is.EqualTo(450));
        }
    }

    [Test]
    public void UpdateFromGrid_SkipsIndexWithoutColumnName()
    {
        var viewModel = new NamelessSecondColumnViewModel();
        var grid = new FakeGrid(
            new FakeColumnDefinition { Width = new FakeGridLength(310) },
            new FakeColumnDefinition { Width = new FakeGridLength(0) });

        viewModel.UpdateFromGrid(grid);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(viewModel.Columns["Left"].Width, Is.EqualTo(310));
            Assert.That(viewModel.Columns["Right"].IsExpanded, Is.True);
        }
    }

    private static PageLayoutViewModel CreateViewModel()
    {
        var viewModel = new PageLayoutViewModel("TestPage", new UiSettings());
        viewModel.AddColumn("Right", 500);
        viewModel.AddColumn("Left", 300);
        return viewModel;
    }

    private sealed partial class NamelessSecondColumnViewModel : PageLayoutViewModel
    {
        public NamelessSecondColumnViewModel()
            : base("TestPage", new UiSettings())
        {
            AddColumn("Left", 300);
            AddColumn("Right", 500);
        }

        protected override string? GetColumnNameByIndex(int index) => index == 0 ? "Left" : null;
    }

    private sealed class FakeGrid(params FakeColumnDefinition?[] columns)
    {
        public ArrayList ColumnDefinitions { get; } = new(columns);

        public FakeColumnDefinition Column(int index) => (FakeColumnDefinition)ColumnDefinitions[index]!;
    }

    private sealed class FakeColumnDefinition
    {
        public FakeGridLength Width { get; set; } = new(-1);
    }

    private sealed class FakeGridLength(double value)
    {
        public double Value { get; } = value;
    }
}
