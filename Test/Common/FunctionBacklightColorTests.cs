// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.Common;

using Moba.Common.Display;

/// <summary>
/// Tests for function button appearance used by MOBAsmart and MOBAflow.
/// </summary>
[TestFixture]
internal sealed class FunctionBacklightColorTests
{
    [Test]
    public void Resolve_WhenOff_DarkTheme_UsesOpaqueSurfaceBackground()
    {
        var appearance = FunctionBacklightColor.Resolve(false, "#FFD700", FunctionBacklightColor.AppearanceTheme.Dark);

        Assert.That((appearance.BackgroundArgb >> 24) & 0xFF, Is.EqualTo(0xFF));
        Assert.That(appearance.BackgroundArgb & 0xFFFFFF, Is.EqualTo(0x2C2C2C));
        Assert.That(appearance.PrimaryTextArgb, Is.EqualTo(0xFFFFFFFF));
    }

    [Test]
    public void Resolve_WhenOff_LightTheme_UsesReadableDarkText()
    {
        var appearance = FunctionBacklightColor.Resolve(false, "#FFD700", FunctionBacklightColor.AppearanceTheme.Light);

        Assert.That(appearance.BackgroundArgb & 0xFFFFFF, Is.EqualTo(0xEEEEEE));
        Assert.That(appearance.PrimaryTextArgb, Is.EqualTo(0xFF212121));
    }

    [Test]
    public void Resolve_WhenOn_DarkTheme_IsBrighterThanOff_ForGrayAccent()
    {
        var off = FunctionBacklightColor.Resolve(false, "#808080", FunctionBacklightColor.AppearanceTheme.Dark);
        var on = FunctionBacklightColor.Resolve(true, "#808080", FunctionBacklightColor.AppearanceTheme.Dark);

        var offLuminance = GetLuminance(off.BackgroundArgb);
        var onLuminance = GetLuminance(on.BackgroundArgb);
        Assert.That(onLuminance, Is.GreaterThan(offLuminance));
    }

    [Test]
    public void Resolve_WhenOn_DarkTheme_UsesHighContrastText_OnGrayAccent()
    {
        var appearance = FunctionBacklightColor.Resolve(true, "#888888", FunctionBacklightColor.AppearanceTheme.Dark);

        Assert.That(appearance.PrimaryTextArgb, Is.EqualTo(0xFFFFFFFF));
        Assert.That(appearance.SecondaryTextArgb, Is.EqualTo(0xFFE8E8E8));
    }

    [Test]
    public void Resolve_WhenOn_LightTheme_UsesDarkText_OnBrightAccent()
    {
        var appearance = FunctionBacklightColor.Resolve(true, "#FFD700", FunctionBacklightColor.AppearanceTheme.Light);

        Assert.That(appearance.PrimaryTextArgb, Is.EqualTo(0xFF121212));
    }

    [Test]
    public void ToArgb_WithNullHex_UsesFallbackGray()
    {
        var argb = FunctionBacklightColor.ToArgb(false, null, FunctionBacklightColor.AppearanceTheme.Dark);
        var r = (argb >> 16) & 0xFF;
        var g = (argb >> 8) & 0xFF;
        var b = argb & 0xFF;

        Assert.That(r, Is.EqualTo(0x2C));
        Assert.That(g, Is.EqualTo(0x2C));
        Assert.That(b, Is.EqualTo(0x2C));
    }

    private static double GetLuminance(uint argb)
    {
        static double Channel(uint value)
        {
            var s = value / 255d;
            return s <= 0.03928 ? s / 12.92 : Math.Pow((s + 0.055) / 1.055, 2.4);
        }

        var r = Channel((argb >> 16) & 0xFF);
        var g = Channel((argb >> 8) & 0xFF);
        var b = Channel(argb & 0xFF);
        return (0.2126 * r) + (0.7152 * g) + (0.0722 * b);
    }
}
