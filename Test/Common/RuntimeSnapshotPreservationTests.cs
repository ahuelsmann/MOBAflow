// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Test.Common;

using Moba.Common.Runtime;
using Moba.Domain;

[TestFixture]
internal sealed class RuntimeSnapshotPreservationTests
{
    [Test]
    public void PreserveSignalBoxElementsFrom_KeepsPrevious_WhenIncomingIsEmpty()
    {
        var signalId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var previous = new MobaRuntimeSnapshot
        {
            IsConnected = true,
            SignalBoxElements =
            [
                new SignalBoxElementRuntimeSnapshot
                {
                    ElementId = signalId,
                    Name = "S1",
                    Kind = SignalBoxElementKind.Signal,
                    SignalAspect = SignalAspect.Ks1
                }
            ]
        };

        var incoming = new MobaRuntimeSnapshot
        {
            IsConnected = true,
            MainCurrent = 42,
            SignalBoxElements = []
        };

        var merged = RuntimeSnapshotPreservation.PreserveSignalBoxElementsFrom(incoming, previous);

        Assert.Multiple(() =>
        {
            Assert.That(merged.MainCurrent, Is.EqualTo(42));
            Assert.That(merged.SignalBoxElements, Has.Count.EqualTo(1));
            Assert.That(merged.SignalBoxElements[0].ElementId, Is.EqualTo(signalId));
        });
    }

    [Test]
    public void PreserveSignalBoxElementsFrom_UsesIncoming_WhenIncomingHasElements()
    {
        var previousId = Guid.NewGuid();
        var incomingId = Guid.NewGuid();
        var previous = new MobaRuntimeSnapshot
        {
            SignalBoxElements =
            [
                new SignalBoxElementRuntimeSnapshot { ElementId = previousId, Name = "Old", Kind = SignalBoxElementKind.Signal }
            ]
        };

        var incoming = new MobaRuntimeSnapshot
        {
            SignalBoxElements =
            [
                new SignalBoxElementRuntimeSnapshot { ElementId = incomingId, Name = "New", Kind = SignalBoxElementKind.Signal }
            ]
        };

        var merged = RuntimeSnapshotPreservation.PreserveSignalBoxElementsFrom(incoming, previous);

        Assert.Multiple(() =>
        {
            Assert.That(merged.SignalBoxElements, Has.Count.EqualTo(1));
            Assert.That(merged.SignalBoxElements[0].ElementId, Is.EqualTo(incomingId));
        });
    }

    [Test]
    public void PreserveSignalBoxElementsFrom_BorrowsPreviousAspect_WhenIncomingOmitsAspectOnSameElement()
    {
        var signalId = Guid.NewGuid();
        var previous = new MobaRuntimeSnapshot
        {
            SignalBoxElements =
            [
                new SignalBoxElementRuntimeSnapshot
                {
                    ElementId = signalId,
                    Name = "S1",
                    Kind = SignalBoxElementKind.Signal,
                    SignalAspect = SignalAspect.Ks1
                }
            ]
        };

        var incoming = new MobaRuntimeSnapshot
        {
            SignalBoxElements =
            [
                new SignalBoxElementRuntimeSnapshot
                {
                    ElementId = signalId,
                    Name = "S1",
                    Kind = SignalBoxElementKind.Signal
                }
            ]
        };

        var merged = RuntimeSnapshotPreservation.PreserveSignalBoxElementsFrom(incoming, previous);

        Assert.Multiple(() =>
        {
            Assert.That(merged.SignalBoxElements, Has.Count.EqualTo(1));
            Assert.That(merged.SignalBoxElements[0].SignalAspect, Is.EqualTo(SignalAspect.Ks1));
        });
    }

    [Test]
    public void PreserveSignalBoxElementsFrom_PrefersIncomingAspect_OnSameElement()
    {
        var signalId = Guid.NewGuid();
        var previous = new MobaRuntimeSnapshot
        {
            SignalBoxElements =
            [
                new SignalBoxElementRuntimeSnapshot
                {
                    ElementId = signalId,
                    Kind = SignalBoxElementKind.Signal,
                    SignalAspect = SignalAspect.Hp0
                }
            ]
        };

        var incoming = new MobaRuntimeSnapshot
        {
            SignalBoxElements =
            [
                new SignalBoxElementRuntimeSnapshot
                {
                    ElementId = signalId,
                    Kind = SignalBoxElementKind.Signal,
                    SignalAspect = SignalAspect.Ks1
                }
            ]
        };

        var merged = RuntimeSnapshotPreservation.PreserveSignalBoxElementsFrom(incoming, previous);

        Assert.That(merged.SignalBoxElements.Single().SignalAspect, Is.EqualTo(SignalAspect.Ks1));
    }

    [Test]
    public void PreserveLocomotiveFleetFrom_KeepsPrevious_WhenIncomingIsEmpty()
    {
        var locomotiveId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        var previous = new MobaRuntimeSnapshot
        {
            LocomotiveFleet =
            [
                new LocomotiveFleetSnapshot
                {
                    LocomotiveId = locomotiveId,
                    Name = "BR 110",
                    DigitalAddress = 7
                }
            ]
        };

        var incoming = new MobaRuntimeSnapshot
        {
            IsConnected = true,
            MainCurrent = 42,
            LocomotiveFleet = []
        };

        var merged = RuntimeSnapshotPreservation.PreserveLocomotiveFleetFrom(incoming, previous);

        Assert.Multiple(() =>
        {
            Assert.That(merged.MainCurrent, Is.EqualTo(42));
            Assert.That(merged.LocomotiveFleet, Has.Count.EqualTo(1));
            Assert.That(merged.LocomotiveFleet[0].LocomotiveId, Is.EqualTo(locomotiveId));
        });
    }

    [Test]
    public void PreserveProjectElementsFrom_PreservesBothSignalBoxAndFleet()
    {
        var signalId = Guid.NewGuid();
        var locomotiveId = Guid.NewGuid();
        var previous = new MobaRuntimeSnapshot
        {
            SignalBoxElements =
            [
                new SignalBoxElementRuntimeSnapshot { ElementId = signalId, Name = "S1", Kind = SignalBoxElementKind.Signal }
            ],
            LocomotiveFleet =
            [
                new LocomotiveFleetSnapshot { LocomotiveId = locomotiveId, Name = "BR 218", DigitalAddress = 3 }
            ]
        };

        var incoming = new MobaRuntimeSnapshot
        {
            MainCurrent = 12,
            SignalBoxElements = [],
            LocomotiveFleet = []
        };

        var merged = RuntimeSnapshotPreservation.PreserveProjectElementsFrom(incoming, previous);

        Assert.Multiple(() =>
        {
            Assert.That(merged.MainCurrent, Is.EqualTo(12));
            Assert.That(merged.SignalBoxElements, Has.Count.EqualTo(1));
            Assert.That(merged.LocomotiveFleet, Has.Count.EqualTo(1));
        });
    }
}
