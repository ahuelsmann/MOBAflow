// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.ViewModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Controls.Matrix;

using Microsoft.UI;
using Microsoft.UI.Xaml.Media;

using Windows.UI;

public sealed partial class MatrixPageViewModel : ObservableObject
{
    [ObservableProperty]
    private ViewModel5x5 matrixViewModel = new();

    [ObservableProperty]
    private Color selectedColor = Colors.Red;

    public MatrixPageViewModel()
    {
        SelectedColorBrush = new SolidColorBrush(SelectedColor);
    }

    [ObservableProperty]
    private SolidColorBrush selectedColorBrush;

    partial void OnSelectedColorChanged(Color value)
    {
        SelectedColorBrush = new SolidColorBrush(value);
    }

    [RelayCommand]
    private void CellClicked(int cellIndex)
    {
        if (cellIndex >= 0 && cellIndex <= 24)
        {
            MatrixViewModel.SetCellColor(cellIndex, SelectedColorBrush);
        }
    }

    [RelayCommand]
    private void ClearCell(int cellIndex)
    {
        MatrixViewModel.ClearCellColor(cellIndex);
    }
}
