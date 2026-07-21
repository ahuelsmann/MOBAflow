// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.SharedUI.ViewModel.WorkflowSteps;

using Action;

using Domain;
using Domain.Enum;

/// <summary>Creates typed graph nodes and their editor wrappers.</summary>
public sealed class WorkflowStepViewModelFactory(WorkflowActionViewModelFactory actionFactory)
{
    /// <summary>Creates the matching editor wrapper for a persisted graph node.</summary>
    public WorkflowStepViewModel CreateViewModel(WorkflowStep step) => step switch
    {
        WorkflowActionStep action => new WorkflowActionStepViewModel(action, actionFactory),
        WorkflowDelayStep delay => new WorkflowDelayStepViewModel(delay),
        WorkflowConditionStep condition => new WorkflowConditionStepViewModel(condition),
        WorkflowParallelStep parallel => new WorkflowParallelStepViewModel(parallel),
        WorkflowNestedStep nested => new WorkflowNestedStepViewModel(nested),
        WorkflowTerminateStep terminate => new WorkflowTerminateStepViewModel(terminate),
        _ => throw new NotSupportedException($"Workflow step type '{step.GetType().Name}' is not supported.")
    };

    /// <summary>Creates a valid typed default where possible; graph references remain explicit editor work.</summary>
    public WorkflowStep CreateDefaultStep(WorkflowStepKind kind) => kind switch
    {
        WorkflowStepKind.Action => new WorkflowActionStep
        {
            Name = "New announcement",
            Action = actionFactory.CreateDefaultAction(ActionType.Announcement, 1)
        },
        WorkflowStepKind.Delay => new WorkflowDelayStep { Name = "Delay", DelayMs = 1000 },
        WorkflowStepKind.Condition => new WorkflowConditionStep
        {
            Name = "Feedback condition",
            Condition = new FeedbackSourceWorkflowCondition { InPort = 1 }
        },
        WorkflowStepKind.Parallel => new WorkflowParallelStep { Name = "Parallel branches" },
        WorkflowStepKind.NestedWorkflow => new WorkflowNestedStep { Name = "Nested workflow" },
        WorkflowStepKind.Terminate => new WorkflowTerminateStep
        {
            Name = "Complete workflow",
            Result = WorkflowTerminationResult.Succeeded
        },
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unsupported workflow step kind.")
    };
}
