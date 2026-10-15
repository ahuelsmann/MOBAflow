// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Test.SharedUI;

using Moba.SharedUI.Appearance;

using Domain;

[TestFixture]
internal sealed class LocomotiveFunctionAppearanceResolverTests
{
    [Test]
    public void GetGlyph_UsesLocomotiveSymbol_WhenConfigured()
    {
        var locomotive = new Locomotive
        {
            FunctionSymbols = ["custom.png"]
        };

        Assert.That(LocomotiveFunctionAppearanceResolver.GetGlyph(locomotive, 0), Is.EqualTo("custom.png"));
    }

    [Test]
    public void GetGlyph_FallsBackToDefault_ForF0()
    {
        Assert.That(LocomotiveFunctionAppearanceResolver.GetGlyph(null, 0), Is.EqualTo("headlight.png"));
    }

    [Test]
    public void GetColor_UsesLocomotiveColor_WhenConfigured()
    {
        var locomotive = new Locomotive
        {
            FunctionColors = ["#AABBCC"]
        };

        Assert.That(LocomotiveFunctionAppearanceResolver.GetColor(locomotive, 0), Is.EqualTo("#AABBCC"));
    }

    [Test]
    public void GetDescription_UsesLocomotiveLabel_WhenConfigured()
    {
        var locomotive = new Locomotive
        {
            FunctionLabels = ["Headlight", "", "Horn"]
        };

        Assert.That(LocomotiveFunctionAppearanceResolver.GetDescription(locomotive, 0), Is.EqualTo("Headlight"));
        Assert.That(LocomotiveFunctionAppearanceResolver.GetDescription(locomotive, 1), Is.EqualTo(string.Empty));
        Assert.That(LocomotiveFunctionAppearanceResolver.GetDescription(locomotive, 2), Is.EqualTo("Horn"));
    }

    [TestCase(-1)]
    [TestCase(32)]
    public void InvalidFunctionIndicesReturnSafeAppearance(int functionIndex)
    {
        var locomotive = new Locomotive
        {
            FunctionSymbols = Enumerable.Repeat("custom.png", 33).ToList(),
            FunctionColors = Enumerable.Repeat("#AABBCC", 33).ToList(),
            FunctionLabels = Enumerable.Repeat("Horn", 33).ToList()
        };

        using (Assert.EnterMultipleScope())
        {
            Assert.That(LocomotiveFunctionAppearanceResolver.GetGlyph(locomotive, functionIndex), Is.Empty);
            Assert.That(LocomotiveFunctionAppearanceResolver.GetColor(locomotive, functionIndex), Is.EqualTo("#808080"));
            Assert.That(LocomotiveFunctionAppearanceResolver.GetDescription(locomotive, functionIndex), Is.Empty);
        }
    }

    [TestCase(0)]
    [TestCase(31)]
    public void BoundaryFunctionsUseConfiguredAppearance(int functionIndex)
    {
        var locomotive = new Locomotive
        {
            FunctionSymbols = Enumerable.Repeat(" custom.PNG", 32).ToList(),
            FunctionColors = Enumerable.Repeat("#aBcDeF", 32).ToList(),
            FunctionLabels = Enumerable.Repeat("  Horn  ", 32).ToList()
        };

        using (Assert.EnterMultipleScope())
        {
            Assert.That(LocomotiveFunctionAppearanceResolver.GetGlyph(locomotive, functionIndex), Is.EqualTo("custom.PNG"));
            Assert.That(LocomotiveFunctionAppearanceResolver.GetColor(locomotive, functionIndex), Is.EqualTo("#aBcDeF"));
            Assert.That(LocomotiveFunctionAppearanceResolver.GetDescription(locomotive, functionIndex), Is.EqualTo("Horn"));
        }
    }

    [TestCase(1, "f1__driving_sound.png")]
    [TestCase(2, "")]
    [TestCase(31, "")]
    public void MissingSymbolsUseOnlyAvailableDefaults(int functionIndex, string expected)
    {
        var locomotive = new Locomotive { FunctionSymbols = ["custom.png"] };
        using (Assert.EnterMultipleScope())
        {
            Assert.That(LocomotiveFunctionAppearanceResolver.GetGlyph(null, functionIndex), Is.EqualTo(expected));
            Assert.That(LocomotiveFunctionAppearanceResolver.GetGlyph(locomotive, functionIndex), Is.EqualTo(expected));
        }
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase(" ")]
    [TestCase("horn.svg")]
    public void InvalidStoredSymbolFallsBackToHeadlight(string? stored)
    {
        var locomotive = new Locomotive { FunctionSymbols = [stored!] };
        Assert.That(LocomotiveFunctionAppearanceResolver.GetGlyph(locomotive, 0), Is.EqualTo("headlight.png"));
    }

    [Test]
    public void NoneExplicitlyDisablesSymbolAndBacklight()
    {
        var locomotive = new Locomotive { FunctionSymbols = ["none"], FunctionColors = ["none"] };
        using (Assert.EnterMultipleScope())
        {
            Assert.That(LocomotiveFunctionAppearanceResolver.GetGlyph(locomotive, 0), Is.Empty);
            Assert.That(LocomotiveFunctionAppearanceResolver.GetColor(locomotive, 0), Is.EqualTo("#808080"));
        }
    }

    [TestCase(0, "#FFD700")]
    [TestCase(1, "#0078D4")]
    [TestCase(2, "#FF8C00")]
    [TestCase(3, "#E81123")]
    [TestCase(4, "#107C10")]
    [TestCase(5, "#00B7C3")]
    [TestCase(6, "#FFB900")]
    [TestCase(7, "#767676")]
    [TestCase(8, "#E81B23")]
    [TestCase(9, "#7B68EE")]
    [TestCase(10, "#8764B8")]
    [TestCase(11, "#038387")]
    [TestCase(12, "#C239B3")]
    [TestCase(13, "#FF1493")]
    [TestCase(14, "#7A7574")]
    [TestCase(15, "#567C73")]
    [TestCase(16, "#8E562E")]
    [TestCase(17, "#847545")]
    [TestCase(18, "#525E54")]
    [TestCase(19, "#4A5459")]
    [TestCase(20, "#69797E")]
    [TestCase(21, "#69797E")]
    [TestCase(22, "#69797E")]
    [TestCase(23, "#69797E")]
    [TestCase(24, "#69797E")]
    [TestCase(25, "#69797E")]
    [TestCase(26, "#69797E")]
    [TestCase(27, "#69797E")]
    [TestCase(28, "#69797E")]
    [TestCase(29, "#69797E")]
    [TestCase(30, "#69797E")]
    [TestCase(31, "#69797E")]
    public void MissingColorsUseFunctionSpecificPalette(int functionIndex, string expected)
    {
        Assert.That(LocomotiveFunctionAppearanceResolver.GetColor(null, functionIndex), Is.EqualTo(expected));
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase(" ")]
    [TestCase("#GGGGGG")]
    public void InvalidStoredColorFallsBackToDefault(string? stored)
    {
        var locomotive = new Locomotive { FunctionColors = [stored!] };
        Assert.That(LocomotiveFunctionAppearanceResolver.GetColor(locomotive, 0), Is.EqualTo("#FFD700"));
    }

    [Test]
    public void ShortOrMissingCollectionsDoNotIndexPastTheirEnd()
    {
        var locomotive = new Locomotive { FunctionColors = [], FunctionLabels = ["Headlight"] };
        using (Assert.EnterMultipleScope())
        {
            Assert.That(LocomotiveFunctionAppearanceResolver.GetColor(locomotive, 0), Is.EqualTo("#FFD700"));
            Assert.That(LocomotiveFunctionAppearanceResolver.GetDescription(locomotive, 1), Is.Empty);
            Assert.That(LocomotiveFunctionAppearanceResolver.GetDescription(null, 0), Is.Empty);
            Assert.That(LocomotiveFunctionAppearanceResolver.GetDescription(new Locomotive { FunctionLabels = [null!] }, 0), Is.Empty);
        }
    }

    [TestCase(null, false)]
    [TestCase("", false)]
    [TestCase(" ", false)]
    [TestCase("horn.svg", false)]
    [TestCase("horn.png ", false)]
    [TestCase("horn.png", true)]
    [TestCase("horn.PNG", true)]
    public void AssetReferencesRequirePngSuffix(string? value, bool expected)
    {
        Assert.That(LocomotiveFunctionAppearanceResolver.IsValidAssetReference(value), Is.EqualTo(expected));
    }

    [TestCase(null, false)]
    [TestCase("", false)]
    [TestCase(" ", false)]
    [TestCase("AABBCC", false)]
    [TestCase("XAABBCC", false)]
    [TestCase("#ABC", false)]
    [TestCase("#AABBCCDD", false)]
    [TestCase("#aBc012", true)]
    [TestCase("#ABCDEF", true)]
    [TestCase("#000000", true)]
    public void ColorsRequireHashAndExactlySixAsciiHexDigits(string? value, bool expected)
    {
        Assert.That(LocomotiveFunctionAppearanceResolver.IsValidHexColor(value), Is.EqualTo(expected));
    }

    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    [TestCase(4)]
    [TestCase(5)]
    [TestCase(6)]
    public void EveryColorDigitIsValidated(int position)
    {
        var characters = "#AABBCC".ToCharArray();
        characters[position] = 'G';
        Assert.That(LocomotiveFunctionAppearanceResolver.IsValidHexColor(new string(characters)), Is.False);
    }
}
