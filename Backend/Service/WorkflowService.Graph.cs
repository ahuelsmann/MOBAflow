// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Backend.Service;

using Common.Events;

using Domain;

using Interface;

using Microsoft.Extensions.Logging;

/// <summary>
/// Workflow 2.0 graph execution implementation.
/// </summary>
public partial class WorkflowService
{
    private const int MaximumNestedWorkflowDepth = 16;

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
        var sourceCorrelationId = request.SourceCorrelationId == Guid.Empty
            ? Guid.NewGuid()
            : request.SourceCorrelationId;
        var sequence = new WorkflowTraceSequence();
        var validation = _workflowValidator.Validate(request.Project);
        if (!request.Project.Workflows.Any(workflow => workflow.Id == request.Workflow.Id))
        {
            validation.Add(new WorkflowValidationIssue(
                WorkflowValidationCodes.MissingWorkflow,
                WorkflowValidationSeverity.Error,
                request.Workflow.Id,
                null,
                "workflowId",
                "Execution workflow must belong to the active project snapshot."));
        }
        if (!validation.IsValid)
        {
            foreach (var issue in validation.Issues)
            {
                PublishLifecycle(
                    WorkflowLifecycleKind.ValidationFailed,
                    sourceCorrelationId,
                    executionId,
                    null,
                    issue.WorkflowId,
                    issue.StepId,
                    sequence,
                    request.Mode,
                    detail: $"{issue.Code}: {issue.Message}");
            }

            return new WorkflowExecutionResult
            {
                ExecutionId = executionId,
                WorkflowId = request.Workflow.Id,
                SourceCorrelationId = sourceCorrelationId,
                Status = WorkflowExecutionStatus.NotStarted,
                ValidationIssues = validation.Issues
            };
        }

        var executionWorkflow = request.Project.Workflows.Single(workflow => workflow.Id == request.Workflow.Id);
        var plannedEffects = new List<WorkflowPlannedEffect>();
        var frame = new WorkflowExecutionFrame(
            sourceCorrelationId,
            executionId,
            null,
            request.Mode,
            sequence,
            1,
            new HashSet<Guid>());
        var internalResult = await ExecuteWorkflowGraphAsync(
                request.Project,
                executionWorkflow,
                request.Context,
                frame,
                plannedEffects,
                cancellationToken)
            .ConfigureAwait(false);

        return new WorkflowExecutionResult
        {
            ExecutionId = executionId,
            WorkflowId = executionWorkflow.Id,
            SourceCorrelationId = sourceCorrelationId,
            Status = internalResult.Status,
            PlannedEffects = plannedEffects,
            FailureDetail = internalResult.FailureDetail
        };
    }

    private async Task<InternalWorkflowResult> ExecuteWorkflowGraphAsync(
        Project project,
        Workflow workflow,
        ActionExecutionContext context,
        WorkflowExecutionFrame frame,
        List<WorkflowPlannedEffect> plannedEffects,
        CancellationToken cancellationToken)
    {
        var startedTimestamp = _timeProvider.GetTimestamp();
        PublishLifecycle(
            WorkflowLifecycleKind.WorkflowStarted,
            frame,
            workflow.Id,
            mode: frame.Mode);

        InternalWorkflowResult result;
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (frame.Depth > MaximumNestedWorkflowDepth || frame.CallStack.Contains(workflow.Id))
            {
                result = InternalWorkflowResult.Failed("Nested workflow depth or recursion guard rejected the call.");
            }
            else
            {
                var callStack = new HashSet<Guid>(frame.CallStack) { workflow.Id };
                result = await TraverseAsync(
                        project,
                        workflow,
                        workflow.EntryStepId!.Value,
                        null,
                        context,
                        frame with { CallStack = callStack },
                        plannedEffects,
                        cancellationToken)
                    .ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            result = InternalWorkflowResult.Cancelled();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Workflow graph execution failed for {WorkflowId}", workflow.Id);
            result = InternalWorkflowResult.Failed(SanitizeFailure(ex));
        }

        var terminalKind = result.Status switch
        {
            WorkflowExecutionStatus.Succeeded => WorkflowLifecycleKind.WorkflowCompleted,
            WorkflowExecutionStatus.Cancelled => WorkflowLifecycleKind.WorkflowCancelled,
            _ => WorkflowLifecycleKind.WorkflowFailed
        };
        PublishLifecycle(
            terminalKind,
            frame,
            workflow.Id,
            elapsed: _timeProvider.GetElapsedTime(startedTimestamp),
            result: result.Status.ToString(),
            detail: result.FailureDetail,
            mode: frame.Mode);
        return result;
    }

    private async Task<InternalWorkflowResult> TraverseAsync(
        Project project,
        Workflow workflow,
        Guid entryStepId,
        Guid? stopBeforeStepId,
        ActionExecutionContext context,
        WorkflowExecutionFrame frame,
        List<WorkflowPlannedEffect> plannedEffects,
        CancellationToken cancellationToken)
    {
        var steps = workflow.Steps!.ToDictionary(step => step.Id);
        var currentStepId = entryStepId;
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (stopBeforeStepId.HasValue && currentStepId == stopBeforeStepId.Value)
                return InternalWorkflowResult.Succeeded();

            var step = steps[currentStepId];
            var outcome = await ExecuteStepWithPolicyAsync(
                    project,
                    workflow,
                    step,
                    context,
                    frame,
                    plannedEffects,
                    cancellationToken)
                .ConfigureAwait(false);
            if (outcome.TerminalResult != null)
                return outcome.TerminalResult;
            if (!outcome.NextStepId.HasValue)
                return InternalWorkflowResult.Failed("A non-terminal step did not select a successor.");

            currentStepId = outcome.NextStepId.Value;
        }
    }

    private async Task<WorkflowStepOutcome> ExecuteStepWithPolicyAsync(
        Project project,
        Workflow workflow,
        WorkflowStep step,
        ActionExecutionContext context,
        WorkflowExecutionFrame frame,
        List<WorkflowPlannedEffect> plannedEffects,
        CancellationToken cancellationToken)
    {
        var policy = step.ErrorPolicy ?? workflow.DefaultErrorPolicy ?? new WorkflowErrorPolicy();
        var maximumAttempts = 1 + (policy.Retry?.AdditionalAttempts ?? 0);
        for (var attempt = 1; attempt <= maximumAttempts; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var startedTimestamp = _timeProvider.GetTimestamp();
            PublishLifecycle(
                WorkflowLifecycleKind.StepStarted,
                frame,
                workflow.Id,
                step.Id,
                attempt,
                mode: frame.Mode);
            try
            {
                var outcome = await ExecuteStepOnceAsync(
                        project,
                        workflow,
                        step,
                        context,
                        frame,
                        plannedEffects,
                        cancellationToken)
                    .ConfigureAwait(false);
                PublishLifecycle(
                    WorkflowLifecycleKind.StepCompleted,
                    frame,
                    workflow.Id,
                    step.Id,
                    attempt,
                    _timeProvider.GetElapsedTime(startedTimestamp),
                    result: outcome.TerminalResult?.Status.ToString(),
                    detail: outcome.Detail,
                    mode: frame.Mode);
                return outcome;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                var failure = SanitizeFailure(ex);
                PublishLifecycle(
                    WorkflowLifecycleKind.StepFailed,
                    frame,
                    workflow.Id,
                    step.Id,
                    attempt,
                    _timeProvider.GetElapsedTime(startedTimestamp),
                    detail: failure,
                    mode: frame.Mode);

                if (attempt < maximumAttempts)
                {
                    PublishLifecycle(
                        WorkflowLifecycleKind.RetryScheduled,
                        frame,
                        workflow.Id,
                        step.Id,
                        attempt + 1,
                        detail: $"Retry after {policy.Retry!.DelayMs} ms.",
                        mode: frame.Mode);
                    if (frame.Mode == WorkflowRunMode.Live && policy.Retry.DelayMs > 0)
                    {
                        await Task.Delay(
                                TimeSpan.FromMilliseconds(policy.Retry.DelayMs),
                                _timeProvider,
                                cancellationToken)
                            .ConfigureAwait(false);
                    }
                    continue;
                }

                return policy.Behavior switch
                {
                    WorkflowFailureBehavior.Continue when step.NextStepId.HasValue =>
                        WorkflowStepOutcome.ContinueAt(step.NextStepId.Value),
                    WorkflowFailureBehavior.FailureBranch when policy.FailureStepId.HasValue =>
                        WorkflowStepOutcome.ContinueAt(policy.FailureStepId.Value),
                    _ => WorkflowStepOutcome.Terminate(InternalWorkflowResult.Failed(failure))
                };
            }
        }

        return WorkflowStepOutcome.Terminate(InternalWorkflowResult.Failed("Retry policy exhausted."));
    }

    private async Task<WorkflowStepOutcome> ExecuteStepOnceAsync(
        Project project,
        Workflow workflow,
        WorkflowStep step,
        ActionExecutionContext context,
        WorkflowExecutionFrame frame,
        List<WorkflowPlannedEffect> plannedEffects,
        CancellationToken cancellationToken)
    {
        switch (step)
        {
            case WorkflowActionStep { Action: { } action }:
                if (frame.Mode == WorkflowRunMode.DryRun)
                {
                    var plan = _effectPlanner.Plan(action);
                    var effect = plan.Effect ?? throw new InvalidOperationException("Validated action did not produce an effect plan.");
                    plannedEffects.Add(effect);
                    PublishLifecycle(
                        WorkflowLifecycleKind.PlannedEffect,
                        frame,
                        workflow.Id,
                        step.Id,
                        detail: effect.Description,
                        mode: frame.Mode);
                }
                else
                {
                    await _actionExecutor.ExecuteAsync(action, context, cancellationToken).ConfigureAwait(false);
                }
                return WorkflowStepOutcome.ContinueAt(step.NextStepId!.Value);

            case WorkflowDelayStep delay:
                if (frame.Mode == WorkflowRunMode.Live && delay.DelayMs > 0)
                {
                    await Task.Delay(
                            TimeSpan.FromMilliseconds(delay.DelayMs),
                            _timeProvider,
                            cancellationToken)
                        .ConfigureAwait(false);
                }
                return WorkflowStepOutcome.ContinueAt(
                    step.NextStepId!.Value,
                    frame.Mode == WorkflowRunMode.DryRun ? $"Planned delay: {delay.DelayMs} ms." : null);

            case WorkflowConditionStep conditionStep:
                var decision = await _conditionEvaluator.EvaluateAsync(
                        conditionStep.Condition!,
                        context,
                        cancellationToken)
                    .ConfigureAwait(false);
                PublishLifecycle(
                    WorkflowLifecycleKind.ConditionDecided,
                    frame,
                    workflow.Id,
                    step.Id,
                    result: decision.ToString(),
                    mode: frame.Mode);
                return WorkflowStepOutcome.ContinueAt(decision ? conditionStep.TrueStepId : conditionStep.FalseStepId);

            case WorkflowParallelStep parallel:
                var branchEffects = parallel.Branches.Select(_ => new List<WorkflowPlannedEffect>()).ToArray();
                var branchTasks = parallel.Branches
                    .Select((branch, index) => TraverseAsync(
                        project,
                        workflow,
                        branch.EntryStepId,
                        parallel.JoinStepId,
                        context,
                        frame,
                        branchEffects[index],
                        cancellationToken))
                    .ToArray();
                var branchResults = await Task.WhenAll(branchTasks).ConfigureAwait(false);
                foreach (var effects in branchEffects)
                    plannedEffects.AddRange(effects);
                var failedBranch = branchResults.FirstOrDefault(result => result.Status != WorkflowExecutionStatus.Succeeded);
                if (failedBranch != null)
                    throw new ParallelWorkflowExecutionException(failedBranch.Status);
                return WorkflowStepOutcome.ContinueAt(parallel.JoinStepId);

            case WorkflowNestedStep nested:
            {
                var childWorkflow = project.Workflows.Single(candidate => candidate.Id == nested.WorkflowId);
                var childExecutionId = Guid.NewGuid();
                PublishLifecycle(
                    WorkflowLifecycleKind.NestedWorkflowEntered,
                    frame,
                    workflow.Id,
                    step.Id,
                    detail: childWorkflow.Id.ToString("D"),
                    mode: frame.Mode);
                var childFrame = frame with
                {
                    ExecutionId = childExecutionId,
                    ParentExecutionId = frame.ExecutionId,
                    Depth = frame.Depth + 1
                };
                using var childCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                var childResult = await ExecuteWorkflowGraphAsync(
                        project,
                        childWorkflow,
                        context,
                        childFrame,
                        plannedEffects,
                        childCancellation.Token)
                    .ConfigureAwait(false);
                PublishLifecycle(
                    WorkflowLifecycleKind.NestedWorkflowExited,
                    frame,
                    workflow.Id,
                    step.Id,
                    result: childResult.Status.ToString(),
                    mode: frame.Mode);
                if (childResult.Status == WorkflowExecutionStatus.Cancelled && cancellationToken.IsCancellationRequested)
                    cancellationToken.ThrowIfCancellationRequested();
                if (childResult.Status != WorkflowExecutionStatus.Succeeded)
                    throw new NestedWorkflowExecutionException(childResult.Status);
                return WorkflowStepOutcome.ContinueAt(step.NextStepId!.Value);
            }

            case WorkflowTerminateStep terminal:
                return WorkflowStepOutcome.Terminate(terminal.Result switch
                {
                    WorkflowTerminationResult.Succeeded => InternalWorkflowResult.Succeeded(),
                    WorkflowTerminationResult.Cancelled => InternalWorkflowResult.Cancelled(),
                    _ => InternalWorkflowResult.Failed("Workflow reached an explicit failed termination.")
                });

            default:
                throw new NotSupportedException($"Workflow step type '{step.GetType().Name}' is not supported.");
        }
    }

    private void PublishLifecycle(
        WorkflowLifecycleKind kind,
        WorkflowExecutionFrame frame,
        Guid workflowId,
        Guid? stepId = null,
        int attempt = 0,
        TimeSpan? elapsed = null,
        string? result = null,
        string? detail = null,
        WorkflowRunMode? mode = null) =>
        PublishLifecycle(
            kind,
            frame.SourceCorrelationId,
            frame.ExecutionId,
            frame.ParentExecutionId,
            workflowId,
            stepId,
            frame.Sequence,
            mode ?? frame.Mode,
            attempt,
            elapsed,
            result,
            detail);

    private void PublishLifecycle(
        WorkflowLifecycleKind kind,
        Guid sourceCorrelationId,
        Guid executionId,
        Guid? parentExecutionId,
        Guid workflowId,
        Guid? stepId,
        WorkflowTraceSequence sequence,
        WorkflowRunMode mode,
        int attempt = 0,
        TimeSpan? elapsed = null,
        string? result = null,
        string? detail = null)
    {
        lock (sequence.SyncRoot)
        {
            var lifecycleEvent = new WorkflowLifecycleEvent
            {
                Kind = kind,
                SourceCorrelationId = sourceCorrelationId,
                ExecutionId = executionId,
                ParentExecutionId = parentExecutionId,
                WorkflowId = workflowId,
                StepId = stepId,
                Sequence = sequence.Next(),
                Attempt = attempt,
                Mode = mode == WorkflowRunMode.Live
                    ? WorkflowLifecycleMode.Live
                    : WorkflowLifecycleMode.DryRun,
                TimestampUtc = _timeProvider.GetUtcNow(),
                Elapsed = elapsed,
                Result = result,
                Detail = detail
            };
            _traceStore.Append(lifecycleEvent);
            _eventBus?.Publish(lifecycleEvent);
        }
    }

    private static string SanitizeFailure(Exception exception) => exception switch
    {
        FileNotFoundException => "A required file was not found.",
        ArgumentException => "The step payload was rejected.",
        NestedWorkflowExecutionException nested => $"Nested workflow ended with status {nested.Status}.",
        ParallelWorkflowExecutionException parallel => $"Parallel branch ended with status {parallel.Status}.",
        _ => $"Step execution failed ({exception.GetType().Name})."
    };

    private sealed class WorkflowTraceSequence
    {
        private long _value;

        public object SyncRoot { get; } = new();

        public long Next() => ++_value;
    }

    private sealed record WorkflowExecutionFrame(
        Guid SourceCorrelationId,
        Guid ExecutionId,
        Guid? ParentExecutionId,
        WorkflowRunMode Mode,
        WorkflowTraceSequence Sequence,
        int Depth,
        IReadOnlySet<Guid> CallStack);

    private sealed record InternalWorkflowResult(WorkflowExecutionStatus Status, string? FailureDetail)
    {
        public static InternalWorkflowResult Succeeded() => new(WorkflowExecutionStatus.Succeeded, null);
        public static InternalWorkflowResult Cancelled() => new(WorkflowExecutionStatus.Cancelled, null);
        public static InternalWorkflowResult Failed(string detail) => new(WorkflowExecutionStatus.Failed, detail);
    }

    private sealed record WorkflowStepOutcome(
        Guid? NextStepId,
        InternalWorkflowResult? TerminalResult,
        string? Detail)
    {
        public static WorkflowStepOutcome ContinueAt(Guid stepId, string? detail = null) => new(stepId, null, detail);
        public static WorkflowStepOutcome Terminate(InternalWorkflowResult result) => new(null, result, null);
    }

    private sealed class NestedWorkflowExecutionException(WorkflowExecutionStatus status) : Exception
    {
        public WorkflowExecutionStatus Status { get; } = status;
    }

    private sealed class ParallelWorkflowExecutionException(WorkflowExecutionStatus status) : Exception
    {
        public WorkflowExecutionStatus Status { get; } = status;
    }
}
