// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.MAUI.Converters;

using System.Globalization;

/// <summary>
/// Returns accent color for measured laps and secondary text for idle 00:00 display.
/// </summary>
public sealed class LapTimeColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not TimeSpan lapTime || lapTime == TimeSpan.Zero)
        {
            return Application.Current?.Resources["TextSecondary"];
        }

        return Application.Current?.Resources["RailwaySecondary"];
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
