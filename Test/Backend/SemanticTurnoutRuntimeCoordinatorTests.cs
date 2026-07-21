// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Test.Backend;

using global::Moba.Backend.Service.Interlocking;
using global::Moba.Domain;

internal sealed class SemanticTurnoutRuntimeCoordinatorTests
{
    private static readonly Guid TurnoutId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    [Test]
    public async Task RequestAsync_Success_ExposesRequestedThenPendingState()
    {
        var clock = new ManualTimeProvider(new DateTimeOffset(2026, 7, 20, 10, 0, 0, TimeSpan.Zero));
        var gateway = new BlockingTurnoutEffectGateway();
        var coordinator = CreateCoordinator(gateway, clock);
        var correlationId = Guid.NewGuid();

        var request = coordinator.RequestAsync(TurnoutId, TurnoutPosition.Straight, correlationId);
        await gateway.CommandStarted.Task.WaitAsync(TimeSpan.FromSeconds(2));

        Assert.Multiple(() =>
        {
            Assert.That(coordinator.Snapshot[TurnoutId].Lifecycle, Is.EqualTo(TurnoutLifecycle.Requested));
            Assert.That(coordinator.Snapshot[TurnoutId].CommandCorrelationId, Is.EqualTo(correlationId));
        });

        gateway.Complete(new TurnoutEffectResult(TurnoutEffectStatus.Succeeded));
        var transition = await request;

        Assert.Multiple(() =>
        {
            Assert.That(transition.Code, Is.EqualTo("turnout.command.pending"));
            Assert.That(transition.State.Lifecycle, Is.EqualTo(TurnoutLifecycle.Pending));
            Assert.That(transition.State.ConfirmationDeadlineUtc, Is.EqualTo(clock.GetUtcNow() + TimeSpan.FromSeconds(5)));
        });
    }

    [Test]
    public async Task ObserveFeedback_CompleteConfirmation_ConfirmsAndIgnoresDuplicate()
    {
        var coordinator = CreateCoordinator(new RecordingTurnoutEffectGateway());
        await coordinator.RequestAsync(TurnoutId, TurnoutPosition.Straight, Guid.NewGuid());

        var incomplete = coordinator.ObserveFeedback(500, true, Guid.NewGuid()).Single();
        var confirmed = coordinator.ObserveFeedback(501, false, Guid.NewGuid()).Single();
        var duplicate = coordinator.ObserveFeedback(501, false, Guid.NewGuid()).Single();

        Assert.Multiple(() =>
        {
            Assert.That(incomplete.Code, Is.EqualTo("turnout.confirmation.incomplete"));
            Assert.That(confirmed.Code, Is.EqualTo("turnout.confirmed"));
            Assert.That(confirmed.State.Lifecycle, Is.EqualTo(TurnoutLifecycle.Confirmed));
            Assert.That(confirmed.State.ConfirmedPosition, Is.EqualTo(TurnoutPosition.Straight));
            Assert.That(confirmed.State.ConfirmationDeadlineUtc, Is.Null);
            Assert.That(duplicate.Status, Is.EqualTo(TurnoutRuntimeTransitionStatus.IgnoredDuplicate));
        });
    }

    [Test]
    public async Task ObserveFeedback_BeforeDispatchCompletes_DoesNotConfirmOutOfOrderObservation()
    {
        var gateway = new BlockingTurnoutEffectGateway();
        var coordinator = CreateCoordinator(gateway);
        var request = coordinator.RequestAsync(TurnoutId, TurnoutPosition.Straight, Guid.NewGuid());
        await gateway.CommandStarted.Task.WaitAsync(TimeSpan.FromSeconds(2));

        coordinator.ObserveFeedback(500, true, Guid.NewGuid());
        var outOfOrder = coordinator.ObserveFeedback(501, false, Guid.NewGuid()).Single();
        gateway.Complete(new TurnoutEffectResult(TurnoutEffectStatus.Succeeded));
        await request;

        Assert.Multiple(() =>
        {
            Assert.That(outOfOrder.Status, Is.EqualTo(TurnoutRuntimeTransitionStatus.IgnoredOutOfOrder));
            Assert.That(coordinator.Snapshot[TurnoutId].Lifecycle, Is.EqualTo(TurnoutLifecycle.Pending));
        });

        coordinator.ObserveFeedback(500, true, Guid.NewGuid());
        var confirmed = coordinator.ObserveFeedback(501, false, Guid.NewGuid()).Single();

        Assert.That(confirmed.State.Lifecycle, Is.EqualTo(TurnoutLifecycle.Confirmed));
    }

    [Test]
    public async Task ExpirePending_AfterDeadline_FailsSafe()
    {
        var clock = new ManualTimeProvider(new DateTimeOffset(2026, 7, 20, 10, 0, 0, TimeSpan.Zero));
        var coordinator = CreateCoordinator(new RecordingTurnoutEffectGateway(), clock);
        await coordinator.RequestAsync(TurnoutId, TurnoutPosition.Straight, Guid.NewGuid());
        clock.Advance(TimeSpan.FromSeconds(6));

        var transition = coordinator.ExpirePending(Guid.NewGuid()).Single();

        Assert.Multiple(() =>
        {
            Assert.That(transition.Code, Is.EqualTo("turnout.confirmation.timeout"));
            Assert.That(transition.State.Lifecycle, Is.EqualTo(TurnoutLifecycle.Failed));
            Assert.That(transition.State.ConfirmedPosition, Is.Null);
        });
    }

    [Test]
    public async Task RequestAsync_CancelledCommand_FailsAndRequiresReconciliation()
    {
        var coordinator = CreateCoordinator(new RecordingTurnoutEffectGateway());
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        var transition = await coordinator.RequestAsync(
            TurnoutId,
            TurnoutPosition.Straight,
            Guid.NewGuid(),
            cancellation.Token);

        Assert.Multiple(() =>
        {
            Assert.That(transition.Code, Is.EqualTo("turnout.command.cancelled"));
            Assert.That(transition.State.Lifecycle, Is.EqualTo(TurnoutLifecycle.Failed));
        });
    }

    [Test]
    public async Task RequestAsync_OfflineGateway_RemainsUnknown()
    {
        var gateway = new RecordingTurnoutEffectGateway(
            _ => new TurnoutEffectResult(TurnoutEffectStatus.Offline, "Offline"));
        var coordinator = CreateCoordinator(gateway);

        var transition = await coordinator.RequestAsync(
            TurnoutId,
            TurnoutPosition.Straight,
            Guid.NewGuid());

        Assert.Multiple(() =>
        {
            Assert.That(transition.Code, Is.EqualTo("turnout.gateway.offline"));
            Assert.That(transition.State.Lifecycle, Is.EqualTo(TurnoutLifecycle.Unknown));
            Assert.That(transition.State.ConfirmationDeadlineUtc, Is.Null);
        });
    }

    [Test]
    public async Task MarkDisconnected_PendingCommand_BecomesUnknown()
    {
        var coordinator = CreateCoordinator(new RecordingTurnoutEffectGateway());
        await coordinator.RequestAsync(TurnoutId, TurnoutPosition.Straight, Guid.NewGuid());

        var transition = coordinator.MarkDisconnected(Guid.NewGuid()).Single();

        Assert.Multiple(() =>
        {
            Assert.That(transition.Code, Is.EqualTo("turnout.disconnected"));
            Assert.That(transition.State.Lifecycle, Is.EqualTo(TurnoutLifecycle.Unknown));
            Assert.That(transition.State.RequestedPosition, Is.Null);
            Assert.That(transition.State.ConfirmedPosition, Is.Null);
        });
    }

    [Test]
    public async Task MarkDisconnected_DuringDispatch_StaleCompletionCannotRestorePendingState()
    {
        var gateway = new BlockingTurnoutEffectGateway();
        var coordinator = CreateCoordinator(gateway);
        var request = coordinator.RequestAsync(TurnoutId, TurnoutPosition.Straight, Guid.NewGuid());
        await gateway.CommandStarted.Task.WaitAsync(TimeSpan.FromSeconds(2));

        coordinator.MarkDisconnected(Guid.NewGuid());
        gateway.Complete(new TurnoutEffectResult(TurnoutEffectStatus.Succeeded));
        var completion = await request;

        Assert.Multiple(() =>
        {
            Assert.That(completion.Status, Is.EqualTo(TurnoutRuntimeTransitionStatus.IgnoredOutOfOrder));
            Assert.That(completion.Code, Is.EqualTo("turnout.command.stale-completion"));
            Assert.That(coordinator.Snapshot[TurnoutId].Lifecycle, Is.EqualTo(TurnoutLifecycle.Unknown));
        });
    }

    [Test]
    public async Task RequestAsync_DuplicateCorrelation_DoesNotDispatchTwice()
    {
        var gateway = new RecordingTurnoutEffectGateway();
        var coordinator = CreateCoordinator(gateway);
        var correlationId = Guid.NewGuid();

        await coordinator.RequestAsync(TurnoutId, TurnoutPosition.Straight, correlationId);
        var duplicate = await coordinator.RequestAsync(TurnoutId, TurnoutPosition.Straight, correlationId);

        Assert.Multiple(() =>
        {
            Assert.That(duplicate.Status, Is.EqualTo(TurnoutRuntimeTransitionStatus.IgnoredDuplicate));
            Assert.That(gateway.Commands, Has.Count.EqualTo(1));
        });
    }

    private static SemanticTurnoutRuntimeCoordinator CreateCoordinator(
        ITurnoutEffectGateway gateway,
        TimeProvider? timeProvider = null)
    {
        var definition = CreateDefinition();
        return new SemanticTurnoutRuntimeCoordinator(
            definition,
            new SemanticTurnoutCommandService(definition, gateway),
            timeProvider ?? TimeProvider.System,
            TimeSpan.FromSeconds(5));
    }

    private static InterlockingDefinition CreateDefinition() =>
        new()
        {
            Turnouts =
            [
                new TurnoutDefinition
                {
                    Id = TurnoutId,
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
                                new TurnoutFeedbackCondition { FunctionAddress = 500, OutputPosition = true },
                                new TurnoutFeedbackCondition { FunctionAddress = 501, OutputPosition = false }
                            ]
                        }
                    ]
                }
            ]
        };

    private sealed class ManualTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        private DateTimeOffset _utcNow = utcNow;

        public override DateTimeOffset GetUtcNow() => _utcNow;

        public void Advance(TimeSpan duration) => _utcNow += duration;
    }

    private sealed class BlockingTurnoutEffectGateway : ITurnoutEffectGateway
    {
        private readonly TaskCompletionSource<TurnoutEffectResult> _completion =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource CommandStarted { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public async Task<TurnoutEffectResult> ExecuteAsync(
            TurnoutEffectCommand command,
            CancellationToken cancellationToken = default)
        {
            CommandStarted.TrySetResult();
            return await _completion.Task.WaitAsync(cancellationToken);
        }

        public void Complete(TurnoutEffectResult result) => _completion.TrySetResult(result);
    }
}
