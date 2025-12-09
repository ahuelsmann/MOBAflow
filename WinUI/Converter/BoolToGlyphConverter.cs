// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.Converter;

using Microsoft.UI.Xaml.Data;

/// <summary>
/// Converts Boolean to Glyph string (for FontIcon).
/// Used for ToggleButton icons that change based on IsChecked state.
/// </summary>
public class BoolToGlyphConverter : IValueConverter
{
    /// <summary>
    /// Glyph to use when value is true.
    /// </summary>
    public string TrueValue { get; set; } = "\uE76C"; // ChevronRight

    /// <summary>
    /// Glyph to use when value is false.
    /// </summary>
    public string FalseValue { get; set; } = "\uE76B"; // ChevronLeft

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is bool boolValue)
        {
            return boolValue ? TrueValue : FalseValue;
        }
        return FalseValue;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
