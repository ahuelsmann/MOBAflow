// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.Converter;

using Microsoft.UI.Xaml.Data;

/// <summary>
/// Converts a boolean to an opacity value for glow effects.
/// When true (ON), returns higher opacity for visible glow.
/// When false (OFF), returns 0 for no glow.
/// </summary>
public class BoolToGlowOpacityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is bool isOn && isOn)
        {
            return 0.85; // Strong visible glow when ON
        }
        return 0.0; // No glow when OFF
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
