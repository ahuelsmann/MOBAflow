// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.View;

using ViewModel;

/// <summary>
/// Display page showing the 5x5 LED matrix display.
/// </summary>
internal sealed partial class DisplayPage
{
    public DisplayPageViewModel ViewModel { get; }

    public DisplayPage(DisplayPageViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
    }
}
