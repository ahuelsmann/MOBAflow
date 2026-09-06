// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Common.Recording;

/// <summary>Identifies the lifecycle state of an isolated recording replay.</summary>
public enum RecordingReplayState
{
    Idle,
    Ready,
    Playing,
    Paused,
    Completed,
    Blocked,
    Faulted
}

/// <summary>Identifies a structured isolated replay failure.</summary>
public enum RecordingReplayFailureCode
{
    None,
    InvalidState,
    InvalidPosition,
    InvalidSpeed,
    LiveHardwareConnected,
    Cancelled,
    ApplyFailed,
    InternalError
}

/// <summary>Describes the outcome of one isolated replay control operation.</summary>
public sealed record RecordingReplayOperationResult(
    bool Succeeded,
    bool IsIdempotent,
    RecordingReplayFailureCode FailureCode,
    string Message)
{
    /// <summary>Creates a successful replay operation result.</summary>
    public static RecordingReplayOperationResult Success(bool isIdempotent = false) =>
        new(true, isIdempotent, RecordingReplayFailureCode.None, string.Empty);

    /// <summary>Creates a failed replay operation result.</summary>
    public static RecordingReplayOperationResult Failure(RecordingReplayFailureCode code, string message) =>
        new(false, false, code, message);
}

/// <summary>Provides the current immutable position and state of isolated replay.</summary>
public sealed record RecordingReplaySnapshot(
    RecordingReplayState State,
    Guid? SessionId,
    int Position,
    int TotalEntryCount,
    int AppliedEntryCount,
    int SkippedEntryCount,
    TimeSpan Elapsed,
    double Speed,
    RecordingEntry? CurrentEntry,
    RecordingReplayFailureCode LastFailureCode,
    string? LastFailureMessage)
{
    /// <summary>Gets whether this snapshot represents an artifact loaded into an isolated runtime.</summary>
    public bool IsArtifactLoaded => SessionId.HasValue;
}

/// <summary>Describes whether isolated replay may currently execute.</summary>
public sealed record RecordingReplaySafetyStatus(bool CanReplay, string? BlockingReason);

/// <summary>Describes the latest state projected inside the dependency-free replay runtime.</summary>
public sealed record IsolatedReplayRuntimeSnapshot(
    int AppliedEntryCount,
    long? LastAppliedSequence,
    string? LastAppliedTypeKey);

/// <summary>Describes the outcome of applying one allow-listed entry inside the isolated runtime.</summary>
public sealed record IsolatedReplayApplyResult(bool Succeeded, string? ErrorMessage)
{
    /// <summary>Creates a successful application result.</summary>
    public static IsolatedReplayApplyResult Success() => new(true, null);

    /// <summary>Creates a failed application result.</summary>
    public static IsolatedReplayApplyResult Failure(string message) => new(false, message);
}