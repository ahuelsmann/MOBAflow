// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.Converter;

using Microsoft.UI.Xaml.Data;

/// <summary>
/// Converter that converts scroll pause state to a label text.
/// True (paused) → "Paused", False (auto-scroll) → "Live".
/// </summary>
public class BoolToScrollTextConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, string language)
    {
        if (value is bool boolValue)
        {
            return boolValue ? "Paused" : "Live";
        }

        return "Live"; // Default: Auto-scroll active
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, string language)
    {
        throw new NotImplementedException();
    }
}
