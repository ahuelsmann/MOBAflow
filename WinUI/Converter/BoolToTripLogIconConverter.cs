// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.Converter;

using Microsoft.UI.Xaml.Data;

/// <summary>
/// Konvertiert bool (IsStopSegment) zu Segoe MDL2 Icon: Halt = Pause-Symbol, Fahrt = Play-Symbol.
/// </summary>
public sealed class BoolToTripLogIconConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, string language)
    {
        return value is bool b && b ? "\uE769" : "\uE768"; // Pause vs Play
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, string language) =>
        throw new NotImplementedException();
}
