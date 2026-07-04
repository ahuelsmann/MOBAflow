// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.Common;

using Moba.Common.Display;

using NUnit.Framework;

[TestFixture]
internal sealed class GaugeThemeAppearanceTests
{
    [Test]
    public void IsLightBackground_ReturnsFalse_WhenModeIsDark_EvenWithLightThemeResourceColors()
    {
        // Stale light-theme TextPrimary/Surface after switching back to dark mode.
        var isLight = GaugeThemeAppearance.IsLightBackground(
            GaugeBackgroundMode.Dark,
            surfaceR: 255,
            surfaceG: 255,
            surfaceB: 255,
            textR: 33,
            textG: 33,
            textB: 33);

        Assert.That(isLight, Is.False);
    }

    [Test]
    public void IsLightBackground_ReturnsTrue_WhenModeIsLight()
    {
        var isLight = GaugeThemeAppearance.IsLightBackground(
            GaugeBackgroundMode.Light,
            surfaceR: 30,
            surfaceG: 30,
            surfaceB: 30,
            textR: 255,
            textG: 255,
            textB: 255);

        Assert.That(isLight, Is.True);
    }

    [Test]
    public void IsLightBackground_Auto_UsesSurfaceAndTextLuminance()
    {
        var lightSurface = GaugeThemeAppearance.IsLightBackground(
            GaugeBackgroundMode.Auto,
            surfaceR: 250,
            surfaceG: 250,
            surfaceB: 250,
            textR: 33,
            textG: 33,
            textB: 33);

        var darkSurface = GaugeThemeAppearance.IsLightBackground(
            GaugeBackgroundMode.Auto,
            surfaceR: 30,
            surfaceG: 30,
            surfaceB: 30,
            textR: 255,
            textG: 255,
            textB: 255);

        Assert.That(lightSurface, Is.True);
        Assert.That(darkSurface, Is.False);
    }
}
