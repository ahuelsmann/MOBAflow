// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.Controls.Matrix;

using Microsoft.UI.Xaml.Controls;

public sealed partial class View5x5 : UserControl
{
    /// <summary>
    /// The ViewModel for the 5x5 matrix display.
    /// </summary>
    public ViewModel5x5 ViewModel { get; } = new();

    public View5x5()
    {
        InitializeComponent();
        DataContext = ViewModel;
    }
}