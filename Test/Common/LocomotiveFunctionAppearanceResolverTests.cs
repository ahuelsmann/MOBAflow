// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Test.Common;

using Moba.Common.Display;

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
}
