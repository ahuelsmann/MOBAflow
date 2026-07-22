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
    /// <summary>Executes or dry-runs one validated Workflow 2.0 graph.</summary>
    /// <param name="request">Immutable execution request.</param>
    /// <param name="cancellationToken">Cancellation token for graph traversal and external effects.</param>
    Task<WorkflowExecutionResult> ExecuteAsync(
        WorkflowExecutionRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a Workflow 2.0 graph through the compatibility entry point.
    /// </summary>
    /// <param name="workflow">The workflow to execute</param>
    /// <param name="context">Execution context containing dependencies and state</param>
    /// <param name="options">Superseded compatibility options; graph error policies are authoritative.</param>
    /// <exception cref="ArgumentNullException">Thrown when workflow or context is null</exception>
    Task ExecuteAsync(Workflow workflow, ActionExecutionContext context, WorkflowExecutionOptions options = default);

    /// <summary>
    /// Executes a Workflow 2.0 graph while propagating cancellation through every boundary.
    /// </summary>
    /// <param name="workflow">The workflow to execute.</param>
    /// <param name="context">Execution context containing dependencies and state.</param>
    /// <param name="options">Superseded compatibility options; graph error policies are authoritative.</param>
    /// <param name="cancellationToken">Cancellation token for the complete workflow run.</param>
    Task ExecuteAsync(
        Workflow workflow,
        ActionExecutionContext context,
        WorkflowExecutionOptions options,
        CancellationToken cancellationToken);
}
