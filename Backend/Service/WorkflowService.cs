// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Backend.Service;

using Domain;
using Domain.Enum;

using Interface;

using Microsoft.Extensions.Logging;

using System.Runtime.ExceptionServices;

/// <summary>
/// Event args for action execution errors.
/// </summary>
public class ActionExecutionErrorEventArgs : EventArgs
{
    /// <summary>
    /// Gets the workflow action that failed during execution.
    /// </summary>
    public required WorkflowAction Action { get; init; }

    /// <summary>
    /// Gets the exception that was thrown while executing the action.
    /// </summary>
    public required Exception Exception { get; init; }

    /// <summary>
    /// Gets a human-readable error message describing the failure.
    /// </summary>
    public required string ErrorMessage { get; init; }
}

/// <summary>
/// Workflow execution service.
/// Orchestrates the execution of workflows and their actions.
/// Platform-independent: No UI thread dispatching.
/// </summary>
public class WorkflowService : IWorkflowService
{
    private readonly IActionExecutor _actionExecutor;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<WorkflowService>? _logger;

    /// <summary>
    /// Creates a workflow service that uses system time.
    /// </summary>
    public WorkflowService(IActionExecutor actionExecutor, ILogger<WorkflowService>? logger = null)
        : this(actionExecutor, TimeProvider.System, logger)
    {
    }

    /// <summary>
    /// Creates a workflow service with an injectable time source for deterministic orchestration.
    /// </summary>
    public WorkflowService(
        IActionExecutor actionExecutor,
        TimeProvider timeProvider,
        ILogger<WorkflowService>? logger = null)
    {
        ArgumentNullException.ThrowIfNull(actionExecutor);
        ArgumentNullException.ThrowIfNull(timeProvider);
        _actionExecutor = actionExecutor;
        _timeProvider = timeProvider;
        _logger = logger;
    }

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
    /// <param name="options">Execution options controlling workflow failure behavior.</param>
    /// <exception cref="ArgumentNullException">Thrown when workflow or context is null</exception>
    public Task ExecuteAsync(
        Workflow workflow,
        ActionExecutionContext context,
        WorkflowExecutionOptions options = default) =>
        ExecuteAsync(workflow, context, options, CancellationToken.None);

    /// <inheritdoc />
    public async Task ExecuteAsync(
        Workflow workflow,
        ActionExecutionContext context,
        WorkflowExecutionOptions options,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(workflow);
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();

        _logger?.LogInformation("Starting workflow: {WorkflowName} (Mode: {ExecutionMode})", workflow.Name, workflow.ExecutionMode);

        if (workflow.Actions.Count == 0)
        {
            _logger?.LogWarning("Workflow '{WorkflowName}' has no actions", workflow.Name);
            return;
        }

        if (workflow.ExecutionMode == WorkflowExecutionMode.Parallel)
        {
            await ExecuteParallelAsync(workflow, context, cancellationToken).ConfigureAwait(false);
        }
        else  // Sequential (default)
        {
            await ExecuteSequentialAsync(workflow, context, options, cancellationToken).ConfigureAwait(false);
        }

        _logger?.LogInformation("Workflow '{WorkflowName}' completed", workflow.Name);
    }

    /// <summary>
    /// Executes actions sequentially, waiting for each to complete.
    /// Respects DelayAfterMs property for precise timing control.
    /// </summary>
    private async Task ExecuteSequentialAsync(
        Workflow workflow,
        ActionExecutionContext context,
        WorkflowExecutionOptions options,
        CancellationToken cancellationToken)
    {
        foreach (var action in workflow.Actions.OrderBy(a => a.Number))
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                await _actionExecutor.ExecuteAsync(action, context, cancellationToken).ConfigureAwait(false);

                // Apply per-action delay if specified
                if (action.DelayAfterMs > 0)
                {
                    _logger?.LogDebug("Waiting {DelayMs}ms after action #{ActionNumber}", action.DelayAfterMs, action.Number);
                    await Task.Delay(
                            TimeSpan.FromMilliseconds(action.DelayAfterMs),
                            _timeProvider,
                            cancellationToken)
                        .ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (FileNotFoundException fnfEx)
            {
                var errorMsg = $"Audio file not found for action '{action.Name}': {fnfEx.FileName}";
                _logger?.LogError(fnfEx, "{ErrorMessage}", errorMsg);
                OnActionExecutionError(action, fnfEx, errorMsg);
                if (options.StopOnFirstActionFailure)
                    ExceptionDispatchInfo.Capture(fnfEx).Throw();
            }
            catch (Exception ex)
            {
                var errorMsg = $"Error executing action #{action.Number} '{action.Name}': {ex.Message}";
                _logger?.LogError(ex, "{ErrorMessage}", errorMsg);
                OnActionExecutionError(action, ex, errorMsg);
                if (options.StopOnFirstActionFailure)
                    ExceptionDispatchInfo.Capture(ex).Throw();
            }
        }
    }

    /// <summary>
    /// Executes all actions in parallel (fire-and-forget).
    /// Actions start with staggered delays based on DelayAfterMs of previous actions.
    /// Example: Action1 (DelayAfterMs=0) starts at t=0, Action2 (DelayAfterMs=500) starts at t=500.
    /// Waits for all actions to complete before returning.
    /// </summary>
    private async Task ExecuteParallelAsync(
        Workflow workflow,
        ActionExecutionContext context,
        CancellationToken cancellationToken)
    {
        var tasks = new List<Task>();
        int cumulativeDelay = 0;

        foreach (var action in workflow.Actions.OrderBy(a => a.Number))
        {
            // Capture delay for this action's task
            var startDelay = cumulativeDelay;

            tasks.Add(ExecuteParallelActionAsync(action, startDelay, context, cancellationToken));

            // Accumulate delay for next action
            cumulativeDelay += action.DelayAfterMs;
        }

        await Task.WhenAll(tasks).ConfigureAwait(false);  // Wait for all actions to complete
    }

    private async Task ExecuteParallelActionAsync(
        WorkflowAction action,
        int startDelay,
        ActionExecutionContext context,
        CancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            // Wait before starting this action (staggered start)
            if (startDelay > 0)
            {
                _logger?.LogDebug("Action #{ActionNumber} waiting {StartDelay}ms before start", action.Number, startDelay);
                await Task.Delay(
                        TimeSpan.FromMilliseconds(startDelay),
                        _timeProvider,
                        cancellationToken)
                    .ConfigureAwait(false);
            }

            await _actionExecutor.ExecuteAsync(action, context, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (FileNotFoundException fnfEx)
        {
            var errorMsg = $"Audio file not found for action '{action.Name}': {fnfEx.FileName}";
            _logger?.LogError(fnfEx, "{ErrorMessage}", errorMsg);
            OnActionExecutionError(action, fnfEx, errorMsg);
        }
        catch (Exception ex)
        {
            var errorMsg = $"Error executing action #{action.Number} '{action.Name}': {ex.Message}";
            _logger?.LogError(ex, "{ErrorMessage}", errorMsg);
            OnActionExecutionError(action, ex, errorMsg);
        }
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
