// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.Controls.Matrix;

using Microsoft.UI;
using Microsoft.UI.Xaml.Media;

using CommunityToolkit.Mvvm.ComponentModel;

public partial class ViewModel5x5 : ObservableObject
{
    // Row 1
    [ObservableProperty] private SolidColorBrush cell11 = new(Colors.LightGray);
    [ObservableProperty] private SolidColorBrush cell12 = new(Colors.LightGray);
    [ObservableProperty] private SolidColorBrush cell13 = new(Colors.LightGray);
    [ObservableProperty] private SolidColorBrush cell14 = new(Colors.LightGray);
    [ObservableProperty] private SolidColorBrush cell15 = new(Colors.LightGray);

    // Row 2
    [ObservableProperty] private SolidColorBrush cell21 = new(Colors.LightGray);
    [ObservableProperty] private SolidColorBrush cell22 = new(Colors.LightGray);
    [ObservableProperty] private SolidColorBrush cell23 = new(Colors.LightGray);
    [ObservableProperty] private SolidColorBrush cell24 = new(Colors.LightGray);
    [ObservableProperty] private SolidColorBrush cell25 = new(Colors.LightGray);

    // Row 3
    [ObservableProperty] private SolidColorBrush cell31 = new(Colors.LightGray);
    [ObservableProperty] private SolidColorBrush cell32 = new(Colors.LightGray);
    [ObservableProperty] private SolidColorBrush cell33 = new(Colors.LightGray);
    [ObservableProperty] private SolidColorBrush cell34 = new(Colors.LightGray);
    [ObservableProperty] private SolidColorBrush cell35 = new(Colors.LightGray);

    // Row 4
    [ObservableProperty] private SolidColorBrush cell41 = new(Colors.LightGray);
    [ObservableProperty] private SolidColorBrush cell42 = new(Colors.LightGray);
    [ObservableProperty] private SolidColorBrush cell43 = new(Colors.LightGray);
    [ObservableProperty] private SolidColorBrush cell44 = new(Colors.LightGray);
    [ObservableProperty] private SolidColorBrush cell45 = new(Colors.LightGray);

    // Row 5
    [ObservableProperty] private SolidColorBrush cell51 = new(Colors.LightGray);
    [ObservableProperty] private SolidColorBrush cell52 = new(Colors.LightGray);
    [ObservableProperty] private SolidColorBrush cell53 = new(Colors.LightGray);
    [ObservableProperty] private SolidColorBrush cell54 = new(Colors.LightGray);
    [ObservableProperty] private SolidColorBrush cell55 = new(Colors.LightGray);

    public ViewModel5x5()
    {
        // Beispiel: ein paar Zellen einfärben
        Cell11 = new SolidColorBrush(Colors.Red);
        Cell22 = new SolidColorBrush(Colors.Green);
        Cell33 = new SolidColorBrush(Colors.Blue);
    }
}