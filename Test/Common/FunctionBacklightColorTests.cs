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

    [TestCase("#FF0000", FunctionBacklightColor.AppearanceTheme.Dark, 0xFFA90B0Bu)]
    [TestCase("#0000FF", FunctionBacklightColor.AppearanceTheme.Dark, 0xFF0B0BA9u)]
    [TestCase("#FF0000", FunctionBacklightColor.AppearanceTheme.Light, 0xFFFF9393u)]
    [TestCase("#0000FF", FunctionBacklightColor.AppearanceTheme.Light, 0xFF9393FFu)]
    public void Resolve_WhenOn_PreservesAccentChannelsAndOpaqueBackground(
        string accent, FunctionBacklightColor.AppearanceTheme theme, uint expectedBackground)
    {
        Assert.That(FunctionBacklightColor.Resolve(true, accent, theme).BackgroundArgb,
            Is.EqualTo(expectedBackground));
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase(" ")]
    [TestCase("12345")]
    [TestCase("1234567")]
    public void Resolve_WhenOn_MissingOrUnsupportedColorUsesGray(string? accent)
    {
        Assert.That(FunctionBacklightColor.Resolve(true, accent, FunctionBacklightColor.AppearanceTheme.Dark),
            Is.EqualTo(FunctionBacklightColor.Resolve(true, "#808080", FunctionBacklightColor.AppearanceTheme.Dark)));
    }

    [TestCase(FunctionBacklightColor.AppearanceTheme.Dark)]
    [TestCase(FunctionBacklightColor.AppearanceTheme.Light)]
    public void Resolve_RgbAndArgbInputDescribeSameOpaqueAccent(FunctionBacklightColor.AppearanceTheme theme)
    {
        var expected = FunctionBacklightColor.Resolve(true, "#12AB34", theme);
        Assert.Multiple(() =>
        {
            Assert.That(FunctionBacklightColor.Resolve(true, "12AB34", theme), Is.EqualTo(expected));
            Assert.That(FunctionBacklightColor.Resolve(true, "#0012AB34", theme), Is.EqualTo(expected));
            Assert.That(FunctionBacklightColor.Resolve(true, "#FF12AB34", theme), Is.EqualTo(expected));
            Assert.That(FunctionBacklightColor.ToArgb(true, "#12AB34", theme), Is.EqualTo(expected.BackgroundArgb));
        });
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
