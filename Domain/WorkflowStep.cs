// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Domain;

using System.Text.Json.Serialization;

/// <summary>
/// Base type for a node in an ordered workflow graph.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")]
[JsonDerivedType(typeof(WorkflowActionStep), "action")]
[JsonDerivedType(typeof(WorkflowDelayStep), "delay")]
[JsonDerivedType(typeof(WorkflowConditionStep), "condition")]
[JsonDerivedType(typeof(WorkflowParallelStep), "parallel")]
[JsonDerivedType(typeof(WorkflowNestedStep), "nestedWorkflow")]
[JsonDerivedType(typeof(WorkflowTerminateStep), "terminate")]
public abstract class WorkflowStep
{
    /// <summary>Gets or sets the stable step identifier.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Gets or sets the editor-facing step name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the normal successor step.</summary>
    public Guid? NextStepId { get; set; }

    /// <summary>Gets or sets the optional step-level policy that overrides the workflow default.</summary>
    public WorkflowErrorPolicy? ErrorPolicy { get; set; }
}

/// <summary>
/// Executes one existing typed workflow action.
/// </summary>
public sealed class WorkflowActionStep : WorkflowStep
{
    /// <summary>Gets or sets the typed action payload.</summary>
    public WorkflowAction? Action { get; set; }
}

/// <summary>
/// Waits for a deterministic duration before continuing.
/// </summary>
public sealed class WorkflowDelayStep : WorkflowStep
{
    /// <summary>Gets or sets the non-negative delay duration.</summary>
    public int DelayMs { get; set; }
}

/// <summary>
/// Selects one of two explicit successors using a typed condition.
/// </summary>
public sealed class WorkflowConditionStep : WorkflowStep
{
    /// <summary>Gets or sets the typed condition.</summary>
    public WorkflowCondition? Condition { get; set; }

    /// <summary>Gets or sets the successor used when the condition evaluates to true.</summary>
    public Guid TrueStepId { get; set; }

    /// <summary>Gets or sets the successor used when the condition evaluates to false.</summary>
    public Guid FalseStepId { get; set; }
}

/// <summary>
/// Describes one persisted branch of a parallel workflow step.
/// </summary>
public sealed class WorkflowParallelBranch
{
    /// <summary>Gets or sets the editor-facing branch name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the entry step of this branch.</summary>
    public Guid EntryStepId { get; set; }
}

/// <summary>
/// Launches ordered branches and joins them at one explicit step.
/// </summary>
public sealed class WorkflowParallelStep : WorkflowStep
{
    /// <summary>Gets the branches in deterministic launch and reduction order.</summary>
    public List<WorkflowParallelBranch> Branches { get; set; } = [];

    /// <summary>Gets or sets the step at which all branches join.</summary>
    public Guid JoinStepId { get; set; }
}

/// <summary>
/// Invokes another reusable workflow by stable identifier.
/// </summary>
public sealed class WorkflowNestedStep : WorkflowStep
{
    /// <summary>Gets or sets the referenced child workflow identifier.</summary>
    public Guid WorkflowId { get; set; }
}

/// <summary>
/// Defines the explicit result emitted by a termination step.
/// </summary>
public enum WorkflowTerminationResult
{
    /// <summary>Complete the workflow successfully.</summary>
    Succeeded,

    /// <summary>Complete the workflow as cancelled.</summary>
    Cancelled,

    /// <summary>Complete the workflow as failed.</summary>
    Failed
}

/// <summary>
/// Ends a workflow graph with an explicit result.
/// </summary>
public sealed class WorkflowTerminateStep : WorkflowStep
{
    /// <summary>Gets or sets the terminal workflow result.</summary>
    public WorkflowTerminationResult Result { get; set; }
}
