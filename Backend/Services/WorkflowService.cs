// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Backend.Services;

using Interface;
using Domain;

using System.Diagnostics;
using System.Threading.Tasks;

/// <summary>
/// Workflow execution service.
/// Orchestrates the execution of workflows and their actions.
/// Platform-independent: No UI thread dispatching.
/// </summary>
public class WorkflowService
{
    private readonly ActionExecutor _actionExecutor;

    public WorkflowService(ActionExecutor actionExecutor)
    {
        _actionExecutor = actionExecutor;
    }

    /// <summary>
    /// Executes a workflow with all its actions sequentially.
    /// </summary>
    /// <param name="workflow">The workflow to execute</param>
    /// <param name="context">Execution context containing dependencies and state</param>
    public async Task ExecuteAsync(Workflow workflow, ActionExecutionContext context)
    {
        Debug.WriteLine($"▶ Starting workflow: {workflow.Name}");

        if (workflow.Actions == null || workflow.Actions.Count == 0)
        {
            Debug.WriteLine($"⚠ Workflow '{workflow.Name}' has no actions");
            return;
        }

        foreach (var action in workflow.Actions.OrderBy(a => a.Number))
        {
            try
            {
                await _actionExecutor.ExecuteAsync(action, context);
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
