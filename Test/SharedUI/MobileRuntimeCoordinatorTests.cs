// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Test.SharedUI;

using Moba.Backend.Interface;
using Moba.Domain;
using Moba.SharedUI.Interface;
using Moba.SharedUI.Service;

using Moq;

[TestFixture]
internal sealed class MobileRuntimeCoordinatorTests
{
    [Test]
    public async Task JourneyLifecycleCommands_UseLocalRuntimeWithoutRemoteSession()
    {
        var journeyId = Guid.NewGuid();
        using var cancellation = new CancellationTokenSource();
        var token = cancellation.Token;
        var runtime = new Mock<IMobaRuntime>(MockBehavior.Strict);
        runtime.Setup(value => value.StartJourneyAsync(journeyId, token)).Returns(Task.CompletedTask);
        runtime.Setup(value => value.StopJourneyAsync(journeyId, token)).Returns(Task.CompletedTask);
        runtime.Setup(value => value.ResetInPortCountersAsync(token)).Returns(Task.CompletedTask);
        var remote = new Mock<IRuntimeHubRemoteClient>(MockBehavior.Strict);
        var coordinator = new MobileRuntimeCoordinator(runtime.Object, remote.Object);

        await coordinator.StartJourneyAsync(journeyId, token);
        await coordinator.StopJourneyAsync(journeyId, token);
        await coordinator.ResetInPortCountersAsync(token);

        runtime.VerifyAll();
        remote.VerifyNoOtherCalls();
    }

    [Test]
    public void JourneyLifecycleCommands_RejectRemoteSessionWithoutMutatingLocalRuntime()
    {
        var runtime = new Mock<IMobaRuntime>(MockBehavior.Strict);
        var remote = new Mock<IRuntimeHubRemoteClient>(MockBehavior.Strict);
        var coordinator = new MobileRuntimeCoordinator(runtime.Object, remote.Object);
        coordinator.SetMobaflowSessionActive(true);
        coordinator.SetLocalZ21Connected(true);
        var journeyId = Guid.NewGuid();

        Assert.Multiple(() =>
        {
            Assert.ThrowsAsync<NotSupportedException>(async () => await coordinator.StartJourneyAsync(journeyId));
            Assert.ThrowsAsync<NotSupportedException>(async () => await coordinator.StopJourneyAsync(journeyId));
            Assert.ThrowsAsync<NotSupportedException>(async () => await coordinator.ResetInPortCountersAsync());
        });

        runtime.VerifyNoOtherCalls();
        remote.VerifyNoOtherCalls();
    }

    [Test]
    public void CancelledJourneyLifecycleCommands_DoNotReachRuntime()
    {
        var runtime = new Mock<IMobaRuntime>(MockBehavior.Strict);
        var remote = new Mock<IRuntimeHubRemoteClient>(MockBehavior.Strict);
        var coordinator = new MobileRuntimeCoordinator(runtime.Object, remote.Object);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var journeyId = Guid.NewGuid();

        Assert.Multiple(() =>
        {
            Assert.ThrowsAsync<OperationCanceledException>(async () => await coordinator.StartJourneyAsync(journeyId, cancellation.Token));
            Assert.ThrowsAsync<OperationCanceledException>(async () => await coordinator.StopJourneyAsync(journeyId, cancellation.Token));
            Assert.ThrowsAsync<OperationCanceledException>(async () => await coordinator.ResetInPortCountersAsync(cancellation.Token));
        });

        runtime.VerifyNoOtherCalls();
        remote.VerifyNoOtherCalls();
    }

    [Test]
    public void PreferRemoteRuntime_IsTrue_WhenMobaflowSessionActive()
    {
        var mobaRuntime = new Mock<IMobaRuntime>().Object;
        var remoteClient = new Mock<IRuntimeHubRemoteClient>();
        var coordinator = new MobileRuntimeCoordinator(mobaRuntime, remoteClient.Object);

        coordinator.SetMobaflowSessionActive(true);
        coordinator.SetLocalZ21Connected(false);

        Assert.Multiple(() =>
        {
            Assert.That(coordinator.PreferRemoteRuntime, Is.True);
            Assert.That(coordinator.CanExecuteCommands, Is.True);
        });
    }

    [Test]
    public async Task RoutesToRemoteGateway_WhenMobaflowSessionActive()
    {
        var mobaRuntime = new Mock<IMobaRuntime>().Object;
        var remoteClient = new Mock<IRuntimeHubRemoteClient>();
        var coordinator = new MobileRuntimeCoordinator(mobaRuntime, remoteClient.Object);
        var signalId = Guid.NewGuid();

        coordinator.SetMobaflowSessionActive(true);
        coordinator.SetLocalZ21Connected(true);

        await coordinator.SetSignalAspectAsync(signalId, SignalAspect.Hp0);

        remoteClient.Verify(
            client => client.SetSignalAspectAsync(signalId, SignalAspect.Hp0, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task RoutesLocomotiveCommandsToLocalZ21_WhenMobaflowSessionAndZ21Connected()
    {
        var mobaRuntime = new Mock<IMobaRuntime>();
        mobaRuntime
            .Setup(runtime => runtime.SetLocomotiveFunctionAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        mobaRuntime
            .Setup(runtime => runtime.SetLocomotiveDriveAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var remoteClient = new Mock<IRuntimeHubRemoteClient>();
        var coordinator = new MobileRuntimeCoordinator(mobaRuntime.Object, remoteClient.Object);

        coordinator.SetMobaflowSessionActive(true);
        coordinator.SetLocalZ21Connected(true);

        await coordinator.SetLocomotiveFunctionAsync(3, 14, true);
        await coordinator.SetLocomotiveDriveAsync(3, 20, true);

        mobaRuntime.Verify(
            runtime => runtime.SetLocomotiveFunctionAsync(3, 14, true, It.IsAny<CancellationToken>()),
            Times.Once);
        mobaRuntime.Verify(
            runtime => runtime.SetLocomotiveDriveAsync(3, 20, true, It.IsAny<CancellationToken>()),
            Times.Once);
        remoteClient.Verify(
            client => client.SetLocomotiveFunctionAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()),
            Times.Never);
        remoteClient.Verify(
            client => client.SetLocomotiveDriveAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task RoutesToLocalGateway_WhenMobaflowInactiveAndZ21Connected()
    {
        var mobaRuntime = new Mock<IMobaRuntime>();
        var remoteClient = new Mock<IRuntimeHubRemoteClient>();
        var coordinator = new MobileRuntimeCoordinator(mobaRuntime.Object, remoteClient.Object);

        coordinator.SetMobaflowSessionActive(false);
        coordinator.SetLocalZ21Connected(true);

        mobaRuntime
            .Setup(runtime => runtime.SetSignalAspectAsync(It.IsAny<Guid>(), It.IsAny<SignalAspect>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await coordinator.SetSignalAspectAsync(Guid.NewGuid(), SignalAspect.Ks1);

        mobaRuntime.Verify(
            runtime => runtime.SetSignalAspectAsync(It.IsAny<Guid>(), SignalAspect.Ks1, It.IsAny<CancellationToken>()),
            Times.Once);
        remoteClient.Verify(
            client => client.SetSignalAspectAsync(It.IsAny<Guid>(), It.IsAny<SignalAspect>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
