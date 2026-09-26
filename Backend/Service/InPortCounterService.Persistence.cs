// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Backend.Service;

using Common.Runtime;
using Interface;
using Microsoft.Extensions.Logging;

public sealed partial class InPortCounterService
{
    private readonly IInPortCounterStore? _store;
    private readonly Queue<(FeedbackResult Feedback, DateTimeOffset ReceivedAt)> _feedbackBeforeLoad = new();
    private Task<bool>? _loadTask;
    private bool _initialized;
    private bool _loadFailed;
    private bool _saving;
    private Dictionary<uint, ulong>? _pendingSave;
    private Task _saveTask = Task.CompletedTask;
    private TaskCompletionSource _saveProgress = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private long _saveVersion;
    private long _completedSaveVersion;
    private string? _persistenceError;

    /// <summary>An operator-visible load/save failure, cleared after successful persistence.</summary>
    public string? PersistenceError
    {
        get { lock (_sync) { return _persistenceError; } }
    }

    /// <summary>Restores counts before publishing any activations received during loading.</summary>
    /// <exception cref="InvalidOperationException">The saved counts could not be loaded.</exception>
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (!await TryInitializeAsync(cancellationToken).ConfigureAwait(false))
            throw new InvalidOperationException(PersistenceError);
    }

    /// <summary>
    /// Restores counts, reporting an unreadable store through <see cref="PersistenceError"/> instead of throwing
    /// so the runtime can still start and connect. Counting stays disabled until a later load succeeds or
    /// <see cref="ResetAll"/> explicitly replaces the saved counts.
    /// </summary>
    public Task<bool> TryInitializeAsync(CancellationToken cancellationToken = default)
    {
        Task<bool> load;
        lock (_sync)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (_initialized) return Task.FromResult(true);

            // Concurrent callers share one load; the next caller retries a failed load.
            // Task.Run keeps store continuations and subscriber callbacks outside this lock.
            // One caller's cancellation only ends its own wait, never the shared load.
            if (_loadTask is null || _loadTask.IsCompleted)
                _loadTask = Task.Run(LoadSavedCountsAsync, CancellationToken.None);
            load = _loadTask;
        }

        return load.WaitAsync(cancellationToken);
    }

    private async Task<bool> LoadSavedCountsAsync()
    {
        IReadOnlyDictionary<uint, ulong> saved;
        try
        {
            saved = await _store!.LoadAsync().ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            LogCounterLoadFailed(_logger, ex);
            lock (_sync)
            {
                _loadFailed = true;
                // Activations cannot be counted against unknown saved values.
                _feedbackBeforeLoad.Clear();
                _persistenceError = "Could not load InPort counters: " + ex.Message
                    + " Reset all counters to replace the saved counts.";
            }

            PublishSnapshotChanged();
            return false;
        }

        bool startPublishing;
        lock (_sync)
        {
            if (_disposed) return false;
            // An explicit reset may already have replaced unreadable counts during this retry.
            if (_initialized) return true;
            ReconcileInputsLocked();
            foreach (var inPort in _counters.Keys.ToArray())
                _counters[inPort] = new InPortCounterSnapshot(inPort, saved.GetValueOrDefault(inPort), null, null);
            _initialized = true;
            _loadFailed = false;
            _persistenceError = null;
            while (_feedbackBeforeLoad.TryDequeue(out var pending))
                AcceptFeedbackLocked(pending.Feedback, pending.ReceivedAt);
            QueueSaveLocked();
            startPublishing = !_publishingCounts && _pendingCounts.Count > 0;
            if (startPublishing) _publishingCounts = true;
        }

        PublishSnapshotChanged();
        if (startPublishing) PublishPendingCounts();
        return true;
    }

    private void EnsureInitialized()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (!_initialized)
            throw new InvalidOperationException(_loadFailed ? _persistenceError : "InPort counters have not been loaded yet.");
    }

    private void OnFeedbackPointsChanged(object? sender, EventArgs args)
    {
        lock (_sync)
        {
            if (_disposed) return;
            ReconcileInputsLocked();
            if (_initialized) QueueSaveLocked();
        }

        PublishSnapshotChanged();
    }

    private void ReconcileInputsLocked()
    {
        var count = _settings.Counter.CountOfFeedbackPoints;
        foreach (var inPort in _counters.Keys.Where(inPort => inPort > count).ToArray())
        {
            _counters.Remove(inPort);
            // Keep the revision after removal so re-adding cannot revive an old activation.
            _inputRevisions[inPort] = _inputRevisions.GetValueOrDefault(inPort) + 1;
        }

        for (uint inPort = 1; inPort <= count; inPort++)
            _counters.TryAdd(inPort, new InPortCounterSnapshot(inPort, 0, null, null));
    }

    // Only one writer runs. While it is busy, retain the latest complete snapshot.
    private void QueueSaveLocked()
    {
        if (_store is null || !_initialized) return;
        _pendingSave = _counters.ToDictionary(pair => pair.Key, pair => pair.Value.Count);
        _saveVersion++;
        if (_saving) return;
        _saving = true;
        _saveTask = Task.Run(SavePendingAsync);
    }

    private async Task SavePendingAsync()
    {
        while (true)
        {
            Dictionary<uint, ulong> next;
            long version;
            lock (_sync)
            {
                if (_pendingSave is null)
                {
                    _saving = false;
                    return;
                }

                next = _pendingSave;
                version = _saveVersion;
                _pendingSave = null;
            }

            string? error = null;
            try
            {
                await _store!.SaveAsync(next).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OutOfMemoryException)
            {
                error = "Could not save InPort counters: " + ex.Message;
                LogCounterSaveFailed(_logger, ex);
            }

            bool errorChanged;
            lock (_sync)
            {
                errorChanged = _persistenceError != error;
                _persistenceError = error;
                _completedSaveVersion = version;
                var progress = _saveProgress;
                _saveProgress = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                progress.TrySetResult();
            }

            if (errorChanged) PublishSnapshotChanged();
        }
    }

    /// <summary>Waits for queued writes and reports failure instead of claiming durability.</summary>
    public async Task FlushAsync(CancellationToken cancellationToken = default)
    {
        long requiredVersion;
        lock (_sync) { requiredVersion = _saveVersion; }
        while (true)
        {
            Task progress;
            lock (_sync)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (_completedSaveVersion >= requiredVersion)
                {
                    if (_persistenceError is { } error) throw new IOException(error);
                    return;
                }

                progress = _saveProgress.Task;
            }

            await progress.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>Stops accepting feedback and drains the final saved state.</summary>
    /// <remarks>
    /// Persistence errors are already logged and shown; rethrowing them here would abort the disposal of
    /// other services during application shutdown. No save is queued after <see cref="Dispose"/>, and the
    /// writer never faults, so awaiting it drains every accepted change.
    /// </remarks>
    public async ValueTask DisposeAsync()
    {
        Dispose();
        Task pending;
        lock (_sync) { pending = _saveTask; }
        await pending.ConfigureAwait(false);
        GC.SuppressFinalize(this);
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Local InPort counter persistence failed")]
    private static partial void LogCounterSaveFailed(ILogger logger, Exception exception);

    [LoggerMessage(Level = LogLevel.Error, Message = "Local InPort counters could not be loaded; counting is disabled")]
    private static partial void LogCounterLoadFailed(ILogger logger, Exception exception);
}
