// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.SharedUI;

using Moba.SharedUI.Service;

[TestFixture]
internal sealed class SignalBoxElementDescriptionsTests
{
    [Test]
    public void Signal_NamesItsAspect()
    {
        var signal = new SbSignal { Name = "A1", SignalAspect = SignalAspect.Ks1 };

        Assert.That(SignalBoxElementDescriptions.Describe(signal), Is.EqualTo("Signal A1, Ks1"));
    }

    [TestCase(SwitchPosition.Straight, "Switch W2, straight")]
    [TestCase(SwitchPosition.DivergingLeft, "Switch W2, diverging left")]
    [TestCase(SwitchPosition.DivergingRight, "Switch W2, diverging right")]
    public void Switch_NamesItsPosition(SwitchPosition position, string expected)
    {
        var @switch = new SbSwitch { Name = "W2", SwitchPosition = position };

        Assert.That(SignalBoxElementDescriptions.Describe(@switch), Is.EqualTo(expected));
    }

    [TestCase(SignalBoxElementState.Free, "Feedback detector D3")]
    [TestCase(SignalBoxElementState.Occupied, "Feedback detector D3, occupied")]
    [TestCase(SignalBoxElementState.Fault, "Feedback detector D3, fault")]
    public void State_IsSpokenOnlyWhenNotFree(SignalBoxElementState state, string expected)
    {
        var detector = new SbDetector { Name = "D3", State = state };

        Assert.That(SignalBoxElementDescriptions.Describe(detector), Is.EqualTo(expected));
    }

    [Test]
    public void UnnamedTracks_AreDescribedByTheirKind()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(SignalBoxElementDescriptions.Describe(new SbTrackStraight()), Is.EqualTo("Track"));
            Assert.That(SignalBoxElementDescriptions.Describe(new SbTrackCurve { Name = "  " }), Is.EqualTo("Curved track"));
        }
    }
}
