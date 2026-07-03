// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.MAUI.Converters;

using System.Globalization;

/// <summary>
/// Formats lap times for MOBAsmart; null maps to 00:00 instead of --:--.
/// </summary>
public sealed class LapTimeDisplayConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is TimeSpan timeSpan)
        {
            return $"{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}";
        }

        return "00:00";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
