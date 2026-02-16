// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.View;

using Common.Navigation;

using SharedUI.ViewModel;

[NavigationItem(
    Tag = "locomotives",
    Title = "Locomotives",
    Icon = "\uE7C0",
    Category = NavigationCategory.Solution,
    Order = 25,
    FeatureToggleKey = "IsLocomotivesPageAvailable",
    BadgeLabelKey = "LocomotivesPageLabel")]
public sealed partial class LocomotivesPage
{
    public MainWindowViewModel ViewModel { get; }

    public LocomotivesPage(MainWindowViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
    }
}
