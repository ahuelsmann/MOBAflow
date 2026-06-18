// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Common.Display;

/// <summary>
/// Platform-neutral backlight color calculation for locomotive function buttons (F0-F31).
/// When ON: lightened accent color with high opacity. When OFF: subtle tint.
/// </summary>
public static class FunctionBacklightColor
{
    private const uint FallbackGrayArgb = 0xFF808080;

    /// <summary>
    /// Returns an ARGB color for a function button background.
    /// </summary>
    /// <param name="isOn">Whether the function is currently active.</param>
    /// <param name="hexColor">Accent color hex (e.g. "#FFD700"). Falls back to neutral gray.</param>
    public static uint ToArgb(bool isOn, string? hexColor)
    {
        var baseColor = ParseHexColor(hexColor);

        if (isOn)
        {
            var lightened = LightenColor(baseColor, 0.4);
            return 0xDC000000u | ((uint)lightened.R << 16) | ((uint)lightened.G << 8) | lightened.B;
        }

        return 0x28000000u | ((uint)baseColor.R << 16) | ((uint)baseColor.G << 8) | baseColor.B;
    }

    private static (byte R, byte G, byte B) ParseHexColor(string? hexColor)
    {
        if (string.IsNullOrWhiteSpace(hexColor))
        {
            return DecomposeRgb(FallbackGrayArgb);
        }

        var hex = hexColor.TrimStart('#');
        if (hex.Length == 6)
        {
            return (
                Convert.ToByte(hex[..2], 16),
                Convert.ToByte(hex.Substring(2, 2), 16),
                Convert.ToByte(hex.Substring(4, 2), 16));
        }

        if (hex.Length == 8)
        {
            return (
                Convert.ToByte(hex.Substring(2, 2), 16),
                Convert.ToByte(hex.Substring(4, 2), 16),
                Convert.ToByte(hex.Substring(6, 2), 16));
        }

        return DecomposeRgb(FallbackGrayArgb);
    }

    private static (byte R, byte G, byte B) DecomposeRgb(uint argb)
    {
        return ((byte)((argb >> 16) & 0xFF), (byte)((argb >> 8) & 0xFF), (byte)(argb & 0xFF));
    }

    private static (byte R, byte G, byte B) LightenColor((byte R, byte G, byte B) color, double amount)
    {
        amount = Math.Clamp(amount, 0, 1);
        return (
            (byte)(color.R + ((255 - color.R) * amount)),
            (byte)(color.G + ((255 - color.G) * amount)),
            (byte)(color.B + ((255 - color.B) * amount)));
    }
}