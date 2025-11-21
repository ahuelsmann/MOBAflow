// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.Converter;

using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

/// <summary>
/// Converter that converts a hex color string (e.g., "#66BB6A") to a SolidColorBrush.
/// Used for dynamic background colors in track cards based on lap statistics.
/// </summary>
public class HexColorToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is string hexColor && !string.IsNullOrEmpty(hexColor))
        {
            try
            {
                // Remove '#' if present
                hexColor = hexColor.TrimStart('#');
                
                // Parse hex string (RRGGBB format)
                if (hexColor.Length == 6)
                {
                    var r = System.Convert.ToByte(hexColor.Substring(0, 2), 16);
                    var g = System.Convert.ToByte(hexColor.Substring(2, 2), 16);
                    var b = System.Convert.ToByte(hexColor.Substring(4, 2), 16);
                    
                    return new SolidColorBrush(Color.FromArgb(255, r, g, b));
                }
            }
            catch
            {
                // Fallback to default color on parse error
            }
        }
        
        // Default fallback: Red 400 (no activity)
        return new SolidColorBrush(Color.FromArgb(255, 239, 83, 80));
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
