// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Test.Backend;

using global::Moba.Backend.Events;
using global::Moba.Backend.Interface;
using global::Moba.Backend.Service.Interlocking;
using global::Moba.Common.Events;
using global::Moba.Domain;

using Microsoft.Extensions.Logging.Abstractions;

using Moq;

[TestFixture]
internal sealed class InterlockingRuntimeServiceTests
{
    [Test]
    public async Task FeedbackProjection_Should_PreserveFifoOrder()
    {
        var fixture = CreateFixture();
        var observedOccupancies = new List<BlockOccupancy>();
        fixture.EventBus.Subscribe<InterlockingRuntimeSnapshotChangedEvent>(@event =>
        {
            if (@event.Code == "block.observed")
                observedOccupancies.Add(@event.Snapshot.Blocks[fixture.BlockId].Occupancy);
        });
        await fixture.Runtime.ActivateAsync(fixture.Definition);

        fixture.EventBus.Publish(new FeedbackStateChangedEvent(10, true, Guid.NewGuid()));
        fixture.EventBus.Publish(new FeedbackStateChangedEvent(11, true, Guid.NewGuid()));
        await WaitForStateAsync(
            fixture.Runtime,
            state => state.Blocks[fixture.BlockId].Occupancy == BlockOccupancy.Fault);

        Assert.That(observedOccupancies, Is.EqualTo(new[] { BlockOccupancy.Free, BlockOccupancy.Fault }));
    }

    [Test]
    public async Task DisconnectDuringCommand_Should_MakeInputsUnknownWithoutAutomaticEffects()
    {
        var fixture = CreateFixture();
        await fixture.Runtime.ActivateAsync(fixture.Definition);
        fixture.EventBus.Publish(new FeedbackStateChangedEvent(10, true, Guid.NewGuid()));
        fixture.EventBus.Publish(new TurnoutInfoChangedEvent(500, true, Guid.NewGuid()));
        await WaitForSynchronizationAsync(fixture.Runtime);
        await fixture.Runtime.SetTurnoutAsync(fixture.TurnoutId, TurnoutPosition.Straight, Guid.NewGuid());

        fixture.EventBus.Publish(new Z21ConnectionLostEvent());
        var disconnected = await WaitForStateAsync(
            fixture.Runtime,
            state => state.Turnouts[fixture.TurnoutId].Lifecycle == TurnoutLifecycle.Unknown);

        Assert.Multiple(() =>
        {
            Assert.That(disconnected.Turnouts[fixture.TurnoutId].Lifecycle, Is.EqualTo(TurnoutLifecycle.Unknown));
            Assert.That(disconnected.Blocks[fixture.BlockId].Occupancy, Is.EqualTo(BlockOccupancy.Unknown));
            Assert.That(fixture.Runtime.IsSynchronized, Is.False);
        });
    }

    [Test]
    public async Task Reconnect_Should_QueryTurnoutsAndRequireCompleteSnapshot()
    {
        var fixture = CreateFixture();
        await fixture.Runtime.ActivateAsync(fixture.Definition);

        fixture.EventBus.Publish(new Z21ConnectionEstablishedEvent());
        await WaitUntilAsync(() => fixture.Z21.Invocations.Any(invocation =>
            invocation.Method.Name == nameof(IZ21.GetTurnoutInfoAsync)));

        Assert.That(fixture.Runtime.IsSynchronized, Is.False);

        fixture.EventBus.Publish(new FeedbackStateChangedEvent(10, true, Guid.NewGuid()));
        fixture.EventBus.Publish(new TurnoutInfoChangedEvent(500, true, Guid.NewGuid()));
        await WaitForSynchronizationAsync(fixture.Runtime);

        Assert.Multiple(() =>
        {
            fixture.Z21.Verify(z21 => z21.GetStatusAsync(It.IsAny<CancellationToken>()), Times.Once);
            fixture.Z21.Verify(z21 => z21.GetTurnoutInfoAsync(100, It.IsAny<CancellationToken>()), Times.Once);
            fixture.Z21.Verify(z21 => z21.GetTurnoutInfoAsync(500, It.IsAny<CancellationToken>()), Times.Once);
            Assert.That(fixture.Runtime.IsSynchronized, Is.True);
        });
    }

    [Test]
    public async Task NotSwitchedObservation_Should_InvalidateConfirmedTurnoutState()
    {
        var fixture = CreateFixture();
        await fixture.Runtime.ActivateAsync(fixture.Definition);
        fixture.EventBus.Publish(new FeedbackStateChangedEvent(10, true, Guid.NewGuid()));
        fixture.EventBus.Publish(new TurnoutInfoChangedEvent(500, true, Guid.NewGuid()));
        await WaitForSynchronizationAsync(fixture.Runtime);

        fixture.EventBus.Publish(new TurnoutInfoChangedEvent(500, false, Guid.NewGuid(), IsSwitched: false));
        var invalidated = await WaitForStateAsync(
            fixture.Runtime,
            state => state.Turnouts[fixture.TurnoutId].Lifecycle == TurnoutLifecycle.Unknown);

        Assert.Multiple(() =>
        {
            Assert.That(invalidated.Turnouts[fixture.TurnoutId].ConfirmedPosition, Is.Null);
            Assert.That(fixture.Runtime.IsSynchronized, Is.False);
        });
    }

    [Test]
    public async Task SetTurnoutAsync_SynchronizedRuntime_DispatchesSemanticCommand()
    {
        var fixture = CreateFixture();
        await fixture.Runtime.ActivateAsync(fixture.Definition).ConfigureAwait(false);
        fixture.EventBus.Publish(new FeedbackStateChangedEvent(10, true, Guid.NewGuid()));
        fixture.EventBus.Publish(new TurnoutInfoChangedEvent(500, true, Guid.NewGuid()));
        await WaitForSynchronizationAsync(fixture.Runtime).ConfigureAwait(false);

        var result = await fixture.Runtime.SetTurnoutAsync(
            fixture.TurnoutId,
            TurnoutPosition.Straight,
            Guid.NewGuid()).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Status, Is.EqualTo(TurnoutCoordinatorStatus.Pending));
            Assert.That(result.State.Turnouts[fixture.TurnoutId].Lifecycle, Is.EqualTo(TurnoutLifecycle.Pending));
            fixture.Z21.Verify(z21 => z21.SetTurnoutAsync(
                100,
                0,
                true,
                false,
                It.IsAny<CancellationToken>()), Times.Once);
        }
    }

    [Test]
    public async Task SetTurnoutAsync_UnknownBlock_DoesNotRequireCompleteSnapshot()
    {
        var fixture = CreateFixture();
        await fixture.Runtime.ActivateAsync(fixture.Definition).ConfigureAwait(false);

        var result = await fixture.Runtime.SetTurnoutAsync(
            fixture.TurnoutId,
            TurnoutPosition.Straight,
            Guid.NewGuid()).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Status, Is.EqualTo(TurnoutCoordinatorStatus.Pending));
            Assert.That(result.State.Blocks[fixture.BlockId].Occupancy, Is.EqualTo(BlockOccupancy.Unknown));
            fixture.Z21.Verify(z21 => z21.SetTurnoutAsync(
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<bool>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()), Times.Once);
        }
    }

    [Test]
    public async Task DuplicateObservation_Should_NotAdvanceRevisionTwice()
    {
        var fixture = CreateFixture();
        await fixture.Runtime.ActivateAsync(fixture.Definition);
        var observation = new FeedbackStateChangedEvent(10, true, Guid.NewGuid());

        fixture.EventBus.Publish(observation);
        var first = await WaitForStateAsync(
            fixture.Runtime,
            state => state.Blocks[fixture.BlockId].Occupancy == BlockOccupancy.Free);
        fixture.EventBus.Publish(observation);
        await fixture.Runtime.WhenIdleAsync();

        Assert.That(fixture.Runtime.Current.Revision, Is.EqualTo(first.Revision));
    }

    [Test]
    public async Task DisposeAsync_Should_UnsubscribeFromOrderedObservations()
    {
        var fixture = CreateFixture();
        await fixture.Runtime.ActivateAsync(fixture.Definition);

        await fixture.Runtime.DisposeAsync();
        var revision = fixture.Runtime.Current.Revision;
        fixture.EventBus.Publish(new FeedbackStateChangedEvent(10, true, Guid.NewGuid()));

        Assert.That(fixture.Runtime.Current.Revision, Is.EqualTo(revision));
        Assert.Multiple(() =>
        {
            Assert.That(fixture.EventBus.GetSubscriberCount<FeedbackStateChangedEvent>(), Is.Zero);
            Assert.That(fixture.EventBus.GetSubscriberCount<TurnoutInfoChangedEvent>(), Is.Zero);
            Assert.That(fixture.EventBus.GetSubscriberCount<Z21ConnectionLostEvent>(), Is.Zero);
        });
    }

    [Test]
    public async Task OccupiedBlock_DoesNotPreventDirectTurnoutCommand()
    {
        var fixture = CreateFixture();
        var runtime = fixture.Runtime;
        await using var runtimeLifetime = runtime.ConfigureAwait(false);
        await runtime.ActivateAsync(fixture.Definition).ConfigureAwait(false);
        fixture.EventBus.Publish(new FeedbackStateChangedEvent(11, true, Guid.NewGuid()));
        await runtime.WhenIdleAsync().ConfigureAwait(false);

        var result = await runtime.SetTurnoutAsync(fixture.TurnoutId, TurnoutPosition.Straight, Guid.NewGuid()).ConfigureAwait(false);

        using var assertions = Assert.EnterMultipleScope();
        Assert.That(result.Status, Is.EqualTo(TurnoutCoordinatorStatus.Pending));
        Assert.That(result.State.Blocks[fixture.BlockId].Occupancy, Is.EqualTo(BlockOccupancy.Occupied));
    }

    [Test]
    public async Task DuplicateFeedback_DoesNotPolluteLaterOccupancyOrPreviousSnapshots()
    {
        var fixture = CreateFixture();
        var runtime = fixture.Runtime;
        await using var runtimeLifetime = runtime.ConfigureAwait(false);
        var secondBlock = new BlockDefinition
        {
            FeedbackInputs = fixture.Definition.Blocks.Single().FeedbackInputs
        };
        fixture.Definition.Blocks.Add(secondBlock);
        await runtime.ActivateAsync(fixture.Definition).ConfigureAwait(false);
        var first = new FeedbackStateChangedEvent(10, true, Guid.NewGuid());
        fixture.EventBus.Publish(first);
        await runtime.WhenIdleAsync().ConfigureAwait(false);
        var earlier = runtime.Current;
        Assert.That(earlier.Blocks.Values.Select(block => block.Occupancy), Is.All.EqualTo(BlockOccupancy.Free));

        fixture.EventBus.Publish(new FeedbackStateChangedEvent(10, false, Guid.NewGuid()));
        fixture.EventBus.Publish(first);
        fixture.EventBus.Publish(new FeedbackStateChangedEvent(11, true, Guid.NewGuid()));
        await runtime.WhenIdleAsync().ConfigureAwait(false);

        using var assertions = Assert.EnterMultipleScope();
        Assert.That(runtime.Current.Blocks.Values.Select(block => block.Occupancy), Is.All.EqualTo(BlockOccupancy.Occupied));
        Assert.That(earlier.Blocks.Values.Select(block => block.Occupancy), Is.All.EqualTo(BlockOccupancy.Free));
    }

    [Test]
    public async Task PendingDispatch_DoesNotBlockOrderedObservations()
    {
        var fixture = CreateFixture();
        var runtime = fixture.Runtime;
        await using var runtimeLifetime = runtime.ConfigureAwait(false);
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        fixture.Z21.Setup(z21 => z21.SetTurnoutAsync(100, 0, true, false, It.IsAny<CancellationToken>()))
            .Returns(async (int address, int output, bool activate, bool queue, CancellationToken token) =>
            {
                started.TrySetResult();
                await release.Task.WaitAsync(token).ConfigureAwait(false);
            });
        var snapshots = new List<InterlockingRuntimeSnapshotChangedEvent>();
        fixture.EventBus.Subscribe<InterlockingRuntimeSnapshotChangedEvent>(snapshots.Add);
        await runtime.ActivateAsync(fixture.Definition).ConfigureAwait(false);

        var command = runtime.SetTurnoutAsync(fixture.TurnoutId, TurnoutPosition.Straight, Guid.NewGuid());
        await started.Task.WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
        await runtime.WhenIdleAsync().ConfigureAwait(false);
        Assert.That(runtime.Current.Turnouts[fixture.TurnoutId].Lifecycle, Is.EqualTo(TurnoutLifecycle.Requested));
        fixture.EventBus.Publish(new FeedbackStateChangedEvent(10, true, Guid.NewGuid()));
        fixture.EventBus.Publish(new FeedbackStateChangedEvent(11, true, Guid.NewGuid()));
        await runtime.WhenIdleAsync().ConfigureAwait(false);
        release.TrySetResult();
        var result = await command.WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);

        using var assertions = Assert.EnterMultipleScope();
        Assert.That(result.Status, Is.EqualTo(TurnoutCoordinatorStatus.Pending));
        Assert.That(snapshots.Where(item => item.Code == "block.observed")
            .Select(item => item.Snapshot.Blocks[fixture.BlockId].Occupancy),
            Is.EqualTo(new[] { BlockOccupancy.Free, BlockOccupancy.Fault }));
        Assert.That(snapshots.Select(item => item.Snapshot.Revision), Is.Ordered.Ascending);
        Assert.That(snapshots.Select(item => item.Snapshot.Revision), Is.Unique);
    }

    [Test]
    public async Task ProjectActivation_DiscardsOldCommandCompletion()
    {
        var fixture = CreateFixture();
        var runtime = fixture.Runtime;
        await using var runtimeLifetime = runtime.ConfigureAwait(false);
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        fixture.Z21.Setup(z21 => z21.SetTurnoutAsync(100, 0, true, false, It.IsAny<CancellationToken>()))
            .Returns(async () =>
            {
                started.TrySetResult();
                await release.Task.ConfigureAwait(false);
            });
        await runtime.ActivateAsync(fixture.Definition).ConfigureAwait(false);
        var command = runtime.SetTurnoutAsync(fixture.TurnoutId, TurnoutPosition.Straight, Guid.NewGuid());
        await started.Task.WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
        await runtime.WhenIdleAsync().ConfigureAwait(false);
        await runtime.ActivateAsync(new InterlockingDefinition()).ConfigureAwait(false);
        var replacement = runtime.Current;
        release.TrySetResult();
        var result = await command.WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);

        using var assertions = Assert.EnterMultipleScope();
        Assert.That(result.Code, Is.EqualTo("turnout.project.changed"));
        Assert.That(runtime.Current, Is.SameAs(replacement));
        Assert.That(runtime.Current.Turnouts, Is.Empty);
    }

    [Test]
    public async Task CancellationAfterActivationCommit_StillCancelsPreviousProjectCommand()
    {
        var fixture = CreateFixture();
        var runtime = fixture.Runtime;
        await using var runtimeLifetime = runtime.ConfigureAwait(false);
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        fixture.Z21.Setup(z21 => z21.SetTurnoutAsync(100, 0, true, false, It.IsAny<CancellationToken>()))
            .Returns(async (int address, int output, bool activate, bool queue, CancellationToken token) =>
            {
                started.TrySetResult();
                await Task.Delay(Timeout.InfiniteTimeSpan, token).ConfigureAwait(false);
            });
        await runtime.ActivateAsync(fixture.Definition).ConfigureAwait(false);
        var command = runtime.SetTurnoutAsync(fixture.TurnoutId, TurnoutPosition.Straight, Guid.NewGuid());
        await started.Task.WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
        await runtime.WhenIdleAsync().ConfigureAwait(false);
        using var cancellation = new CancellationTokenSource();
        fixture.EventBus.Subscribe<InterlockingRuntimeSnapshotChangedEvent>(snapshot =>
        {
            if (snapshot.Code == "interlocking.activated")
                cancellation.Cancel();
        });

        await runtime.ActivateAsync(new InterlockingDefinition(), cancellation.Token).ConfigureAwait(false);
        var result = await command.WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);

        using var assertions = Assert.EnterMultipleScope();
        Assert.That(cancellation.IsCancellationRequested, Is.True);
        Assert.That(result.Code, Is.EqualTo("turnout.project.changed"));
        Assert.That(runtime.Current.Turnouts, Is.Empty);
    }

    [Test]
    public async Task DisposeDuringDispatch_CancelsBeforeWaitingAndUnsubscribes()
    {
        var fixture = CreateFixture();
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        fixture.Z21.Setup(z21 => z21.SetTurnoutAsync(100, 0, true, false, It.IsAny<CancellationToken>()))
            .Returns(async (int address, int output, bool activate, bool queue, CancellationToken token) =>
            {
                started.TrySetResult();
                await Task.Delay(Timeout.InfiniteTimeSpan, token).ConfigureAwait(false);
            });
        await fixture.Runtime.ActivateAsync(fixture.Definition).ConfigureAwait(false);
        var command = fixture.Runtime.SetTurnoutAsync(fixture.TurnoutId, TurnoutPosition.Straight, Guid.NewGuid());
        await started.Task.WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
        await fixture.Runtime.WhenIdleAsync().ConfigureAwait(false);

        await fixture.Runtime.DisposeAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
        var result = await command.WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);

        using var assertions = Assert.EnterMultipleScope();
        Assert.That(result.Code, Is.EqualTo("turnout.shutdown"));
        Assert.That(fixture.EventBus.GetSubscriberCount<FeedbackStateChangedEvent>(), Is.Zero);
    }
    private static RuntimeFixture CreateFixture()
    {
        var turnoutId = Guid.Parse("10000000-0000-0000-0000-000000000001");
        var blockId = Guid.Parse("10000000-0000-0000-0000-000000000002");
        var definition = new InterlockingDefinition
        {
            Turnouts =
            [
                new TurnoutDefinition
                {
                    Id = turnoutId,
                    Name = "W1",
                    DecoderAddress = 100,
                    Commands =
                    [
                        new TurnoutCommandMapping
                        {
                            Position = TurnoutPosition.Straight,
                            Commands = [new TurnoutAccessoryCommand { Output = 0 }]
                        }
                    ],
                    Confirmations =
                    [
                        new TurnoutConfirmationMapping
                        {
                            Position = TurnoutPosition.Straight,
                            Conditions =
                            [
                                new TurnoutFeedbackCondition
                                {
                                    FunctionAddress = 500,
                                    OutputPosition = true
                                }
                            ]
                        }
                    ]
                }
            ],
            Blocks =
            [
                new BlockDefinition
                {
                    Id = blockId,
                    Name = "B1",
                    FeedbackInputs =
                    [
                        new BlockFeedbackInput { InPort = 10, Role = BlockFeedbackRole.Clear },
                        new BlockFeedbackInput { InPort = 11, Role = BlockFeedbackRole.Occupied }
                    ]
                }
            ]
        };
        var eventBus = new EventBus(NullLogger<EventBus>.Instance);
        var z21 = new Mock<IZ21>();
        z21.SetupGet(item => item.IsConnected).Returns(true);
        z21.Setup(item => item.GetStatusAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        z21.Setup(item => item.GetTurnoutInfoAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        z21.Setup(item => item.SetTurnoutAsync(
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<bool>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var runtime = new InterlockingRuntimeService(
            z21.Object,
            eventBus,
            TimeProvider.System,
            NullLogger<InterlockingRuntimeService>.Instance);
        return new RuntimeFixture(runtime, eventBus, z21, definition, turnoutId, blockId);
    }

    private static async Task WaitForSynchronizationAsync(IInterlockingRuntime runtime) =>
        await WaitUntilAsync(() => runtime.IsSynchronized);

    private static async Task<InterlockingRuntimeState> WaitForStateAsync(
        IInterlockingRuntime runtime,
        Func<InterlockingRuntimeState, bool> predicate)
    {
        await WaitUntilAsync(() => predicate(runtime.Current));
        return runtime.Current;
    }

    private static async Task WaitUntilAsync(Func<bool> predicate)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        while (!predicate())
            await Task.Delay(10, timeout.Token);
    }

    private sealed record RuntimeFixture(
        InterlockingRuntimeService Runtime,
        EventBus EventBus,
        Mock<IZ21> Z21,
        InterlockingDefinition Definition,
        Guid TurnoutId,
        Guid BlockId);
}
