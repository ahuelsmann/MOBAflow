// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.View;

using Microsoft.UI.Xaml.Controls;
using SharedUI.ViewModel;

/// <summary>
/// Journey Map page showing the virtual route with station progress.
/// Displays schematic station-to-station visualization with current position indicator.
/// </summary>
// ReSharper disable once PartialTypeWithSinglePart
public sealed partial class JourneyMapPage : Page
{
    public JourneyMapViewModel ViewModel { get; }

    public JourneyMapPage(JourneyMapViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
    }
}
