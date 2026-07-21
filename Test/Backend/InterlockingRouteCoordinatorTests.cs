// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Test.Backend;

using global::Moba.Backend.Service.Interlocking;
using global::Moba.Domain;

internal sealed class InterlockingRouteCoordinatorTests
{
    [Test]
    public async Task SetRouteAsync_ConfirmedTurnout_EstablishesRouteAndClearsConfiguredSignal()
    {
        var fixture = CreateFixture();
        await fixture.Coordinator.ObserveBlockAsync(
            fixture.BlockId,
            BlockOccupancy.Free,
            Guid.NewGuid());

        var pending = await fixture.Coordinator.SetRouteAsync(fixture.RouteId, Guid.NewGuid());
        var established = await fixture.Coordinator.ObserveTurnoutFeedbackAsync(
            500,
            true,
            Guid.NewGuid());

        Assert.Multiple(() =>
        {
            Assert.That(pending.Status, Is.EqualTo(RouteCoordinatorStatus.Pending));
            Assert.That(established.Status, Is.EqualTo(RouteCoordinatorStatus.Accepted));
            Assert.That(established.State.Routes[fixture.RouteId].Lifecycle, Is.EqualTo(RouteLifecycle.Established));
            Assert.That(established.State.Signals[fixture.SignalIds[0]].Aspect, Is.EqualTo(SignalAspect.Ks1));
            Assert.That(fixture.TurnoutGateway.Commands, Has.Count.EqualTo(1));
            Assert.That(fixture.SignalGateway.Commands.Select(command => command.Aspect), Is.EqualTo(new[] { SignalAspect.Ks1 }));
        });
    }

    [Test]
    public async Task SetRouteAsync_UnknownBlock_RejectsWithoutDispatchingEffects()
    {
        var fixture = CreateFixture();

        var result = await fixture.Coordinator.SetRouteAsync(fixture.RouteId, Guid.NewGuid());

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(RouteCoordinatorStatus.Rejected));
            Assert.That(result.Code, Is.EqualTo("route.block.unsafe"));
            Assert.That(fixture.TurnoutGateway.Commands, Is.Empty);
            Assert.That(fixture.SignalGateway.Commands, Is.Empty);
        });
    }

    [Test]
    public async Task SetRouteAsync_EmptyCorrelation_RejectsWithoutStateChange()
    {
        var fixture = CreateFixture();
        await fixture.Coordinator.ObserveBlockAsync(fixture.BlockId, BlockOccupancy.Free, Guid.NewGuid());
        var revision = fixture.Coordinator.Snapshot.Revision;

        var result = await fixture.Coordinator.SetRouteAsync(fixture.RouteId, Guid.Empty);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(RouteCoordinatorStatus.Rejected));
            Assert.That(result.Code, Is.EqualTo("input.correlation.empty"));
            Assert.That(result.State.Revision, Is.EqualTo(revision));
            Assert.That(fixture.TurnoutGateway.Commands, Is.Empty);
        });
    }

    [Test]
    public async Task ObserveTurnoutFeedbackAsync_PartialSignalFailure_SafeStopsAndRetainsLocks()
    {
        var fixture = CreateFixture(
            signalCount: 2,
            signalResultFactory: command =>
                command.SignalId == FixtureIds.Signal2 && command.Aspect == SignalAspect.Ks1
                    ? new SignalEffectResult(SignalEffectStatus.Failed, "Injected failure")
                    : new SignalEffectResult(SignalEffectStatus.Succeeded));
        await fixture.Coordinator.ObserveBlockAsync(fixture.BlockId, BlockOccupancy.Free, Guid.NewGuid());
        await fixture.Coordinator.SetRouteAsync(fixture.RouteId, Guid.NewGuid());

        var result = await fixture.Coordinator.ObserveTurnoutFeedbackAsync(500, true, Guid.NewGuid());

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(RouteCoordinatorStatus.Failed));
            Assert.That(result.State.Routes[fixture.RouteId].Lifecycle, Is.EqualTo(RouteLifecycle.Failed));
            Assert.That(result.State.Signals.Values.All(signal => signal.Aspect == SignalAspect.Hp0), Is.True);
            Assert.That(result.State.Signals.Values.All(signal => signal.LockOwnerRouteId == fixture.RouteId), Is.True);
            Assert.That(fixture.SignalGateway.Commands.Any(command => command.Aspect == SignalAspect.Hp0), Is.True);
        });
    }

    [Test]
    public async Task ReleaseRouteAsync_AfterTrainClears_RestoresStopAndReleasesAllResources()
    {
        var fixture = CreateFixture();
        await fixture.Coordinator.ObserveBlockAsync(fixture.BlockId, BlockOccupancy.Free, Guid.NewGuid());
        await fixture.Coordinator.SetRouteAsync(fixture.RouteId, Guid.NewGuid());
        await fixture.Coordinator.ObserveTurnoutFeedbackAsync(500, true, Guid.NewGuid());
        await fixture.Coordinator.ObserveBlockAsync(fixture.BlockId, BlockOccupancy.Occupied, Guid.NewGuid());
        await fixture.Coordinator.ObserveBlockAsync(fixture.BlockId, BlockOccupancy.Free, Guid.NewGuid());

        var released = await fixture.Coordinator.ReleaseRouteAsync(fixture.RouteId, Guid.NewGuid());

        Assert.Multiple(() =>
        {
            Assert.That(released.Status, Is.EqualTo(RouteCoordinatorStatus.Accepted));
            Assert.That(released.State.Routes[fixture.RouteId].Lifecycle, Is.EqualTo(RouteLifecycle.Available));
            Assert.That(released.State.Blocks[fixture.BlockId].ReservationOwnerRouteId, Is.Null);
            Assert.That(released.State.Turnouts[fixture.TurnoutId].LockOwnerRouteId, Is.Null);
            Assert.That(released.State.Signals[fixture.SignalIds[0]].LockOwnerRouteId, Is.Null);
            Assert.That(fixture.SignalGateway.Commands.Last().Aspect, Is.EqualTo(SignalAspect.Hp0));
        });
    }

    [Test]
    public async Task CancelRouteAsync_WhileAwaitingConfirmation_FailsAndRetainsLocks()
    {
        var fixture = CreateFixture();
        await fixture.Coordinator.ObserveBlockAsync(fixture.BlockId, BlockOccupancy.Free, Guid.NewGuid());
        await fixture.Coordinator.SetRouteAsync(fixture.RouteId, Guid.NewGuid());

        var cancelled = await fixture.Coordinator.CancelRouteAsync(fixture.RouteId, Guid.NewGuid());

        Assert.Multiple(() =>
        {
            Assert.That(cancelled.Status, Is.EqualTo(RouteCoordinatorStatus.Failed));
            Assert.That(cancelled.State.Routes[fixture.RouteId].Lifecycle, Is.EqualTo(RouteLifecycle.Failed));
            Assert.That(cancelled.State.Blocks[fixture.BlockId].ReservationOwnerRouteId, Is.EqualTo(fixture.RouteId));
            Assert.That(cancelled.State.Signals[fixture.SignalIds[0]].Aspect, Is.EqualTo(SignalAspect.Hp0));
        });
    }

    [Test]
    public async Task ExpirePendingAsync_ConfirmationTimeout_FailsRouteAndRetainsLocks()
    {
        var timeProvider = new TestTimeProvider();
        var fixture = CreateFixture(timeProvider: timeProvider);
        await fixture.Coordinator.ObserveBlockAsync(fixture.BlockId, BlockOccupancy.Free, Guid.NewGuid());
        await fixture.Coordinator.SetRouteAsync(fixture.RouteId, Guid.NewGuid());
        timeProvider.Advance(TimeSpan.FromSeconds(6));

        var timedOut = await fixture.Coordinator.ExpirePendingAsync(Guid.NewGuid());

        Assert.Multiple(() =>
        {
            Assert.That(timedOut.Status, Is.EqualTo(RouteCoordinatorStatus.Failed));
            Assert.That(timedOut.Code, Is.EqualTo("turnout.confirmation.timeout"));
            Assert.That(timedOut.State.Routes[fixture.RouteId].Lifecycle, Is.EqualTo(RouteLifecycle.Failed));
            Assert.That(timedOut.State.Turnouts[fixture.TurnoutId].Lifecycle, Is.EqualTo(TurnoutLifecycle.Failed));
            Assert.That(timedOut.State.Blocks[fixture.BlockId].ReservationOwnerRouteId, Is.EqualTo(fixture.RouteId));
            Assert.That(fixture.SignalGateway.Commands.Last().Aspect, Is.EqualTo(SignalAspect.Hp0));
        });
    }

    [Test]
    public async Task ObserveTurnoutFeedbackAsync_AfterTimeout_ReconcilesTurnoutWithoutClearingFailedRoute()
    {
        var timeProvider = new TestTimeProvider();
        var fixture = CreateFixture(timeProvider: timeProvider);
        await fixture.Coordinator.ObserveBlockAsync(fixture.BlockId, BlockOccupancy.Free, Guid.NewGuid());
        await fixture.Coordinator.SetRouteAsync(fixture.RouteId, Guid.NewGuid());
        timeProvider.Advance(TimeSpan.FromSeconds(6));
        await fixture.Coordinator.ExpirePendingAsync(Guid.NewGuid());

        var reconciled = await fixture.Coordinator.ObserveTurnoutFeedbackAsync(500, true, Guid.NewGuid());

        Assert.Multiple(() =>
        {
            Assert.That(reconciled.Code, Is.EqualTo("turnout.reconciled"));
            Assert.That(reconciled.State.Turnouts[fixture.TurnoutId].Lifecycle, Is.EqualTo(TurnoutLifecycle.Confirmed));
            Assert.That(reconciled.State.Routes[fixture.RouteId].Lifecycle, Is.EqualTo(RouteLifecycle.Failed));
            Assert.That(reconciled.State.Signals[fixture.SignalIds[0]].Aspect, Is.EqualTo(SignalAspect.Hp0));
            Assert.That(reconciled.State.Signals[fixture.SignalIds[0]].LockOwnerRouteId, Is.EqualTo(fixture.RouteId));
        });
    }

    [Test]
    public async Task MarkDisconnectedAsync_EstablishedRoute_FailsSafeAndPublishesCorrelatedLifecycleEvent()
    {
        var fixture = CreateFixture();
        await fixture.Coordinator.ObserveBlockAsync(fixture.BlockId, BlockOccupancy.Free, Guid.NewGuid());
        await fixture.Coordinator.SetRouteAsync(fixture.RouteId, Guid.NewGuid());
        await fixture.Coordinator.ObserveTurnoutFeedbackAsync(500, true, Guid.NewGuid());
        var correlationId = Guid.NewGuid();

        var disconnected = await fixture.Coordinator.MarkDisconnectedAsync(correlationId);

        Assert.Multiple(() =>
        {
            Assert.That(disconnected.Status, Is.EqualTo(RouteCoordinatorStatus.Failed));
            Assert.That(disconnected.Code, Is.EqualTo("route.effect.disconnected"));
            Assert.That(disconnected.State.Routes[fixture.RouteId].Lifecycle, Is.EqualTo(RouteLifecycle.Failed));
            Assert.That(disconnected.State.Turnouts[fixture.TurnoutId].Lifecycle, Is.EqualTo(TurnoutLifecycle.Unknown));
            Assert.That(disconnected.State.Signals[fixture.SignalIds[0]].Aspect, Is.EqualTo(SignalAspect.Hp0));
            Assert.That(fixture.LifecycleSink.Events.Last().CorrelationId, Is.EqualTo(correlationId));
            Assert.That(fixture.LifecycleSink.Events.Last().State, Is.SameAs(disconnected.State));
        });
    }

    [Test]
    public async Task SetRouteAsync_LifecycleSinkFails_DoesNotCompromiseCoordinatorResult()
    {
        var fixture = CreateFixture(lifecycleSink: new ThrowingLifecycleEventSink());

        var observed = await fixture.Coordinator.ObserveBlockAsync(
            fixture.BlockId,
            BlockOccupancy.Free,
            Guid.NewGuid());

        Assert.Multiple(() =>
        {
            Assert.That(observed.Status, Is.EqualTo(RouteCoordinatorStatus.Accepted));
            Assert.That(observed.State.Blocks[fixture.BlockId].Occupancy, Is.EqualTo(BlockOccupancy.Free));
        });
    }

    [Test]
    public async Task ShutdownAsync_InFlightTurnoutCommand_CancelsCommandAndRejectsFutureOperations()
    {
        var blockingGateway = new BlockingTurnoutEffectGateway();
        var fixture = CreateFixture(turnoutEffectGateway: blockingGateway);
        await fixture.Coordinator.ObserveBlockAsync(fixture.BlockId, BlockOccupancy.Free, Guid.NewGuid());
        var settingTask = fixture.Coordinator.SetRouteAsync(fixture.RouteId, Guid.NewGuid());
        await blockingGateway.Entered.Task.WaitAsync(TimeSpan.FromSeconds(5));

        var shutdown = await fixture.Coordinator
            .ShutdownAsync(Guid.NewGuid())
            .WaitAsync(TimeSpan.FromSeconds(5));
        var rejected = await fixture.Coordinator.ObserveBlockAsync(
            fixture.BlockId,
            BlockOccupancy.Free,
            Guid.NewGuid());

        Assert.Multiple(() =>
        {
            Assert.ThrowsAsync<OperationCanceledException>(async () => await settingTask);
            Assert.That(shutdown.Status, Is.EqualTo(RouteCoordinatorStatus.Accepted));
            Assert.That(shutdown.Code, Is.EqualTo("coordinator.shutdown"));
            Assert.That(shutdown.State.Routes[fixture.RouteId].Lifecycle, Is.EqualTo(RouteLifecycle.Failed));
            Assert.That(shutdown.State.Blocks[fixture.BlockId].ReservationOwnerRouteId, Is.EqualTo(fixture.RouteId));
            Assert.That(shutdown.State.Signals[fixture.SignalIds[0]].Aspect, Is.EqualTo(SignalAspect.Hp0));
            Assert.That(rejected.Status, Is.EqualTo(RouteCoordinatorStatus.Rejected));
            Assert.That(rejected.Code, Is.EqualTo("coordinator.shutdown"));
        });
    }

    [Test]
    public async Task PreviewRouteAsync_FreeRoute_AcceptsWithoutChangingStateOrDispatchingEffects()
    {
        var fixture = CreateFixture();
        await fixture.Coordinator.ObserveBlockAsync(fixture.BlockId, BlockOccupancy.Free, Guid.NewGuid());
        var revision = fixture.Coordinator.Snapshot.Revision;

        var preview = await fixture.Coordinator.PreviewRouteAsync(fixture.RouteId, Guid.NewGuid());

        Assert.Multiple(() =>
        {
            Assert.That(preview.Status, Is.EqualTo(RouteCoordinatorStatus.Accepted));
            Assert.That(preview.Code, Is.EqualTo("route.preview.available"));
            Assert.That(preview.State.Revision, Is.EqualTo(revision));
            Assert.That(preview.State.Routes[fixture.RouteId].Lifecycle, Is.EqualTo(RouteLifecycle.Available));
            Assert.That(fixture.TurnoutGateway.Commands, Is.Empty);
            Assert.That(fixture.SignalGateway.Commands, Is.Empty);
        });
    }

    [Test]
    public async Task SelectThenSetRouteAsync_ReservesBeforeDispatchingTurnoutCommand()
    {
        var fixture = CreateFixture();
        await fixture.Coordinator.ObserveBlockAsync(fixture.BlockId, BlockOccupancy.Free, Guid.NewGuid());

        var selected = await fixture.Coordinator.SelectRouteAsync(fixture.RouteId, Guid.NewGuid());
        var setting = await fixture.Coordinator.SetRouteAsync(fixture.RouteId, Guid.NewGuid());

        Assert.Multiple(() =>
        {
            Assert.That(selected.Status, Is.EqualTo(RouteCoordinatorStatus.Accepted));
            Assert.That(selected.Code, Is.EqualTo("route.reserved"));
            Assert.That(selected.State.Routes[fixture.RouteId].Lifecycle, Is.EqualTo(RouteLifecycle.Selected));
            Assert.That(setting.Status, Is.EqualTo(RouteCoordinatorStatus.Pending));
            Assert.That(setting.State.Routes[fixture.RouteId].Lifecycle, Is.EqualTo(RouteLifecycle.Setting));
            Assert.That(fixture.TurnoutGateway.Commands, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public async Task ReconcileRouteAsync_AfterTimeoutAndLateFeedback_ReleasesVerifiedSafeRoute()
    {
        var timeProvider = new TestTimeProvider();
        var fixture = CreateFixture(timeProvider: timeProvider);
        await fixture.Coordinator.ObserveBlockAsync(fixture.BlockId, BlockOccupancy.Free, Guid.NewGuid());
        await fixture.Coordinator.SetRouteAsync(fixture.RouteId, Guid.NewGuid());
        timeProvider.Advance(TimeSpan.FromSeconds(6));
        await fixture.Coordinator.ExpirePendingAsync(Guid.NewGuid());
        await fixture.Coordinator.ObserveTurnoutFeedbackAsync(500, true, Guid.NewGuid());

        var reconciled = await fixture.Coordinator.ReconcileRouteAsync(fixture.RouteId, Guid.NewGuid());

        Assert.Multiple(() =>
        {
            Assert.That(reconciled.Status, Is.EqualTo(RouteCoordinatorStatus.Accepted));
            Assert.That(reconciled.Code, Is.EqualTo("route.reconciled"));
            Assert.That(reconciled.State.Routes[fixture.RouteId].Lifecycle, Is.EqualTo(RouteLifecycle.Available));
            Assert.That(reconciled.State.Blocks[fixture.BlockId].ReservationOwnerRouteId, Is.Null);
            Assert.That(reconciled.State.Turnouts[fixture.TurnoutId].LockOwnerRouteId, Is.Null);
            Assert.That(reconciled.State.Signals[fixture.SignalIds[0]].LockOwnerRouteId, Is.Null);
        });
    }

    [Test]
    public async Task SetRouteAsync_Success_PublishesEveryInternalTransitionWithOperationCorrelation()
    {
        var fixture = CreateFixture();
        await fixture.Coordinator.ObserveBlockAsync(fixture.BlockId, BlockOccupancy.Free, Guid.NewGuid());
        var settingCorrelation = Guid.NewGuid();
        var feedbackCorrelation = Guid.NewGuid();

        await fixture.Coordinator.SetRouteAsync(fixture.RouteId, settingCorrelation);
        await fixture.Coordinator.ObserveTurnoutFeedbackAsync(500, true, feedbackCorrelation);

        var settingEvents = fixture.LifecycleSink.Events
            .Where(lifecycleEvent => lifecycleEvent.CorrelationId == settingCorrelation)
            .Select(lifecycleEvent => lifecycleEvent.Code)
            .ToArray();
        var feedbackEvents = fixture.LifecycleSink.Events
            .Where(lifecycleEvent => lifecycleEvent.CorrelationId == feedbackCorrelation)
            .Select(lifecycleEvent => lifecycleEvent.Code)
            .ToArray();
        Assert.Multiple(() =>
        {
            Assert.That(settingEvents, Does.Contain("route.reserved"));
            Assert.That(settingEvents, Does.Contain("route.setting"));
            Assert.That(feedbackEvents, Does.Contain("turnout.observed"));
            Assert.That(feedbackEvents, Does.Contain("route.established"));
            Assert.That(feedbackEvents, Does.Contain("route.signal.proceed"));
        });
    }

    [Test]
    public async Task SafeStopRouteAsync_EstablishedRoute_RestoresStopAndRetainsLocks()
    {
        var fixture = CreateFixture();
        await fixture.Coordinator.ObserveBlockAsync(fixture.BlockId, BlockOccupancy.Free, Guid.NewGuid());
        await fixture.Coordinator.SetRouteAsync(fixture.RouteId, Guid.NewGuid());
        await fixture.Coordinator.ObserveTurnoutFeedbackAsync(500, true, Guid.NewGuid());

        var stopped = await fixture.Coordinator.SafeStopRouteAsync(fixture.RouteId, Guid.NewGuid());

        Assert.Multiple(() =>
        {
            Assert.That(stopped.Status, Is.EqualTo(RouteCoordinatorStatus.Accepted));
            Assert.That(stopped.Code, Is.EqualTo("route.safe-stopped"));
            Assert.That(stopped.State.Routes[fixture.RouteId].Lifecycle, Is.EqualTo(RouteLifecycle.Failed));
            Assert.That(stopped.State.Blocks[fixture.BlockId].ReservationOwnerRouteId, Is.EqualTo(fixture.RouteId));
            Assert.That(stopped.State.Signals[fixture.SignalIds[0]].Aspect, Is.EqualTo(SignalAspect.Hp0));
            Assert.That(fixture.SignalGateway.Commands.Last().Aspect, Is.EqualTo(SignalAspect.Hp0));
        });
    }

    private static CoordinatorFixture CreateFixture(
        int signalCount = 1,
        Func<SignalEffectCommand, SignalEffectResult>? signalResultFactory = null,
        TimeProvider? timeProvider = null,
        IInterlockingLifecycleEventSink? lifecycleSink = null,
        ITurnoutEffectGateway? turnoutEffectGateway = null)
    {
        var ids = new FixtureIds();
        var signalIds = signalCount == 1
            ? new[] { ids.Signal1 }
            : new[] { ids.Signal1, FixtureIds.Signal2 };
        var definition = new InterlockingDefinition
        {
            Turnouts =
            [
                new TurnoutDefinition
                {
                    Id = ids.Turnout,
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
                                new TurnoutFeedbackCondition { FunctionAddress = 500, OutputPosition = true }
                            ]
                        }
                    ]
                }
            ],
            Signals = signalIds.Select((signalId, index) => new SignalDefinition
            {
                Id = signalId,
                Name = $"N{index + 1}",
                SafeAspect = SignalAspect.Hp0,
                BaseAddress = 200 + index
            }).ToList(),
            Blocks = [new BlockDefinition { Id = ids.Block, Name = "B1" }],
            Routes =
            [
                new RouteDefinition
                {
                    Id = ids.Route,
                    Name = "Route 1",
                    ProtectedBlockIds = [ids.Block],
                    TurnoutRequirements =
                    [
                        new RouteTurnoutRequirement
                        {
                            TurnoutId = ids.Turnout,
                            Position = TurnoutPosition.Straight
                        }
                    ],
                    SignalRequirements = signalIds.Select(signalId => new RouteSignalRequirement
                    {
                        SignalId = signalId,
                        ProceedAspect = SignalAspect.Ks1
                    }).ToList()
                }
            ]
        };
        var turnoutGateway = new RecordingTurnoutEffectGateway();
        var turnoutRuntime = new SemanticTurnoutRuntimeCoordinator(
            definition,
            new SemanticTurnoutCommandService(definition, turnoutEffectGateway ?? turnoutGateway),
            timeProvider ?? TimeProvider.System,
            TimeSpan.FromSeconds(5));
        var signalGateway = new RecordingSignalEffectGateway(signalResultFactory);
        var recordingLifecycleSink = lifecycleSink as RecordingInterlockingLifecycleEventSink
                                     ?? new RecordingInterlockingLifecycleEventSink();
        var coordinator = new InterlockingRouteCoordinator(
            definition,
            turnoutRuntime,
            signalGateway,
            lifecycleSink ?? recordingLifecycleSink);
        return new CoordinatorFixture(
            coordinator,
            turnoutGateway,
            signalGateway,
            recordingLifecycleSink,
            ids.Route,
            ids.Block,
            ids.Turnout,
            signalIds);
    }

    private sealed record CoordinatorFixture(
        InterlockingRouteCoordinator Coordinator,
        RecordingTurnoutEffectGateway TurnoutGateway,
        RecordingSignalEffectGateway SignalGateway,
        RecordingInterlockingLifecycleEventSink LifecycleSink,
        Guid RouteId,
        Guid BlockId,
        Guid TurnoutId,
        IReadOnlyList<Guid> SignalIds);

    private sealed class FixtureIds
    {
        public static readonly Guid Signal2 = Guid.Parse("00000000-0000-0000-0000-000000000003");

        public Guid Turnout { get; } = Guid.Parse("00000000-0000-0000-0000-000000000001");
        public Guid Signal1 { get; } = Guid.Parse("00000000-0000-0000-0000-000000000002");
        public Guid Block { get; } = Guid.Parse("00000000-0000-0000-0000-000000000004");
        public Guid Route { get; } = Guid.Parse("00000000-0000-0000-0000-000000000005");
    }

    private sealed class TestTimeProvider : TimeProvider
    {
        private DateTimeOffset _utcNow = new(2026, 7, 20, 12, 0, 0, TimeSpan.Zero);

        public override DateTimeOffset GetUtcNow() => _utcNow;

        public void Advance(TimeSpan duration) => _utcNow += duration;
    }

    private sealed class ThrowingLifecycleEventSink : IInterlockingLifecycleEventSink
    {
        public void Publish(InterlockingLifecycleEvent lifecycleEvent) =>
            throw new InvalidOperationException("Injected lifecycle sink failure.");
    }

    private sealed class BlockingTurnoutEffectGateway : ITurnoutEffectGateway
    {
        public TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public async Task<TurnoutEffectResult> ExecuteAsync(
            TurnoutEffectCommand command,
            CancellationToken cancellationToken = default)
        {
            Entered.TrySetResult();
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return new TurnoutEffectResult(TurnoutEffectStatus.Succeeded);
        }
    }
}
