// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.Converter;

using Microsoft.UI.Xaml.Data;

/// <summary>
/// Converter that converts a DateTime? to TimeSpan and back.
/// Used for TimePicker binding where only the time component is relevant.
/// If DateTime is null, returns TimeSpan.Zero.
/// </summary>
public class DateTimeToTimeSpanConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, string language)
    {
        if (value is DateTime dateTime)
        {
            return dateTime.TimeOfDay;
        }

        return TimeSpan.Zero;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, string language)
    {
        if (value is TimeSpan timeSpan)
        {
            // Create a DateTime with today's date and the selected time
            return DateTime.Today.Add(timeSpan);
        }

        return null;
    }
}
