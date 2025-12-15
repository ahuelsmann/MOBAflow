// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.View;

using Microsoft.UI.Xaml.Controls;

using SharedUI.ViewModel;

/// <summary>
/// Track Plan page showing the physical layout imported from AnyRail.
/// Displays sensor positions (InPort markers) and current train location.
/// </summary>
public sealed partial class TrackPlanPage : Page
{
    public TrackPlanViewModel ViewModel { get; }

    public TrackPlanPage(TrackPlanViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
    }
}
