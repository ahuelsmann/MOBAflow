// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Backend.Service;

using Common.Events;

using Interface;

/// <summary>Thread-safe bounded trace projection for recent workflow executions.</summary>
/// <param name="maximumExecutions">Maximum retained execution identifiers.</param>
/// <param name="maximumEntries">Hard maximum retained lifecycle entries.</param>
public sealed class WorkflowTraceStore(int maximumExecutions = 100, int maximumEntries = 10_000) : IWorkflowTraceStore
{
    private readonly object _sync = new();
    private readonly List<WorkflowLifecycleEvent> _entries = [];

    /// <inheritdoc />
    public void Append(WorkflowLifecycleEvent lifecycleEvent)
    {
        ArgumentNullException.ThrowIfNull(lifecycleEvent);
        lock (_sync)
        {
            _entries.Add(lifecycleEvent);
            TrimEntries();
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<WorkflowLifecycleEvent> GetEntries()
    {
        lock (_sync)
            return _entries.ToArray();
    }

    private void TrimEntries()
    {
        while (_entries.Count > maximumEntries)
            _entries.RemoveAt(0);

        while (_entries.Select(entry => entry.ExecutionId).Distinct().Count() > maximumExecutions)
        {
            var completedExecution = _entries.FirstOrDefault(entry => entry.Kind is
                WorkflowLifecycleKind.WorkflowCompleted or
                WorkflowLifecycleKind.WorkflowCancelled or
                WorkflowLifecycleKind.WorkflowFailed);
            if (completedExecution == null)
                return;

            _entries.RemoveAll(entry => entry.ExecutionId == completedExecution.ExecutionId);
        }
    }
}
