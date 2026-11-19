// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.Converter;

using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;

/// <summary>
/// Converts a case-insensitive string ("Success", "Error", "Warning", "Informational")
/// into the matching <see cref="InfoBarSeverity"/> enum value. Unknown values fall back to
/// <see cref="InfoBarSeverity.Informational"/>.
/// </summary>
public partial class StringToInfoBarSeverityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var raw = value as string ?? string.Empty;
        return raw.Trim().ToLowerInvariant() switch
        {
            "success" => InfoBarSeverity.Success,
            "error" => InfoBarSeverity.Error,
            "warning" => InfoBarSeverity.Warning,
            _ => InfoBarSeverity.Informational,
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => value is InfoBarSeverity s ? s.ToString() : "Informational";
}
