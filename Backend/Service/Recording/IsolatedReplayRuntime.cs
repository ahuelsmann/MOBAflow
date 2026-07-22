// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Backend.Service.Recording;

using Interface;

using Moba.Common.Recording;

using System.Collections.Frozen;
using System.Text.Json;

/// <summary>
/// Projects allow-listed recording payloads into private in-memory state and owns no live-effect dependency.
/// </summary>
public sealed class IsolatedReplayRuntime : IIsolatedReplayRuntime
{
    private static readonly FrozenSet<string> SupportedTypeKeys = new[]
    {
        "z21.connection.established",
        "z21.connection.lost",
        "z21.track-power.changed",
        "z21.xbus-status.changed",
        "z21.system-state.changed",
        "z21.feedback.activated",
        "z21.signal-aspect.changed",
        "z21.switch-position.changed",
        "runtime.state.changed",
        "journey.transition"
    }.ToFrozenSet(StringComparer.Ordinal);

    private readonly Dictionary<string, JsonElement> _latestPayloads = new(StringComparer.Ordinal);
    private int _appliedEntryCount;
    private long? _lastAppliedSequence;
    private string? _lastAppliedTypeKey;

    /// <inheritdoc />
    public IsolatedReplayRuntimeSnapshot Current =>
        new(_appliedEntryCount, _lastAppliedSequence, _lastAppliedTypeKey);

    /// <inheritdoc />
    public IsolatedReplayApplyResult Apply(RecordingEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        if (entry.ReplayApplicability != RecordingReplayApplicability.ReplayApplicable)
        {
            return IsolatedReplayApplyResult.Failure("Display-only entries cannot be applied to isolated runtime state.");
        }

        if (entry.Payload.ValueKind != JsonValueKind.Object)
        {
            return IsolatedReplayApplyResult.Failure("Replay payload must be an object.");
        }

        if (!SupportedTypeKeys.Contains(entry.TypeKey))
        {
            return IsolatedReplayApplyResult.Failure($"Replay type '{entry.TypeKey}' is not allow-listed.");
        }

        _latestPayloads[entry.TypeKey] = entry.Payload.Clone();
        _appliedEntryCount++;
        _lastAppliedSequence = entry.Sequence;
        _lastAppliedTypeKey = entry.TypeKey;
        return IsolatedReplayApplyResult.Success();
    }

    /// <inheritdoc />
    public void Reset()
    {
        _latestPayloads.Clear();
        _appliedEntryCount = 0;
        _lastAppliedSequence = null;
        _lastAppliedTypeKey = null;
    }
}

/// <summary>Constructs isolated runtimes directly, without a production DI scope.</summary>
public sealed class IsolatedReplayRuntimeFactory : IIsolatedReplayRuntimeFactory
{
    /// <inheritdoc />
    public IIsolatedReplayRuntime Create() => new IsolatedReplayRuntime();
}

/// <summary>Uses the injected <see cref="TimeProvider"/> for replay waits.</summary>
public sealed class TimeProviderRecordingReplayDelayScheduler : IRecordingReplayDelayScheduler
{
    private readonly TimeProvider _timeProvider;

    /// <summary>Initializes a time-provider-backed delay scheduler.</summary>
    public TimeProviderRecordingReplayDelayScheduler(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    /// <inheritdoc />
    public Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken) =>
        delay <= TimeSpan.Zero
            ? Task.CompletedTask
            : Task.Delay(delay, _timeProvider, cancellationToken);
}

/// <summary>Blocks replay whenever the production runtime reports an active Z21 connection.</summary>
public sealed class RecordingReplaySafetyGate : IRecordingReplaySafetyGate
{
    private readonly IRuntimeSnapshotProvider _runtimeSnapshotProvider;

    /// <summary>Initializes a read-only production runtime safety gate.</summary>
    public RecordingReplaySafetyGate(IRuntimeSnapshotProvider runtimeSnapshotProvider)
    {
        _runtimeSnapshotProvider = runtimeSnapshotProvider ?? throw new ArgumentNullException(nameof(runtimeSnapshotProvider));
    }

    /// <inheritdoc />
    public RecordingReplaySafetyStatus GetStatus() => _runtimeSnapshotProvider.Current.IsConnected
        ? new RecordingReplaySafetyStatus(false, "Disconnect the live Z21 runtime before starting isolated replay.")
        : new RecordingReplaySafetyStatus(true, null);
}