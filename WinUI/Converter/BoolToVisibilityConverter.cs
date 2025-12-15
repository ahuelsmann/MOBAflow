// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.Converter;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

/// <summary>
/// Converter that converts a boolean value to a Visibility enum.
/// True → Visible, False → Collapsed.
/// Use ConverterParameter="Invert" to invert the logic (True → Collapsed, False → Visible).
/// </summary>
public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var invert = parameter is string s && s.Equals("Invert", StringComparison.OrdinalIgnoreCase);

        if (value is bool boolValue)
        {
            if (invert)
            {
                return boolValue ? Visibility.Collapsed : Visibility.Visible;
            }

            return boolValue ? Visibility.Visible : Visibility.Collapsed;
        }

        return invert ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        var invert = parameter is string s && s.Equals("Invert", StringComparison.OrdinalIgnoreCase);

        if (value is Visibility visibility)
        {
            var isVisible = visibility == Visibility.Visible;
            return invert ? !isVisible : isVisible;
        }

        return invert;
    }
}
