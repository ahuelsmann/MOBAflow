// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Test.MOBApi;

using Microsoft.AspNetCore.SignalR;

using Moba.Common.Runtime;
using Moba.MOBApi.Hubs;
using Moba.MOBApi.Security;
using Moba.MOBApi.Service;

using Moq;

using System.Globalization;

/// <summary>
/// Verifies that remote commands are validated once and queued with a bound, on REST and SignalR alike.
/// </summary>
[TestFixture]
internal sealed class RuntimeCommandAdmissionTests
{
    [Test]
    public void Queue_RejectsCommands_WhenCapacityIsReached()
    {
        var queue = new RuntimeCommandQueue(capacity: 2);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(queue.TryEnqueue(ValidDrive(1)), Is.True);
            Assert.That(queue.TryEnqueue(ValidDrive(2)), Is.True);
            Assert.That(queue.TryEnqueue(ValidDrive(3)), Is.False);
        }
    }

    [Test]
    public void Queue_KeepsFirstInFirstOutOrder()
    {
        var queue = new RuntimeCommandQueue(capacity: 4);
        queue.TryEnqueue(ValidDrive(1));
        queue.TryEnqueue(ValidDrive(2));

        queue.TryDequeue(out var first);
        queue.TryDequeue(out var second);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(first?.LocomotiveAddress, Is.EqualTo(1));
            Assert.That(second?.LocomotiveAddress, Is.EqualTo(2));
        }
    }

    [Test]
    public void Enqueue_InvalidCommand_IsRejectedAndNotQueued()
    {
        var queue = new RuntimeCommandQueue(capacity: 4);
        var admission = new RuntimeCommandAdmission(queue);

        var result = admission.Enqueue(ValidDrive(0));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Status, Is.EqualTo(RuntimeCommandAdmissionStatus.Invalid));
            Assert.That(result.Error, Is.Not.Empty);
            Assert.That(queue.TryDequeue(out _), Is.False);
        }
    }

    [Test]
    public void Enqueue_WhenQueueIsFull_ReturnsQueueFull()
    {
        var admission = new RuntimeCommandAdmission(new RuntimeCommandQueue(capacity: 1));
        admission.Enqueue(ValidDrive(1));

        var result = admission.Enqueue(ValidDrive(2));

        Assert.That(result.Status, Is.EqualTo(RuntimeCommandAdmissionStatus.QueueFull));
    }

    [TestCase(HubCommand.Drive, RuntimeHubMethods.ExecuteSetLocomotiveDrive)]
    [TestCase(HubCommand.Function, RuntimeHubMethods.ExecuteSetLocomotiveFunction)]
    [TestCase(HubCommand.SignalAspect, RuntimeHubMethods.ExecuteSetSignalAspect)]
    public async Task HubCommand_ValidWithHost_IsForwardedAndNotQueued(HubCommand command, string hostMethod)
    {
        var fixture = new HubFixture(hostConnectionId: "host-1");

        await fixture.InvokeValidAsync(command).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            fixture.HostProxy.Verify(
                proxy => proxy.SendCoreAsync(hostMethod, It.IsAny<object?[]>(), It.IsAny<CancellationToken>()),
                Times.Once);
            Assert.That(fixture.Queue.TryDequeue(out _), Is.False);
        }
    }

    [TestCase(HubCommand.Drive, RuntimeCommandType.SetLocomotiveDrive)]
    [TestCase(HubCommand.Function, RuntimeCommandType.SetLocomotiveFunction)]
    [TestCase(HubCommand.SignalAspect, RuntimeCommandType.SetSignalAspect)]
    public async Task HubCommand_ValidWithoutHost_IsQueued(HubCommand command, RuntimeCommandType expectedType)
    {
        var fixture = new HubFixture(hostConnectionId: null);

        await fixture.InvokeValidAsync(command).ConfigureAwait(false);

        Assert.That(fixture.Queue.TryDequeue(out var queued) ? queued?.Type : null, Is.EqualTo(expectedType));
    }

    [TestCase(HubCommand.Drive, "host-1")]
    [TestCase(HubCommand.Function, "host-1")]
    [TestCase(HubCommand.SignalAspect, "host-1")]
    [TestCase(HubCommand.Drive, null)]
    [TestCase(HubCommand.Function, null)]
    [TestCase(HubCommand.SignalAspect, null)]
    public void HubCommand_Invalid_IsRejectedAndNeitherForwardedNorQueued(HubCommand command, string? hostConnectionId)
    {
        var fixture = new HubFixture(hostConnectionId);

        Assert.ThrowsAsync<HubException>(() => fixture.InvokeInvalidAsync(command));

        using (Assert.EnterMultipleScope())
        {
            fixture.HostProxy.Verify(
                proxy => proxy.SendCoreAsync(It.IsAny<string>(), It.IsAny<object?[]>(), It.IsAny<CancellationToken>()),
                Times.Never);
            Assert.That(fixture.Queue.TryDequeue(out _), Is.False);
        }
    }

    [TestCase(HubCommand.Drive)]
    [TestCase(HubCommand.Function)]
    [TestCase(HubCommand.SignalAspect)]
    public async Task HubCommand_WhenQueueIsFull_IsRejectedAndNotQueued(HubCommand command)
    {
        var fixture = new HubFixture(hostConnectionId: null, queueCapacity: 1);
        await fixture.InvokeValidAsync(command).ConfigureAwait(false);

        var exception = Assert.ThrowsAsync<HubException>(() => fixture.InvokeValidAsync(command));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(exception?.Message, Is.EqualTo("Command queue is full."));
            Assert.That(fixture.Queue.TryDequeue(out _), Is.True);
            Assert.That(fixture.Queue.TryDequeue(out _), Is.False);
        }
    }

    [Test]
    public async Task HubSignalAspect_WithHost_ForwardsTheValidatedAspectName()
    {
        var fixture = new HubFixture(hostConnectionId: "host-1");
        var signalId = Guid.NewGuid();

        await fixture.Hub.SetSignalAspect(signalId.ToString("N"), ((int)SignalAspect.Zs1).ToString(CultureInfo.InvariantCulture))
            .ConfigureAwait(false);

        fixture.HostProxy.Verify(
            proxy => proxy.SendCoreAsync(
                RuntimeHubMethods.ExecuteSetSignalAspect,
                It.Is<object?[]>(args => (string?)args[0] == signalId.ToString() && (string?)args[1] == nameof(SignalAspect.Zs1)),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private static RuntimeCommandEnvelope ValidDrive(int address) => new()
    {
        Type = RuntimeCommandType.SetLocomotiveDrive,
        LocomotiveAddress = address,
        Speed = 10,
        Forward = true
    };

    internal enum HubCommand
    {
        Drive,
        Function,
        SignalAspect
    }

    private sealed class HubFixture
    {
        public HubFixture(string? hostConnectionId, int queueCapacity = 8)
        {
            Queue = new RuntimeCommandQueue(queueCapacity);
            var hostRegistry = new Mock<IRuntimeHostRegistry>();
            hostRegistry.SetupGet(registry => registry.HostConnectionId).Returns(hostConnectionId);
            HostProxy
                .Setup(proxy => proxy.SendCoreAsync(It.IsAny<string>(), It.IsAny<object?[]>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            var clients = new Mock<IHubCallerClients>();
            clients.Setup(candidate => candidate.Client(It.IsAny<string>())).Returns(HostProxy.Object);

            Hub = new RuntimeHub(
                new Mock<IRuntimeSnapshotCache>().Object,
                new Mock<ISolutionCache>().Object,
                hostRegistry.Object,
                new Mock<IRuntimeBroadcastMetrics>().Object,
                new RuntimeCommandAdmission(Queue),
                new Mock<IControlPlaneHubConnectionRegistry>().Object)
            {
                Clients = clients.Object
            };
        }

        public RuntimeHub Hub { get; }

        public Task InvokeValidAsync(HubCommand command) => command switch
        {
            HubCommand.Drive => Hub.SetLocomotiveDrive(3, 40, forward: true),
            HubCommand.Function => Hub.SetLocomotiveFunction(3, 5, isOn: true),
            _ => Hub.SetSignalAspect(Guid.NewGuid().ToString(), nameof(SignalAspect.Hp0))
        };

        public Task InvokeInvalidAsync(HubCommand command) => command switch
        {
            HubCommand.Drive => Hub.SetLocomotiveDrive(3, 500, forward: true),
            HubCommand.Function => Hub.SetLocomotiveFunction(3, 32, isOn: true),
            _ => Hub.SetSignalAspect(Guid.NewGuid().ToString(), "999")
        };

        public RuntimeCommandQueue Queue { get; }

        public Mock<ISingleClientProxy> HostProxy { get; } = new();
    }
}
