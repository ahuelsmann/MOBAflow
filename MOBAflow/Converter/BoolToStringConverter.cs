// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.Converter;

using Microsoft.UI.Xaml.Data;

/// <summary>
/// Configurable converter that maps a boolean value to one of two string values.
/// Set <see cref="TrueValue"/> and <see cref="FalseValue"/> as XAML properties to customize output.
///
/// Replaces dedicated bool-to-string converters (scroll icons, tooltips, labels).
///
/// Usage:
/// <code>
/// &lt;converter:BoolToStringConverter x:Key="ScrollIconConverter"
///     TrueValue="&amp;#xE769;" FalseValue="&amp;#xE768;" /&gt;
/// </code>
/// </summary>
internal sealed class BoolToStringConverter : IValueConverter
{
    /// <summary>
    /// String returned when value is true.
    /// </summary>
    public string TrueValue { get; set; } = string.Empty;

    /// <summary>
    /// String returned when value is false.
    /// </summary>
    public string FalseValue { get; set; } = string.Empty;

    public object Convert(object? value, Type targetType, object? parameter, string language)
    {
        return value is bool b && b ? TrueValue : FalseValue;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, string language)
    {
        throw new NotImplementedException();
    }
}
