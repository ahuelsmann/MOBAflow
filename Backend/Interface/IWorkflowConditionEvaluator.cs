// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Backend.Interface;

using Domain;

using Service;

/// <summary>Evaluates supported typed workflow conditions against a captured execution context.</summary>
public interface IWorkflowConditionEvaluator
{
    /// <summary>Evaluates one condition deterministically.</summary>
    ValueTask<bool> EvaluateAsync(
        WorkflowCondition condition,
        ActionExecutionContext context,
        CancellationToken cancellationToken);
}

/// <summary>Evaluates one concrete workflow-condition type for the condition registry.</summary>
public interface IWorkflowConditionHandler
{
    /// <summary>Gets the concrete condition type handled by this strategy.</summary>
    Type ConditionType { get; }

    /// <summary>Evaluates the typed condition against a captured context.</summary>
    ValueTask<bool> EvaluateAsync(
        WorkflowCondition condition,
        ActionExecutionContext context,
        CancellationToken cancellationToken);
}
