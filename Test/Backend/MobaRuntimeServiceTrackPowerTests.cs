// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.Backend;

using Microsoft.Extensions.Logging;

using Moba.Backend;
using Moba.Backend.Interface;
using Moba.Backend.Protocol;
using Moba.Backend.Service;
using Moba.Common.Configuration;

using Moq;

/// <summary>
/// Verifies track power synchronization behavior between runtime commands and Z21 status events.
/// </summary>
[TestFixture]
internal sealed class MobaRuntimeServiceTrackPowerTests
{
    [Test]
    public async Task SetTrackPowerAsync_OnlySendsCommand_StateChangesOnSystemStateEvent()
    {
        var z21Mock = CreateZ21Mock();
        using var runtime = CreateRuntime(z21Mock.Object);

        Assert.That(runtime.Current.IsTrackPowerOn, Is.False);

        await runtime.SetTrackPowerAsync(true);

        z21Mock.Verify(z => z.SetTrackPowerOnAsync(It.IsAny<CancellationToken>()), Times.Once);
        Assert.That(runtime.Current.IsTrackPowerOn, Is.False, "State must wait for a Z21 confirmation event.");

        z21Mock.Raise(
            z => z.OnSystemStateChanged += null,
            new SystemState
            {
                // Bit 1 not set => track power ON
                CentralState = 0x00
            });

        Assert.That(runtime.Current.IsTrackPowerOn, Is.True);
    }

    [Test]
    public void ExternalTrackPowerStatus_FromXBusUpdatesRuntimeWithoutSendingCommands()
    {
        var z21Mock = CreateZ21Mock();
        using var runtime = CreateRuntime(z21Mock.Object);

        z21Mock.Raise(
            z => z.OnXBusStatusChanged += null,
            new XBusStatus(emergencyStop: false, trackOff: false, shortCircuit: false, programming: false));

        Assert.That(runtime.Current.IsTrackPowerOn, Is.True);

        z21Mock.Raise(
            z => z.OnXBusStatusChanged += null,
            new XBusStatus(emergencyStop: false, trackOff: true, shortCircuit: false, programming: false));

        Assert.That(runtime.Current.IsTrackPowerOn, Is.False);
        z21Mock.Verify(z => z.SetTrackPowerOnAsync(It.IsAny<CancellationToken>()), Times.Never);
        z21Mock.Verify(z => z.SetTrackPowerOffAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    private static Mock<IZ21> CreateZ21Mock()
    {
        var z21Mock = new Mock<IZ21>();
        z21Mock.SetupGet(z => z.TrafficMonitor).Returns((Z21Monitor?)null);
        z21Mock.SetupGet(z => z.IsConnected).Returns(false);
        z21Mock.Setup(z => z.SetTrackPowerOnAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        z21Mock.Setup(z => z.SetTrackPowerOffAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        z21Mock.Setup(z => z.ConnectAsync(It.IsAny<System.Net.IPAddress>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        z21Mock.Setup(z => z.DisconnectAsync()).Returns(Task.CompletedTask);
        return z21Mock;
    }

    private static MobaRuntimeService CreateRuntime(IZ21 z21)
    {
        var workflowServiceMock = new Mock<IWorkflowService>();
        var loggerMock = new Mock<ILogger<MobaRuntimeService>>();

        return new MobaRuntimeService(
            z21,
            workflowServiceMock.Object,
            new ActionExecutionContext { Z21 = z21 },
            new AppSettings
            {
                // Disable auto-connect during tests to keep behavior deterministic.
                Z21 = new Z21Settings { CurrentIpAddress = string.Empty }
            },
            loggerMock.Object);
    }
}
