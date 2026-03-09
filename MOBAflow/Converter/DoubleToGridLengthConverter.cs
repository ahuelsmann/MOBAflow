// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.Converter;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

/// <summary>
/// Converts a numeric column width (double, pixels) to a <see cref="GridLength"/> for use with
/// Values less than or equal to zero are converted to <see cref="GridLength.Auto"/>.
/// </summary>
internal sealed class DoubleToGridLengthConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is double d && d > 0)
            return new GridLength(d, GridUnitType.Pixel);
        return new GridLength(1, GridUnitType.Auto);
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        if (value is GridLength gl && gl.GridUnitType == GridUnitType.Pixel)
            return gl.Value;
        return 0d;
    }
}