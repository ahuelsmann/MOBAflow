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
    /// <summary>
    /// Raised when an action execution fails during workflow processing.
    /// Subscribe to display error messages in UI.
    /// </summary>
    event EventHandler<ActionExecutionErrorEventArgs>? ActionExecutionError;

    /// <summary>
    /// Executes a workflow with all its actions according to its execution mode.
    /// Sequential: Executes actions one-by-one, respecting DelayAfterMs.
    /// Parallel: Fires all actions simultaneously without waiting.
    /// </summary>
    /// <param name="workflow">The workflow to execute</param>
    /// <param name="context">Execution context containing dependencies and state</param>
    /// <exception cref="ArgumentNullException">Thrown when workflow or context is null</exception>
    Task ExecuteAsync(Workflow workflow, ActionExecutionContext context);
}
