// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

#if WINDOWS

namespace Moba.Test.WinUI;

using Moba.SharedUI.Appearance;
using Moba.MOBAflow.Converter;

[TestFixture]
internal sealed class ThemeConverterTests
{
    [TestCase("Background", true, "AccentFillColorDefaultBrush")]
    [TestCase("Background", false, "CardBackgroundFillColorDefaultBrush")]
    [TestCase("Border", true, "AccentFillColorDefaultBrush")]
    [TestCase("Border", false, "CardStrokeColorDefaultBrush")]
    [TestCase("Foreground", true, "TextOnAccentFillColorPrimaryBrush")]
    [TestCase("Foreground", false, "TextFillColorPrimaryBrush")]
    public void ResolveResourceKey_MapsRoleAndSelection(string role, bool isSelected, string expectedKey)
    {
        Assert.That(BoolToPickerStateBrushConverter.ResolveResourceKey(role, isSelected), Is.EqualTo(expectedKey));
    }

    [TestCase(true, null, "AccentFillColorDefaultBrush")]
    [TestCase(true, "", "AccentFillColorDefaultBrush")]
    [TestCase(true, "   ", "AccentFillColorDefaultBrush")]
    [TestCase(true, "SystemFillColorSuccessBrush", "SystemFillColorSuccessBrush")]
    [TestCase(false, "SystemFillColorSuccessBrush", "TextFillColorSecondaryBrush")]
    [TestCase(false, null, "TextFillColorSecondaryBrush")]
    public void BoolToAccentBrushConverter_ResolveResourceKey_UsesConfiguredTrueKey(
        bool isTrue,
        string? trueResourceKey,
        string expectedKey)
    {
        Assert.That(BoolToAccentBrushConverter.ResolveResourceKey(isTrue, trueResourceKey), Is.EqualTo(expectedKey));
    }

    [Test]
    public void BoolToAccentBrushConverter_DefaultTrueResourceKey_PreservesAccentDefault()
    {
        var converter = new BoolToAccentBrushConverter();

        Assert.That(converter.TrueResourceKey, Is.EqualTo("AccentFillColorDefaultBrush"));
    }

    [Test]
    public void GetAppearanceTheme_WithNullElement_UsesApplicationThemeFallback()
    {
        var theme = BoolToBacklightBrushConverter.GetAppearanceTheme(null);

        Assert.That(theme, Is.AnyOf(
            FunctionBacklightColor.AppearanceTheme.Light,
            FunctionBacklightColor.AppearanceTheme.Dark));
    }

    [Test]
    public void FunctionBacklightColor_OffState_LightTheme_UsesDarkText()
    {
        var appearance = FunctionBacklightColor.Resolve(false, "#808080", FunctionBacklightColor.AppearanceTheme.Light);

        Assert.That(appearance.PrimaryTextArgb, Is.EqualTo(0xFF212121u));
    }

    [Test]
    public void FunctionBacklightColor_OffState_DarkTheme_UsesLightText()
    {
        var appearance = FunctionBacklightColor.Resolve(false, "#808080", FunctionBacklightColor.AppearanceTheme.Dark);

        Assert.That(appearance.PrimaryTextArgb, Is.EqualTo(0xFFFFFFFFu));
    }
}

#endif
