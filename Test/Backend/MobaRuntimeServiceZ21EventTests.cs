// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.Backend;

using Microsoft.Extensions.Logging;

using Moba.Backend.Interface;
using Moba.Backend.Model;
using Moba.Backend.Protocol;
using Moba.Backend.Service;
using Moba.Common.Configuration;

using Moq;

/// <summary>
/// How <see cref="MobaRuntimeService"/> projects Z21 callbacks into its runtime snapshot, including the fail-safe after
/// an unexpected connection loss.
/// </summary>
[TestFixture]
internal sealed class MobaRuntimeServiceZ21EventTests
{
    [Test]
    public void ConnectionConfirmation_EndsConnectingAndRemembersTheSuccessfulConnection()
    {
        var z21 = CreateZ21Mock();
        using var runtime = CreateRuntime(z21.Object);
        Assert.That(runtime.Current.IsZ21Connecting, Is.True);

        z21.Raise(value => value.OnConnectedChanged += null, true);

        var snapshot = runtime.Current;
        using (Assert.EnterMultipleScope())
        {
            Assert.That(snapshot.IsConnected, Is.True);
            Assert.That(snapshot.IsZ21Connecting, Is.False);
            Assert.That(snapshot.HasSeenSuccessfulConnection, Is.True);
            Assert.That(snapshot.IsManualDisconnectRequested, Is.False);
        }
    }

    [Test]
    public void SystemState_ConfirmsTheConnectionAndPublishesMeasurementsAndFlags()
    {
        var z21 = CreateZ21Mock();
        using var runtime = CreateRuntime(z21.Object);

        z21.Raise(value => value.OnSystemStateChanged += null, new SystemState
        {
            MainCurrent = 1000,
            ProgCurrent = 20,
            FilteredMainCurrent = 990,
            Temperature = 35,
            SupplyVoltage = 18000,
            VccVoltage = 17500,
            CentralState = 0x01 | 0x04 | 0x20 // emergency stop, short circuit, programming mode; track power on
        });

        var snapshot = runtime.Current;
        using (Assert.EnterMultipleScope())
        {
            Assert.That(snapshot.IsConnected, Is.True);
            Assert.That(snapshot.IsZ21Connecting, Is.False);
            Assert.That(snapshot.HasSeenSuccessfulConnection, Is.True);
            Assert.That(snapshot.IsTrackPowerOn, Is.True);
            Assert.That(snapshot.IsEmergencyStopActive, Is.True);
            Assert.That(snapshot.IsShortCircuitActive, Is.True);
            Assert.That(snapshot.IsProgrammingModeActive, Is.True);
            Assert.That(
                (snapshot.MainCurrent, snapshot.ProgCurrent, snapshot.FilteredMainCurrent, snapshot.Temperature,
                    snapshot.SupplyVoltage, snapshot.VccVoltage),
                Is.EqualTo((1000, 20, 990, 35, 18000, 17500)));
        }
    }

    [Test]
    public void XBusStatus_PublishesEveryFlag()
    {
        var z21 = CreateZ21Mock();
        using var runtime = CreateRuntime(z21.Object);

        z21.Raise(value => value.OnXBusStatusChanged += null,
            new XBusStatus(emergencyStop: true, trackOff: true, shortCircuit: true, programming: true));

        var snapshot = runtime.Current;
        using (Assert.EnterMultipleScope())
        {
            Assert.That(snapshot.IsTrackPowerOn, Is.False);
            Assert.That(snapshot.IsEmergencyStopActive, Is.True);
            Assert.That(snapshot.IsShortCircuitActive, Is.True);
            Assert.That(snapshot.IsProgrammingModeActive, Is.True);
        }
    }

    [Test]
    public void UnexpectedConnectionLoss_ClearsTheZ21StateAndRequiresOperatorRelease()
    {
        var z21 = CreateZ21Mock();
        using var runtime = CreateRuntime(z21.Object);
        z21.Raise(value => value.OnConnectedChanged += null, true);
        z21.Raise(value => value.OnXBusStatusChanged += null,
            new XBusStatus(emergencyStop: true, trackOff: false, shortCircuit: true, programming: true));

        z21.Raise(value => value.OnConnectionLost += null);

        var snapshot = runtime.Current;
        using (Assert.EnterMultipleScope())
        {
            Assert.That(snapshot.IsConnected, Is.False);
            Assert.That(snapshot.IsTrackPowerOn, Is.False);
            Assert.That(snapshot.IsEmergencyStopActive, Is.False);
            Assert.That(snapshot.IsShortCircuitActive, Is.False);
            Assert.That(snapshot.IsProgrammingModeActive, Is.False);
            Assert.That(snapshot.IsZ21Connecting, Is.False);
            Assert.That(snapshot.IsManualDisconnectRequested, Is.False);
            Assert.That(snapshot.IsOperatorAckRequired, Is.True);
            Assert.That(snapshot.LastFailSafeAt, Is.Not.Null);
            Assert.That(snapshot.LastFailSafeReason, Is.EqualTo("Unexpected loss of the Z21 connection."));
            Assert.That(snapshot.StatusText, Is.EqualTo("Connection lost - reconnect required"));
        }
    }

    [Test]
    public void VersionInfo_ShowsKnownValuesAndADashForUnknownOnes()
    {
        var z21 = CreateZ21Mock();
        using var runtime = CreateRuntime(z21.Object);
        var known = new Z21VersionInfo { SerialNumber = 123456, HardwareTypeCode = 0x0211, FirmwareVersionCode = 0x0142 };

        z21.Raise(value => value.OnVersionInfoChanged += null, new Z21VersionInfo());
        var unknown = runtime.Current;
        z21.Raise(value => value.OnVersionInfoChanged += null, known);

        using (Assert.EnterMultipleScope())
        {
            Assert.That((unknown.SerialNumber, unknown.HardwareType, unknown.FirmwareVersion), Is.EqualTo(("-", "-", "-")));
            Assert.That(runtime.Current.SerialNumber, Is.EqualTo("123456"));
            Assert.That(runtime.Current.HardwareType, Is.EqualTo(known.HardwareType).And.Not.EqualTo("-"));
            Assert.That(runtime.Current.FirmwareVersion, Is.EqualTo(known.FirmwareVersion).And.Not.EqualTo("-"));
        }
    }

    private static Mock<IZ21> CreateZ21Mock()
    {
        var z21Mock = new Mock<IZ21>();
        z21Mock.SetupGet(z => z.TrafficMonitor).Returns((Z21Monitor?)null);
        return z21Mock;
    }

    private static MobaRuntimeService CreateRuntime(IZ21 z21) =>
        new(
            z21,
            new Mock<IWorkflowService>().Object,
            new ActionExecutionContext { Z21 = z21 },
            new AppSettings(),
            new Mock<ILogger<MobaRuntimeService>>().Object);
}
