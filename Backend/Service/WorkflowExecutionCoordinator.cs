// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Backend.Service;

using Interface;

/// <summary>Provides per-source FIFO execution with independent cancellation ownership.</summary>
public sealed class WorkflowExecutionCoordinator : IWorkflowExecutionCoordinator
{
    private readonly object _sync = new();
    private readonly IWorkflowService _workflowService;
    private readonly TimeProvider _timeProvider;
    private readonly Dictionary<string, QueuedEntry> _sourceTails = new(StringComparer.Ordinal);
    private readonly Dictionary<Guid, HashSet<QueuedEntry>> _ownerEntries = [];
    private bool _disposed;

    /// <summary>Creates a workflow execution coordinator.</summary>
    /// <param name="workflowService">Validated graph executor.</param>
    /// <param name="timeProvider">Time source used for cancellable source delays.</param>
    public WorkflowExecutionCoordinator(IWorkflowService workflowService, TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(workflowService);
        ArgumentNullException.ThrowIfNull(timeProvider);

        _workflowService = workflowService;
        _timeProvider = timeProvider;
    }

    /// <inheritdoc />
    public Task<WorkflowExecutionResult> EnqueueAsync(
        QueuedWorkflowExecution execution,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(execution);
        ArgumentException.ThrowIfNullOrWhiteSpace(execution.SourceKey);
        ArgumentOutOfRangeException.ThrowIfLessThan(execution.Delay, TimeSpan.Zero);

        lock (_sync)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            var predecessor = _sourceTails.TryGetValue(execution.SourceKey, out var tail)
                ? tail.Completion
                : Task.CompletedTask;
            var entry = new QueuedEntry(CancellationTokenSource.CreateLinkedTokenSource(cancellationToken));
            entry.Completion = ExecuteQueuedAsync(entry, predecessor, execution);
            _sourceTails[execution.SourceKey] = entry;

            if (!_ownerEntries.TryGetValue(execution.OwnerId, out var entries))
            {
                entries = [];
                _ownerEntries.Add(execution.OwnerId, entries);
            }

            entries.Add(entry);
            return entry.Completion;
        }
    }

    /// <inheritdoc />
    public void CancelOwner(Guid ownerId)
    {
        QueuedEntry[] entries;
        lock (_sync)
        {
            entries = _ownerEntries.TryGetValue(ownerId, out var ownedEntries)
                ? [.. ownedEntries]
                : [];
        }

        Cancel(entries);
    }

    /// <inheritdoc />
    public void CancelPending()
    {
        QueuedEntry[] entries;
        lock (_sync)
        {
            entries = [.. _ownerEntries.Values.SelectMany(value => value).Distinct()];
        }

        Cancel(entries);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        QueuedEntry[] entries;
        lock (_sync)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            entries = [.. _ownerEntries.Values.SelectMany(value => value).Distinct()];
        }

        Cancel(entries);
        GC.SuppressFinalize(this);
    }

    private async Task<WorkflowExecutionResult> ExecuteQueuedAsync(
        QueuedEntry entry,
        Task predecessor,
        QueuedWorkflowExecution execution)
    {
        await Task.Yield();
        try
        {
            await AwaitPredecessorAsync(predecessor).ConfigureAwait(false);
            entry.Cancellation.Token.ThrowIfCancellationRequested();

            if (execution.Delay > TimeSpan.Zero)
            {
                await Task.Delay(execution.Delay, _timeProvider, entry.Cancellation.Token).ConfigureAwait(false);
            }

            return await _workflowService.ExecuteAsync(execution.Request, entry.Cancellation.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (entry.Cancellation.IsCancellationRequested)
        {
            return new WorkflowExecutionResult
            {
                ExecutionId = Guid.NewGuid(),
                WorkflowId = execution.Request.Workflow.Id,
                SourceCorrelationId = execution.Request.SourceCorrelationId,
                Status = WorkflowExecutionStatus.Cancelled
            };
        }
        finally
        {
            Remove(entry, execution.SourceKey, execution.OwnerId);
            entry.Cancellation.Dispose();
        }
    }

    private static async Task AwaitPredecessorAsync(Task predecessor)
    {
        try
        {
            await predecessor.ConfigureAwait(false);
        }
        catch
        {
            // A failed source execution is observed by its caller and must not poison the FIFO tail.
        }
    }

    private void Remove(QueuedEntry entry, string sourceKey, Guid ownerId)
    {
        lock (_sync)
        {
            if (_sourceTails.TryGetValue(sourceKey, out var tail) && ReferenceEquals(tail, entry))
            {
                _sourceTails.Remove(sourceKey);
            }

            if (_ownerEntries.TryGetValue(ownerId, out var entries))
            {
                entries.Remove(entry);
                if (entries.Count == 0)
                {
                    _ownerEntries.Remove(ownerId);
                }
            }
        }
    }

    private static void Cancel(IEnumerable<QueuedEntry> entries)
    {
        foreach (var entry in entries)
        {
            try
            {
                entry.Cancellation.Cancel();
            }
            catch (ObjectDisposedException)
            {
                // Completion won the race with targeted cancellation.
            }
        }
    }

    private sealed class QueuedEntry(CancellationTokenSource cancellation)
    {
        public CancellationTokenSource Cancellation { get; } = cancellation;

        public Task<WorkflowExecutionResult> Completion { get; set; } = null!;
    }
}
