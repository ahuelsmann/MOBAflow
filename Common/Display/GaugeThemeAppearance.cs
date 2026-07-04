// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Common.Display;

/// <summary>
/// Platform-neutral gauge appearance mode derived from app theme.
/// </summary>
public enum GaugeBackgroundMode
{
    Light,
    Dark,
    Auto
}

/// <summary>
/// Resolves whether a gauge is drawn on a light background.
/// Explicit light/dark modes ignore stale resource colors after theme switches.
/// </summary>
public static class GaugeThemeAppearance
{
    public static bool IsLightBackground(
        GaugeBackgroundMode mode,
        byte surfaceR,
        byte surfaceG,
        byte surfaceB,
        byte textR,
        byte textG,
        byte textB)
    {
        return mode switch
        {
            GaugeBackgroundMode.Light => true,
            GaugeBackgroundMode.Dark => false,
            _ => IsLightColor(surfaceR, surfaceG, surfaceB) || !IsLightColor(textR, textG, textB)
        };
    }

    public static bool IsLightColor(byte red, byte green, byte blue)
    {
        var r = red / 255d;
        var g = green / 255d;
        var b = blue / 255d;
        return (0.2126 * r) + (0.7152 * g) + (0.0722 * b) >= 0.55;
    }
}
