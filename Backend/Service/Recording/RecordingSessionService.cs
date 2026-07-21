// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Backend.Service.Recording;

using Interface;
using Moba.Common.Recording;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;

/// <summary>
/// Produces deterministic recording artifacts through a bounded, single-consumer ingestion pipeline.
/// </summary>
public sealed class RecordingSessionService : IRecordingSessionService
{
    private const string RecorderCategory = "recorder";
    private const string RecorderSource = "recording-session";
    private const string InformationSeverity = "information";
    private const string WarningSeverity = "warning";
    private const string ErrorSeverity = "error";
    private const int ReservedTerminalEntryCount = 3;

    private readonly object _gate = new();
    private readonly TimeProvider _timeProvider;
    private readonly RecorderOptions _options;
    private readonly List<RecordingEntry> _entries = [];
    private readonly Dictionary<long, int> _pendingEntries = [];

    private RecordingSessionState _state;
    private Guid? _sessionId;
    private string? _sessionName;
    private string? _sourceApplicationVersion;
    private RecordingProjectIdentity? _project;
    private DateTimeOffset _startedUtc;
    private DateTimeOffset? _pausedUtc;
    private TimeSpan _lastElapsed;
    private long _sequence;
    private long _totalDroppedEntryCount;
    private long _pendingDroppedEntryCount;
    private long _firstPendingDroppedSequence;
    private long _lastPendingDroppedSequence;
    private long _estimatedPayloadBytes;
    private bool _limitReached;
    private RecordingFailureCode _lastFailureCode;
    private string? _lastFailureMessage;
    private Channel<RecordingEntry>? _channel;
    private CancellationTokenSource? _consumerCancellation;
    private Task? _consumerTask;
    private Task<RecordingStopResult>? _stopTask;
    private RecordingArtifact? _artifact;

    /// <summary>Initializes the session service with an injectable clock and validated limits.</summary>
    public RecordingSessionService(TimeProvider timeProvider, RecorderOptions? options = null)
    {
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _options = options ?? new RecorderOptions();
    }

    /// <inheritdoc />
    public event Action<RecordingSessionSnapshot>? StatusChanged;

    /// <inheritdoc />
    public RecordingSessionSnapshot CurrentStatus
    {
        get
        {
            lock (_gate)
            {
                return CreateStatusLocked();
            }
        }
    }

    /// <inheritdoc />
    public RecordingArtifact? CurrentArtifact
    {
        get
        {
            lock (_gate)
            {
                return _artifact;
            }
        }
    }

    /// <inheritdoc />
    public RecordingOperationResult Start(RecordingSessionStartRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var validation = ValidateStartRequest(request);
        if (validation is not null) return validation;

        RecordingSessionSnapshot snapshot;
        lock (_gate)
        {
            if (_state is RecordingSessionState.Recording or RecordingSessionState.Paused or RecordingSessionState.Stopping)
            {
                return FailLocked(RecordingFailureCode.InvalidState, "A recording session is already active.");
            }

            ResetLocked();
            _sessionId = Guid.NewGuid();
            _sessionName = request.Name.Trim();
            _sourceApplicationVersion = request.SourceApplicationVersion.Trim();
            _project = request.Project;
            _startedUtc = _timeProvider.GetUtcNow();
            _state = RecordingSessionState.Recording;

            var channel = Channel.CreateBounded<RecordingEntry>(new BoundedChannelOptions(_options.PendingCapacity)
            {
                AllowSynchronousContinuations = false,
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = true,
                SingleWriter = false
            });
            _channel = channel;
            _consumerCancellation = new CancellationTokenSource();

            _entries.Add(CreateEntryLocked(
                "recorder.started",
                InformationSeverity,
                JsonSerializer.SerializeToElement(new { name = _sessionName }),
                $"Recording started: {_sessionName}",
                timestampUtc: _startedUtc));

            _consumerTask = Task.Run(() => ConsumeAsync(channel.Reader, _consumerCancellation.Token));
            snapshot = CreateStatusLocked();
        }

        NotifyStatusChanged(snapshot);
        return RecordingOperationResult.Success();
    }

    /// <inheritdoc />
    public RecordingOperationResult Pause()
    {
        RecordingSessionSnapshot? snapshot = null;
        RecordingOperationResult result;
        lock (_gate)
        {
            if (_state == RecordingSessionState.Paused) return RecordingOperationResult.Success(isIdempotent: true);
            if (_state != RecordingSessionState.Recording)
            {
                return FailLocked(RecordingFailureCode.InvalidState, "Only an active recording can be paused.");
            }

            var now = _timeProvider.GetUtcNow();
            result = AppendControlLocked(
                "recorder.paused",
                InformationSeverity,
                JsonSerializer.SerializeToElement(new { pausedUtc = now }),
                "Recording paused",
                estimatedPayloadBytes: 0,
                timestampUtc: now);
            if (result.Succeeded)
            {
                _pausedUtc = now;
                _state = RecordingSessionState.Paused;
                snapshot = CreateStatusLocked();
            }
        }

        if (snapshot is not null) NotifyStatusChanged(snapshot);
        return result;
    }

    /// <inheritdoc />
    public RecordingOperationResult Resume()
    {
        RecordingSessionSnapshot? snapshot = null;
        RecordingOperationResult result;
        lock (_gate)
        {
            if (_state == RecordingSessionState.Recording) return RecordingOperationResult.Success(isIdempotent: true);
            if (_state != RecordingSessionState.Paused || _pausedUtc is null)
            {
                return FailLocked(RecordingFailureCode.InvalidState, "Only a paused recording can be resumed.");
            }

            var now = _timeProvider.GetUtcNow();
            var pauseDuration = now - _pausedUtc.Value;
            if (pauseDuration < TimeSpan.Zero) pauseDuration = TimeSpan.Zero;
            result = AppendControlLocked(
                "recorder.resumed",
                InformationSeverity,
                JsonSerializer.SerializeToElement(new
                {
                    pausedUtc = _pausedUtc.Value,
                    resumedUtc = now,
                    durationTicks = pauseDuration.Ticks
                }),
                "Recording resumed",
                estimatedPayloadBytes: 0,
                timestampUtc: now);
            if (result.Succeeded)
            {
                _pausedUtc = null;
                _state = RecordingSessionState.Recording;
                snapshot = CreateStatusLocked();
            }
        }

        if (snapshot is not null) NotifyStatusChanged(snapshot);
        return result;
    }

    /// <inheritdoc />
    public RecordingOperationResult AddMarker(string text) => AddAnnotation("recorder.marker", "Marker", text);

    /// <inheritdoc />
    public RecordingOperationResult AddNote(string text) => AddAnnotation("recorder.note", "Note", text);

    /// <inheritdoc />
    public RecordingSubmissionResult TryRecord(RecordingEntryProjection projection)
    {
        ArgumentNullException.ThrowIfNull(projection);

        var estimatedPayloadBytes = ValidateProjection(projection);
        if (estimatedPayloadBytes < 0) return RecordingSubmissionResult.InvalidProjection;

        RecordingSessionSnapshot? snapshot = null;
        RecordingSubmissionResult result;
        lock (_gate)
        {
            if (_state == RecordingSessionState.Paused) return RecordingSubmissionResult.IgnoredWhilePaused;
            if (_state != RecordingSessionState.Recording) return RecordingSubmissionResult.RejectedNotRecording;
            if (_limitReached) return RecordingSubmissionResult.RejectedLimit;

            if (!TryEmitRecoveredGapLocked())
            {
                snapshot = CreateStatusLocked();
                result = RecordingSubmissionResult.RejectedLimit;
            }
            else if (!CanAcceptEntryLocked(estimatedPayloadBytes))
            {
                TriggerLimitLocked(
                    _estimatedPayloadBytes + estimatedPayloadBytes > _options.EstimatedPayloadByteLimit
                        ? "estimated-payload-bytes"
                        : "entry-count");
                snapshot = CreateStatusLocked();
                result = RecordingSubmissionResult.RejectedLimit;
            }
            else
            {
                var entry = CreateEntryLocked(
                    projection.TypeKey,
                    projection.Severity,
                    projection.Payload,
                    projection.DisplayText,
                    projection.Category,
                    projection.Source,
                    projection.CorrelationId,
                    projection.EntityReferences,
                    projection.ReplayApplicability);

                if (_channel?.Writer.TryWrite(entry) == true)
                {
                    _pendingEntries.Add(entry.Sequence, estimatedPayloadBytes);
                    _estimatedPayloadBytes += estimatedPayloadBytes;
                    result = RecordingSubmissionResult.Accepted;
                }
                else
                {
                    TrackDroppedSequenceLocked(entry.Sequence);
                    result = RecordingSubmissionResult.DroppedCapacity;
                    snapshot = CreateStatusLocked();
                }
            }
        }

        if (snapshot is not null) NotifyStatusChanged(snapshot);
        return result;
    }

    /// <inheritdoc />
    public Task<RecordingStopResult> StopAsync(CancellationToken cancellationToken = default)
    {
        RecordingSessionSnapshot? snapshot = null;
        Task<RecordingStopResult> result;
        lock (_gate)
        {
            if ((_state is RecordingSessionState.Completed or RecordingSessionState.Faulted) && _artifact is not null)
            {
                var operation = _state == RecordingSessionState.Faulted
                    ? RecordingOperationResult.Failure(_lastFailureCode, _lastFailureMessage ?? "Recording failed.")
                    : RecordingOperationResult.Success(isIdempotent: true);
                return Task.FromResult(new RecordingStopResult(operation, _artifact));
            }

            if (_state == RecordingSessionState.Stopping && _stopTask is not null) return _stopTask;
            if (_state is not RecordingSessionState.Recording and not RecordingSessionState.Paused)
            {
                return Task.FromResult(new RecordingStopResult(
                    FailLocked(RecordingFailureCode.InvalidState, "There is no active recording to stop."),
                    null));
            }

            _state = RecordingSessionState.Stopping;
            var consumerTask = _consumerTask!;
            var consumerWasCompletedAtStop = consumerTask.IsCompleted;
            _channel!.Writer.TryComplete();
            _stopTask = StopCoreAsync(
                consumerTask,
                _consumerCancellation!,
                consumerWasCompletedAtStop,
                cancellationToken);
            result = _stopTask;
            snapshot = CreateStatusLocked();
        }

        NotifyStatusChanged(snapshot);
        return result;
    }

    /// <inheritdoc />
    public RecordingOperationResult Import(RecordingArtifact artifact)
    {
        ArgumentNullException.ThrowIfNull(artifact);

        RecordingSessionSnapshot snapshot;
        lock (_gate)
        {
            if (_state is RecordingSessionState.Recording or RecordingSessionState.Paused or RecordingSessionState.Stopping)
            {
                return FailLocked(RecordingFailureCode.InvalidState, "An active recording must be stopped before import.");
            }

            ResetLocked();
            _artifact = artifact;
            _state = RecordingSessionState.Completed;
            _sessionId = artifact.Metadata.SessionId;
            _sessionName = artifact.Metadata.Name;
            _sourceApplicationVersion = artifact.SourceApplicationVersion;
            _project = artifact.Project;
            _startedUtc = artifact.Metadata.StartedUtc;
            _sequence = artifact.Summary.LastSequence;
            _entries.AddRange(artifact.Entries);
            snapshot = CreateStatusLocked();
        }

        NotifyStatusChanged(snapshot);
        return RecordingOperationResult.Success();
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        RecordingSessionState state;
        lock (_gate)
        {
            state = _state;
        }

        if (state is RecordingSessionState.Recording or RecordingSessionState.Paused or RecordingSessionState.Stopping)
        {
            await StopAsync().ConfigureAwait(false);
        }

        lock (_gate)
        {
            _consumerCancellation?.Dispose();
        }
    }

    private RecordingOperationResult AddAnnotation(string typeKey, string label, string text)
    {
        if (string.IsNullOrWhiteSpace(text) || text.Length > RecordingFormat.MaxDisplayTextLength)
        {
            return RecordingOperationResult.Failure(
                RecordingFailureCode.InvalidRequest,
                $"{label} text must contain between 1 and {RecordingFormat.MaxDisplayTextLength} characters.");
        }

        var trimmedText = text.Trim();
        var payload = JsonSerializer.SerializeToElement(new { text = trimmedText });
        var payloadBytes = Encoding.UTF8.GetByteCount(payload.GetRawText());
        RecordingSessionSnapshot? snapshot = null;
        RecordingOperationResult result;
        lock (_gate)
        {
            if (_state is not RecordingSessionState.Recording and not RecordingSessionState.Paused)
            {
                return FailLocked(RecordingFailureCode.InvalidState, $"A {label.ToLowerInvariant()} requires an active recording.");
            }

            result = AppendControlLocked(
                typeKey,
                InformationSeverity,
                payload,
                $"{label}: {trimmedText}",
                payloadBytes);
            if (result.Succeeded || result.FailureCode == RecordingFailureCode.LimitReached)
            {
                snapshot = CreateStatusLocked();
            }
        }

        if (snapshot is not null) NotifyStatusChanged(snapshot);
        return result;
    }

    private RecordingOperationResult AppendControlLocked(
        string typeKey,
        string severity,
        JsonElement payload,
        string displayText,
        int estimatedPayloadBytes,
        DateTimeOffset? timestampUtc = null)
    {
        if (_limitReached)
        {
            return FailLocked(RecordingFailureCode.LimitReached, "The recording session limit has been reached.");
        }

        if (!TryEmitRecoveredGapLocked())
        {
            return FailLocked(RecordingFailureCode.LimitReached, "The recording session limit has been reached.");
        }

        if (!CanAcceptEntryLocked(estimatedPayloadBytes))
        {
            TriggerLimitLocked(
                _estimatedPayloadBytes + estimatedPayloadBytes > _options.EstimatedPayloadByteLimit
                    ? "estimated-payload-bytes"
                    : "entry-count");
            return FailLocked(RecordingFailureCode.LimitReached, "The recording session limit has been reached.");
        }

        _entries.Add(CreateEntryLocked(typeKey, severity, payload, displayText, timestampUtc: timestampUtc));
        _estimatedPayloadBytes += estimatedPayloadBytes;
        return RecordingOperationResult.Success();
    }

    private async Task ConsumeAsync(ChannelReader<RecordingEntry> reader, CancellationToken cancellationToken)
    {
        try
        {
            while (await reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
            {
                var changed = false;
                while (reader.TryRead(out var entry))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    lock (_gate)
                    {
                        if (cancellationToken.IsCancellationRequested || _artifact is not null) break;
                        _pendingEntries.Remove(entry.Sequence);
                        _entries.Add(entry);
                    }

                    changed = true;
                }

                if (changed && !cancellationToken.IsCancellationRequested) NotifyStatusChanged(CurrentStatus);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            FinalizeUnexpectedConsumerFault(exception);
        }
    }

    private async Task<RecordingStopResult> StopCoreAsync(
        Task consumerTask,
        CancellationTokenSource consumerCancellation,
        bool consumerWasCompletedAtStop,
        CancellationToken cancellationToken)
    {
        await Task.Yield();

        RecordingFailureCode stopFailure = RecordingFailureCode.None;
        string? stopFailureMessage = null;
        if (cancellationToken.IsCancellationRequested)
        {
            stopFailure = RecordingFailureCode.Cancelled;
            stopFailureMessage = "Recording stop was cancelled before the accepted entries drained.";
        }
        else if (_options.StopDrainTimeout == TimeSpan.Zero && !consumerWasCompletedAtStop)
        {
            stopFailure = RecordingFailureCode.DrainTimeout;
            stopFailureMessage = "Recording entries did not drain within the zero stop timeout.";
        }
        else
        {
            try
            {
                await consumerTask.WaitAsync(_options.StopDrainTimeout, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                stopFailure = RecordingFailureCode.Cancelled;
                stopFailureMessage = "Recording stop was cancelled before the accepted entries drained.";
            }
            catch (TimeoutException)
            {
                stopFailure = RecordingFailureCode.DrainTimeout;
                stopFailureMessage = $"Recording entries did not drain within {_options.StopDrainTimeout}.";
            }
        }

        if (stopFailure != RecordingFailureCode.None)
        {
            consumerCancellation.Cancel();
        }

        RecordingStopResult result;
        RecordingSessionSnapshot snapshot;
        lock (_gate)
        {
            if (_artifact is not null)
            {
                return new RecordingStopResult(
                    _state == RecordingSessionState.Faulted
                        ? RecordingOperationResult.Failure(_lastFailureCode, _lastFailureMessage ?? "Recording failed.")
                        : RecordingOperationResult.Success(),
                    _artifact);
            }

            if (stopFailure == RecordingFailureCode.None)
            {
                AppendTerminalGapLocked();
                CompleteArtifactLocked(RecordingSessionState.Completed, "completed");
                result = new RecordingStopResult(RecordingOperationResult.Success(), _artifact);
            }
            else
            {
                AppendTerminalFaultLocked(stopFailure, stopFailureMessage!);
                CompleteArtifactLocked(RecordingSessionState.Faulted, "faulted");
                result = new RecordingStopResult(
                    RecordingOperationResult.Failure(stopFailure, stopFailureMessage!),
                    _artifact);
            }

            snapshot = CreateStatusLocked();
        }

        NotifyStatusChanged(snapshot);
        return result;
    }

    private void FinalizeUnexpectedConsumerFault(Exception exception)
    {
        RecordingSessionSnapshot? snapshot = null;
        lock (_gate)
        {
            if (_artifact is not null) return;

            _channel?.Writer.TryComplete();
            AppendTerminalFaultLocked(RecordingFailureCode.InternalError, "The recording consumer stopped unexpectedly.");
            CompleteArtifactLocked(RecordingSessionState.Faulted, "faulted");
            _lastFailureMessage = $"The recording consumer stopped unexpectedly ({exception.GetType().Name}).";
            snapshot = CreateStatusLocked();
        }

        NotifyStatusChanged(snapshot);
    }

    private void AppendTerminalFaultLocked(RecordingFailureCode failureCode, string message)
    {
        var pendingSequences = _pendingEntries.Keys.Order().ToArray();
        var pendingPayloadBytes = _pendingEntries.Values.Sum(value => (long)value);
        _estimatedPayloadBytes -= pendingPayloadBytes;
        _pendingEntries.Clear();

        var firstLostSequence = CombineFirstSequence(
            pendingSequences.Length == 0 ? null : pendingSequences[0],
            _pendingDroppedEntryCount == 0 ? null : _firstPendingDroppedSequence);
        var lastLostSequence = CombineLastSequence(
            pendingSequences.Length == 0 ? null : pendingSequences[^1],
            _pendingDroppedEntryCount == 0 ? null : _lastPendingDroppedSequence);
        var lostCount = pendingSequences.LongLength + _pendingDroppedEntryCount;

        _entries.Add(CreateEntryLocked(
            "recorder.fault",
            ErrorSeverity,
            JsonSerializer.SerializeToElement(new
            {
                failureCode = failureCode.ToString(),
                message,
                firstLostSequence,
                lastLostSequence,
                lostCount
            }),
            message));

        ClearPendingDroppedLocked();
        _lastFailureCode = failureCode;
        _lastFailureMessage = message;
    }

    private void CompleteArtifactLocked(RecordingSessionState finalState, string result)
    {
        var completedUtc = _timeProvider.GetUtcNow();
        _entries.Add(CreateEntryLocked(
            "recorder.completed",
            finalState == RecordingSessionState.Completed ? InformationSeverity : ErrorSeverity,
            JsonSerializer.SerializeToElement(new { result }),
            finalState == RecordingSessionState.Completed ? "Recording completed" : "Recording completed with a fault",
            timestampUtc: completedUtc));

        _state = finalState;
        var orderedEntries = _entries.OrderBy(entry => entry.Sequence).ToArray();
        _artifact = new RecordingArtifact(
            new RecordingSessionMetadata(_sessionId!.Value, _sessionName!, _startedUtc, completedUtc),
            _sourceApplicationVersion!,
            _project,
            new RecordingArtifactOptions(_options.EntryLimit, _options.EstimatedPayloadByteLimit),
            orderedEntries);
    }

    private bool TryEmitRecoveredGapLocked()
    {
        if (_pendingDroppedEntryCount == 0) return true;
        if (!HasNonTerminalEntryRoomLocked())
        {
            TriggerLimitLocked("entry-count");
            return false;
        }

        AppendGapLocked("ingestion-capacity");
        return true;
    }

    private void AppendTerminalGapLocked()
    {
        if (_pendingDroppedEntryCount == 0) return;
        AppendGapLocked("ingestion-capacity");
    }

    private void AppendGapLocked(string reason)
    {
        _entries.Add(CreateEntryLocked(
            "recorder.gap",
            WarningSeverity,
            JsonSerializer.SerializeToElement(new
            {
                reason,
                firstDroppedSequence = _firstPendingDroppedSequence,
                lastDroppedSequence = _lastPendingDroppedSequence,
                droppedCount = _pendingDroppedEntryCount
            }),
            $"Recording gap: {_pendingDroppedEntryCount} entries were dropped"));
        ClearPendingDroppedLocked();
    }

    private void TriggerLimitLocked(string reason)
    {
        if (_limitReached) return;

        _entries.Add(CreateEntryLocked(
            "recorder.limit",
            WarningSeverity,
            JsonSerializer.SerializeToElement(new
            {
                reason,
                entryLimit = _options.EntryLimit,
                estimatedPayloadByteLimit = _options.EstimatedPayloadByteLimit,
                firstDroppedSequence = _pendingDroppedEntryCount == 0 ? (long?)null : _firstPendingDroppedSequence,
                lastDroppedSequence = _pendingDroppedEntryCount == 0 ? (long?)null : _lastPendingDroppedSequence,
                droppedCount = _pendingDroppedEntryCount
            }),
            "Recording session limit reached"));
        ClearPendingDroppedLocked();
        _limitReached = true;
        _lastFailureCode = RecordingFailureCode.LimitReached;
        _lastFailureMessage = "The recording session limit has been reached.";
    }

    private void TrackDroppedSequenceLocked(long sequence)
    {
        if (_pendingDroppedEntryCount == 0) _firstPendingDroppedSequence = sequence;
        _lastPendingDroppedSequence = sequence;
        _pendingDroppedEntryCount++;
        _totalDroppedEntryCount++;
    }

    private void ClearPendingDroppedLocked()
    {
        _pendingDroppedEntryCount = 0;
        _firstPendingDroppedSequence = 0;
        _lastPendingDroppedSequence = 0;
    }

    private RecordingEntry CreateEntryLocked(
        string typeKey,
        string severity,
        JsonElement payload,
        string displayText,
        string category = RecorderCategory,
        string source = RecorderSource,
        Guid? correlationId = null,
        IEnumerable<RecordingEntityReference>? entityReferences = null,
        RecordingReplayApplicability replayApplicability = RecordingReplayApplicability.DisplayOnly,
        DateTimeOffset? timestampUtc = null)
    {
        var now = timestampUtc ?? _timeProvider.GetUtcNow();
        var elapsed = now - _startedUtc;
        if (elapsed < _lastElapsed) elapsed = _lastElapsed;
        _lastElapsed = elapsed;

        return new RecordingEntry(
            Interlocked.Increment(ref _sequence),
            now,
            elapsed,
            category,
            source,
            typeKey,
            severity,
            correlationId,
            entityReferences,
            payload,
            displayText,
            replayApplicability);
    }

    private bool CanAcceptEntryLocked(int estimatedPayloadBytes) =>
        HasNonTerminalEntryRoomLocked() &&
        _estimatedPayloadBytes + estimatedPayloadBytes <= _options.EstimatedPayloadByteLimit;

    private bool HasNonTerminalEntryRoomLocked() =>
        _entries.Count + _pendingEntries.Count < _options.EntryLimit - ReservedTerminalEntryCount;

    private RecordingOperationResult? ValidateStartRequest(RecordingSessionStartRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Trim().Length > RecordingFormat.MaxSessionNameLength)
        {
            return RecordingOperationResult.Failure(
                RecordingFailureCode.InvalidRequest,
                $"Session name must contain between 1 and {RecordingFormat.MaxSessionNameLength} characters.");
        }

        if (string.IsNullOrWhiteSpace(request.SourceApplicationVersion) ||
            request.SourceApplicationVersion.Trim().Length > RecordingFormat.MaxApplicationVersionLength)
        {
            return RecordingOperationResult.Failure(
                RecordingFailureCode.InvalidRequest,
                $"Application version must contain between 1 and {RecordingFormat.MaxApplicationVersionLength} characters.");
        }

        if (request.Project is not null &&
            (request.Project.ProjectId == Guid.Empty ||
             string.IsNullOrWhiteSpace(request.Project.Name) ||
             request.Project.Name.Length > RecordingFormat.MaxProjectNameLength))
        {
            return RecordingOperationResult.Failure(RecordingFailureCode.InvalidRequest, "Project identity is invalid.");
        }

        return null;
    }

    private static int ValidateProjection(RecordingEntryProjection projection)
    {
        if (!IsValidKey(projection.Category) ||
            !IsValidKey(projection.Source) ||
            !IsValidKey(projection.Severity) ||
            string.IsNullOrWhiteSpace(projection.TypeKey) ||
            projection.TypeKey.Length > RecordingFormat.MaxTypeKeyLength ||
            projection.DisplayText.Length > RecordingFormat.MaxDisplayTextLength ||
            projection.EntityReferences.Length > RecordingFormat.MaxEntityReferencesPerEntry ||
            projection.EntityReferences.Any(reference =>
                string.IsNullOrWhiteSpace(reference.Kind) ||
                reference.Kind.Length > RecordingFormat.MaxKeyLength ||
                reference.Id == Guid.Empty))
        {
            return -1;
        }

        if (projection.Payload.ValueKind == JsonValueKind.Undefined) return -1;
        var payloadBytes = Encoding.UTF8.GetByteCount(projection.Payload.GetRawText());
        return payloadBytes <= RecordingFormat.MaxPayloadBytes ? payloadBytes : -1;
    }

    private static bool IsValidKey(string value) =>
        !string.IsNullOrWhiteSpace(value) && value.Length <= RecordingFormat.MaxKeyLength;

    private RecordingOperationResult FailLocked(RecordingFailureCode code, string message)
    {
        _lastFailureCode = code;
        _lastFailureMessage = message;
        return RecordingOperationResult.Failure(code, message);
    }

    private RecordingSessionSnapshot CreateStatusLocked() =>
        new(
            _state,
            _sessionId,
            _sessionName,
            _entries.Count + _pendingEntries.Count,
            _pendingEntries.Count,
            _totalDroppedEntryCount,
            _estimatedPayloadBytes,
            _limitReached,
            _lastFailureCode,
            _lastFailureMessage);

    private void NotifyStatusChanged(RecordingSessionSnapshot snapshot)
    {
        var handlers = StatusChanged;
        if (handlers is null) return;

        foreach (Action<RecordingSessionSnapshot> handler in handlers.GetInvocationList())
        {
            try
            {
                handler(snapshot);
            }
            catch
            {
                // An observer cannot be allowed to terminate capture or block later observers.
            }
        }
    }

    private void ResetLocked()
    {
        _consumerCancellation?.Dispose();
        _entries.Clear();
        _pendingEntries.Clear();
        _state = RecordingSessionState.Idle;
        _sessionId = null;
        _sessionName = null;
        _sourceApplicationVersion = null;
        _project = null;
        _startedUtc = default;
        _pausedUtc = null;
        _lastElapsed = TimeSpan.Zero;
        _sequence = 0;
        _totalDroppedEntryCount = 0;
        ClearPendingDroppedLocked();
        _estimatedPayloadBytes = 0;
        _limitReached = false;
        _lastFailureCode = RecordingFailureCode.None;
        _lastFailureMessage = null;
        _channel = null;
        _consumerCancellation = null;
        _consumerTask = null;
        _stopTask = null;
        _artifact = null;
    }

    private static long? CombineFirstSequence(long? first, long? second)
    {
        if (first is null) return second;
        if (second is null) return first;
        return Math.Min(first.Value, second.Value);
    }

    private static long? CombineLastSequence(long? first, long? second)
    {
        if (first is null) return second;
        if (second is null) return first;
        return Math.Max(first.Value, second.Value);
    }
}