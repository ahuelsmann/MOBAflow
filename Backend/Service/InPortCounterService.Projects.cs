// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Backend.Service;

using Interface;

public sealed partial class InPortCounterService
{
    private readonly Func<Guid, IInPortCounterStore>? _projectStoreFactory;
    private Guid? _counterProjectId;
    private bool _switchingProject;

    /// <summary>
    /// Flushes the previous mobile project's counts and restores the selected project's own store.
    /// Hosts with one counter service per runtime need no switch and retain their existing store.
    /// The owning runtime serializes project activation and closes the previous connection first.
    /// </summary>
    public async Task SelectProjectAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        if (_projectStoreFactory is null || _counterProjectId == projectId) return;
        cancellationToken.ThrowIfCancellationRequested();
        Task<bool>? previousLoad;
        lock (_sync)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            _switchingProject = true;
            previousLoad = _loadTask;
        }

        try
        {
            if (previousLoad is not null)
                await previousLoad.WaitAsync(cancellationToken).ConfigureAwait(false);
            bool flushPrevious;
            lock (_sync) { flushPrevious = !_loadFailed; }
            // A failed load accepted no counts and must leave its unreadable file untouched.
            if (flushPrevious) await FlushAsync(cancellationToken).ConfigureAwait(false);
            Task previousWriter;
            lock (_sync) { previousWriter = _saveTask; }
            await previousWriter.WaitAsync(cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            var store = _projectStoreFactory(projectId);
            lock (_sync)
            {
                _store = store;
                _counterProjectId = projectId;
                _initialized = false;
                _loadFailed = false;
                _loadTask = null;
                _persistenceError = null;
                _counters.Clear();
                _inputRevisions.Clear();
                _pendingCounts.Clear();
                _feedbackBeforeLoad.Clear();
                _generation++;
                ReconcileInputsLocked();
            }

            // The store swap is committed. Finish its load so the runtime can activate the same project,
            // even if this caller cancels; cancellation remains honored before the swap.
            await TryInitializeAsync(CancellationToken.None).ConfigureAwait(false);
        }
        finally
        {
            lock (_sync) { _switchingProject = false; }
        }
    }
}
