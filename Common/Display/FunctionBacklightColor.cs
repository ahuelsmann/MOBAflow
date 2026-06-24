// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Common.Display;

/// <summary>
/// Platform-neutral colors for locomotive function buttons (F0-F31).
/// Supports light and dark themes with readable text contrast.
/// </summary>
public static class FunctionBacklightColor
{
    private const uint FallbackGrayArgb = 0xFF808080;

    /// <summary>Light or dark application theme.</summary>
    public enum AppearanceTheme
    {
        Light,
        Dark
    }

    /// <summary>Resolved function button colors for one visual state.</summary>
    public readonly record struct Appearance(
        uint BackgroundArgb,
        uint PrimaryTextArgb,
        uint SecondaryTextArgb);

    /// <summary>
    /// Returns an ARGB background color (legacy API for simple bindings).
    /// </summary>
    public static uint ToArgb(bool isOn, string? hexColor, AppearanceTheme theme = AppearanceTheme.Dark)
        => Resolve(isOn, hexColor, theme).BackgroundArgb;

    /// <summary>
    /// Resolves background and text colors for a function button in the given theme.
    /// </summary>
    public static Appearance Resolve(bool isOn, string? hexColor, AppearanceTheme theme)
    {
        var accent = ParseHexColor(hexColor);

        if (!isOn)
        {
            return theme == AppearanceTheme.Dark
                ? new Appearance(0xFF2C2C2C, 0xFFFFFFFF, 0xFFB8B8B8)
                : new Appearance(0xFFEEEEEE, 0xFF212121, 0xFF616161);
        }

        var canvas = theme == AppearanceTheme.Dark
            ? (R: (byte)30, G: (byte)30, B: (byte)30)
            : (R: (byte)255, G: (byte)255, B: (byte)255);

        var mix = theme == AppearanceTheme.Dark ? 0.62 : 0.42;
        var background = BlendRgb(accent, canvas, mix);
        var luminance = GetRelativeLuminance(background);

        // Gray accents must not produce a mid-gray "on" plate with gray text.
        if (theme == AppearanceTheme.Dark && luminance is > 0.18 and < 0.42)
        {
            background = BlendRgb(accent, (R: (byte)56, G: (byte)56, B: (byte)56), 0.72);
            luminance = GetRelativeLuminance(background);
        }

        var primaryText = luminance > 0.58 ? 0xFF121212u : 0xFFFFFFFFu;
        var secondaryText = luminance > 0.58 ? 0xFF3D3D3Du : 0xFFE8E8E8u;

        return new Appearance(
            0xFF000000u | ((uint)background.R << 16) | ((uint)background.G << 8) | background.B,
            primaryText,
            secondaryText);
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

    private static (byte R, byte G, byte B) BlendRgb(
        (byte R, byte G, byte B) accent,
        (byte R, byte G, byte B) canvas,
        double accentWeight)
    {
        accentWeight = Math.Clamp(accentWeight, 0, 1);
        var canvasWeight = 1 - accentWeight;
        return (
            (byte)Math.Clamp((accent.R * accentWeight) + (canvas.R * canvasWeight), 0, 255),
            (byte)Math.Clamp((accent.G * accentWeight) + (canvas.G * canvasWeight), 0, 255),
            (byte)Math.Clamp((accent.B * accentWeight) + (canvas.B * canvasWeight), 0, 255));
    }

    private static double GetRelativeLuminance((byte R, byte G, byte B) color)
    {
        static double Channel(byte value)
        {
            var s = value / 255d;
            return s <= 0.03928 ? s / 12.92 : Math.Pow((s + 0.055) / 1.055, 2.4);
        }

        var r = Channel(color.R);
        var g = Channel(color.G);
        var b = Channel(color.B);
        return (0.2126 * r) + (0.7152 * g) + (0.0722 * b);
    }
}
