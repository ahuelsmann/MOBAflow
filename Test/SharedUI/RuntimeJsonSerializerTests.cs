// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.SharedUI;

using Domain;
using Moba.Common.Runtime;
using NUnit.Framework;
[TestFixture]
internal sealed class RuntimeJsonSerializerTests
{
    [Test]
    public void SerializeDeserialize_Should_RoundtripSnapshot()
    {
        var snapshot = new MobaRuntimeSnapshot
        {
            IsConnected = true,
            IsTrackPowerOn = true,
            StatusText = "Connected",
            SignalBoxElements =
            [
                new SignalBoxElementRuntimeSnapshot
                {
                    ElementId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
                    Name = "Signal 1",
                    Kind = SignalBoxElementKind.Signal
                }

            ],
            LocomotiveFleet =
            [
                new LocomotiveFleetSnapshot
                {
                    LocomotiveId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                    Name = "BR 110",
                    DigitalAddress = 7,
                    FunctionSymbols = ["headlight.png"],
                    FunctionColors = ["#FFD700"],
                    FunctionLabels = ["Directional headlight change"]
                }

            ]
        };
        var json = RuntimeJsonSerializer.Serialize(snapshot);
        var restored = RuntimeJsonSerializer.Deserialize(json);
        Assert.That(restored, Is.Not.Null);
        Assert.That(restored!.IsConnected, Is.True);
        Assert.That(restored.IsTrackPowerOn, Is.True);
        Assert.That(restored.StatusText, Is.EqualTo("Connected"));
        Assert.That(restored.SignalBoxElements, Has.Count.EqualTo(1));
        Assert.That(restored.SignalBoxElements[0].ElementId, Is.EqualTo(snapshot.SignalBoxElements[0].ElementId));
        Assert.That(restored.SignalBoxElements[0].Name, Is.EqualTo("Signal 1"));
        Assert.That(restored.LocomotiveFleet, Has.Count.EqualTo(1));
        Assert.That(restored.LocomotiveFleet[0].Name, Is.EqualTo("BR 110"));
        Assert.That(restored.LocomotiveFleet[0].FunctionSymbols, Is.EqualTo(new[] { "headlight.png" }));
        Assert.That(restored.LocomotiveFleet[0].FunctionColors, Is.EqualTo(new[] { "#FFD700" }));
        Assert.That(restored.LocomotiveFleet[0].FunctionLabels, Is.EqualTo(new[] { "Directional headlight change" }));
    }
}