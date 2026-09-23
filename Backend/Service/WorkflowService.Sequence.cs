// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Backend.Service;

using Common.Events;
using Interface;
using Microsoft.Extensions.Logging;

public partial class WorkflowService
{
    /// <inheritdoc />
    public async Task<WorkflowExecutionResult> ExecuteAsync(
        WorkflowExecutionRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Project);
        ArgumentNullException.ThrowIfNull(request.Workflow);
        ArgumentNullException.ThrowIfNull(request.Context);

        var executionId = Guid.NewGuid();
        var sourceId = request.SourceCorrelationId == Guid.Empty
            ? request.Context.SourceEvent is FeedbackReceivedEvent feedback ? feedback.CorrelationId : Guid.NewGuid()
            : request.SourceCorrelationId;
        var sequence = 0L;
        var effects = new List<WorkflowPlannedEffect>();
        void Publish(WorkflowLifecycleKind kind, Guid? actionId = null, string? detail = null, TimeSpan? elapsed = null, string? result = null)
        {
            var entry = new WorkflowLifecycleEvent
            {
                Kind = kind,
                ExecutionId = executionId,
                SourceCorrelationId = sourceId,
                WorkflowId = request.Workflow.Id,
                StepId = actionId,
                Attempt = actionId.HasValue ? 1 : 0,
                Sequence = ++sequence,
                Mode = request.Mode == WorkflowRunMode.DryRun ? WorkflowLifecycleMode.DryRun : WorkflowLifecycleMode.Live,
                TimestampUtc = _timeProvider.GetUtcNow(),
                Detail = detail,
                Result = result,
                Elapsed = elapsed
            };
            _traceStore.Append(entry);
            _eventBus?.Publish(entry);
        }

        // An unfinished library draft must not block another valid workflow's event.
        var issues = _workflowValidator.Validate(request.Project).Issues
            .Where(issue => issue.WorkflowId == request.Workflow.Id).ToList();
        var workflow = request.Project.Workflows.FirstOrDefault(candidate => candidate.Id == request.Workflow.Id);
        if (workflow == null)
            issues.Add(new WorkflowValidationIssue(WorkflowValidationCodes.MissingWorkflow, WorkflowValidationSeverity.Error,
                request.Workflow.Id, null, "workflowId", "Execution workflow must belong to the active project."));
        if (issues.Any(issue => issue.Severity == WorkflowValidationSeverity.Error))
        {
            foreach (var issue in issues)
                Publish(WorkflowLifecycleKind.ValidationFailed, issue.StepId, $"{issue.Code}: {issue.Message}");
            return new WorkflowExecutionResult
            {
                ExecutionId = executionId,
                WorkflowId = request.Workflow.Id,
                SourceCorrelationId = sourceId,
                Status = WorkflowExecutionStatus.NotStarted,
                ValidationIssues = issues
            };
        }

        var context = new ActionExecutionContextFactory(request.Context).CreateForExecution();
        context.CurrentProject = request.Project;
        var actions = workflow!.Actions.ToArray();
        var started = _timeProvider.GetTimestamp();
        var status = WorkflowExecutionStatus.Succeeded;
        string? failure = null;
        Publish(WorkflowLifecycleKind.WorkflowStarted);
        foreach (var action in actions)
        {
            var actionStarted = _timeProvider.GetTimestamp();
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                Publish(WorkflowLifecycleKind.StepStarted, action.Id);
                if (request.Mode == WorkflowRunMode.DryRun)
                {
                    var effect = _effectPlanner.Plan(action).Effect
                        ?? throw new InvalidOperationException("Validated action did not produce an effect plan.");
                    effects.Add(effect);
                    Publish(WorkflowLifecycleKind.PlannedEffect, action.Id, effect.Description);
                }
                else
                {
                    await _actionExecutor.ExecuteAsync(action, context, cancellationToken).ConfigureAwait(false);
                    if (action.DelayAfterMs > 0)
                        await Task.Delay(TimeSpan.FromMilliseconds(action.DelayAfterMs), _timeProvider, cancellationToken).ConfigureAwait(false);
                }
                cancellationToken.ThrowIfCancellationRequested();
                Publish(WorkflowLifecycleKind.StepCompleted, action.Id,
                    request.Mode == WorkflowRunMode.DryRun && action.DelayAfterMs > 0 ? $"Planned delay after action: {action.DelayAfterMs} ms." : null,
                    _timeProvider.GetElapsedTime(actionStarted));
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                status = WorkflowExecutionStatus.Cancelled;
                break;
            }
            catch (Exception ex) when (ex is not (OutOfMemoryException or StackOverflowException or AccessViolationException))
            {
                status = WorkflowExecutionStatus.Failed;
                failure = $"Action execution failed ({ex.GetType().Name}).";
                if (_logger != null)
                    LogActionFailure(_logger, ex, workflow.Id, action.Id);
                Publish(WorkflowLifecycleKind.StepFailed, action.Id, failure);
                break;
            }
        }

        Publish(status switch
        {
            WorkflowExecutionStatus.Succeeded => WorkflowLifecycleKind.WorkflowCompleted,
            WorkflowExecutionStatus.Cancelled => WorkflowLifecycleKind.WorkflowCancelled,
            _ => WorkflowLifecycleKind.WorkflowFailed
        }, detail: failure, elapsed: _timeProvider.GetElapsedTime(started), result: status.ToString());
        return new WorkflowExecutionResult
        {
            ExecutionId = executionId,
            WorkflowId = workflow.Id,
            SourceCorrelationId = sourceId,
            Status = status,
            PlannedEffects = effects,
            FailureDetail = failure
        };
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Workflow {WorkflowId} failed at action {ActionId}")]
    private static partial void LogActionFailure(ILogger logger, Exception exception, Guid workflowId, Guid? actionId);

}
