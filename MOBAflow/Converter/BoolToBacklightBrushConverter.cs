// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.Converter;

using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

/// <summary>
/// Converts a boolean to a backlight brush for function buttons.
/// When ON: Returns a lightened version of the accent color (glow effect).
/// When OFF: Returns a subtle tint of the color.
/// Pass the accent color hex as ConverterParameter (e.g., "#FFD700").
/// </summary>
internal partial class BoolToBacklightBrushConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, string language)
    {
        bool isOn = value is bool b && b;

        if (parameter is not string hexColor || string.IsNullOrEmpty(hexColor))
        {
            // Fallback: use a neutral gray
            hexColor = "#808080";
        }

        var color = ParseHexColor(hexColor);

        if (isOn)
        {
            // ON: Lighten the color toward white for a "glow" effect
            // Mix with white at 50% to create a brighter, more vibrant look
            var lightenedColor = LightenColor(color, 0.4);
            return new SolidColorBrush(Color.FromArgb(220, lightenedColor.R, lightenedColor.G, lightenedColor.B));
        }

        // OFF: very subtle tint (almost transparent)
        return new SolidColorBrush(Color.FromArgb(40, color.R, color.G, color.B));
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, string language)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Lightens a color by mixing it with white.
    /// </summary>
    /// <param name="color">The base color</param>
    /// <param name="amount">How much to lighten (0.0 = no change, 1.0 = pure white)</param>
    private static Color LightenColor(Color color, double amount)
    {
        amount = Math.Clamp(amount, 0, 1);

        byte r = (byte)(color.R + ((255 - color.R) * amount));
        byte g = (byte)(color.G + ((255 - color.G) * amount));
        byte b = (byte)(color.B + ((255 - color.B) * amount));

        return Color.FromArgb(255, r, g, b);
    }

    private static Color ParseHexColor(string hex)
    {
        hex = hex.TrimStart('#');

        byte r = 128, g = 128, b = 128;

        if (hex.Length == 6)
        {
            r = System.Convert.ToByte(hex[..2], 16);
            g = System.Convert.ToByte(hex.Substring(2, 2), 16);
            b = System.Convert.ToByte(hex.Substring(4, 2), 16);
        }
        else if (hex.Length == 8)
        {
            // Skip alpha, use RGB
            r = System.Convert.ToByte(hex.Substring(2, 2), 16);
            g = System.Convert.ToByte(hex.Substring(4, 2), 16);
            b = System.Convert.ToByte(hex.Substring(6, 2), 16);
        }

        return Color.FromArgb(255, r, g, b);
    }
}
