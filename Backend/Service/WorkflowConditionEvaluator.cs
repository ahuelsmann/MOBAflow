// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Backend.Service;

using Domain;

using Interface;

/// <summary>Evaluates the bounded MVP set of typed Workflow 2.0 conditions.</summary>
public sealed class WorkflowConditionEvaluator : IWorkflowConditionEvaluator
{
    private readonly IReadOnlyDictionary<Type, IWorkflowConditionHandler> _handlers;

    /// <summary>Creates the evaluator registry with built-in and optional extension handlers.</summary>
    public WorkflowConditionEvaluator(IEnumerable<IWorkflowConditionHandler>? handlers = null)
    {
        var registered = new IWorkflowConditionHandler[]
        {
            new FeedbackSourceConditionHandler(),
            new CurrentJourneyConditionHandler(),
            new CurrentStationConditionHandler()
        }.Concat(handlers ?? []);
        _handlers = registered
            .GroupBy(handler => handler.ConditionType)
            .ToDictionary(group => group.Key, group => group.Last());
    }

    /// <inheritdoc />
    public ValueTask<bool> EvaluateAsync(
        WorkflowCondition condition,
        ActionExecutionContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(condition);
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();

        if (!_handlers.TryGetValue(condition.GetType(), out var handler))
            throw new NotSupportedException($"Condition type '{condition.GetType().Name}' is not supported.");

        return handler.EvaluateAsync(condition, context, cancellationToken);
    }

    private sealed class FeedbackSourceConditionHandler : IWorkflowConditionHandler
    {
        public Type ConditionType => typeof(FeedbackSourceWorkflowCondition);

        public ValueTask<bool> EvaluateAsync(
            WorkflowCondition condition,
            ActionExecutionContext context,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.FromResult(context.FeedbackInPort == ((FeedbackSourceWorkflowCondition)condition).InPort);
        }
    }

    private sealed class CurrentJourneyConditionHandler : IWorkflowConditionHandler
    {
        public Type ConditionType => typeof(CurrentJourneyWorkflowCondition);

        public ValueTask<bool> EvaluateAsync(
            WorkflowCondition condition,
            ActionExecutionContext context,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.FromResult(context.CurrentJourney?.Id == ((CurrentJourneyWorkflowCondition)condition).JourneyId);
        }
    }

    private sealed class CurrentStationConditionHandler : IWorkflowConditionHandler
    {
        public Type ConditionType => typeof(CurrentStationWorkflowCondition);

        public ValueTask<bool> EvaluateAsync(
            WorkflowCondition condition,
            ActionExecutionContext context,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.FromResult(context.CurrentStation?.Id == ((CurrentStationWorkflowCondition)condition).StationId);
        }
    }
}
