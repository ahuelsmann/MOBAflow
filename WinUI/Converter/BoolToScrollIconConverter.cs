// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.Converter;

using Microsoft.UI.Xaml.Data;

/// <summary>
/// Converter that converts scroll pause state to an icon glyph.
/// True (paused) → Pause icon (&#xE769;), False (auto-scroll) → Play icon (&#xE768;).
/// </summary>
public class BoolToScrollIconConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, string language)
    {
        if (value is bool boolValue)
        {
            return boolValue ? "\uE769" : "\uE768"; // Pause : Play
        }

        return "\uE768"; // Default: Play (auto-scroll active)
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, string language)
    {
        throw new NotImplementedException();
    }
}
