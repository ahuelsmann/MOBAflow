// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Test.SharedUI;

using global::Moba.Backend.Interface;
using global::Moba.Backend.Service.Recording;
using global::Moba.Common.Recording;
using global::Moba.SharedUI.Interface;
using global::Moba.SharedUI.Service;

using Moq;

/// <summary>
/// Routing of the gateway commands added by RF-22: fail-safe acknowledgement, all functions off
/// and locomotive info request.
/// </summary>
[TestFixture]
internal sealed class RuntimeCommandGatewayRoutingTests
{
    [Test]
    public async Task LocalGateway_ForwardsAddedCommandsToRuntime()
    {
        var runtime = new Mock<IMobaRuntime>();
        var gateway = new LocalRuntimeCommandGateway(runtime.Object);

        await gateway.AcknowledgeFailSafeAsync().ConfigureAwait(false);
        await gateway.SetAllLocomotiveFunctionsOffAsync(3).ConfigureAwait(false);
        await gateway.RequestLocomotiveInfoAsync(3).ConfigureAwait(false);

        runtime.Verify(r => r.AcknowledgeFailSafeAsync(It.IsAny<CancellationToken>()), Times.Once);
        runtime.Verify(r => r.SetAllLocomotiveFunctionsOffAsync(3, It.IsAny<CancellationToken>()), Times.Once);
        runtime.Verify(r => r.RequestLocomotiveInfoAsync(3, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task MobileCoordinator_WithoutMobaflowSession_SendsAllOffToLocalRuntime()
    {
        var runtime = new Mock<IMobaRuntime>();
        var remote = new Mock<IRuntimeHubRemoteClient>();
        var coordinator = new MobileRuntimeCoordinator(runtime.Object, remote.Object);
        coordinator.SetLocalZ21Connected(true);

        await coordinator.SetAllLocomotiveFunctionsOffAsync(3).ConfigureAwait(false);
        await coordinator.RequestLocomotiveInfoAsync(3).ConfigureAwait(false);
        await coordinator.AcknowledgeFailSafeAsync().ConfigureAwait(false);

        runtime.Verify(r => r.SetAllLocomotiveFunctionsOffAsync(3, It.IsAny<CancellationToken>()), Times.Once);
        runtime.Verify(r => r.RequestLocomotiveInfoAsync(3, It.IsAny<CancellationToken>()), Times.Once);
        runtime.Verify(r => r.AcknowledgeFailSafeAsync(It.IsAny<CancellationToken>()), Times.Once);
        remote.VerifyNoOtherCalls();
    }

    [Test]
    public async Task MobileCoordinator_WithMobaflowSession_SwitchesEveryFunctionOffSingly()
    {
        var runtime = new Mock<IMobaRuntime>();
        var remote = new Mock<IRuntimeHubRemoteClient>();
        var coordinator = new MobileRuntimeCoordinator(runtime.Object, remote.Object);
        coordinator.SetMobaflowSessionActive(true);

        await coordinator.SetAllLocomotiveFunctionsOffAsync(3).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            for (var functionIndex = 0; functionIndex <= 31; functionIndex++)
            {
                var index = functionIndex;
                remote.Verify(r => r.SetLocomotiveFunctionAsync(3, index, false, It.IsAny<CancellationToken>()), Times.Once);
            }

            runtime.Verify(r => r.SetAllLocomotiveFunctionsOffAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        }
    }

    [Test]
    public async Task RecordingGateway_ForwardsAddedCommandsWithoutRecording()
    {
        var session = new RecordingSessionService(TimeProvider.System);
        await using var sessionLifetime = session.ConfigureAwait(false);
        session.Start(new RecordingSessionStartRequest("Commands", "1.0"));
        var inner = new Mock<IRuntimeCommandGateway>();
        var gateway = new RecordingRuntimeCommandGateway(inner.Object, session);

        await gateway.AcknowledgeFailSafeAsync().ConfigureAwait(false);
        await gateway.SetAllLocomotiveFunctionsOffAsync(3).ConfigureAwait(false);
        await gateway.RequestLocomotiveInfoAsync(3).ConfigureAwait(false);
        var artifact = (await session.StopAsync().ConfigureAwait(false)).Artifact!;

        inner.Verify(g => g.AcknowledgeFailSafeAsync(It.IsAny<CancellationToken>()), Times.Once);
        inner.Verify(g => g.SetAllLocomotiveFunctionsOffAsync(3, It.IsAny<CancellationToken>()), Times.Once);
        inner.Verify(g => g.RequestLocomotiveInfoAsync(3, It.IsAny<CancellationToken>()), Times.Once);
        Assert.That(artifact.Entries.Where(entry => entry.Source == "runtime-command-gateway"), Is.Empty);
    }
}
