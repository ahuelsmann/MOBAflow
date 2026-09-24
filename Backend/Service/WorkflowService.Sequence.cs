// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Backend.Service;

using Common.Events;
using Domain;
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
        var sourceId = GetSourceCorrelationId(request);
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
        if (workflow == null || issues.Any(issue => issue.Severity == WorkflowValidationSeverity.Error))
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
        var actions = workflow.Actions.ToArray();
        var started = _timeProvider.GetTimestamp();
        Publish(WorkflowLifecycleKind.WorkflowStarted);
        var (status, failure) = await ExecuteActionsAsync(actions, context, request.Mode, workflow.Id,
            effects, Publish, cancellationToken).ConfigureAwait(false);

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

    private static Guid GetSourceCorrelationId(WorkflowExecutionRequest request)
    {
        if (request.SourceCorrelationId != Guid.Empty)
            return request.SourceCorrelationId;
        return request.Context.SourceEvent is FeedbackReceivedEvent feedback ? feedback.CorrelationId : Guid.NewGuid();
    }

    private delegate void PublishLifecycle(WorkflowLifecycleKind kind, Guid? actionId = null,
        string? detail = null, TimeSpan? elapsed = null, string? result = null);

    private async Task<(WorkflowExecutionStatus Status, string? Failure)> ExecuteActionsAsync(
        WorkflowAction[] actions, ActionExecutionContext context, WorkflowRunMode mode, Guid workflowId,
        List<WorkflowPlannedEffect> effects, PublishLifecycle publish, CancellationToken cancellationToken)
    {
        foreach (var action in actions)
        {
            var actionStarted = _timeProvider.GetTimestamp();
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                publish(WorkflowLifecycleKind.StepStarted, action.Id);
                await ExecuteActionAsync(action, context, mode, effects, publish, cancellationToken).ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();
                publish(WorkflowLifecycleKind.StepCompleted, action.Id,
                    mode == WorkflowRunMode.DryRun && action.DelayAfterMs > 0 ? $"Planned delay after action: {action.DelayAfterMs} ms." : null,
                    _timeProvider.GetElapsedTime(actionStarted));
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return (WorkflowExecutionStatus.Cancelled, null);
            }
            catch (Exception ex) when (ex is not (OutOfMemoryException or StackOverflowException or AccessViolationException))
            {
                var failure = $"Action execution failed ({ex.GetType().Name}).";
                if (_logger != null)
                    LogActionFailure(_logger, ex, workflowId, action.Id);
                publish(WorkflowLifecycleKind.StepFailed, action.Id, failure, _timeProvider.GetElapsedTime(actionStarted));
                return (WorkflowExecutionStatus.Failed, failure);
            }
        }

        return (WorkflowExecutionStatus.Succeeded, null);
    }

    private async Task ExecuteActionAsync(WorkflowAction action, ActionExecutionContext context, WorkflowRunMode mode,
        List<WorkflowPlannedEffect> effects, PublishLifecycle publish, CancellationToken cancellationToken)
    {
        if (mode == WorkflowRunMode.DryRun)
        {
            var effect = _effectPlanner.Plan(action).Effect
                ?? throw new InvalidOperationException("Validated action did not produce an effect plan.");
            effects.Add(effect);
            publish(WorkflowLifecycleKind.PlannedEffect, action.Id, effect.Description);
            return;
        }

        await _actionExecutor.ExecuteAsync(action, context, cancellationToken).ConfigureAwait(false);
        if (action.DelayAfterMs > 0)
            await Task.Delay(TimeSpan.FromMilliseconds(action.DelayAfterMs), _timeProvider, cancellationToken).ConfigureAwait(false);
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Workflow {WorkflowId} failed at action {ActionId}")]
    private static partial void LogActionFailure(ILogger logger, Exception exception, Guid workflowId, Guid? actionId);

}
