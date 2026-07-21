// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Common.Recording;

using System.Collections.Immutable;
using System.Text.Json;

/// <summary>
/// Identifies the lifecycle state of a recording session.
/// </summary>
public enum RecordingSessionState
{
    Idle,
    Recording,
    Paused,
    Stopping,
    Completed,
    Faulted
}

/// <summary>
/// Identifies a structured recording operation failure.
/// </summary>
public enum RecordingFailureCode
{
    None,
    InvalidState,
    InvalidRequest,
    LimitReached,
    Cancelled,
    DrainTimeout,
    InternalError
}

/// <summary>
/// Describes the outcome of a recording control operation.
/// </summary>
public sealed record RecordingOperationResult(
    bool Succeeded,
    bool IsIdempotent,
    RecordingFailureCode FailureCode,
    string Message)
{
    /// <summary>Creates a successful result.</summary>
    public static RecordingOperationResult Success(bool isIdempotent = false) =>
        new(true, isIdempotent, RecordingFailureCode.None, string.Empty);

    /// <summary>Creates a failed result with a stable failure code.</summary>
    public static RecordingOperationResult Failure(RecordingFailureCode code, string message) =>
        new(false, false, code, message);
}

/// <summary>
/// Describes a non-blocking producer submission.
/// </summary>
public enum RecordingSubmissionResult
{
    Accepted,
    IgnoredWhilePaused,
    DroppedCapacity,
    RejectedNotRecording,
    RejectedLimit,
    InvalidProjection
}

/// <summary>
/// Configures bounded recording ingestion and session limits.
/// </summary>
public sealed class RecorderOptions
{
    /// <summary>Default bounded-channel capacity.</summary>
    public const int DefaultPendingCapacity = 10_000;

    /// <summary>Largest accepted bounded-channel capacity.</summary>
    public const int MaximumPendingCapacity = 100_000;

    /// <summary>Default time allowed for stop/drain.</summary>
    public static readonly TimeSpan DefaultStopDrainTimeout = TimeSpan.FromSeconds(5);

    /// <summary>Largest accepted stop/drain timeout.</summary>
    public static readonly TimeSpan MaximumStopDrainTimeout = TimeSpan.FromMinutes(1);

    /// <summary>Initializes validated recorder limits.</summary>
    public RecorderOptions(
        int pendingCapacity = DefaultPendingCapacity,
        int entryLimit = RecordingFormat.DefaultMaxEntries,
        long estimatedPayloadByteLimit = RecordingFormat.DefaultMaxArtifactBytes,
        TimeSpan? stopDrainTimeout = null)
    {
        if (pendingCapacity is < 1 or > MaximumPendingCapacity)
        {
            throw new ArgumentOutOfRangeException(nameof(pendingCapacity));
        }

        if (entryLimit is < 4 or > RecordingFormat.DefaultMaxEntries)
        {
            throw new ArgumentOutOfRangeException(nameof(entryLimit));
        }

        if (estimatedPayloadByteLimit is < 1 or > RecordingFormat.DefaultMaxArtifactBytes)
        {
            throw new ArgumentOutOfRangeException(nameof(estimatedPayloadByteLimit));
        }

        var resolvedTimeout = stopDrainTimeout ?? DefaultStopDrainTimeout;
        if (resolvedTimeout < TimeSpan.Zero || resolvedTimeout > MaximumStopDrainTimeout)
        {
            throw new ArgumentOutOfRangeException(nameof(stopDrainTimeout));
        }

        PendingCapacity = pendingCapacity;
        EntryLimit = entryLimit;
        EstimatedPayloadByteLimit = estimatedPayloadByteLimit;
        StopDrainTimeout = resolvedTimeout;
    }

    /// <summary>Gets the maximum number of accepted entries awaiting the single consumer.</summary>
    public int PendingCapacity { get; }

    /// <summary>Gets the maximum number of entries in a completed artifact.</summary>
    public int EntryLimit { get; }

    /// <summary>Gets the maximum estimated UTF-8 payload bytes accepted for a session.</summary>
    public long EstimatedPayloadByteLimit { get; }

    /// <summary>Gets the maximum time allowed for a normal stop to drain accepted entries.</summary>
    public TimeSpan StopDrainTimeout { get; }
}

/// <summary>
/// Supplies the immutable metadata needed to start a recording session.
/// </summary>
/// <param name="Name">User-visible session name.</param>
/// <param name="SourceApplicationVersion">Application version that produced the session.</param>
/// <param name="Project">Optional source-project identity.</param>
public sealed record RecordingSessionStartRequest(
    string Name,
    string SourceApplicationVersion,
    RecordingProjectIdentity? Project = null);

/// <summary>
/// Contains a mapper-owned, allow-listed entry before sequence and time assignment.
/// </summary>
public sealed class RecordingEntryProjection
{
    /// <summary>Initializes a safe recording projection.</summary>
    public RecordingEntryProjection(
        string category,
        string source,
        string typeKey,
        string severity,
        Guid? correlationId,
        IEnumerable<RecordingEntityReference>? entityReferences,
        JsonElement payload,
        string displayText,
        RecordingReplayApplicability replayApplicability)
    {
        Category = category ?? throw new ArgumentNullException(nameof(category));
        Source = source ?? throw new ArgumentNullException(nameof(source));
        TypeKey = typeKey ?? throw new ArgumentNullException(nameof(typeKey));
        Severity = severity ?? throw new ArgumentNullException(nameof(severity));
        CorrelationId = correlationId;
        EntityReferences = (entityReferences ?? []).ToImmutableArray();
        Payload = payload.Clone();
        DisplayText = displayText ?? throw new ArgumentNullException(nameof(displayText));
        ReplayApplicability = replayApplicability;
    }

    /// <summary>Gets the stable event category.</summary>
    public string Category { get; }

    /// <summary>Gets the stable producer identifier.</summary>
    public string Source { get; }

    /// <summary>Gets the stable allow-listed type key.</summary>
    public string TypeKey { get; }

    /// <summary>Gets the stable severity key.</summary>
    public string Severity { get; }

    /// <summary>Gets the optional operation correlation identifier.</summary>
    public Guid? CorrelationId { get; }

    /// <summary>Gets immutable domain entity references.</summary>
    public ImmutableArray<RecordingEntityReference> EntityReferences { get; }

    /// <summary>Gets the mapper-owned allow-listed payload.</summary>
    public JsonElement Payload { get; }

    /// <summary>Gets sanitized text for display and filtering.</summary>
    public string DisplayText { get; }

    /// <summary>Gets whether isolated replay may apply this entry.</summary>
    public RecordingReplayApplicability ReplayApplicability { get; }
}

/// <summary>
/// Provides an immutable status snapshot for UI-independent observers.
/// </summary>
public sealed record RecordingSessionSnapshot(
    RecordingSessionState State,
    Guid? SessionId,
    string? SessionName,
    int EntryCount,
    int PendingEntryCount,
    long DroppedEntryCount,
    long EstimatedPayloadBytes,
    bool IsLimitReached,
    RecordingFailureCode LastFailureCode,
    string? LastFailureMessage);

/// <summary>
/// Returns the immutable artifact produced by a stop operation.
/// </summary>
public sealed record RecordingStopResult(
    RecordingOperationResult Operation,
    RecordingArtifact? Artifact);