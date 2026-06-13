// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Backend.Interface;

using Domain;
using Domain.Enum;

using Service;

/// <summary>
/// Executes one concrete workflow action type.
/// </summary>
public interface IWorkflowActionHandler
{
    /// <summary>
    /// Gets the action type handled by this strategy.
    /// </summary>
    ActionType ActionType { get; }

    /// <summary>
    /// Executes the supplied action with the current workflow context.
    /// </summary>
    Task ExecuteAsync(WorkflowAction action, ActionExecutionContext context);
}
