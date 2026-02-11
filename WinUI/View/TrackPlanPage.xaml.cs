// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.WinUI.View;

using Common.Navigation;

using Microsoft.UI.Xaml.Controls;

using SharedUI.ViewModel;

[NavigationItem(
    Tag = "trackplaneditor",
    Title = "Track Plan",
    Icon = "\uE7F9",
    Category = NavigationCategory.TrackManagement,
    Order = 10,
    FeatureToggleKey = "IsTrackPlanEditorPageAvailable",
    BadgeLabelKey = "TrackPlanEditorPageLabel")]
public sealed partial class TrackPlanPage : Page
{
    public TrackPlanViewModel ViewModel { get; }

    public TrackPlanPage(TrackPlanViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
    }
}