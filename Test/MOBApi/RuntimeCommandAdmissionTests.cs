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

    [TestCase(HubCommand.Drive)]
    [TestCase(HubCommand.Function)]
    [TestCase(HubCommand.SignalAspect)]
    public async Task HubCommand_ValidWithoutHost_IsQueuedWithItsPayload(HubCommand command)
    {
        var fixture = new HubFixture(hostConnectionId: null);

        await fixture.InvokeValidAsync(command).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(fixture.Queue.TryDequeue(out var queued), Is.True);
            Assert.That(Payload(queued), Is.EqualTo(HubFixture.ExpectedValid(command)));
        }
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

        var exception = Assert.ThrowsAsync<HubException>(() => fixture.InvokeValidAsync(command, variant: 1));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(exception?.Message, Is.EqualTo("Command queue is full."));
            Assert.That(fixture.Queue.TryDequeue(out var accepted), Is.True);
            Assert.That(Payload(accepted), Is.EqualTo(HubFixture.ExpectedValid(command)));
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

    /// <summary>Clears the generated command id and timestamp so only the command payload is compared.</summary>
    private static RuntimeCommandEnvelope? Payload(RuntimeCommandEnvelope? command) =>
        command is null ? null : command with { CommandId = Guid.Empty, CreatedAt = default };

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

        /// <summary>
        /// Expected queued payload for a valid command; each <paramref name="variant"/> differs in every field.
        /// </summary>
        public static RuntimeCommandEnvelope ExpectedValid(HubCommand command, int variant = 0) => command switch
        {
            HubCommand.Drive => new RuntimeCommandEnvelope
            {
                Type = RuntimeCommandType.SetLocomotiveDrive,
                LocomotiveAddress = 3 + variant,
                Speed = 40 + variant,
                Forward = variant == 0
            },
            HubCommand.Function => new RuntimeCommandEnvelope
            {
                Type = RuntimeCommandType.SetLocomotiveFunction,
                LocomotiveAddress = 3 + variant,
                FunctionIndex = 5 + variant,
                FunctionIsOn = variant == 0
            },
            _ => new RuntimeCommandEnvelope
            {
                Type = RuntimeCommandType.SetSignalAspect,
                SignalId = new Guid(variant + 1, 0, 0, new byte[8]),
                SignalAspect = variant == 0 ? SignalAspect.Hp0 : SignalAspect.Ks1
            }
        } with { CommandId = Guid.Empty, CreatedAt = default };

        public Task InvokeValidAsync(HubCommand command, int variant = 0)
        {
            var expected = ExpectedValid(command, variant);
            return command switch
            {
                HubCommand.Drive => Hub.SetLocomotiveDrive(
                    expected.LocomotiveAddress!.Value,
                    expected.Speed!.Value,
                    expected.Forward!.Value),
                HubCommand.Function => Hub.SetLocomotiveFunction(
                    expected.LocomotiveAddress!.Value,
                    expected.FunctionIndex!.Value,
                    expected.FunctionIsOn!.Value),
                _ => Hub.SetSignalAspect(expected.SignalId!.Value.ToString(), expected.SignalAspect!.Value.ToString())
            };
        }

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
