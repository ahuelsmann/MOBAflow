// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Backend.Interface;

using Common.Recording;

/// <summary>
/// Exposes immutable recording status without coupling the service to the EventBus or a UI framework.
/// </summary>
public interface IRecordingStatusSource
{
    /// <summary>Gets the latest immutable status snapshot.</summary>
    RecordingSessionSnapshot CurrentStatus { get; }

    /// <summary>Occurs when lifecycle or journal status changes.</summary>
    event Action<RecordingSessionSnapshot>? StatusChanged;
}

/// <summary>
/// Owns one platform-neutral recording session and its bounded ingestion pipeline.
/// </summary>
public interface IRecordingSessionService : IRecordingStatusSource, IAsyncDisposable
{
    /// <summary>Gets the immutable completed or imported artifact, when available.</summary>
    RecordingArtifact? CurrentArtifact { get; }

    /// <summary>Starts a new session when no session is active.</summary>
    RecordingOperationResult Start(RecordingSessionStartRequest request);

    /// <summary>Pauses normal producer capture while retaining annotations.</summary>
    RecordingOperationResult Pause();

    /// <summary>Resumes normal producer capture.</summary>
    RecordingOperationResult Resume();

    /// <summary>Adds an ordered user marker while recording or paused.</summary>
    RecordingOperationResult AddMarker(string text);

    /// <summary>Adds an ordered operator note while recording or paused.</summary>
    RecordingOperationResult AddNote(string text);

    /// <summary>Attempts to submit a mapped entry without blocking the producer.</summary>
    RecordingSubmissionResult TryRecord(RecordingEntryProjection projection);

    /// <summary>Stops once, drains accepted entries within the configured timeout, and returns an immutable artifact.</summary>
    Task<RecordingStopResult> StopAsync(CancellationToken cancellationToken = default);

    /// <summary>Loads an immutable artifact as a read-only completed session.</summary>
    RecordingOperationResult Import(RecordingArtifact artifact);
}