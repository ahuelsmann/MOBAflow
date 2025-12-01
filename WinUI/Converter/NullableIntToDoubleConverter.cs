// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.Converter;

using Microsoft.UI.Xaml.Data;

/// <summary>
/// Converts nullable int to double for NumberBox binding.
/// </summary>
public class NullableIntToDoubleConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is int intValue)
        {
            return (double)intValue;
        }
        
        // Null or 0 → return NaN to show placeholder
        return double.NaN;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        if (value is double doubleValue)
        {
            if (double.IsNaN(doubleValue))
            {
                return null;
            }
            
            return (int)doubleValue;
        }
        
        return null;
    }
}
