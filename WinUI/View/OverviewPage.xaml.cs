// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.View;

using SharedUI.ViewModel;

/// <summary>
/// Overview page showing the Lap Counter Dashboard with Z21 connection and track statistics.
/// Uses MainWindowViewModel (unified cross-platform ViewModel).
/// </summary>
public sealed partial class OverviewPage
{
    public MainWindowViewModel ViewModel { get; }

    public OverviewPage(MainWindowViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
    }
}