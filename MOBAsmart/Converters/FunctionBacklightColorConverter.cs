// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.MAUI.Converters;

using Moba.Common.Display;

using System.Globalization;

/// <summary>
/// Converts function on-state and hex accent color to a MAUI Color for button backgrounds.
/// </summary>
public sealed class FunctionBacklightColorConverter : IMultiValueConverter
{
    public object? Convert(object?[] values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Length < 2)
        {
            return Colors.Transparent;
        }

        var isOn = values[0] is bool on && on;
        var hexColor = values[1] as string;
        var argb = FunctionBacklightColor.ToArgb(isOn, hexColor);

        return Color.FromRgba(
            ((argb >> 16) & 0xFF) / 255f,
            ((argb >> 8) & 0xFF) / 255f,
            (argb & 0xFF) / 255f,
            ((argb >> 24) & 0xFF) / 255f);
    }

    public object[] ConvertBack(object? value, Type[] targetTypes, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}