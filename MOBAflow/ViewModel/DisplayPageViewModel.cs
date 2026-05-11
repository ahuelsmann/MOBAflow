// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.ViewModel;

using CommunityToolkit.Mvvm.ComponentModel;

using Controls.Matrix;

/// <summary>
/// ViewModel for the DisplayPage hosting the 5x5 LED matrix display.
/// </summary>
public sealed partial class DisplayPageViewModel : ObservableObject
{
    /// <summary>
    /// The 5x5 matrix display ViewModel.
    /// </summary>
    [ObservableProperty]
    private ViewModel5x5 matrixViewModel = new();
}
