// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Test.Common;

using Moba.Common.Display;

using Domain;

[TestFixture]
internal sealed class KsSignalAspectNamesTests
{
    [Test]
    public void ResolvePreviewSignalArticleNumber_Should_Return4046_OnlyFor4046()
    {
        Assert.That(KsSignalAspectNames.ResolvePreviewSignalArticleNumber("4046"), Is.EqualTo("4046"));
        Assert.That(KsSignalAspectNames.ResolvePreviewSignalArticleNumber("4032"), Is.EqualTo(string.Empty));
        Assert.That(KsSignalAspectNames.ResolvePreviewSignalArticleNumber(null), Is.EqualTo(string.Empty));
    }

    [Test]
    public void GetAspectLabel_Should_Use4046Names_WhenSignalIs4046()
    {
        Assert.That(KsSignalAspectNames.GetAspectLabel(SignalAspect.Ks2, is4046: true), Is.EqualTo("Ks2+K"));
        Assert.That(KsSignalAspectNames.GetAspectLabel(SignalAspect.Dunkel, is4046: true), Is.EqualTo("GrBl+K+G"));
        Assert.That(KsSignalAspectNames.GetAspectLabel(SignalAspect.Ks2, is4046: false), Is.EqualTo("Ks2"));
        Assert.That(KsSignalAspectNames.GetAspectLabel(SignalAspect.Ra12, is4046: true), Is.EqualTo("Hp0+Rg"));
        Assert.That(KsSignalAspectNames.GetAspectLabel(SignalAspect.Zs1, is4046: true), Is.EqualTo("Ks1+G"));
        Assert.That(KsSignalAspectNames.GetAspectLabel(SignalAspect.Ks1Blink, is4046: true), Is.EqualTo("Ks2+K+G"));
    }
}

[TestFixture]
internal sealed class KsSignalScreenVisualStateTests
{
    private const string Signal4046 = "4046";

    [Test]
    public void Create_StandardHp0_Should_OnlyLightRedLamp()
    {
        var state = KsSignalScreenVisualState.Create(null, KsSignalAspectNames.Hp0);

        Assert.That(state.Hp0, Is.EqualTo(KsSignalLampColor.Red));
        Assert.That(state.Ks1, Is.EqualTo(KsSignalLampColor.Off));
        Assert.That(state.ShowTopSpeed, Is.False);
        Assert.That(state.ShowBottomSpeed, Is.False);
    }

    [Test]
    public void Create_4046Hp0_Should_OnlyLightRedLamp()
    {
        var state = KsSignalScreenVisualState.Create(Signal4046, KsSignalAspectNames.Hp0);

        Assert.That(state.Hp0, Is.EqualTo(KsSignalLampColor.Red));
        Assert.That(state.Ks1, Is.EqualTo(KsSignalLampColor.Off));
        Assert.That(state.W1, Is.EqualTo(KsSignalLampColor.Off));
        Assert.That(state.ShowTopSpeed, Is.False);
        Assert.That(state.ShowBottomSpeed, Is.False);
    }

    [Test]
    public void Create_4046Ra12_Should_LightHp0AndWhiteCenter()
    {
        var state = KsSignalScreenVisualState.Create(Signal4046, KsSignalAspectNames.Ra12);

        Assert.That(state.Hp0, Is.EqualTo(KsSignalLampColor.Red));
        Assert.That(state.Zs7Center, Is.EqualTo(KsSignalLampColor.White));
        Assert.That(state.Ks1, Is.EqualTo(KsSignalLampColor.Off));
    }

    [Test]
    public void Create_4046Ks1_Should_OnlyLightGreenLamp()
    {
        var state = KsSignalScreenVisualState.Create(Signal4046, KsSignalAspectNames.Ks1);

        Assert.That(state.Ks1, Is.EqualTo(KsSignalLampColor.Green));
        Assert.That(state.ShowTopSpeed, Is.False);
    }

    [Test]
    public void Create_4046Zs1_Should_ShowTopSpeedIndicator()
    {
        var state = KsSignalScreenVisualState.Create(Signal4046, KsSignalAspectNames.Zs1, topSpeedValue: "8");

        Assert.That(state.Ks1, Is.EqualTo(KsSignalLampColor.Green));
        Assert.That(state.ShowTopSpeed, Is.True);
        Assert.That(state.TopSpeedText, Is.EqualTo("8"));
        Assert.That(state.ShowBottomSpeed, Is.False);
    }

    [Test]
    public void Create_4046Dunkel_Should_ShowBothSpeedIndicatorsAndBlinkKs1()
    {
        var state = KsSignalScreenVisualState.Create(Signal4046, KsSignalAspectNames.Dunkel, "3", "1");

        Assert.That(state.W1, Is.EqualTo(KsSignalLampColor.White));
        Assert.That(state.Ks1, Is.EqualTo(KsSignalLampColor.Green));
        Assert.That(state.BlinkLamp, Is.EqualTo(KsSignalBlinkLamp.Ks1));
        Assert.That(state.ShowTopSpeed, Is.True);
        Assert.That(state.ShowBottomSpeed, Is.True);
        Assert.That(state.TopSpeedText, Is.EqualTo("3"));
        Assert.That(state.BottomSpeedText, Is.EqualTo("1"));
    }

    [Test]
    public void Create_4046Ks2_Should_LightYellowAndMarkerWhite()
    {
        var state = KsSignalScreenVisualState.Create(Signal4046, KsSignalAspectNames.Ks2);

        Assert.That(state.Ks2, Is.EqualTo(KsSignalLampColor.Yellow));
        Assert.That(state.W1, Is.EqualTo(KsSignalLampColor.White));
        Assert.That(state.ShowTopSpeed, Is.False);
    }

    [Test]
    public void Create_4046Ks1Blink_Should_LightYellowMarkerAndTopSpeed()
    {
        var state = KsSignalScreenVisualState.Create(Signal4046, KsSignalAspectNames.Ks1Blink, topSpeedValue: "10");

        Assert.That(state.Ks2, Is.EqualTo(KsSignalLampColor.Yellow));
        Assert.That(state.W1, Is.EqualTo(KsSignalLampColor.White));
        Assert.That(state.ShowTopSpeed, Is.True);
        Assert.That(state.TopSpeedText, Is.EqualTo("10"));
        Assert.That(state.ShowBottomSpeed, Is.False);
    }

    [Test]
    public void Create_4046Kennlicht_Should_LightMarkerWhite()
    {
        var state = KsSignalScreenVisualState.Create(Signal4046, KsSignalAspectNames.Kennlicht);

        Assert.That(state.W1, Is.EqualTo(KsSignalLampColor.White));
        Assert.That(state.Hp0, Is.EqualTo(KsSignalLampColor.Off));
    }

    [Test]
    public void Create_4046Zs7_Should_LightThreeYellowLamps()
    {
        var state = KsSignalScreenVisualState.Create(Signal4046, KsSignalAspectNames.Zs7);

        Assert.That(state.W2, Is.EqualTo(KsSignalLampColor.Yellow));
        Assert.That(state.Zs7Center, Is.EqualTo(KsSignalLampColor.Yellow));
        Assert.That(state.Zs7Right, Is.EqualTo(KsSignalLampColor.Yellow));
    }

    [Test]
    public void Create_StandardRa12_Should_LightWhiteLowerLamps()
    {
        var state = KsSignalScreenVisualState.Create("4032", KsSignalAspectNames.Ra12);

        Assert.That(state.W3, Is.EqualTo(KsSignalLampColor.White));
        Assert.That(state.Ra12Right, Is.EqualTo(KsSignalLampColor.White));
        Assert.That(state.Hp0, Is.EqualTo(KsSignalLampColor.Off));
    }

    [Test]
    public void Create_StandardZs1_Should_BlinkW1()
    {
        var state = KsSignalScreenVisualState.Create(null, KsSignalAspectNames.Zs1);

        Assert.That(state.W1, Is.EqualTo(KsSignalLampColor.White));
        Assert.That(state.BlinkLamp, Is.EqualTo(KsSignalBlinkLamp.W1));
    }

    [Test]
    public void GetDesignHeight_Should_IncludeSpeedRowsOnlyWhenVisible()
    {
        Assert.That(KsSignalScreenLayout.GetDesignHeight(false, false), Is.LessThan(KsSignalScreenLayout.GetDesignHeight(true, false)));
        Assert.That(KsSignalScreenLayout.GetDesignHeight(true, true), Is.GreaterThan(KsSignalScreenLayout.GetDesignHeight(true, false)));
    }
}
