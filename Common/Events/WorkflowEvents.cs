// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Common.Events;

/// <summary>Identifies whether a lifecycle event belongs to live execution or dry-run planning.</summary>
public enum WorkflowLifecycleMode
{
    /// <summary>Live handlers may execute.</summary>
    Live,

    /// <summary>Only side-effect-free planning may execute.</summary>
    DryRun
}

/// <summary>Identifies one observable Workflow 2.0 lifecycle transition.</summary>
public enum WorkflowLifecycleKind
{
    /// <summary>A validated workflow execution began.</summary>
    WorkflowStarted,
    /// <summary>A workflow completed successfully.</summary>
    WorkflowCompleted,
    /// <summary>A workflow ended through cancellation.</summary>
    WorkflowCancelled,
    /// <summary>A workflow ended through failure.</summary>
    WorkflowFailed,
    /// <summary>Validation prevented execution.</summary>
    ValidationFailed,
    /// <summary>A step attempt began.</summary>
    StepStarted,
    /// <summary>A step attempt completed.</summary>
    StepCompleted,
    /// <summary>A step attempt failed.</summary>
    StepFailed,
    /// <summary>A condition selected a branch.</summary>
    ConditionDecided,
    /// <summary>A bounded retry was scheduled.</summary>
    RetryScheduled,
    /// <summary>Execution entered a nested workflow.</summary>
    NestedWorkflowEntered,
    /// <summary>Execution returned from a nested workflow.</summary>
    NestedWorkflowExited,
    /// <summary>A dry run projected an external effect.</summary>
    PlannedEffect
}

/// <summary>Platform-neutral correlated Workflow 2.0 lifecycle event.</summary>
public sealed record WorkflowLifecycleEvent : EventBase
{
    /// <summary>Gets the lifecycle transition kind.</summary>
    public required WorkflowLifecycleKind Kind { get; init; }
    /// <summary>Gets the originating event correlation identifier.</summary>
    public required Guid SourceCorrelationId { get; init; }
    /// <summary>Gets this workflow execution identifier.</summary>
    public required Guid ExecutionId { get; init; }
    /// <summary>Gets the parent execution for a nested workflow.</summary>
    public Guid? ParentExecutionId { get; init; }
    /// <summary>Gets the workflow identifier.</summary>
    public required Guid WorkflowId { get; init; }
    /// <summary>Gets the affected step identifier, when applicable.</summary>
    public Guid? StepId { get; init; }
    /// <summary>Gets the monotonic source-correlation trace sequence.</summary>
    public required long Sequence { get; init; }
    /// <summary>Gets the one-based attempt number, when applicable.</summary>
    public int Attempt { get; init; }
    /// <summary>Gets the live or dry-run mode.</summary>
    public required WorkflowLifecycleMode Mode { get; init; }
    /// <summary>Gets the injected UTC event timestamp.</summary>
    public required DateTimeOffset TimestampUtc { get; init; }
    /// <summary>Gets elapsed execution time, when applicable.</summary>
    public TimeSpan? Elapsed { get; init; }
    /// <summary>Gets a result or branch decision.</summary>
    public string? Result { get; init; }
    /// <summary>Gets sanitized transition detail.</summary>
    public string? Detail { get; init; }
}
