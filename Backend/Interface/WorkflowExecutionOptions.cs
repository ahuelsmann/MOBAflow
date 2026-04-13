// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Backend.Interface;

/// <summary>
/// Optional behavior flags for <see cref="IWorkflowService.ExecuteAsync"/>.
/// </summary>
public readonly record struct WorkflowExecutionOptions
{
    /// <summary>
    /// When true, sequential workflows stop after the first action that throws.
    /// The error is still reported via <see cref="IWorkflowService.ActionExecutionError"/> before rethrowing.
    /// Parallel workflows ignore this flag (all actions still run; failures are isolated per task).
    /// </summary>
    public bool StopOnFirstActionFailure { get; init; }
}
