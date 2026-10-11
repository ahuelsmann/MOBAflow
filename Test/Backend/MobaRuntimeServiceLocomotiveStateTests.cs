// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.Backend;

using Microsoft.Extensions.Logging;

using Moba.Backend.Interface;
using Moba.Backend.Model;
using Moba.Backend.Service;
using Moba.Common.Configuration;
using Moba.Domain;

using Moq;

/// <summary>
/// Verifies locomotive drive/function state synchronization between runtime commands and Z21 loco info.
/// </summary>
[TestFixture]
internal sealed class MobaRuntimeServiceLocomotiveStateTests
{
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

    private static Mock<IZ21> CreateZ21Mock()
    {
        var z21Mock = new Mock<IZ21>();
        z21Mock.SetupGet(z => z.TrafficMonitor).Returns((Z21Monitor?)null);
        z21Mock.SetupGet(z => z.IsConnected).Returns(false);
        z21Mock.Setup(z => z.SetLocoDriveAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
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
                Z21 = new Z21Settings { CurrentIpAddress = string.Empty }
            },
            loggerMock.Object);
    }
}
