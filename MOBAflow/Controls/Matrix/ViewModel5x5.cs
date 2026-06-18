// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.Controls.Matrix;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI;
using Microsoft.UI.Xaml.Media;
using Moba.Common.Display;
using Moba.Domain;

public partial class ViewModel5x5 : ObservableObject
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [ObservableProperty] private string name = string.Empty;

    [ObservableProperty] private SolidColorBrush selectedColorBrush = new(Colors.Red);

    public IRelayCommand<ViewModel5x5?>? DeleteCommand { get; set; }

    [ObservableProperty] private SolidColorBrush cell11 = new(Colors.LightGray);
    [ObservableProperty] private SolidColorBrush cell12 = new(Colors.LightGray);
    [ObservableProperty] private SolidColorBrush cell13 = new(Colors.LightGray);
    [ObservableProperty] private SolidColorBrush cell14 = new(Colors.LightGray);
    [ObservableProperty] private SolidColorBrush cell15 = new(Colors.LightGray);

    [ObservableProperty] private SolidColorBrush cell21 = new(Colors.LightGray);
    [ObservableProperty] private SolidColorBrush cell22 = new(Colors.LightGray);
    [ObservableProperty] private SolidColorBrush cell23 = new(Colors.LightGray);
    [ObservableProperty] private SolidColorBrush cell24 = new(Colors.LightGray);
    [ObservableProperty] private SolidColorBrush cell25 = new(Colors.LightGray);

    [ObservableProperty] private SolidColorBrush cell31 = new(Colors.LightGray);
    [ObservableProperty] private SolidColorBrush cell32 = new(Colors.LightGray);
    [ObservableProperty] private SolidColorBrush cell33 = new(Colors.LightGray);
    [ObservableProperty] private SolidColorBrush cell34 = new(Colors.LightGray);
    [ObservableProperty] private SolidColorBrush cell35 = new(Colors.LightGray);

    [ObservableProperty] private SolidColorBrush cell41 = new(Colors.LightGray);
    [ObservableProperty] private SolidColorBrush cell42 = new(Colors.LightGray);
    [ObservableProperty] private SolidColorBrush cell43 = new(Colors.LightGray);
    [ObservableProperty] private SolidColorBrush cell44 = new(Colors.LightGray);
    [ObservableProperty] private SolidColorBrush cell45 = new(Colors.LightGray);

    [ObservableProperty] private SolidColorBrush cell51 = new(Colors.LightGray);
    [ObservableProperty] private SolidColorBrush cell52 = new(Colors.LightGray);
    [ObservableProperty] private SolidColorBrush cell53 = new(Colors.LightGray);
    [ObservableProperty] private SolidColorBrush cell54 = new(Colors.LightGray);
    [ObservableProperty] private SolidColorBrush cell55 = new(Colors.LightGray);

    private readonly LedMatrix5x5State state = new();

    /// <summary>
    /// Gets the current brush of a specific cell by index (0-24).
    /// </summary>
    public SolidColorBrush GetCellBrush(int index)
    {
        return index switch
        {
            0 => Cell11,
            1 => Cell12,
            2 => Cell13,
            3 => Cell14,
            4 => Cell15,
            5 => Cell21,
            6 => Cell22,
            7 => Cell23,
            8 => Cell24,
            9 => Cell25,
            10 => Cell31,
            11 => Cell32,
            12 => Cell33,
            13 => Cell34,
            14 => Cell35,
            15 => Cell41,
            16 => Cell42,
            17 => Cell43,
            18 => Cell44,
            19 => Cell45,
            20 => Cell51,
            21 => Cell52,
            22 => Cell53,
            23 => Cell54,
            24 => Cell55,
            _ => new SolidColorBrush(Colors.LightGray)
        };
    }

    /// <summary>
    /// Sets the color of a specific cell by index (0-24).
    /// </summary>
    public void SetCellColor(int index, SolidColorBrush brush)
    {
        if (!state.SetCellColorArgb(index, ToArgb(brush.Color)))
        {
            return;
        }

        switch (index)
        {
            case 0: Cell11 = brush; break;
            case 1: Cell12 = brush; break;
            case 2: Cell13 = brush; break;
            case 3: Cell14 = brush; break;
            case 4: Cell15 = brush; break;
            case 5: Cell21 = brush; break;
            case 6: Cell22 = brush; break;
            case 7: Cell23 = brush; break;
            case 8: Cell24 = brush; break;
            case 9: Cell25 = brush; break;
            case 10: Cell31 = brush; break;
            case 11: Cell32 = brush; break;
            case 12: Cell33 = brush; break;
            case 13: Cell34 = brush; break;
            case 14: Cell35 = brush; break;
            case 15: Cell41 = brush; break;
            case 16: Cell42 = brush; break;
            case 17: Cell43 = brush; break;
            case 18: Cell44 = brush; break;
            case 19: Cell45 = brush; break;
            case 20: Cell51 = brush; break;
            case 21: Cell52 = brush; break;
            case 22: Cell53 = brush; break;
            case 23: Cell54 = brush; break;
            case 24: Cell55 = brush; break;
        }
    }

    public void ClearCellColor(int index)
    {
        if (state.ClearCellColor(index))
        {
            ApplyCellBrush(index, FromArgb(LedMatrix5x5State.OffColorArgb));
        }
    }

    [RelayCommand]
    private void PaintCell(int cellIndex)
    {
        if (cellIndex >= 0 && cellIndex < MatrixImage.CellCount)
        {
            SetCellColor(cellIndex, SelectedColorBrush);
        }
    }

    [RelayCommand]
    private void ClearCell(int cellIndex)
    {
        ClearCellColor(cellIndex);
    }

    public uint GetCellColorArgb(int index)
    {
        return state.GetCellColorArgb(index);
    }

    public MatrixImage ToModel()
    {
        return new MatrixImage
        {
            Id = Id,
            Name = Name,
            Cells = Enumerable.Range(0, MatrixImage.CellCount)
                .Select(GetCellColorArgb)
                .ToList()
        };
    }

    public static ViewModel5x5 FromModel(MatrixImage model)
    {
        ArgumentNullException.ThrowIfNull(model);
        model.NormalizeCells();

        var viewModel = new ViewModel5x5
        {
            Id = model.Id,
            Name = model.Name
        };

        for (var i = 0; i < MatrixImage.CellCount; i++)
        {
            viewModel.SetCellColorArgb(i, model.Cells[i]);
        }

        return viewModel;
    }

    public void SetCellColorArgb(int index, uint argb)
    {
        if (!state.SetCellColorArgb(index, argb))
        {
            return;
        }

        ApplyCellBrush(index, FromArgb(argb));
    }

    private static uint ToArgb(Windows.UI.Color color)
    {
        return ((uint)color.A << 24) | ((uint)color.R << 16) | ((uint)color.G << 8) | color.B;
    }

    private static SolidColorBrush FromArgb(uint argb)
    {
        var a = (byte)(argb >> 24);
        var r = (byte)(argb >> 16);
        var g = (byte)(argb >> 8);
        var b = (byte)argb;
        return new SolidColorBrush(Windows.UI.Color.FromArgb(a, r, g, b));
    }

    private void ApplyCellBrush(int index, SolidColorBrush brush)
    {
        switch (index)
        {
            case 0: Cell11 = brush; break;
            case 1: Cell12 = brush; break;
            case 2: Cell13 = brush; break;
            case 3: Cell14 = brush; break;
            case 4: Cell15 = brush; break;
            case 5: Cell21 = brush; break;
            case 6: Cell22 = brush; break;
            case 7: Cell23 = brush; break;
            case 8: Cell24 = brush; break;
            case 9: Cell25 = brush; break;
            case 10: Cell31 = brush; break;
            case 11: Cell32 = brush; break;
            case 12: Cell33 = brush; break;
            case 13: Cell34 = brush; break;
            case 14: Cell35 = brush; break;
            case 15: Cell41 = brush; break;
            case 16: Cell42 = brush; break;
            case 17: Cell43 = brush; break;
            case 18: Cell44 = brush; break;
            case 19: Cell45 = brush; break;
            case 20: Cell51 = brush; break;
            case 21: Cell52 = brush; break;
            case 22: Cell53 = brush; break;
            case 23: Cell54 = brush; break;
            case 24: Cell55 = brush; break;
        }
    }
}