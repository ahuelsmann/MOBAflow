// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Backend.Interface;

using Domain;
using Service;

/// <summary>
/// Interface for executing workflow actions.
/// Enables testing and mocking of action execution logic.
/// </summary>
public interface IActionExecutor
{
    /// <summary>
    /// Executes a WorkflowAction based on its type.
    /// </summary>
    /// <param name="action">The workflow action to execute</param>
    /// <param name="context">Execution context containing dependencies and state</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task ExecuteAsync(WorkflowAction action, ActionExecutionContext context);
}
