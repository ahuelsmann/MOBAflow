// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.MAUI.Converters;

using System.Globalization;

/// <summary>
/// Returns true when the bound integer equals the converter parameter.
/// </summary>
public sealed class IntEqualsConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not int intValue || parameter is null)
        {
            return false;
        }

        return int.TryParse(parameter.ToString(), out var expected) && intValue == expected;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}