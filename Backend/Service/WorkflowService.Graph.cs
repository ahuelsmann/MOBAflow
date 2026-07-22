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
        var frame = new WorkflowExecutionFrame(
            sourceCorrelationId,
            executionId,
            null,
            request.Mode,
            sequence,
            1,
            new HashSet<Guid>());
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
                    frame,
                    issue.WorkflowId,
                    new WorkflowLifecycleDetails
                    {
                        StepId = issue.StepId,
                        Detail = $"{issue.Code}: {issue.Message}"
                    });
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
            workflow.Id);

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
                var graphContext = new WorkflowGraphContext(
                    project,
                    workflow,
                    context,
                    frame with { CallStack = callStack },
                    plannedEffects);
                result = await TraverseAsync(
                        graphContext,
                        workflow.EntryStepId!.Value,
                        null,
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
            new WorkflowLifecycleDetails
            {
                Elapsed = _timeProvider.GetElapsedTime(startedTimestamp),
                Result = result.Status.ToString(),
                Detail = result.FailureDetail
            });
        return result;
    }

    private async Task<InternalWorkflowResult> TraverseAsync(
        WorkflowGraphContext graphContext,
        Guid entryStepId,
        Guid? stopBeforeStepId,
        CancellationToken cancellationToken)
    {
        var steps = graphContext.Workflow.Steps!.ToDictionary(step => step.Id);
        var currentStepId = entryStepId;
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (stopBeforeStepId.HasValue && currentStepId == stopBeforeStepId.Value)
                return InternalWorkflowResult.Succeeded();

            var step = steps[currentStepId];
            var outcome = await ExecuteStepWithPolicyAsync(
                    graphContext,
                    step,
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
        WorkflowGraphContext graphContext,
        WorkflowStep step,
        CancellationToken cancellationToken)
    {
        var workflow = graphContext.Workflow;
        var frame = graphContext.Frame;
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
                new WorkflowLifecycleDetails
                {
                    StepId = step.Id,
                    Attempt = attempt
                });
            try
            {
                var outcome = await ExecuteStepOnceAsync(
                        graphContext,
                        step,
                        cancellationToken)
                    .ConfigureAwait(false);
                PublishLifecycle(
                    WorkflowLifecycleKind.StepCompleted,
                    frame,
                    workflow.Id,
                    new WorkflowLifecycleDetails
                    {
                        StepId = step.Id,
                        Attempt = attempt,
                        Elapsed = _timeProvider.GetElapsedTime(startedTimestamp),
                        Result = outcome.TerminalResult?.Status.ToString(),
                        Detail = outcome.Detail
                    });
                return outcome;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                var resolution = await HandleStepFailureAsync(
                        graphContext,
                        new WorkflowStepAttempt(step, policy, attempt, maximumAttempts, startedTimestamp),
                        ex,
                        cancellationToken)
                    .ConfigureAwait(false);
                if (resolution.ShouldRetry)
                {
                    continue;
                }

                return resolution.Outcome!;
            }
        }

        return WorkflowStepOutcome.Terminate(InternalWorkflowResult.Failed("Retry policy exhausted."));
    }

    private async Task<StepFailureResolution> HandleStepFailureAsync(
        WorkflowGraphContext graphContext,
        WorkflowStepAttempt stepAttempt,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var failure = SanitizeFailure(exception);
        PublishLifecycle(
            WorkflowLifecycleKind.StepFailed,
            graphContext.Frame,
            graphContext.Workflow.Id,
            new WorkflowLifecycleDetails
            {
                StepId = stepAttempt.Step.Id,
                Attempt = stepAttempt.Attempt,
                Elapsed = _timeProvider.GetElapsedTime(stepAttempt.StartedTimestamp),
                Detail = failure
            });

        if (stepAttempt.Attempt < stepAttempt.MaximumAttempts)
        {
            var retryDelay = stepAttempt.Policy.Retry!.DelayMs;
            PublishLifecycle(
                WorkflowLifecycleKind.RetryScheduled,
                graphContext.Frame,
                graphContext.Workflow.Id,
                new WorkflowLifecycleDetails
                {
                    StepId = stepAttempt.Step.Id,
                    Attempt = stepAttempt.Attempt + 1,
                    Detail = $"Retry after {retryDelay} ms."
                });
            if (graphContext.Frame.Mode == WorkflowRunMode.Live && retryDelay > 0)
            {
                await Task.Delay(
                        TimeSpan.FromMilliseconds(retryDelay),
                        _timeProvider,
                        cancellationToken)
                    .ConfigureAwait(false);
            }

            return StepFailureResolution.Retry();
        }

        var outcome = stepAttempt.Policy.Behavior switch
        {
            WorkflowFailureBehavior.Continue when stepAttempt.Step.NextStepId.HasValue =>
                WorkflowStepOutcome.ContinueAt(stepAttempt.Step.NextStepId.Value),
            WorkflowFailureBehavior.FailureBranch when stepAttempt.Policy.FailureStepId.HasValue =>
                WorkflowStepOutcome.ContinueAt(stepAttempt.Policy.FailureStepId.Value),
            _ => WorkflowStepOutcome.Terminate(InternalWorkflowResult.Failed(failure))
        };
        return StepFailureResolution.Complete(outcome);
    }

    private async Task<WorkflowStepOutcome> ExecuteStepOnceAsync(
        WorkflowGraphContext graphContext,
        WorkflowStep step,
        CancellationToken cancellationToken)
    {
        return step switch
        {
            WorkflowActionStep { Action: { } } actionStep =>
                await ExecuteActionStepAsync(graphContext, actionStep, cancellationToken).ConfigureAwait(false),
            WorkflowDelayStep delayStep =>
                await ExecuteDelayStepAsync(graphContext, delayStep, cancellationToken).ConfigureAwait(false),
            WorkflowConditionStep conditionStep =>
                await ExecuteConditionStepAsync(graphContext, conditionStep, cancellationToken).ConfigureAwait(false),
            WorkflowParallelStep parallelStep =>
                await ExecuteParallelStepAsync(graphContext, parallelStep, cancellationToken).ConfigureAwait(false),
            WorkflowNestedStep nestedStep =>
                await ExecuteNestedStepAsync(graphContext, nestedStep, cancellationToken).ConfigureAwait(false),
            WorkflowTerminateStep terminalStep => ExecuteTerminateStep(terminalStep),
            _ => throw new NotSupportedException($"Workflow step type '{step.GetType().Name}' is not supported.")
        };
    }

    private async Task<WorkflowStepOutcome> ExecuteActionStepAsync(
        WorkflowGraphContext graphContext,
        WorkflowActionStep step,
        CancellationToken cancellationToken)
    {
        var action = step.Action!;
        if (graphContext.Frame.Mode == WorkflowRunMode.DryRun)
        {
            var plan = _effectPlanner.Plan(action);
            var effect = plan.Effect ?? throw new InvalidOperationException("Validated action did not produce an effect plan.");
            graphContext.PlannedEffects.Add(effect);
            PublishLifecycle(
                WorkflowLifecycleKind.PlannedEffect,
                graphContext.Frame,
                graphContext.Workflow.Id,
                new WorkflowLifecycleDetails
                {
                    StepId = step.Id,
                    Detail = effect.Description
                });
        }
        else
        {
            await _actionExecutor.ExecuteAsync(action, graphContext.ActionContext, cancellationToken).ConfigureAwait(false);
        }

        return WorkflowStepOutcome.ContinueAt(step.NextStepId!.Value);
    }

    private async Task<WorkflowStepOutcome> ExecuteDelayStepAsync(
        WorkflowGraphContext graphContext,
        WorkflowDelayStep step,
        CancellationToken cancellationToken)
    {
        if (graphContext.Frame.Mode == WorkflowRunMode.Live && step.DelayMs > 0)
        {
            await Task.Delay(
                    TimeSpan.FromMilliseconds(step.DelayMs),
                    _timeProvider,
                    cancellationToken)
                .ConfigureAwait(false);
        }

        return WorkflowStepOutcome.ContinueAt(
            step.NextStepId!.Value,
            graphContext.Frame.Mode == WorkflowRunMode.DryRun ? $"Planned delay: {step.DelayMs} ms." : null);
    }

    private async Task<WorkflowStepOutcome> ExecuteConditionStepAsync(
        WorkflowGraphContext graphContext,
        WorkflowConditionStep step,
        CancellationToken cancellationToken)
    {
        var decision = await _conditionEvaluator.EvaluateAsync(
                step.Condition!,
                graphContext.ActionContext,
                cancellationToken)
            .ConfigureAwait(false);
        PublishLifecycle(
            WorkflowLifecycleKind.ConditionDecided,
            graphContext.Frame,
            graphContext.Workflow.Id,
            new WorkflowLifecycleDetails
            {
                StepId = step.Id,
                Result = decision.ToString()
            });
        return WorkflowStepOutcome.ContinueAt(decision ? step.TrueStepId : step.FalseStepId);
    }

    private async Task<WorkflowStepOutcome> ExecuteParallelStepAsync(
        WorkflowGraphContext graphContext,
        WorkflowParallelStep step,
        CancellationToken cancellationToken)
    {
        var branchEffects = step.Branches.Select(_ => new List<WorkflowPlannedEffect>()).ToArray();
        var branchTasks = step.Branches
            .Select((branch, index) => TraverseAsync(
                graphContext with { PlannedEffects = branchEffects[index] },
                branch.EntryStepId,
                step.JoinStepId,
                cancellationToken))
            .ToArray();
        var branchResults = await Task.WhenAll(branchTasks).ConfigureAwait(false);
        foreach (var effects in branchEffects)
        {
            graphContext.PlannedEffects.AddRange(effects);
        }

        var failedBranch = branchResults.FirstOrDefault(result => result.Status != WorkflowExecutionStatus.Succeeded);
        if (failedBranch != null)
        {
            throw new ParallelWorkflowExecutionException(failedBranch.Status);
        }

        return WorkflowStepOutcome.ContinueAt(step.JoinStepId);
    }

    private async Task<WorkflowStepOutcome> ExecuteNestedStepAsync(
        WorkflowGraphContext graphContext,
        WorkflowNestedStep step,
        CancellationToken cancellationToken)
    {
        var childWorkflow = graphContext.Project.Workflows.Single(candidate => candidate.Id == step.WorkflowId);
        PublishLifecycle(
            WorkflowLifecycleKind.NestedWorkflowEntered,
            graphContext.Frame,
            graphContext.Workflow.Id,
            new WorkflowLifecycleDetails
            {
                StepId = step.Id,
                Detail = childWorkflow.Id.ToString("D")
            });
        var childFrame = graphContext.Frame with
        {
            ExecutionId = Guid.NewGuid(),
            ParentExecutionId = graphContext.Frame.ExecutionId,
            Depth = graphContext.Frame.Depth + 1
        };
        using var childCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var childResult = await ExecuteWorkflowGraphAsync(
                graphContext.Project,
                childWorkflow,
                graphContext.ActionContext,
                childFrame,
                graphContext.PlannedEffects,
                childCancellation.Token)
            .ConfigureAwait(false);
        PublishLifecycle(
            WorkflowLifecycleKind.NestedWorkflowExited,
            graphContext.Frame,
            graphContext.Workflow.Id,
            new WorkflowLifecycleDetails
            {
                StepId = step.Id,
                Result = childResult.Status.ToString()
            });
        if (childResult.Status == WorkflowExecutionStatus.Cancelled && cancellationToken.IsCancellationRequested)
        {
            cancellationToken.ThrowIfCancellationRequested();
        }

        if (childResult.Status != WorkflowExecutionStatus.Succeeded)
        {
            throw new NestedWorkflowExecutionException(childResult.Status);
        }

        return WorkflowStepOutcome.ContinueAt(step.NextStepId!.Value);
    }

    private static WorkflowStepOutcome ExecuteTerminateStep(WorkflowTerminateStep step) =>
        WorkflowStepOutcome.Terminate(step.Result switch
        {
            WorkflowTerminationResult.Succeeded => InternalWorkflowResult.Succeeded(),
            WorkflowTerminationResult.Cancelled => InternalWorkflowResult.Cancelled(),
            _ => InternalWorkflowResult.Failed("Workflow reached an explicit failed termination.")
        });

    private void PublishLifecycle(
        WorkflowLifecycleKind kind,
        WorkflowExecutionFrame frame,
        Guid workflowId,
        WorkflowLifecycleDetails? details = null)
    {
        details ??= new WorkflowLifecycleDetails();
        lock (frame.Sequence.SyncRoot)
        {
            var lifecycleEvent = new WorkflowLifecycleEvent
            {
                Kind = kind,
                SourceCorrelationId = frame.SourceCorrelationId,
                ExecutionId = frame.ExecutionId,
                ParentExecutionId = frame.ParentExecutionId,
                WorkflowId = workflowId,
                StepId = details.StepId,
                Sequence = frame.Sequence.Next(),
                Attempt = details.Attempt,
                Mode = frame.Mode == WorkflowRunMode.Live
                    ? WorkflowLifecycleMode.Live
                    : WorkflowLifecycleMode.DryRun,
                TimestampUtc = _timeProvider.GetUtcNow(),
                Elapsed = details.Elapsed,
                Result = details.Result,
                Detail = details.Detail
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

    private sealed record WorkflowGraphContext(
        Project Project,
        Workflow Workflow,
        ActionExecutionContext ActionContext,
        WorkflowExecutionFrame Frame,
        List<WorkflowPlannedEffect> PlannedEffects);

    private sealed record WorkflowStepAttempt(
        WorkflowStep Step,
        WorkflowErrorPolicy Policy,
        int Attempt,
        int MaximumAttempts,
        long StartedTimestamp);

    private sealed record StepFailureResolution(bool ShouldRetry, WorkflowStepOutcome? Outcome)
    {
        public static StepFailureResolution Retry() => new(true, null);
        public static StepFailureResolution Complete(WorkflowStepOutcome outcome) => new(false, outcome);
    }

    private sealed class WorkflowLifecycleDetails
    {
        public Guid? StepId { get; init; }
        public int Attempt { get; init; }
        public TimeSpan? Elapsed { get; init; }
        public string? Result { get; init; }
        public string? Detail { get; init; }
    }

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

    /// <summary>Signals that a nested workflow ended without succeeding.</summary>
    public sealed class NestedWorkflowExecutionException : Exception
    {
        /// <summary>Creates an exception for the terminal nested-workflow status.</summary>
        public NestedWorkflowExecutionException(WorkflowExecutionStatus status)
        {
            Status = status;
        }

        /// <summary>Gets the terminal status returned by the nested workflow.</summary>
        public WorkflowExecutionStatus Status { get; }
    }

    /// <summary>Signals that at least one parallel branch ended without succeeding.</summary>
    public sealed class ParallelWorkflowExecutionException : Exception
    {
        /// <summary>Creates an exception for the terminal parallel-branch status.</summary>
        public ParallelWorkflowExecutionException(WorkflowExecutionStatus status)
        {
            Status = status;
        }

        /// <summary>Gets the terminal status returned by the failed parallel branch.</summary>
        public WorkflowExecutionStatus Status { get; }
    }
}
