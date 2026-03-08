// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.Converter;

using Microsoft.UI.Xaml.Data;

/// <summary>
/// Converts between uint? and double for NumberBox binding.
/// </summary>
internal partial class NullableUIntConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, string language)
    {
        if (value is uint uintValue)
        {
            return (double)uintValue;
        }
        return double.NaN; // NumberBox displays empty when value is NaN
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, string language)
    {
        return value is double doubleValue && !double.IsNaN(doubleValue) ? (uint)doubleValue : null;
    }
}
