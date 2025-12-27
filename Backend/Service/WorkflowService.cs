// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Backend.Service;

using Domain;
using System.Diagnostics;

/// <summary>
/// Workflow execution service.
/// Orchestrates the execution of workflows and their actions.
/// Platform-independent: No UI thread dispatching.
/// </summary>
public class WorkflowService(Interface.IActionExecutor actionExecutor)
{
    /// <summary>
    /// Executes a workflow with all its actions sequentially.
    /// </summary>
    /// <param name="workflow">The workflow to execute</param>
    /// <param name="context">Execution context containing dependencies and state</param>
    /// <exception cref="ArgumentNullException">Thrown when workflow or context is null</exception>
    public async Task ExecuteAsync(Workflow workflow, ActionExecutionContext context)
    {
        ArgumentNullException.ThrowIfNull(workflow);
        ArgumentNullException.ThrowIfNull(context);

        Debug.WriteLine($"▶ Starting workflow: {workflow.Name}");

        if (workflow.Actions.Count == 0)
        {
            Debug.WriteLine($"⚠ Workflow '{workflow.Name}' has no actions");
            return;
        }

        foreach (var action in workflow.Actions.OrderBy(a => a.Number))
        {
            try
            {
                await actionExecutor.ExecuteAsync(action, context);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error executing action #{action.Number} '{action.Name}': {ex.Message}");
                // Continue with next action even if one fails
            }
        }

        Debug.WriteLine($"✅ Workflow '{workflow.Name}' completed");
    }
}