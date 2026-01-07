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
        if (value is not bool isOn || !isOn)
        {
            return 0.0;
        }

        // Subtle backlight: keep glow present but avoid harsh outer halos (notably for light/pastel accents).
        // Optional parameter can attenuate the layer intensity further (outer layers should be lower).
        if (parameter is string s && double.TryParse(s, out var multiplier))
        {
            return 0.45 * multiplier;
        }

        return 0.45;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
