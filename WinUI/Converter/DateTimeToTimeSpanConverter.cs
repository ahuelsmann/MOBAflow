// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.Converter;

using Microsoft.UI.Xaml.Data;
using System;

/// <summary>
/// Converts between DateTime? and TimeSpan for TimePicker binding.
/// TimePicker expects TimeSpan (time of day), but Station model uses DateTime?.
/// </summary>
public class DateTimeToTimeSpanConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is DateTime dateTime)
        {
            return dateTime.TimeOfDay;
        }
        
        // Return null for null DateTime (TimePicker will show empty)
        return null;
    }

    public object? ConvertBack(object value, Type targetType, object parameter, string language)
    {
        if (value is TimeSpan timeSpan)
        {
            // Create a DateTime with today's date and the specified time
            return DateTime.Today.Add(timeSpan);
        }
        
        // Return null if TimePicker is empty
        return null;
    }
}
