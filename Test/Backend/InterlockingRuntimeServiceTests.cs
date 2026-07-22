// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Test.Backend;

using global::Moba.Backend.Events;
using global::Moba.Backend.Interface;
using global::Moba.Backend.Service.Interlocking;
using global::Moba.Common.Configuration;
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
    public async Task DisconnectDuringSetting_Should_FailRouteAndMakeInputsUnknown()
    {
        var fixture = CreateFixture();
        await fixture.Runtime.ActivateAsync(fixture.Definition);
        fixture.EventBus.Publish(new FeedbackStateChangedEvent(10, true, Guid.NewGuid()));
        fixture.EventBus.Publish(new TurnoutInfoChangedEvent(500, true, Guid.NewGuid()));
        await WaitForSynchronizationAsync(fixture.Runtime);
        await fixture.Runtime.SetRouteAsync(fixture.RouteId, Guid.NewGuid());

        fixture.EventBus.Publish(new Z21ConnectionLostEvent());
        var disconnected = await WaitForStateAsync(
            fixture.Runtime,
            state => state.Routes[fixture.RouteId].Lifecycle == RouteLifecycle.Failed);

        Assert.Multiple(() =>
        {
            Assert.That(disconnected.Turnouts[fixture.TurnoutId].Lifecycle, Is.EqualTo(TurnoutLifecycle.Unknown));
            Assert.That(disconnected.Blocks[fixture.BlockId].Occupancy, Is.EqualTo(BlockOccupancy.Unknown));
            Assert.That(disconnected.Blocks[fixture.BlockId].ReservationOwnerRouteId, Is.EqualTo(fixture.RouteId));
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

    private static RuntimeFixture CreateFixture()
    {
        var turnoutId = Guid.Parse("10000000-0000-0000-0000-000000000001");
        var blockId = Guid.Parse("10000000-0000-0000-0000-000000000002");
        var routeId = Guid.Parse("10000000-0000-0000-0000-000000000003");
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
            ],
            Routes =
            [
                new RouteDefinition
                {
                    Id = routeId,
                    Name = "R1",
                    ProtectedBlockIds = [blockId],
                    TurnoutRequirements =
                    [
                        new RouteTurnoutRequirement
                        {
                            TurnoutId = turnoutId,
                            Position = TurnoutPosition.Straight
                        }
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
            new AppSettings(),
            TimeProvider.System,
            NullLogger<InterlockingRuntimeService>.Instance);
        return new RuntimeFixture(runtime, eventBus, z21, definition, turnoutId, blockId, routeId);
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
        Guid BlockId,
        Guid RouteId);
}
