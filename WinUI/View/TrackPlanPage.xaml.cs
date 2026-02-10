// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.WinUI.View;

using Common.Navigation;

using Microsoft.UI.Xaml.Controls;
using SharedUI.ViewModel;

[NavigationItem(
    Tag = "trackplaneditor",
    Title = "Track Plan",
    Category = NavigationCategory.TrackManagement,
    Order = 10,
    FeatureToggleKey = "IsTrackPlanEditorPageAvailable",
    BadgeLabelKey = "TrackPlanEditorPageLabel",
    PathIconData = "M2,3 L4,3 L14,13 L12,13 Z M12,3 L14,3 L4,13 L2,13 Z")]
public sealed partial class TrackPlanPage : Page
{
    public TrackPlanViewModel ViewModel { get; }

    public TrackPlanPage(TrackPlanViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
    }
}