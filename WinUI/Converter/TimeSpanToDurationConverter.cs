// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.Converter;

using Microsoft.UI.Xaml.Data;

/// <summary>
/// Converts TimeSpan? to display string for duration (e.g. "2 min 30 s" or "1 h 5 min").
/// </summary>
internal sealed class TimeSpanToDurationConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, string language)
    {
        if (value is not TimeSpan ts || ts == default)
            return "–";

        if (ts.TotalHours >= 1)
            return $"{(int)ts.TotalHours} h {ts.Minutes} min";
        if (ts.TotalMinutes >= 1)
            return $"{(int)ts.TotalMinutes} min {ts.Seconds} s";
        return $"{ts.Seconds} s";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, string language) =>
        throw new NotImplementedException();
}
