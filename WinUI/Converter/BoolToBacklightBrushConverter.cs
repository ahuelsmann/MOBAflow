// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.Converter;

using Microsoft.UI;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;

/// <summary>
/// Converts a boolean to a backlight brush for function buttons.
/// When ON: Returns the accent color with moderate opacity (backlit effect).
/// When OFF: Returns transparent.
/// Pass the accent color hex as ConverterParameter (e.g., "#FFD700").
/// </summary>
public class BoolToBacklightBrushConverter : IValueConverter
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
            // Backlit: moderate opacity for a soft glow from within
            return new SolidColorBrush(Windows.UI.Color.FromArgb(180, color.R, color.G, color.B));
        }
        else
        {
            // OFF: very subtle tint (almost transparent)
            return new SolidColorBrush(Windows.UI.Color.FromArgb(40, color.R, color.G, color.B));
        }
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, string language)
    {
        throw new NotImplementedException();
    }

    private static Windows.UI.Color ParseHexColor(string hex)
    {
        hex = hex.TrimStart('#');
        
        byte r = 128, g = 128, b = 128;
        
        if (hex.Length == 6)
        {
            r = System.Convert.ToByte(hex.Substring(0, 2), 16);
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

        return Windows.UI.Color.FromArgb(255, r, g, b);
    }
}
