// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.Converter;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

/// <summary>
/// Converter that converts a boolean value to a Visibility enum.
/// True → Visible, False → Collapsed.
/// Use ConverterParameter="Invert" to invert the logic (True → Collapsed, False → Visible).
/// </summary>
internal partial class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var invert = parameter is string s && s.Equals("Invert", StringComparison.OrdinalIgnoreCase);

        return value is bool boolValue
            ? invert ? boolValue ? Visibility.Collapsed : Visibility.Visible : boolValue ? Visibility.Visible : Visibility.Collapsed
            : invert ? Visibility.Visible : Visibility.Collapsed;
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
