// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.Converter;

using Microsoft.UI.Xaml.Data;

/// <summary>
/// Converts between DateTime? and DateTimeOffset for CalendarDatePicker binding.
/// </summary>
public class DateTimeOffsetConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, string language)
    {
        if (value is DateTime dateTime)
        {
            return new DateTimeOffset(dateTime);
        }
        return null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, string language)
    {
        if (value is DateTimeOffset dateTimeOffset)
        {
            return dateTimeOffset.DateTime;
        }
        return null;
    }
}
