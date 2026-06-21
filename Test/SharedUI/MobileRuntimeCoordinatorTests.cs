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
