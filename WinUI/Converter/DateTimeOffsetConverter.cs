// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.Converter;

using Microsoft.UI.Xaml.Data;

/// <summary>
/// Converts between DateTime? and DateTimeOffset for CalendarDatePicker binding.
/// </summary>
internal partial class DateTimeOffsetConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, string language)
    {
        return value is DateTime dateTime ? new DateTimeOffset(dateTime) : null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, string language)
    {
        return value is DateTimeOffset dateTimeOffset ? dateTimeOffset.DateTime : null;
    }
}
