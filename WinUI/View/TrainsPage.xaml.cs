// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.View;

using Common.Navigation;

using SharedUI.ViewModel;

[NavigationItem(
    Tag = "trains",
    Title = "Trains",
    Icon = "\uE7C0",
    Category = NavigationCategory.Solution,
    Order = 30,
    FeatureToggleKey = "IsTrainsPageAvailable",
    BadgeLabelKey = "TrainsPageLabel")]
public sealed partial class TrainsPage
{
    public MainWindowViewModel ViewModel { get; }

    public TrainsPage(MainWindowViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
    }
}