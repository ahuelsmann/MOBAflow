// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Backend.Interface;

using Domain;

using Service;

/// <summary>
/// Interface for workflow execution services.
/// Decouples workflow orchestration from concrete implementations,
/// enabling testability and loose coupling in JourneyManager and other consumers.
/// </summary>
public interface IWorkflowService
{
    /// <summary>Executes or dry-runs one validated ordered workflow.</summary>
    /// <param name="request">Immutable execution request.</param>
    /// <param name="cancellationToken">Cancellation token for ordered actions and external effects.</param>
    Task<WorkflowExecutionResult> ExecuteAsync(
        WorkflowExecutionRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes an ordered workflow through the compatibility entry point.
    /// </summary>
    /// <param name="workflow">The workflow to execute</param>
    /// <param name="context">Execution context containing dependencies and state</param>
    /// <param name="options">Superseded compatibility options; execution stops on the first failure.</param>
    /// <exception cref="ArgumentNullException">Thrown when workflow or context is null</exception>
    Task ExecuteAsync(Workflow workflow, ActionExecutionContext context, WorkflowExecutionOptions options = default);

    /// <summary>
    /// Executes an ordered workflow while propagating cancellation through every boundary.
    /// </summary>
    /// <param name="workflow">The workflow to execute.</param>
    /// <param name="context">Execution context containing dependencies and state.</param>
    /// <param name="options">Superseded compatibility options; execution stops on the first failure.</param>
    /// <param name="cancellationToken">Cancellation token for the complete workflow run.</param>
    Task ExecuteAsync(
        Workflow workflow,
        ActionExecutionContext context,
        WorkflowExecutionOptions options,
        CancellationToken cancellationToken);
}
