// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Backend.Service.Recording;

using Interface;

using Moba.Common.Recording;

/// <summary>Schedules deterministic journal replay against an isolated in-memory runtime.</summary>
public sealed class RecordingReplayService : IRecordingReplayService
{
    private static readonly double[] SupportedSpeeds = [0.25, 0.5, 1, 2, 4, 8];
    private readonly object _gate = new();
    private readonly IRecordingReplaySafetyGate _safetyGate;
    private readonly IIsolatedReplayRuntimeFactory _runtimeFactory;
    private readonly IRecordingReplayDelayScheduler _delayScheduler;
    private RecordingArtifact? _artifact;
    private IIsolatedReplayRuntime? _runtime;
    private RecordingReplayState _state;
    private int _position;
    private int _appliedEntryCount;
    private int _skippedEntryCount;
    private TimeSpan _elapsed;
    private double _speed = 1;
    private RecordingEntry? _currentEntry;
    private RecordingReplayFailureCode _lastFailureCode;
    private string? _lastFailureMessage;
    private CancellationTokenSource? _playbackCancellation;
    private Task? _playbackTask;
    private bool _isDisposed;

    /// <summary>Initializes isolated replay with explicit safety, runtime, and delay boundaries.</summary>
    public RecordingReplayService(
        IRecordingReplaySafetyGate safetyGate,
        IIsolatedReplayRuntimeFactory runtimeFactory,
        IRecordingReplayDelayScheduler delayScheduler)
    {
        _safetyGate = safetyGate ?? throw new ArgumentNullException(nameof(safetyGate));
        _runtimeFactory = runtimeFactory ?? throw new ArgumentNullException(nameof(runtimeFactory));
        _delayScheduler = delayScheduler ?? throw new ArgumentNullException(nameof(delayScheduler));
    }

    /// <inheritdoc />
    public event Action<RecordingReplaySnapshot>? StatusChanged;

    /// <inheritdoc />
    public RecordingReplaySnapshot CurrentStatus
    {
        get
        {
            lock (_gate)
            {
                return CreateSnapshotLocked();
            }
        }
    }

    /// <inheritdoc />
    public RecordingReplayOperationResult Load(RecordingArtifact artifact)
    {
        ArgumentNullException.ThrowIfNull(artifact);
        RecordingReplaySnapshot snapshot;
        lock (_gate)
        {
            if (_state == RecordingReplayState.Playing)
            {
                return FailLocked(RecordingReplayFailureCode.InvalidState, "Pause or cancel replay before loading another artifact.");
            }

            _artifact = artifact;
            _runtime = _runtimeFactory.Create();
            ResetPositionLocked();
            _state = RecordingReplayState.Ready;
            ClearFailureLocked();
            snapshot = CreateSnapshotLocked();
        }

        NotifyStatusChanged(snapshot);
        return RecordingReplayOperationResult.Success();
    }

    /// <inheritdoc />
    public RecordingReplayOperationResult Play(double speed)
    {
        if (!IsSupportedSpeed(speed))
        {
            return RecordingReplayOperationResult.Failure(
                RecordingReplayFailureCode.InvalidSpeed,
                "Replay speed must be one of 0.25x, 0.5x, 1x, 2x, 4x, or 8x.");
        }

        var safety = _safetyGate.GetStatus();
        if (!safety.CanReplay)
        {
            return SetBlocked(safety.BlockingReason);
        }

        RecordingReplaySnapshot snapshot;
        var isIdempotent = false;
        lock (_gate)
        {
            if (_artifact is null || _runtime is null)
            {
                return FailLocked(RecordingReplayFailureCode.InvalidState, "Load a recording before starting replay.");
            }

            if (_state == RecordingReplayState.Playing)
            {
                _speed = speed;
                snapshot = CreateSnapshotLocked();
                isIdempotent = true;
            }
            else
            {
                if (_state == RecordingReplayState.Completed)
                {
                    ResetPositionLocked();
                }

                if (_state is RecordingReplayState.Idle or RecordingReplayState.Faulted)
                {
                    return FailLocked(RecordingReplayFailureCode.InvalidState, "Load a recording before starting replay.");
                }

                _speed = speed;
                _state = RecordingReplayState.Playing;
                ClearFailureLocked();
                _playbackCancellation?.Dispose();
                _playbackCancellation = new CancellationTokenSource();
                _playbackTask = PlayCoreAsync(_playbackCancellation.Token);
                snapshot = CreateSnapshotLocked();
            }
        }

        NotifyStatusChanged(snapshot);
        return RecordingReplayOperationResult.Success(isIdempotent);
    }

    /// <inheritdoc />
    public RecordingReplayOperationResult Pause()
    {
        RecordingReplaySnapshot snapshot;
        lock (_gate)
        {
            if (_state == RecordingReplayState.Paused)
            {
                return RecordingReplayOperationResult.Success(isIdempotent: true);
            }

            if (_state != RecordingReplayState.Playing)
            {
                return FailLocked(RecordingReplayFailureCode.InvalidState, "Only active replay can be paused.");
            }

            _state = RecordingReplayState.Paused;
            _playbackCancellation?.Cancel();
            snapshot = CreateSnapshotLocked();
        }

        NotifyStatusChanged(snapshot);
        return RecordingReplayOperationResult.Success();
    }

    /// <inheritdoc />
    public async Task<RecordingReplayOperationResult> StepAsync(CancellationToken cancellationToken = default)
    {
        var preparation = PrepareExclusiveOperation();
        if (!preparation.Succeeded) return preparation;

        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var result = ApplyNextEntry();
            await Task.CompletedTask;
            return result;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return RecordingReplayOperationResult.Failure(RecordingReplayFailureCode.Cancelled, "Replay step was cancelled.");
        }
    }

    /// <inheritdoc />
    public async Task<RecordingReplayOperationResult> SeekAsync(
        int position,
        CancellationToken cancellationToken = default)
    {
        var preparation = PrepareExclusiveOperation();
        if (!preparation.Succeeded) return preparation;

        RecordingArtifact artifact;
        RecordingReplaySnapshot initialSnapshot;
        lock (_gate)
        {
            artifact = _artifact!;
            if (position < 0 || position > artifact.Entries.Length)
            {
                return FailLocked(RecordingReplayFailureCode.InvalidPosition, "Replay position is outside the loaded journal.");
            }

            ResetPositionLocked();
            _state = RecordingReplayState.Paused;
            initialSnapshot = CreateSnapshotLocked();
        }

        NotifyStatusChanged(initialSnapshot);
        try
        {
            while (CurrentStatus.Position < position)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var safety = _safetyGate.GetStatus();
                if (!safety.CanReplay) return SetBlocked(safety.BlockingReason);
                var result = ApplyNextEntry();
                if (!result.Succeeded) return result;
                await Task.Yield();
            }

            return RecordingReplayOperationResult.Success();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return await CancelAsync(CancellationToken.None).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public async Task<RecordingReplayOperationResult> CancelAsync(CancellationToken cancellationToken = default)
    {
        Task? playbackTask;
        CancellationTokenSource? playbackCancellation;
        lock (_gate)
        {
            playbackTask = _playbackTask;
            playbackCancellation = _playbackCancellation;
        }

        if (playbackCancellation is not null)
        {
            await playbackCancellation.CancelAsync().ConfigureAwait(false);
        }

        if (playbackTask is not null)
        {
            try
            {
                await playbackTask.WaitAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return RecordingReplayOperationResult.Failure(RecordingReplayFailureCode.Cancelled, "Replay cancellation was cancelled.");
            }
        }

        RecordingReplaySnapshot snapshot;
        lock (_gate)
        {
            if (_artifact is null || _runtime is null)
            {
                return RecordingReplayOperationResult.Success(isIdempotent: true);
            }

            ResetPositionLocked();
            _state = RecordingReplayState.Ready;
            _lastFailureCode = RecordingReplayFailureCode.Cancelled;
            _lastFailureMessage = "Replay was cancelled and reset.";
            snapshot = CreateSnapshotLocked();
        }

        NotifyStatusChanged(snapshot);
        return RecordingReplayOperationResult.Success();
    }

    private async Task PlayCoreAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (true)
            {
                if (!TryPrepareNextDelay(out var delay, out var completedSnapshot))
                {
                    if (completedSnapshot is not null) NotifyStatusChanged(completedSnapshot);
                    return;
                }

                if (!EnsureReplayIsSafe()) return;

                await _delayScheduler.DelayAsync(delay, cancellationToken).ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();
                if (!EnsureReplayIsSafe()) return;

                var result = ApplyNextEntry(requirePlaying: true);
                if (!result.Succeeded) return;
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Pause and cancel own the externally visible target state.
        }
        catch (Exception exception)
        {
            RecordingReplaySnapshot snapshot;
            lock (_gate)
            {
                _state = RecordingReplayState.Faulted;
                _lastFailureCode = RecordingReplayFailureCode.InternalError;
                _lastFailureMessage = $"Isolated replay failed ({exception.GetType().Name}).";
                snapshot = CreateSnapshotLocked();
            }

            NotifyStatusChanged(snapshot);
        }
    }

    private bool TryPrepareNextDelay(
        out TimeSpan delay,
        out RecordingReplaySnapshot? completedSnapshot)
    {
        delay = TimeSpan.Zero;
        completedSnapshot = null;
        lock (_gate)
        {
            if (_state != RecordingReplayState.Playing || _artifact is null) return false;
            if (_position >= _artifact.Entries.Length)
            {
                CompleteLocked(out completedSnapshot);
                return false;
            }

            var entry = _artifact.Entries[_position];
            var unscaledDelay = entry.Elapsed - _elapsed;
            if (unscaledDelay < TimeSpan.Zero) unscaledDelay = TimeSpan.Zero;
            delay = TimeSpan.FromTicks((long)(unscaledDelay.Ticks / _speed));
            return true;
        }
    }

    private bool EnsureReplayIsSafe()
    {
        var safety = _safetyGate.GetStatus();
        if (safety.CanReplay) return true;

        SetBlocked(safety.BlockingReason);
        return false;
    }

    private RecordingReplayOperationResult ApplyNextEntry(bool requirePlaying = false)
    {
        RecordingReplaySnapshot snapshot;
        RecordingReplayOperationResult result;
        lock (_gate)
        {
            if (requirePlaying && _state != RecordingReplayState.Playing)
            {
                return RecordingReplayOperationResult.Success(isIdempotent: true);
            }

            if (_artifact is null || _runtime is null || _position >= _artifact.Entries.Length)
            {
                if (_artifact is not null && _position >= _artifact.Entries.Length)
                {
                    CompleteLocked(out snapshot);
                    result = RecordingReplayOperationResult.Success(isIdempotent: true);
                }
                else
                {
                    return FailLocked(RecordingReplayFailureCode.InvalidState, "Load a recording before replaying entries.");
                }
            }
            else
            {
                var entry = _artifact.Entries[_position];
                result = ApplyEntryLocked(entry);
                snapshot = CreateSnapshotLocked();
            }
        }

        NotifyStatusChanged(snapshot);
        return result;
    }

    private RecordingReplayOperationResult ApplyEntryLocked(RecordingEntry entry)
    {
        if (entry.ReplayApplicability == RecordingReplayApplicability.ReplayApplicable)
        {
            var applyResult = _runtime!.Apply(entry);
            if (!applyResult.Succeeded)
            {
                _state = RecordingReplayState.Faulted;
                _lastFailureCode = RecordingReplayFailureCode.ApplyFailed;
                _lastFailureMessage = applyResult.ErrorMessage ?? "The isolated runtime rejected the replay entry.";
                return RecordingReplayOperationResult.Failure(_lastFailureCode, _lastFailureMessage);
            }

            _appliedEntryCount++;
        }
        else
        {
            _skippedEntryCount++;
        }

        _position++;
        _elapsed = entry.Elapsed;
        _currentEntry = entry;
        if (_position >= _artifact!.Entries.Length)
        {
            _state = RecordingReplayState.Completed;
        }
        else if (_state != RecordingReplayState.Playing)
        {
            _state = RecordingReplayState.Paused;
        }

        ClearFailureLocked();
        return RecordingReplayOperationResult.Success();
    }

    private RecordingReplayOperationResult PrepareExclusiveOperation()
    {
        var safety = _safetyGate.GetStatus();
        if (!safety.CanReplay) return SetBlocked(safety.BlockingReason);

        lock (_gate)
        {
            if (_artifact is null || _runtime is null)
            {
                return FailLocked(RecordingReplayFailureCode.InvalidState, "Load a recording before replaying entries.");
            }

            if (_state == RecordingReplayState.Playing)
            {
                return FailLocked(RecordingReplayFailureCode.InvalidState, "Pause replay before stepping or seeking.");
            }

            return RecordingReplayOperationResult.Success();
        }
    }

    private RecordingReplayOperationResult SetBlocked(string? reason)
    {
        RecordingReplaySnapshot snapshot;
        lock (_gate)
        {
            _playbackCancellation?.Cancel();
            _state = RecordingReplayState.Blocked;
            _lastFailureCode = RecordingReplayFailureCode.LiveHardwareConnected;
            _lastFailureMessage = reason ?? "Live hardware is connected.";
            snapshot = CreateSnapshotLocked();
        }

        NotifyStatusChanged(snapshot);
        return RecordingReplayOperationResult.Failure(snapshot.LastFailureCode, snapshot.LastFailureMessage!);
    }

    private void ResetPositionLocked()
    {
        _runtime?.Reset();
        _position = 0;
        _appliedEntryCount = 0;
        _skippedEntryCount = 0;
        _elapsed = TimeSpan.Zero;
        _currentEntry = null;
        _playbackTask = null;
    }

    private void CompleteLocked(out RecordingReplaySnapshot snapshot)
    {
        _state = RecordingReplayState.Completed;
        ClearFailureLocked();
        snapshot = CreateSnapshotLocked();
    }

    private RecordingReplayOperationResult FailLocked(RecordingReplayFailureCode code, string message)
    {
        _lastFailureCode = code;
        _lastFailureMessage = message;
        return RecordingReplayOperationResult.Failure(code, message);
    }

    private void ClearFailureLocked()
    {
        _lastFailureCode = RecordingReplayFailureCode.None;
        _lastFailureMessage = null;
    }

    private RecordingReplaySnapshot CreateSnapshotLocked() =>
        new(
            _state,
            _artifact?.Metadata.SessionId,
            _position,
            _artifact?.Entries.Length ?? 0,
            _appliedEntryCount,
            _skippedEntryCount,
            _elapsed,
            _speed,
            _currentEntry,
            _lastFailureCode,
            _lastFailureMessage);

    private static bool IsSupportedSpeed(double speed) =>
        SupportedSpeeds.Contains(speed);

    private void NotifyStatusChanged(RecordingReplaySnapshot snapshot)
    {
        var handlers = StatusChanged;
        if (handlers is null) return;
        foreach (var handler in handlers.GetInvocationList().Cast<Action<RecordingReplaySnapshot>>())
        {
            try
            {
                handler(snapshot);
            }
            catch
            {
                // Replay observers cannot terminate the isolated scheduler or later observers.
            }
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_isDisposed) return;
        _isDisposed = true;
        await CancelAsync().ConfigureAwait(false);
        lock (_gate)
        {
            _playbackCancellation?.Dispose();
        }
    }
}
