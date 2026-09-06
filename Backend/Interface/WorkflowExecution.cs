// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Backend.Interface;

using Domain;

using Service;

/// <summary>Identifies whether a workflow run executes or only plans external effects.</summary>
public enum WorkflowRunMode
{
    /// <summary>Invoke live action handlers.</summary>
    Live,

    /// <summary>Traverse the graph and plan effects without invoking live handlers or waiting.</summary>
    DryRun
}

/// <summary>Identifies the terminal state of a workflow execution.</summary>
public enum WorkflowExecutionStatus
{
    /// <summary>Validation prevented the workflow from starting.</summary>
    NotStarted,

    /// <summary>The workflow reached a successful termination.</summary>
    Succeeded,

    /// <summary>The workflow was cancelled explicitly or by its token.</summary>
    Cancelled,

    /// <summary>The workflow stopped after a failure.</summary>
    Failed
}

/// <summary>Contains all immutable input required to execute one Workflow 2.0 graph.</summary>
public sealed record WorkflowExecutionRequest
{
    /// <summary>Gets the project snapshot containing nested workflow definitions.</summary>
    public required Project Project { get; init; }

    /// <summary>Gets the root workflow to execute.</summary>
    public required Workflow Workflow { get; init; }

    /// <summary>Gets the captured action and condition context.</summary>
    public required ActionExecutionContext Context { get; init; }

    /// <summary>Gets whether this run is live or side-effect-free.</summary>
    public WorkflowRunMode Mode { get; init; }

    /// <summary>Gets the source-event correlation identifier.</summary>
    public Guid SourceCorrelationId { get; init; }
}

/// <summary>Contains the terminal result and planned effects of one workflow execution.</summary>
public sealed record WorkflowExecutionResult
{
    /// <summary>Gets the root execution identifier.</summary>
    public required Guid ExecutionId { get; init; }

    /// <summary>Gets the workflow identifier.</summary>
    public required Guid WorkflowId { get; init; }

    /// <summary>Gets the source correlation identifier.</summary>
    public required Guid SourceCorrelationId { get; init; }

    /// <summary>Gets the terminal execution status.</summary>
    public required WorkflowExecutionStatus Status { get; init; }

    /// <summary>Gets validation issues that prevented execution.</summary>
    public IReadOnlyList<WorkflowValidationIssue> ValidationIssues { get; init; } = [];

    /// <summary>Gets effects projected by a dry run in deterministic traversal order.</summary>
    public IReadOnlyList<WorkflowPlannedEffect> PlannedEffects { get; init; } = [];

    /// <summary>Gets sanitized failure detail when the workflow failed.</summary>
    public string? FailureDetail { get; init; }
}
