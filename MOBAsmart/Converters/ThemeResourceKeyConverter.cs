// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.MAUI.Converters;

using System.Globalization;

/// <summary>
/// Resolves a theme resource key (e.g. RailwayAccent) to a <see cref="Color"/> for bindings.
/// </summary>
public sealed class ThemeResourceKeyConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string resourceKey || string.IsNullOrWhiteSpace(resourceKey))
        {
            return null;
        }

        if (Application.Current?.Resources.TryGetValue(resourceKey, out var resource) == true
            && resource is Color color)
        {
            return color;
        }

        return null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}