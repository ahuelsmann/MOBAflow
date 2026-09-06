// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Backend.Interface;

using Common.Events;

/// <summary>Stores a bounded in-memory projection of recent workflow lifecycle events.</summary>
public interface IWorkflowTraceStore
{
    /// <summary>Appends one lifecycle event and applies retention limits.</summary>
    void Append(WorkflowLifecycleEvent lifecycleEvent);

    /// <summary>Returns the retained entries in append order.</summary>
    IReadOnlyList<WorkflowLifecycleEvent> GetEntries();
}
