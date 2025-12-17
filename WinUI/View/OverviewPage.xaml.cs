// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.View;

using SharedUI.ViewModel;

/// <summary>
/// Overview page showing the Lap Counter Dashboard with Z21 connection and track statistics.
/// This page provides the same functionality as MAUI MainPage and WebApp Counter page.
/// </summary>
public sealed partial class OverviewPage
{
    public CounterViewModel ViewModel { get; }

    public OverviewPage(CounterViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
    }
}
