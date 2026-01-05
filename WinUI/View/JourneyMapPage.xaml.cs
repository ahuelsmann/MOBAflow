// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.View;

using SharedUI.ViewModel;

/// <summary>
/// Journey Map page showing the virtual route with station progress.
/// Displays schematic station-to-station visualization with current position indicator.
/// </summary>
// ReSharper disable once PartialTypeWithSinglePart
public sealed partial class JourneyMapPage
{
    public JourneyMapViewModel ViewModel { get; }

    public JourneyMapPage(JourneyMapViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
    }
}