// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.Backend;

using Moba.Test.Helpers;

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
internal sealed partial class MobaRuntimeServiceTrackPowerTests : IDisposable
{
    private readonly TestRuntimes _runtimes = new();

    public void Dispose() => _runtimes.Dispose();

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

    [Test]
    public async Task Constructor_DoesNotStartZ21AutoConnect()
    {
        var z21Mock = CreateZ21Mock();

        using var runtime = CreateRuntime(z21Mock.Object, new AppSettings());
        await runtime.ActivateProjectAsync(LayoutWithZ21()).ConfigureAwait(false);

        z21Mock.Verify(
            z => z.ConnectAsync(It.IsAny<System.Net.IPAddress>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task StartAsync_QueuesZ21AutoConnectWithoutWaitingAndOnlyOnce()
    {
        var z21Mock = CreateZ21Mock();
        var connectAttempted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var connectRelease = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        z21Mock.Setup(z => z.ConnectAsync(It.IsAny<System.Net.IPAddress>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Callback(() => connectAttempted.SetResult())
            .Returns(connectRelease.Task);

        using var runtime = CreateRuntime(
            z21Mock.Object,
            new AppSettings
            {
                Z21 = new Z21Settings { AutoConnectRetryIntervalSeconds = 60 }
            });
        await runtime.ActivateProjectAsync(LayoutWithZ21()).ConfigureAwait(false);

        var startTask = runtime.StartAsync();

        var completedTask = await Task.WhenAny(startTask, Task.Delay(200));
        Assert.That(completedTask, Is.SameAs(startTask), "Runtime startup must not wait for the first Z21 connection attempt.");
        await startTask;
        await connectAttempted.Task;

        await runtime.StartAsync();

        z21Mock.Verify(
            z => z.ConnectAsync(It.IsAny<System.Net.IPAddress>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Once);

        connectRelease.SetResult();
    }

    private static Project LayoutWithZ21() => new() { Name = "Layout", Z21 = { IpAddress = "192.168.0.111" } };

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

    // Without an active project the runtime has no Z21 to connect to.
    private MobaRuntimeService CreateRuntime(IZ21 z21, AppSettings? settings = null) =>
        _runtimes.Create(z21, new TestRuntimeOptions { Settings = settings });
}