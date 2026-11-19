// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.MAUI.Converters;

using System.Globalization;

/// <summary>
/// Maps function symbol asset filenames (e.g. headlight.png) to MauiImage resource names.
/// </summary>
public sealed class FunctionIconSourceConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string asset || string.IsNullOrWhiteSpace(asset) || asset == "none")
        {
            return null;
        }

        var fileName = Path.GetFileNameWithoutExtension(asset.Trim());
        return string.IsNullOrWhiteSpace(fileName) ? null : fileName;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
