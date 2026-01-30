// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
using Microsoft.UI.Xaml.Controls;

namespace Moba.WinUI.View;

using SharedUI.ViewModel;

public sealed partial class TrackPlanPage : Page
{
    public TrackPlanViewModel ViewModel { get; }

    public TrackPlanPage(TrackPlanViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
    }
}