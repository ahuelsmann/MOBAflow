// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.Converter;

using Microsoft.UI.Xaml.Data;

/// <summary>
/// Konvertiert DateTime/DateTime? zu Kurzzeit-String (HH:mm).
/// </summary>
public sealed class DateTimeToShortTimeConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, string language)
    {
        if (value is DateTime dt)
            return dt.ToString("HH:mm");
        if (value is DateTime nullable)
            return nullable.ToString("HH:mm");
        return "–";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, string language) =>
        throw new NotImplementedException();
}
