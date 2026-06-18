// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.Common;

using Moba.Common.Display;

/// <summary>
/// Tests for function button backlight color calculation used by MOBAsmart ControlPage.
/// </summary>
[TestFixture]
internal sealed class FunctionBacklightColorTests
{
    [Test]
    public void ToArgb_WhenOff_ReturnsLowAlphaTint()
    {
        var argb = FunctionBacklightColor.ToArgb(isOn: false, hexColor: "#FFD700");

        var alpha = (argb >> 24) & 0xFF;
        Assert.That(alpha, Is.EqualTo(0x28));
    }

    [Test]
    public void ToArgb_WhenOn_ReturnsHigherAlphaLightenedColor()
    {
        var argb = FunctionBacklightColor.ToArgb(isOn: true, hexColor: "#FFD700");

        var alpha = (argb >> 24) & 0xFF;
        Assert.That(alpha, Is.EqualTo(0xDC));
    }

    [Test]
    public void ToArgb_WhenOn_IsBrighterThanOff()
    {
        var off = FunctionBacklightColor.ToArgb(isOn: false, "#804040");
        var on = FunctionBacklightColor.ToArgb(isOn: true, "#804040");

        var offR = (off >> 16) & 0xFF;
        var onR = (on >> 16) & 0xFF;
        Assert.That(onR, Is.GreaterThan(offR));
    }

    [Test]
    public void ToArgb_WithNullHex_UsesFallbackGray()
    {
        var argb = FunctionBacklightColor.ToArgb(isOn: false, hexColor: null);
        var r = (argb >> 16) & 0xFF;
        var g = (argb >> 8) & 0xFF;
        var b = argb & 0xFF;

        Assert.That(r, Is.EqualTo(0x80));
        Assert.That(g, Is.EqualTo(0x80));
        Assert.That(b, Is.EqualTo(0x80));
    }

    [Test]
    public void ToArgb_WithEightDigitHex_ParsesRgb()
    {
        var argb = FunctionBacklightColor.ToArgb(isOn: true, "#FF00FF00");
        var g = (argb >> 8) & 0xFF;

        Assert.That(g, Is.EqualTo(0xFF));
    }
}