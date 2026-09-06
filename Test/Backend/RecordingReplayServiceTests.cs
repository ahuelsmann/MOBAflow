// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Test.Backend;

using global::Moba.Backend.Interface;
using global::Moba.Backend.Service.Recording;
using global::Moba.Common.Recording;

using System.Text.Json;
using System.Threading.Channels;

internal sealed class RecordingReplayServiceTests
{
    [Test]
    public async Task Play_Should_PreserveOrderAndApplySpeedChangesOnlyToFutureWaits()
    {
        var scheduler = new ControlledDelayScheduler();
        var runtime = new CapturingReplayRuntime();
        await using var service = CreateService(scheduler, runtime);
        service.Load(CreateArtifact(
            CreateEntry(1, 0, RecordingReplayApplicability.DisplayOnly),
            CreateEntry(2, 1, RecordingReplayApplicability.ReplayApplicable),
            CreateEntry(3, 3, RecordingReplayApplicability.ReplayApplicable)));
        var completed = WaitForStateAsync(service, RecordingReplayState.Completed);

        var started = service.Play(1);
        var firstDelay = await scheduler.ReadNextAsync();
        var speedChanged = service.Play(4);
        firstDelay.Complete();
        var secondDelay = await scheduler.ReadNextAsync();
        secondDelay.Complete();
        await completed;

        Assert.Multiple(() =>
        {
            Assert.That(started.Succeeded, Is.True);
            Assert.That(speedChanged.IsIdempotent, Is.True);
            Assert.That(firstDelay.Delay, Is.EqualTo(TimeSpan.FromSeconds(1)));
            Assert.That(secondDelay.Delay, Is.EqualTo(TimeSpan.FromSeconds(0.5)));
            Assert.That(runtime.AppliedSequences, Is.EqualTo(new long[] { 2, 3 }));
            Assert.That(service.CurrentStatus.SkippedEntryCount, Is.EqualTo(1));
            Assert.That(service.CurrentStatus.Position, Is.EqualTo(3));
        });
    }

    [Test]
    public async Task PauseAndStep_Should_NotApplyTheWaitingEntryUntilStep()
    {
        var scheduler = new ControlledDelayScheduler();
        var runtime = new CapturingReplayRuntime();
        await using var service = CreateService(scheduler, runtime);
        service.Load(CreateArtifact(CreateEntry(1, 5, RecordingReplayApplicability.ReplayApplicable)));

        service.Play(1);
        var pendingDelay = await scheduler.ReadNextAsync();
        var paused = service.Pause();
        try
        {
            await pendingDelay.Completion;
        }
        catch (OperationCanceledException)
        {
            // Pausing cancels the in-flight replay wait by design.
        }
        var stepped = await service.StepAsync();

        Assert.Multiple(() =>
        {
            Assert.That(paused.Succeeded, Is.True);
            Assert.That(pendingDelay.WasCancelled, Is.True);
            Assert.That(stepped.Succeeded, Is.True);
            Assert.That(runtime.AppliedSequences, Is.EqualTo(new long[] { 1 }));
            Assert.That(service.CurrentStatus.State, Is.EqualTo(RecordingReplayState.Completed));
        });
    }

    [Test]
    public async Task Seek_Should_ResetRuntimeAndReapplyToAbsolutePositionWithoutDelays()
    {
        var runtime = new CapturingReplayRuntime();
        await using var service = CreateService(new ImmediateDelayScheduler(), runtime);
        service.Load(CreateArtifact(
            CreateEntry(1, 1, RecordingReplayApplicability.ReplayApplicable),
            CreateEntry(2, 2, RecordingReplayApplicability.DisplayOnly),
            CreateEntry(3, 3, RecordingReplayApplicability.ReplayApplicable)));

        await service.SeekAsync(3);
        var seekBack = await service.SeekAsync(1);

        Assert.Multiple(() =>
        {
            Assert.That(seekBack.Succeeded, Is.True);
            Assert.That(runtime.ResetCount, Is.EqualTo(3));
            Assert.That(runtime.AppliedSequences, Is.EqualTo(new long[] { 1 }));
            Assert.That(service.CurrentStatus.Position, Is.EqualTo(1));
            Assert.That(service.CurrentStatus.Elapsed, Is.EqualTo(TimeSpan.FromSeconds(1)));
            Assert.That(service.CurrentStatus.State, Is.EqualTo(RecordingReplayState.Paused));
        });
    }

    [Test]
    public async Task LiveHardware_Should_BlockPlayStepAndSeekWithoutApplyingEntries()
    {
        var safetyGate = new StubSafetyGate(canReplay: false);
        var runtime = new CapturingReplayRuntime();
        await using var service = new RecordingReplayService(
            safetyGate,
            new StubRuntimeFactory(runtime),
            new ImmediateDelayScheduler());
        service.Load(CreateArtifact(CreateEntry(1, 0, RecordingReplayApplicability.ReplayApplicable)));

        var play = service.Play(1);
        var step = await service.StepAsync();
        var seek = await service.SeekAsync(1);

        Assert.Multiple(() =>
        {
            Assert.That(play.FailureCode, Is.EqualTo(RecordingReplayFailureCode.LiveHardwareConnected));
            Assert.That(step.FailureCode, Is.EqualTo(RecordingReplayFailureCode.LiveHardwareConnected));
            Assert.That(seek.FailureCode, Is.EqualTo(RecordingReplayFailureCode.LiveHardwareConnected));
            Assert.That(service.CurrentStatus.State, Is.EqualTo(RecordingReplayState.Blocked));
            Assert.That(runtime.AppliedSequences, Is.Empty);
        });
    }

    [Test]
    public async Task Cancel_Should_DiscardProjectedStateAndReturnToReadyPosition()
    {
        var runtime = new CapturingReplayRuntime();
        await using var service = CreateService(new ImmediateDelayScheduler(), runtime);
        service.Load(CreateArtifact(CreateEntry(1, 0, RecordingReplayApplicability.ReplayApplicable)));
        await service.StepAsync();

        var cancelled = await service.CancelAsync();

        Assert.Multiple(() =>
        {
            Assert.That(cancelled.Succeeded, Is.True);
            Assert.That(service.CurrentStatus.State, Is.EqualTo(RecordingReplayState.Ready));
            Assert.That(service.CurrentStatus.Position, Is.Zero);
            Assert.That(service.CurrentStatus.CurrentEntry, Is.Null);
            Assert.That(runtime.AppliedSequences, Is.Empty);
        });
    }

    [Test]
    public async Task CancelWhileWaiting_Should_CancelDelayAndResetWithoutApplyingLaterEntry()
    {
        var scheduler = new ControlledDelayScheduler();
        var runtime = new CapturingReplayRuntime();
        await using var service = CreateService(scheduler, runtime);
        service.Load(CreateArtifact(CreateEntry(1, 10, RecordingReplayApplicability.ReplayApplicable)));
        service.Play(1);
        var pendingDelay = await scheduler.ReadNextAsync();

        var cancelled = await service.CancelAsync();

        Assert.Multiple(() =>
        {
            Assert.That(cancelled.Succeeded, Is.True);
            Assert.That(pendingDelay.WasCancelled, Is.True);
            Assert.That(runtime.AppliedSequences, Is.Empty);
            Assert.That(service.CurrentStatus.State, Is.EqualTo(RecordingReplayState.Ready));
            Assert.That(service.CurrentStatus.Position, Is.Zero);
        });
    }

    [Test]
    public async Task HardwareConnectionDuringWait_Should_BlockBeforeApplyingEntry()
    {
        var scheduler = new ControlledDelayScheduler();
        var safetyGate = new MutableSafetyGate();
        var runtime = new CapturingReplayRuntime();
        await using var service = new RecordingReplayService(
            safetyGate,
            new StubRuntimeFactory(runtime),
            scheduler);
        service.Load(CreateArtifact(CreateEntry(1, 10, RecordingReplayApplicability.ReplayApplicable)));
        var blocked = WaitForStateAsync(service, RecordingReplayState.Blocked);
        service.Play(1);
        var pendingDelay = await scheduler.ReadNextAsync();

        safetyGate.CanReplay = false;
        pendingDelay.Complete();
        await blocked;

        Assert.Multiple(() =>
        {
            Assert.That(runtime.AppliedSequences, Is.Empty);
            Assert.That(service.CurrentStatus.LastFailureCode, Is.EqualTo(RecordingReplayFailureCode.LiveHardwareConnected));
        });
    }

    [Test]
    public void IsolatedRuntimeConstruction_Should_HaveNoLiveDependencies()
    {
        var constructor = typeof(IsolatedReplayRuntime).GetConstructors().Single();
        var factoryConstructor = typeof(IsolatedReplayRuntimeFactory).GetConstructors().Single();

        Assert.Multiple(() =>
        {
            Assert.That(constructor.GetParameters(), Is.Empty);
            Assert.That(factoryConstructor.GetParameters(), Is.Empty);
            Assert.That(typeof(IsolatedReplayRuntime).GetFields(
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic)
                .Select(field => field.FieldType.FullName),
                Has.None.Contains("IZ21").And.None.Contains("IMobaRuntime").And.None.Contains("IEventBus"));
        });
    }

    [Test]
    public void IsolatedRuntime_Should_ApplyOnlyAllowListedReplayEntries()
    {
        var runtime = new IsolatedReplayRuntime();
        var known = CreateEntry(1, 0, RecordingReplayApplicability.ReplayApplicable, "workflow.lifecycle");
        var unknown = CreateEntry(2, 0, RecordingReplayApplicability.ReplayApplicable, "unknown.event");
        var displayOnly = CreateEntry(3, 0, RecordingReplayApplicability.DisplayOnly, "recorder.note");

        var knownResult = runtime.Apply(known);
        var unknownResult = runtime.Apply(unknown);
        var displayResult = runtime.Apply(displayOnly);

        Assert.Multiple(() =>
        {
            Assert.That(knownResult.Succeeded, Is.True);
            Assert.That(unknownResult.Succeeded, Is.False);
            Assert.That(displayResult.Succeeded, Is.False);
            Assert.That(runtime.Current.AppliedEntryCount, Is.EqualTo(1));
            Assert.That(runtime.Current.LastAppliedSequence, Is.EqualTo(1));
        });
    }

    private static RecordingReplayService CreateService(
        IRecordingReplayDelayScheduler scheduler,
        IIsolatedReplayRuntime runtime) =>
        new(new StubSafetyGate(canReplay: true), new StubRuntimeFactory(runtime), scheduler);

    private static RecordingArtifact CreateArtifact(params RecordingEntry[] entries) =>
        new(
            new RecordingSessionMetadata(
                Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
                "Replay",
                new DateTimeOffset(2026, 7, 21, 10, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2026, 7, 21, 10, 1, 0, TimeSpan.Zero)),
            "1.0",
            null,
            new RecordingArtifactOptions(100, 1_000_000),
            entries);

    private static RecordingEntry CreateEntry(
        long sequence,
        int elapsedSeconds,
        RecordingReplayApplicability applicability,
        string? typeKey = null) =>
        new(
            sequence,
            new DateTimeOffset(2026, 7, 21, 10, 0, elapsedSeconds, TimeSpan.Zero),
            TimeSpan.FromSeconds(elapsedSeconds),
            "test",
            "unit-test",
            typeKey ?? (applicability == RecordingReplayApplicability.ReplayApplicable ? "test.replay" : "test.display"),
            "information",
            null,
            null,
            JsonSerializer.SerializeToElement(new { sequence }),
            $"Entry {sequence}",
            applicability);

    private static Task<RecordingReplaySnapshot> WaitForStateAsync(
        IRecordingReplayStatusSource source,
        RecordingReplayState state)
    {
        var completion = new TaskCompletionSource<RecordingReplaySnapshot>(TaskCreationOptions.RunContinuationsAsynchronously);
        source.StatusChanged += snapshot =>
        {
            if (snapshot.State == state) completion.TrySetResult(snapshot);
        };
        return completion.Task.WaitAsync(TimeSpan.FromSeconds(5));
    }

    private sealed class StubSafetyGate(bool canReplay) : IRecordingReplaySafetyGate
    {
        public RecordingReplaySafetyStatus GetStatus() =>
            canReplay
                ? new RecordingReplaySafetyStatus(true, null)
                : new RecordingReplaySafetyStatus(false, "Live Z21 connected.");
    }

    private sealed class MutableSafetyGate : IRecordingReplaySafetyGate
    {
        public bool CanReplay { get; set; } = true;

        public RecordingReplaySafetyStatus GetStatus() =>
            CanReplay
                ? new RecordingReplaySafetyStatus(true, null)
                : new RecordingReplaySafetyStatus(false, "Live Z21 connected.");
    }

    private sealed class StubRuntimeFactory(IIsolatedReplayRuntime runtime) : IIsolatedReplayRuntimeFactory
    {
        public IIsolatedReplayRuntime Create() => runtime;
    }

    private sealed class CapturingReplayRuntime : IIsolatedReplayRuntime
    {
        public List<long> AppliedSequences { get; } = [];

        public int ResetCount { get; private set; }

        public IsolatedReplayRuntimeSnapshot Current =>
            new(AppliedSequences.Count, AppliedSequences.LastOrDefault(), null);

        public IsolatedReplayApplyResult Apply(RecordingEntry entry)
        {
            AppliedSequences.Add(entry.Sequence);
            return IsolatedReplayApplyResult.Success();
        }

        public void Reset()
        {
            ResetCount++;
            AppliedSequences.Clear();
        }
    }

    private sealed class ImmediateDelayScheduler : IRecordingReplayDelayScheduler
    {
        public Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class ControlledDelayScheduler : IRecordingReplayDelayScheduler
    {
        private readonly Channel<DelayRequest> _requests = Channel.CreateUnbounded<DelayRequest>();

        public Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken)
        {
            if (delay <= TimeSpan.Zero) return Task.CompletedTask;
            var request = new DelayRequest(delay, cancellationToken);
            _requests.Writer.TryWrite(request);
            return request.Completion;
        }

        public async Task<DelayRequest> ReadNextAsync() =>
            await _requests.Reader.ReadAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(5));
    }

    private sealed class DelayRequest
    {
        private readonly TaskCompletionSource _completion =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly CancellationTokenRegistration _registration;

        public DelayRequest(TimeSpan delay, CancellationToken cancellationToken)
        {
            Delay = delay;
            _registration = cancellationToken.Register(() =>
            {
                WasCancelled = true;
                _completion.TrySetCanceled(cancellationToken);
            });
        }

        public TimeSpan Delay { get; }

        public bool WasCancelled { get; private set; }

        public Task Completion => _completion.Task;

        public void Complete()
        {
            _registration.Dispose();
            _completion.TrySetResult();
        }
    }
}