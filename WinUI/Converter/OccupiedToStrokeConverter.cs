// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.Converter;

using Microsoft.UI;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;

/// <summary>
/// Converts track segment occupation state to stroke color.
/// Occupied segments shown in warning color (orange).
/// </summary>
public class OccupiedToStrokeConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, string language)
    {
        var isOccupied = value is bool occupied && occupied;
        
        // Occupied: Orange warning color
        // Free: Default red (from existing template)
        return isOccupied 
            ? new SolidColorBrush(Colors.Orange) 
            : new SolidColorBrush(Colors.Red);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, string language)
    {
        throw new NotImplementedException();
    }
}
