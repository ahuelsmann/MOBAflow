// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Backend.Service;

using Domain;
using Domain.Enum;
using Interface;
using Microsoft.Extensions.Logging;

/// <summary>
/// Event args for action execution errors.
/// </summary>
public class ActionExecutionErrorEventArgs : EventArgs
{
    public required WorkflowAction Action { get; init; }
    public required Exception Exception { get; init; }
    public required string ErrorMessage { get; init; }
}

/// <summary>
/// Workflow execution service.
/// Orchestrates the execution of workflows and their actions.
/// Platform-independent: No UI thread dispatching.
/// </summary>
public class WorkflowService(IActionExecutor actionExecutor, ILogger<WorkflowService>? logger = null)
{
    /// <summary>
    /// Raised when an action execution fails.
    /// Subscribe to this event to display error messages in UI.
    /// </summary>
    public event EventHandler<ActionExecutionErrorEventArgs>? ActionExecutionError;

    /// <summary>
    /// Executes a workflow with all its actions according to its execution mode.
    /// Sequential: Executes actions one-by-one, respecting DelayAfterMs.
    /// Parallel: Fires all actions simultaneously without waiting.
    /// </summary>
    /// <param name="workflow">The workflow to execute</param>
    /// <param name="context">Execution context containing dependencies and state</param>
    /// <exception cref="ArgumentNullException">Thrown when workflow or context is null</exception>
    public async Task ExecuteAsync(Workflow workflow, ActionExecutionContext context)
    {
        ArgumentNullException.ThrowIfNull(workflow);
        ArgumentNullException.ThrowIfNull(context);

        logger?.LogInformation("▶ Starting workflow: {WorkflowName} (Mode: {ExecutionMode})", workflow.Name, workflow.ExecutionMode);

        if (workflow.Actions.Count == 0)
        {
            logger?.LogWarning("⚠ Workflow '{WorkflowName}' has no actions", workflow.Name);
            return;
        }

        if (workflow.ExecutionMode == WorkflowExecutionMode.Parallel)
        {
            await ExecuteParallelAsync(workflow, context).ConfigureAwait(false);
        }
        else  // Sequential (default)
        {
            await ExecuteSequentialAsync(workflow, context).ConfigureAwait(false);
        }

        logger?.LogInformation("✅ Workflow '{WorkflowName}' completed", workflow.Name);
    }

    /// <summary>
    /// Executes actions sequentially, waiting for each to complete.
    /// Respects DelayAfterMs property for precise timing control.
    /// </summary>
    private async Task ExecuteSequentialAsync(Workflow workflow, ActionExecutionContext context)
    {
        foreach (var action in workflow.Actions.OrderBy(a => a.Number))
        {
            try
            {
                await actionExecutor.ExecuteAsync(action, context).ConfigureAwait(false);

                // Apply per-action delay if specified
                if (action.DelayAfterMs > 0)
                {
                    logger?.LogDebug("    ⏱ Waiting {DelayMs}ms after action #{ActionNumber}...", action.DelayAfterMs, action.Number);
                    await Task.Delay(action.DelayAfterMs).ConfigureAwait(false);
                }
            }
            catch (FileNotFoundException fnfEx)
            {
                var errorMsg = $"Audio file not found for action '{action.Name}': {fnfEx.FileName}";
                logger?.LogError(fnfEx, "❌ {ErrorMessage}", errorMsg);
                OnActionExecutionError(action, fnfEx, errorMsg);
                // Continue with next action
            }
            catch (Exception ex)
            {
                var errorMsg = $"Error executing action #{action.Number} '{action.Name}': {ex.Message}";
                logger?.LogError(ex, "❌ {ErrorMessage}", errorMsg);
                OnActionExecutionError(action, ex, errorMsg);
                // Continue with next action even if one fails
            }
        }
    }

    /// <summary>
    /// Executes all actions in parallel (fire-and-forget).
    /// Actions start with staggered delays based on DelayAfterMs of previous actions.
    /// Example: Action1 (DelayAfterMs=0) starts at t=0, Action2 (DelayAfterMs=500) starts at t=500.
    /// Waits for all actions to complete before returning.
    /// </summary>
    private async Task ExecuteParallelAsync(Workflow workflow, ActionExecutionContext context)
    {
        var tasks = new List<Task>();
        int cumulativeDelay = 0;

        foreach (var action in workflow.Actions.OrderBy(a => a.Number))
        {
            // Capture delay for this action's task
            var startDelay = cumulativeDelay;
            
            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    // Wait before starting this action (staggered start)
                    if (startDelay > 0)
                    {
                        logger?.LogDebug("    ⏱ Action #{ActionNumber} waiting {StartDelay}ms before start...", action.Number, startDelay);
                        await Task.Delay(startDelay).ConfigureAwait(false);
                    }

                    await actionExecutor.ExecuteAsync(action, context).ConfigureAwait(false);
                }
                catch (FileNotFoundException fnfEx)
                {
                    var errorMsg = $"Audio file not found for action '{action.Name}': {fnfEx.FileName}";
                    logger?.LogError(fnfEx, "❌ {ErrorMessage}", errorMsg);
                    OnActionExecutionError(action, fnfEx, errorMsg);
                }
                catch (Exception ex)
                {
                    var errorMsg = $"Error executing action #{action.Number} '{action.Name}': {ex.Message}";
                    logger?.LogError(ex, "❌ {ErrorMessage}", errorMsg);
                    OnActionExecutionError(action, ex, errorMsg);
                }
            }));

            // Accumulate delay for next action
            cumulativeDelay += action.DelayAfterMs;
        }

        await Task.WhenAll(tasks).ConfigureAwait(false);  // Wait for all actions to complete
    }

    /// <summary>
    /// Raises the ActionExecutionError event.
    /// </summary>
    private void OnActionExecutionError(WorkflowAction action, Exception exception, string errorMessage)
    {
        ActionExecutionError?.Invoke(this, new ActionExecutionErrorEventArgs
        {
            Action = action,
            Exception = exception,
            ErrorMessage = errorMessage
        });
    }
}