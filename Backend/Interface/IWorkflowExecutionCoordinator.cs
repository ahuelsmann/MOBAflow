// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Backend.Interface;

/// <summary>Describes one captured workflow execution waiting behind a source-ordering boundary.</summary>
public sealed record QueuedWorkflowExecution
{
    /// <summary>Gets the stable source key whose executions must retain FIFO order.</summary>
    public required string SourceKey { get; init; }

    /// <summary>Gets the runtime owner used for targeted cancellation, such as a journey identifier.</summary>
    public required Guid OwnerId { get; init; }

    /// <summary>Gets the immutable workflow request captured when the source event was accepted.</summary>
    public required WorkflowExecutionRequest Request { get; init; }

    /// <summary>Gets the delay applied before the workflow starts.</summary>
    public TimeSpan Delay { get; init; }
}

/// <summary>Orders workflow executions per source without blocking unrelated sources.</summary>
public interface IWorkflowExecutionCoordinator : IDisposable
{
    /// <summary>Queues a captured workflow request behind earlier work from the same source.</summary>
    /// <param name="execution">Captured workflow execution and source metadata.</param>
    /// <param name="cancellationToken">Cancels this queued execution.</param>
    Task<WorkflowExecutionResult> EnqueueAsync(
        QueuedWorkflowExecution execution,
        CancellationToken cancellationToken = default);

    /// <summary>Cancels queued and running executions owned by one runtime entity.</summary>
    /// <param name="ownerId">Runtime owner identifier.</param>
    void CancelOwner(Guid ownerId);

    /// <summary>Cancels every queued and running execution while keeping the coordinator reusable.</summary>
    void CancelPending();
}
