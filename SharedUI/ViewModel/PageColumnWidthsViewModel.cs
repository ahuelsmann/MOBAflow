// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using CommunityToolkit.Mvvm.ComponentModel;

/// <summary>
/// Observable column widths for a single page grid. Used as the binding source for
/// ColumnDefinition.Width so the UI updates when values are set (e.g. from loaded settings or from the resize behavior).
/// </summary>
public sealed partial class PageColumnWidthsViewModel : ObservableObject
{
    [ObservableProperty]
    private double _col0;

    [ObservableProperty]
    private double _col1;

    [ObservableProperty]
    private double _col2;

    [ObservableProperty]
    private double _col3;

    [ObservableProperty]
    private double _col4;

    [ObservableProperty]
    private double _col5;

    [ObservableProperty]
    private double _col6;

    [ObservableProperty]
    private double _col7;

    [ObservableProperty]
    private double _col8;

    [ObservableProperty]
    private double _col9;

    [ObservableProperty]
    private double _col10;

    [ObservableProperty]
    private double _col11;

    [ObservableProperty]
    private double _col12;

    [ObservableProperty]
    private double _col13;

    [ObservableProperty]
    private double _col14;

    [ObservableProperty]
    private double _col15;

    [ObservableProperty]
    private double _col16;

    [ObservableProperty]
    private double _col17;

    [ObservableProperty]
    private double _col18;

    [ObservableProperty]
    private double _col19;

    /// <summary>
    /// Gets or sets the width for the given column index (0..19).
    /// Used by the resize behavior and when loading from settings.
    /// </summary>
    public double this[int index]
    {
        get => index switch
        {
            0 => Col0,
            1 => Col1,
            2 => Col2,
            3 => Col3,
            4 => Col4,
            5 => Col5,
            6 => Col6,
            7 => Col7,
            8 => Col8,
            9 => Col9,
            10 => Col10,
            11 => Col11,
            12 => Col12,
            13 => Col13,
            14 => Col14,
            15 => Col15,
            16 => Col16,
            17 => Col17,
            18 => Col18,
            19 => Col19,
            _ => 0
        };
        set
        {
            switch (index)
            {
                case 0: Col0 = value; break;
                case 1: Col1 = value; break;
                case 2: Col2 = value; break;
                case 3: Col3 = value; break;
                case 4: Col4 = value; break;
                case 5: Col5 = value; break;
                case 6: Col6 = value; break;
                case 7: Col7 = value; break;
                case 8: Col8 = value; break;
                case 9: Col9 = value; break;
                case 10: Col10 = value; break;
                case 11: Col11 = value; break;
                case 12: Col12 = value; break;
                case 13: Col13 = value; break;
                case 14: Col14 = value; break;
                case 15: Col15 = value; break;
                case 16: Col16 = value; break;
                case 17: Col17 = value; break;
                case 18: Col18 = value; break;
                case 19: Col19 = value; break;
            }
        }
    }
}