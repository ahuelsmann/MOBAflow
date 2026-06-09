// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Test.SharedUI;

using Moba.Common.Runtime;
using Moba.SharedUI.Service;

/// <summary>
/// Unit tests for runtime snapshot projection helpers used by SharedUI ViewModels.
/// </summary>
[TestFixture]
internal sealed class RuntimeSnapshotProjectorTests
{
    [Test]
    public void ProjectStatus_ShouldCopySharedRuntimeFields()
    {
        var snapshot = new MobaRuntimeSnapshot
        {
            IsConnected = true,
            IsTrackPowerOn = true,
            StatusText = "Connected",
            SerialNumber = "123",
            FirmwareVersion = "1.42",
            HardwareType = "Z21",
            MainCurrent = 101,
            Temperature = 35,
            SupplyVoltage = 18000,
            VccVoltage = 5000,
            IsZ21Connecting = false,
            HasSeenSuccessfulConnection = true,
            IsManualDisconnectRequested = false,
            IsEmergencyStopActive = true,
            IsShortCircuitActive = false,
            IsProgrammingModeActive = true,
            LastFailSafeReason = "Emergency stop",
            LastFailSafeAt = DateTimeOffset.UtcNow,
            IsOperatorAckRequired = true
        };

        var projection = RuntimeSnapshotProjector.ProjectStatus(snapshot);

        Assert.That(projection.IsConnected, Is.True);
        Assert.That(projection.IsTrackPowerOn, Is.True);
        Assert.That(projection.StatusText, Is.EqualTo("Connected"));
        Assert.That(projection.SerialNumber, Is.EqualTo("123"));
        Assert.That(projection.MainCurrent, Is.EqualTo(101));
        Assert.That(projection.IsEmergencyStopActive, Is.True);
        Assert.That(projection.IsOperatorAckRequired, Is.True);
    }

    [Test]
    [TestCase(false, "Disconnected", null)]
    [TestCase(false, "Waiting for Z21", "Waiting for Z21")]
    [TestCase(true, "Any status", "Connected")]
    public void ProjectMaui_ShouldReturnMobileConnectionStatus(bool isConnected, string statusText, string? expectedStatus)
    {
        var snapshot = new MobaRuntimeSnapshot
        {
            IsConnected = isConnected,
            StatusText = statusText
        };

        var projection = RuntimeSnapshotProjector.ProjectMaui(snapshot, wasConnected: false);

        Assert.That(projection.Z21ConnectionStatus, Is.EqualTo(expectedStatus));
        Assert.That(projection.ShouldPersistCurrentIpAddress, Is.EqualTo(isConnected));
    }

    [Test]
    public void ProjectTrainControl_ShouldReturnLocomotiveStateForSelectedAddress()
    {
        var locomotiveState = new LocomotiveRuntimeSnapshot
        {
            Address = 3,
            Speed = 40,
            IsForward = true,
            Functions = 5
        };
        var snapshot = new MobaRuntimeSnapshot
        {
            IsConnected = true,
            LocomotiveStates = new Dictionary<int, LocomotiveRuntimeSnapshot>
            {
                [3] = locomotiveState
            }
        };

        var projection = RuntimeSnapshotProjector.ProjectTrainControl(snapshot, wasConnected: false, locomotiveAddress: 3);

        Assert.That(projection.IsConnected, Is.True);
        Assert.That(projection.ConnectionChanged, Is.True);
        Assert.That(projection.LocomotiveState, Is.SameAs(locomotiveState));
    }
}
