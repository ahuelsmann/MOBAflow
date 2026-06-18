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
public partial class HexColorToBrushConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is not string hexColor || string.IsNullOrEmpty(hexColor))
        {
            return null;
        }

        try
        {
            var cacheKey = hexColor.Trim();

            if (cacheKey.Equals("Transparent", StringComparison.OrdinalIgnoreCase))
            {
                return BrushCache.GetOrAdd("transparent", static () => new SolidColorBrush(Colors.Transparent));
            }

            var normalizedHex = cacheKey.TrimStart('#');
            if (normalizedHex.Length != 6)
            {
                return null;
            }

            return BrushCache.GetOrAdd(
                normalizedHex,
                () =>
                {
                    var r = System.Convert.ToByte(normalizedHex[..2], 16);
                    var g = System.Convert.ToByte(normalizedHex.Substring(2, 2), 16);
                    var b = System.Convert.ToByte(normalizedHex.Substring(4, 2), 16);
                    return new SolidColorBrush(Color.FromArgb(255, r, g, b));
                });
        }
        catch
        {
            return null;
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}