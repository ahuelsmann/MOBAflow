// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.Backend;

using Moba.Test.Helpers;

using Moba.Backend.Interface;
using Moba.Backend.Model;
using Moba.Backend.Service;
using Moba.Domain;

using Moq;

/// <summary>
/// Verifies locomotive drive/function state synchronization between runtime commands and Z21 loco info.
/// </summary>
[TestFixture]
internal sealed partial class MobaRuntimeServiceLocomotiveStateTests : IDisposable
{
    private readonly TestRuntimes _runtimes = new();

    public void Dispose() => _runtimes.Dispose();

    [Test]
    public async Task LocoInfoAfterDriveCommand_PreservesExistingFunctionBitsDuringGracePeriod()
    {
        var z21Mock = CreateZ21Mock();
        using var runtime = CreateRuntime(z21Mock.Object);

        await runtime.SetLocomotiveDriveAsync(address: 3, speed: 0, forward: false);

        z21Mock.Raise(
            z => z.OnLocoInfoChanged += null,
            new LocoInfo
            {
                Address = 3,
                Speed = 0,
                IsForward = false,
                Functions = 0b1111
            });

        Assert.That(runtime.Current.LocomotiveStates[3].Functions, Is.Zero,
            "Z21 loco info must not overwrite function bits immediately after a local drive command");
    }

    [TestCase(0u)]
    [TestCase(10000u)]
    [TestCase(uint.MaxValue)]
    public async Task StopAllLocomotives_SkipsInvalidAddressesAndStopsValidProjectLocomotives(uint invalidAddress)
    {
        var z21 = CreateZ21Mock();
        using var runtime = CreateRuntime(z21.Object);
        await runtime.ActivateProjectAsync(new Project
        {
            Locomotives =
            [
                new Locomotive { DigitalAddress = invalidAddress },
                new Locomotive { DigitalAddress = 3 },
                new Locomotive { DigitalAddress = 9999 }
            ]
        }).ConfigureAwait(false);
        ConfirmConnection(z21);

        await runtime.StopAllLocomotivesAsync().ConfigureAwait(false);

        z21.Verify(value => value.SetLocoDriveAsync(3, 0, true, It.IsAny<CancellationToken>()), Times.Once);
        z21.Verify(value => value.SetLocoDriveAsync(9999, 0, true, It.IsAny<CancellationToken>()), Times.Once);
        z21.Verify(value => value.SetLocoDriveAsync(It.Is<int>(address => address < 1 || address > 9999),
            It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task StopAllLocomotives_WhenOneSendFails_StillStopsTheRemainingLocomotives()
    {
        var z21 = CreateZ21Mock();
        using var runtime = CreateRuntime(z21.Object);
        await runtime.ActivateProjectAsync(new Project
        {
            Locomotives = [new Locomotive { DigitalAddress = 3 }, new Locomotive { DigitalAddress = 5 }]
        }).ConfigureAwait(false);
        ConfirmConnection(z21);
        z21.Setup(value => value.SetLocoDriveAsync(3, 0, true, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new IOException("Send failed"));

        await Assert.ThrowsAsync<AggregateException>(() => runtime.StopAllLocomotivesAsync()).ConfigureAwait(false);

        z21.Verify(value => value.SetLocoDriveAsync(5, 0, true, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task Disconnect_StopsObservedLocomotivesBeforeLoggingOffAndPreservesDirection()
    {
        var z21 = CreateZ21Mock();
        using var runtime = CreateRuntime(z21.Object);
        ConfirmConnection(z21);
        await runtime.SetLocomotiveDriveAsync(3, 40, false).ConfigureAwait(false);
        var commands = new List<string>();
        z21.Setup(value => value.SetLocoDriveAsync(3, 0, false, It.IsAny<CancellationToken>()))
            .Callback(() => commands.Add("stop")).Returns(Task.CompletedTask);
        z21.Setup(value => value.DisconnectAsync()).Callback(() => commands.Add("disconnect"))
            .Returns(Task.CompletedTask);

        await runtime.DisconnectAsync().ConfigureAwait(false);

        z21.Verify(value => value.SetLocoDriveAsync(3, 0, false, It.IsAny<CancellationToken>()), Times.Once);
        z21.Verify(value => value.DisconnectAsync(), Times.Once);
        string[] expectedCommands = ["stop", "disconnect"];
        using (Assert.EnterMultipleScope())
        {
            Assert.That(commands, Is.EqualTo(expectedCommands));
            Assert.That(runtime.Current.IsConnected, Is.False);
        }
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task ProjectHandoff_LogsOffWithoutStoppingLocomotives(bool connected)
    {
        var z21 = CreateZ21Mock();
        using var runtime = CreateRuntime(z21.Object);
        await runtime.ActivateProjectAsync(new Project { Locomotives = [new Locomotive { DigitalAddress = 3 }] }).ConfigureAwait(false);
        if (connected) ConfirmConnection(z21);
        await runtime.SetLocomotiveDriveAsync(3, 40, false).ConfigureAwait(false);
        z21.Invocations.Clear();

        await runtime.DisconnectForProjectSwitchAsync().ConfigureAwait(false);

        z21.Verify(value => value.DisconnectAsync(), Times.Once);
        z21.Verify(value => value.SetLocoDriveAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<bool>(),
            It.IsAny<CancellationToken>()), Times.Never);
        Assert.That(runtime.Current.IsConnected, Is.False);
    }

    [Test]
    public async Task ProjectHandoff_PropagatesLogoffFailure()
    {
        var z21 = CreateZ21Mock();
        using var runtime = CreateRuntime(z21.Object);
        z21.Setup(value => value.DisconnectAsync()).ThrowsAsync(new IOException("Logoff failed"));

        await Assert.ThrowsAsync<IOException>(() => runtime.DisconnectForProjectSwitchAsync()).ConfigureAwait(false);
    }

    [Test]
    public async Task NewProject_DropsOldLocomotiveAddressesAndCommandGracePeriods()
    {
        var z21 = CreateZ21Mock();
        using var runtime = CreateRuntime(z21.Object);
        await runtime.ActivateProjectAsync(new Project()).ConfigureAwait(false);
        ConfirmConnection(z21);
        await runtime.SetLocomotiveDriveAsync(3, 40, false).ConfigureAwait(false);
        await runtime.SetLocomotiveDriveAsync(17, 55, true).ConfigureAwait(false);
        await runtime.SetLocomotiveFunctionAsync(3, 1, true).ConfigureAwait(false);
        await runtime.DisconnectForProjectSwitchAsync().ConfigureAwait(false);
        await runtime.ActivateProjectAsync(new Project { Locomotives = [new Locomotive { DigitalAddress = 3 }] }).ConfigureAwait(false);
        Assert.That(runtime.Current.LocomotiveStates, Is.Empty);
        ConfirmConnection(z21);
        ReportLocomotiveInfo(z21, new LocoInfo
        {
            Address = 3, Speed = 12, IsForward = true, Functions = 5
        });
        z21.Invocations.Clear();

        await runtime.DisconnectAsync().ConfigureAwait(false);

        z21.Verify(value => value.SetLocoDriveAsync(17, It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
        Assert.That(runtime.Current.LocomotiveStates[3].Functions, Is.EqualTo(5U));
    }

    [Test]
    public async Task Disconnect_WhenStoppingFails_KeepsTheConnectionForAnotherStopAttempt()
    {
        var z21 = CreateZ21Mock();
        using var runtime = CreateRuntime(z21.Object);
        ConfirmConnection(z21);
        await runtime.SetLocomotiveDriveAsync(3, 40, true).ConfigureAwait(false);
        z21.Setup(value => value.SetLocoDriveAsync(3, 0, true, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new IOException("Send failed"));

        await runtime.DisconnectAsync().ConfigureAwait(false);

        z21.Verify(value => value.DisconnectAsync(), Times.Never);
        Assert.That(runtime.Current.IsConnected, Is.True);
    }

    private static void ConfirmConnection(Mock<IZ21> z21) =>
        z21.Raise(value => value.OnConnectedChanged += null, true);

    // IZ21 publishes Action<LocoInfo>, so the event has no asynchronous handlers to await.
    private static void ReportLocomotiveInfo(Mock<IZ21> z21, LocoInfo info) =>
        z21.Raise(value => value.OnLocoInfoChanged += null, info);

    private static Mock<IZ21> CreateZ21Mock()
    {
        var z21Mock = new Mock<IZ21>();
        z21Mock.SetupGet(z => z.TrafficMonitor).Returns((Z21Monitor?)null);
        z21Mock.SetupGet(z => z.IsConnected).Returns(false);
        z21Mock.Setup(z => z.SetLocoDriveAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        return z21Mock;
    }

    private MobaRuntimeService CreateRuntime(IZ21 z21) => _runtimes.Create(z21);
}
