// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Test.Common;

using Moba.Common.Runtime;
using Moba.Domain;

[TestFixture]
[Category("Unit")]
internal sealed class RuntimeSnapshotPreservationTests
{
    private static readonly Guid ProjectId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid SignalId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
    private static readonly Guid LocomotiveId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    [Test]
    public void PreserveProjectElementsFrom_NullIncoming_ReportsParameterName()
    {
        var exception = Assert.Throws<ArgumentNullException>(() =>
            RuntimeSnapshotPreservation.PreserveProjectElementsFrom(null!, null));

        Assert.That(exception!.ParamName, Is.EqualTo("incoming"));
    }

    [Test]
    public void PreserveSignalBoxElementsFrom_NullIncoming_ReportsParameterName()
    {
        var exception = Assert.Throws<ArgumentNullException>(() =>
            RuntimeSnapshotPreservation.PreserveSignalBoxElementsFrom(null!, null));

        Assert.That(exception!.ParamName, Is.EqualTo("incoming"));
    }

    [Test]
    public void PreserveLocomotiveFleetFrom_NullIncoming_ReportsParameterName()
    {
        var exception = Assert.Throws<ArgumentNullException>(() =>
            RuntimeSnapshotPreservation.PreserveLocomotiveFleetFrom(null!, null));

        Assert.That(exception!.ParamName, Is.EqualTo("incoming"));
    }

    [Test]
    public void PreserveProjectElementsFrom_KeepsIncomingWhenNoCachedElementsExist(
        [Values] bool hasPrevious, [Values] bool hasIncomingSignals, [Values] bool hasIncomingFleet)
    {
        var incoming = CreateIncomingSnapshot(hasIncomingSignals, hasIncomingFleet);
        var previous = hasPrevious ? new MobaRuntimeSnapshot { ProjectId = ProjectId } : null;

        var result = RuntimeSnapshotPreservation.PreserveProjectElementsFrom(incoming, previous);

        AssertIncomingTelemetry(result, incoming);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.SignalBoxElements, Is.EqualTo(incoming.SignalBoxElements));
            Assert.That(result.LocomotiveFleet, Is.EqualTo(incoming.LocomotiveFleet));
        }
    }

    [Test]
    public void PreserveProjectElementsFrom_UsesCachedElementsOnlyForMissingListsAndKeepsIncomingTelemetry(
        [Values] bool hasIncomingSignals, [Values] bool hasIncomingFleet, [Values] bool hasPersistenceError)
    {
        var incoming = CreateIncomingSnapshot(hasIncomingSignals, hasIncomingFleet, hasPersistenceError);
        var previous = new MobaRuntimeSnapshot
        {
            ProjectId = ProjectId,
            MainCurrent = 1,
            StatusText = "Previous telemetry",
            InPortCounterPersistenceError = "Previous counter error",
            SignalBoxElements =
            [
                new SignalBoxElementRuntimeSnapshot
                {
                    ElementId = SignalId,
                    Name = "Previous signal",
                    Kind = SignalBoxElementKind.Signal,
                    SignalAspect = SignalAspect.Hp0
                }
            ],
            LocomotiveFleet =
            [
                new LocomotiveFleetSnapshot
                {
                    LocomotiveId = LocomotiveId,
                    Name = "Previous locomotive",
                    DigitalAddress = 9,
                    PhotoPath = "photos/previous.jpg"
                }
            ]
        };

        var result = RuntimeSnapshotPreservation.PreserveProjectElementsFrom(incoming, previous);

        AssertIncomingTelemetry(result, incoming);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.SignalBoxElements,
                Is.EqualTo(hasIncomingSignals ? incoming.SignalBoxElements : previous.SignalBoxElements));
            Assert.That(result.LocomotiveFleet,
                Is.EqualTo(hasIncomingFleet ? incoming.LocomotiveFleet : previous.LocomotiveFleet));
            Assert.That(incoming.SignalBoxElements, Has.Count.EqualTo(hasIncomingSignals ? 1 : 0));
            Assert.That(incoming.LocomotiveFleet, Has.Count.EqualTo(hasIncomingFleet ? 1 : 0));
            Assert.That(previous.SignalBoxElements[0].SignalAspect, Is.EqualTo(SignalAspect.Hp0));
            Assert.That(previous.LocomotiveFleet[0].DigitalAddress, Is.EqualTo(9));
        }
    }

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

    private static MobaRuntimeSnapshot CreateIncomingSnapshot(bool hasSignals, bool hasFleet, bool hasPersistenceError = true)
    {
        return new MobaRuntimeSnapshot
        {
            ProjectId = ProjectId,
            IsConnected = true,
            IsTrackPowerOn = true,
            StatusText = "Current telemetry",
            SerialNumber = "Current serial",
            FirmwareVersion = "Current firmware",
            HardwareType = "Current hardware",
            MainCurrent = 42,
            ProgCurrent = 43,
            FilteredMainCurrent = 44,
            Temperature = 45,
            SupplyVoltage = 16000,
            VccVoltage = 5000,
            IsZ21Connecting = false,
            HasSeenSuccessfulConnection = true,
            IsManualDisconnectRequested = true,
            IsEmergencyStopActive = true,
            IsShortCircuitActive = true,
            IsProgrammingModeActive = true,
            LastFailSafeReason = "Current safety diagnostic",
            LastFailSafeAt = new DateTimeOffset(2026, 10, 12, 1, 2, 3, TimeSpan.Zero),
            IsOperatorAckRequired = true,
            InPortCounterPersistenceError = hasPersistenceError ? "Current counters could not be saved." : null,
            InPortCounters = [new InPortCounterSnapshot(3, 17, null, TimeSpan.FromSeconds(2))],
            JourneyStates = new Dictionary<Guid, JourneyRuntimeSnapshot>
            {
                [SignalId] = new JourneyRuntimeSnapshot
                {
                    JourneyId = SignalId,
                    CurrentStationName = "Central Station",
                    CurrentPos = 2,
                    IsActive = true
                }
            },
            LocomotiveStates = new Dictionary<int, LocomotiveRuntimeSnapshot>
            {
                [7] = new LocomotiveRuntimeSnapshot { Address = 7, Speed = 0, IsForward = true, Functions = 1 }
            },
            CreatedAt = new DateTimeOffset(2026, 10, 12, 1, 2, 4, TimeSpan.Zero),
            SignalBoxElements = hasSignals
                ? [new SignalBoxElementRuntimeSnapshot
                {
                    ElementId = SignalId,
                    Name = "Current signal",
                    Kind = SignalBoxElementKind.Signal,
                    X = 8,
                    Y = 5,
                    SignalAspect = SignalAspect.Ks1
                }]
                : [],
            LocomotiveFleet = hasFleet
                ? [new LocomotiveFleetSnapshot
                {
                    LocomotiveId = LocomotiveId,
                    Name = "Current locomotive",
                    DigitalAddress = 7,
                    PhotoPath = "photos/current.jpg"
                }]
                : []
        };
    }

    private static void AssertIncomingTelemetry(MobaRuntimeSnapshot result, MobaRuntimeSnapshot incoming)
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.ProjectId, Is.EqualTo(incoming.ProjectId));
            Assert.That(result.IsConnected, Is.EqualTo(incoming.IsConnected));
            Assert.That(result.IsTrackPowerOn, Is.EqualTo(incoming.IsTrackPowerOn));
            Assert.That(result.StatusText, Is.EqualTo(incoming.StatusText));
            Assert.That(result.SerialNumber, Is.EqualTo(incoming.SerialNumber));
            Assert.That(result.FirmwareVersion, Is.EqualTo(incoming.FirmwareVersion));
            Assert.That(result.HardwareType, Is.EqualTo(incoming.HardwareType));
            Assert.That(result.MainCurrent, Is.EqualTo(incoming.MainCurrent));
            Assert.That(result.ProgCurrent, Is.EqualTo(incoming.ProgCurrent));
            Assert.That(result.FilteredMainCurrent, Is.EqualTo(incoming.FilteredMainCurrent));
            Assert.That(result.Temperature, Is.EqualTo(incoming.Temperature));
            Assert.That(result.SupplyVoltage, Is.EqualTo(incoming.SupplyVoltage));
            Assert.That(result.VccVoltage, Is.EqualTo(incoming.VccVoltage));
            Assert.That(result.IsZ21Connecting, Is.EqualTo(incoming.IsZ21Connecting));
            Assert.That(result.HasSeenSuccessfulConnection, Is.EqualTo(incoming.HasSeenSuccessfulConnection));
            Assert.That(result.IsManualDisconnectRequested, Is.EqualTo(incoming.IsManualDisconnectRequested));
            Assert.That(result.IsEmergencyStopActive, Is.EqualTo(incoming.IsEmergencyStopActive));
            Assert.That(result.IsShortCircuitActive, Is.EqualTo(incoming.IsShortCircuitActive));
            Assert.That(result.IsProgrammingModeActive, Is.EqualTo(incoming.IsProgrammingModeActive));
            Assert.That(result.LastFailSafeReason, Is.EqualTo(incoming.LastFailSafeReason));
            Assert.That(result.LastFailSafeAt, Is.EqualTo(incoming.LastFailSafeAt));
            Assert.That(result.IsOperatorAckRequired, Is.EqualTo(incoming.IsOperatorAckRequired));
            Assert.That(result.InPortCounterPersistenceError, Is.EqualTo(incoming.InPortCounterPersistenceError));
            Assert.That(result.InPortCounters, Is.EqualTo(incoming.InPortCounters));
            Assert.That(result.JourneyStates, Is.EqualTo(incoming.JourneyStates));
            Assert.That(result.LocomotiveStates, Is.EqualTo(incoming.LocomotiveStates));
            Assert.That(result.CreatedAt, Is.EqualTo(incoming.CreatedAt));
        }
    }
}
