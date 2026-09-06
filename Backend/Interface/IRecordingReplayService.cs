// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Backend.Interface;

using Common.Recording;

/// <summary>Exposes immutable isolated replay status to platform-neutral observers.</summary>
public interface IRecordingReplayStatusSource
{
    /// <summary>Gets the latest replay snapshot.</summary>
    RecordingReplaySnapshot CurrentStatus { get; }

    /// <summary>Occurs when replay state or position changes.</summary>
    event Action<RecordingReplaySnapshot>? StatusChanged;
}

/// <summary>Controls deterministic replay against a dedicated in-memory runtime.</summary>
public interface IRecordingReplayService : IRecordingReplayStatusSource, IAsyncDisposable
{
    /// <summary>Loads an immutable artifact into a fresh isolated runtime.</summary>
    RecordingReplayOperationResult Load(RecordingArtifact artifact);

    /// <summary>Starts or resumes non-blocking playback at the selected speed.</summary>
    RecordingReplayOperationResult Play(double speed);

    /// <summary>Pauses playback before the next entry is applied.</summary>
    RecordingReplayOperationResult Pause();

    /// <summary>Applies or skips exactly one entry without a replay delay.</summary>
    Task<RecordingReplayOperationResult> StepAsync(CancellationToken cancellationToken = default);

    /// <summary>Resets and deterministically reapplies entries up to an absolute journal position.</summary>
    Task<RecordingReplayOperationResult> SeekAsync(int position, CancellationToken cancellationToken = default);

    /// <summary>Cancels playback and resets the loaded artifact to its ready position.</summary>
    Task<RecordingReplayOperationResult> CancelAsync(CancellationToken cancellationToken = default);
}

/// <summary>Provides a fail-closed read-only gate for live hardware state.</summary>
public interface IRecordingReplaySafetyGate
{
    /// <summary>Gets whether isolated replay may execute at this instant.</summary>
    RecordingReplaySafetyStatus GetStatus();
}

/// <summary>Creates a dependency-free isolated runtime for one replay artifact.</summary>
public interface IIsolatedReplayRuntimeFactory
{
    /// <summary>Creates a fresh isolated runtime without access to the production service provider.</summary>
    IIsolatedReplayRuntime Create();
}

/// <summary>Applies replay-safe entries only to in-memory state.</summary>
public interface IIsolatedReplayRuntime
{
    /// <summary>Gets the latest immutable isolated runtime snapshot.</summary>
    IsolatedReplayRuntimeSnapshot Current { get; }

    /// <summary>Applies one allow-listed replay entry to isolated state.</summary>
    IsolatedReplayApplyResult Apply(RecordingEntry entry);

    /// <summary>Clears all projected replay state.</summary>
    void Reset();
}

/// <summary>Schedules cancellation-aware replay delays without coupling replay to wall-clock time.</summary>
public interface IRecordingReplayDelayScheduler
{
    /// <summary>Waits for a scaled replay interval.</summary>
    Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken);
}