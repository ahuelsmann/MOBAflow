// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Test.Common;

using Moba.Common.Runtime;
using Moba.Domain;

[TestFixture]
internal sealed class RuntimeSnapshotRemoteFilterTests
{
    [Test]
    public void ForMobasmartBroadcast_KeepsSignalBoxAndJourneys_StripsLocomotiveStates()
    {
        var signalId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var journeyId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

        var full = new MobaRuntimeSnapshot
        {
            IsConnected = true,
            SignalBoxElements =
            [
                new SignalBoxElementRuntimeSnapshot
                {
                    ElementId = signalId,
                    Name = "S1",
                    Kind = SignalBoxElementKind.Signal,
                    SignalAspect = SignalAspect.Hp0
                }
            ],
            JourneyStates = new Dictionary<Guid, JourneyRuntimeSnapshot>
            {
                [journeyId] = new JourneyRuntimeSnapshot
                {
                    JourneyId = journeyId,
                    CurrentStepOccurrence = 2,
                    CurrentStepRepeatCount = 10,
                    IsActive = true
                }
            },
            LocomotiveStates = new Dictionary<int, LocomotiveRuntimeSnapshot>
            {
                [3] = new LocomotiveRuntimeSnapshot
                {
                    Address = 3,
                    Speed = 55,
                    IsForward = true,
                    Functions = 0x04
                }
            },
            LocomotiveFleet =
            [
                new LocomotiveFleetSnapshot
                {
                    LocomotiveId = Guid.NewGuid(),
                    Name = "BR 110",
                    DigitalAddress = 3
                }
            ]
        };

        var slim = RuntimeSnapshotRemoteFilter.ForMobasmartBroadcast(full);

        Assert.Multiple(() =>
        {
            Assert.That(slim.LocomotiveStates, Is.Empty);
            Assert.That(slim.SignalBoxElements, Has.Count.EqualTo(1));
            Assert.That(slim.SignalBoxElements[0].SignalAspect, Is.EqualTo(SignalAspect.Hp0));
            Assert.That(slim.JourneyStates, Has.Count.EqualTo(1));
            Assert.That(slim.LocomotiveFleet, Has.Count.EqualTo(1));
            Assert.That(slim.IsConnected, Is.True);
        });
    }

    [Test]
    public void ForMobasmartBroadcast_ReducesPayloadSize_ForTypicalFleet()
    {
        var locomotiveStates = new Dictionary<int, LocomotiveRuntimeSnapshot>();
        for (var address = 1; address <= 20; address++)
        {
            locomotiveStates[address] = new LocomotiveRuntimeSnapshot
            {
                Address = address,
                Speed = address * 2,
                IsForward = address % 2 == 0,
                Functions = (uint)address
            };
        }

        var full = new MobaRuntimeSnapshot
        {
            IsConnected = true,
            LocomotiveStates = locomotiveStates,
            SignalBoxElements =
            [
                new SignalBoxElementRuntimeSnapshot
                {
                    ElementId = Guid.NewGuid(),
                    Name = "S1",
                    Kind = SignalBoxElementKind.Signal,
                    SignalAspect = SignalAspect.Ks1
                }
            ]
        };

        var fullBytes = System.Text.Encoding.UTF8.GetByteCount(RuntimeJsonSerializer.Serialize(full));
        var slimBytes = System.Text.Encoding.UTF8.GetByteCount(
            RuntimeJsonSerializer.Serialize(RuntimeSnapshotRemoteFilter.ForMobasmartBroadcast(full)));

        Assert.That(slimBytes, Is.LessThan(fullBytes));
        TestContext.Out.WriteLine($"Full snapshot: {fullBytes} bytes, slim: {slimBytes} bytes");
    }
}