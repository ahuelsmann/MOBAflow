// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Domain.Enum;

/// <summary>Identifies a structural problem of a workflow or its actions.</summary>
public enum WorkflowStructureIssueKind
{
    /// <summary>The workflow has no actions.</summary>
    EmptyWorkflow,

    /// <summary>An entry of the action list is missing.</summary>
    NullAction,

    /// <summary>An action has an empty identifier.</summary>
    EmptyActionId,

    /// <summary>An action reuses the identifier of an earlier action in the same workflow.</summary>
    DuplicateActionId,

    /// <summary>An action waits a negative time after it ran.</summary>
    NegativeDelay
}
