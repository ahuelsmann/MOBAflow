// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.Converter;

using Microsoft.UI;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

/// <summary>
/// Converter that converts a hex color string (e.g., "#66BB6A") to a SolidColorBrush.
/// Supports "Transparent" as a special value.
/// Empty string returns null (binding will use FallbackValue or default).
/// Used for dynamic background colors in track cards and station highlighting.
/// </summary>
public class HexColorToBrushConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is string hexColor && !string.IsNullOrEmpty(hexColor))
        {
            try
            {
                // Handle "Transparent" special case
                if (hexColor.Equals("Transparent", StringComparison.OrdinalIgnoreCase))
                {
                    return new SolidColorBrush(Colors.Transparent);
                }

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
                // Fallback to null on parse error
            }
        }
        
        // Empty string or null = return null, XAML will use FallbackValue
        return null;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
