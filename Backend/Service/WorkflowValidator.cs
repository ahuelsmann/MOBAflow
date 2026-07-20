// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Backend.Service;

using Domain;
using Domain.Enum;

using Interface;

/// <summary>
/// Validates Workflow 2.0 graphs without executing actions or external effects.
/// </summary>
public sealed class WorkflowValidator : IWorkflowValidator
{
    private const int MaximumAdditionalAttempts = 10;

    /// <inheritdoc />
    public WorkflowValidationResult Validate(Project project)
    {
        ArgumentNullException.ThrowIfNull(project);

        var result = new WorkflowValidationResult();
        ValidateWorkflowIds(project, result);

        var workflowsById = project.Workflows
            .Where(workflow => workflow.Id != Guid.Empty)
            .GroupBy(workflow => workflow.Id)
            .ToDictionary(group => group.Key, group => group.First());

        foreach (var workflow in project.Workflows.Where(workflow => workflow.Steps != null))
            ValidateWorkflow(workflow, workflowsById, result);

        ValidateNestedWorkflowCycles(project, workflowsById, result);
        return result;
    }

    private static void ValidateWorkflowIds(Project project, WorkflowValidationResult result)
    {
        foreach (var duplicate in project.Workflows
                     .Where(workflow => workflow.Id != Guid.Empty)
                     .GroupBy(workflow => workflow.Id)
                     .Where(group => group.Count() > 1))
        {
            AddError(
                result,
                WorkflowValidationCodes.DuplicateWorkflowId,
                duplicate.Key,
                null,
                "id",
                "Workflow identifiers must be unique within a project.");
        }
    }

    private static void ValidateWorkflow(
        Workflow workflow,
        IReadOnlyDictionary<Guid, Workflow> workflowsById,
        WorkflowValidationResult result)
    {
        var steps = workflow.Steps ?? [];
        if (steps.Count == 0)
        {
            AddError(result, WorkflowValidationCodes.EmptyWorkflow, workflow.Id, null, "steps", "Workflow must contain at least one step.");
            return;
        }

        ValidateStepIds(workflow, steps, result);
        var stepsById = steps
            .Where(step => step.Id != Guid.Empty)
            .GroupBy(step => step.Id)
            .ToDictionary(group => group.Key, group => group.First());

        if (!workflow.EntryStepId.HasValue || !stepsById.ContainsKey(workflow.EntryStepId.Value))
        {
            AddError(result, WorkflowValidationCodes.MissingEntryStep, workflow.Id, null, "entryStepId", "Entry step must reference an existing step.");
        }

        ValidateErrorPolicy(workflow.Id, null, "defaultErrorPolicy", workflow.DefaultErrorPolicy, stepsById, result);
        foreach (var step in steps)
            ValidateStep(workflow, step, stepsById, workflowsById, result);

        ValidateReachability(workflow, stepsById, result);
        ValidateGraphCycles(workflow, stepsById, result);
        ValidateParallelBranches(workflow, stepsById, result);
    }

    private static void ValidateStepIds(Workflow workflow, IReadOnlyCollection<WorkflowStep> steps, WorkflowValidationResult result)
    {
        foreach (var step in steps.Where(step => step.Id == Guid.Empty))
        {
            AddError(result, WorkflowValidationCodes.EmptyStepId, workflow.Id, null, "steps[].id", "Step identifier cannot be empty.");
        }

        foreach (var duplicate in steps
                     .Where(step => step.Id != Guid.Empty)
                     .GroupBy(step => step.Id)
                     .Where(group => group.Count() > 1))
        {
            AddError(result, WorkflowValidationCodes.DuplicateStepId, workflow.Id, duplicate.Key, "steps[].id", "Step identifiers must be unique within a workflow.");
        }
    }

    private static void ValidateStep(
        Workflow workflow,
        WorkflowStep step,
        IReadOnlyDictionary<Guid, WorkflowStep> stepsById,
        IReadOnlyDictionary<Guid, Workflow> workflowsById,
        WorkflowValidationResult result)
    {
        ValidateErrorPolicy(workflow.Id, step.Id, "errorPolicy", step.ErrorPolicy, stepsById, result);

        switch (step)
        {
            case WorkflowActionStep actionStep:
                ValidateActionStep(workflow.Id, actionStep, result);
                ValidateRequiredReference(workflow.Id, step.Id, "nextStepId", step.NextStepId, stepsById, result);
                break;
            case WorkflowDelayStep delayStep:
                if (delayStep.DelayMs < 0)
                    AddInvalidPayload(result, workflow.Id, step.Id, "delayMs", "Delay duration cannot be negative.");
                ValidateRequiredReference(workflow.Id, step.Id, "nextStepId", step.NextStepId, stepsById, result);
                break;
            case WorkflowConditionStep conditionStep:
                ValidateConditionStep(workflow.Id, conditionStep, stepsById, result);
                break;
            case WorkflowParallelStep parallelStep:
                ValidateParallelStep(workflow.Id, parallelStep, stepsById, result);
                break;
            case WorkflowNestedStep nestedStep:
                if (nestedStep.WorkflowId == Guid.Empty || !workflowsById.ContainsKey(nestedStep.WorkflowId))
                    AddError(result, WorkflowValidationCodes.MissingNestedWorkflow, workflow.Id, step.Id, "workflowId", "Nested workflow must reference an existing workflow.");
                ValidateRequiredReference(workflow.Id, step.Id, "nextStepId", step.NextStepId, stepsById, result);
                break;
            case WorkflowTerminateStep:
                if (step.NextStepId.HasValue)
                    AddInvalidPayload(result, workflow.Id, step.Id, "nextStepId", "Termination step cannot have a successor.");
                break;
        }
    }

    private static void ValidateActionStep(Guid workflowId, WorkflowActionStep step, WorkflowValidationResult result)
    {
        if (step.Action == null || !HasSupportedPayload(step.Action))
        {
            AddInvalidPayload(result, workflowId, step.Id, "action", "Action step must contain a supported typed payload.");
        }
    }

    private static bool HasSupportedPayload(WorkflowAction action) => action.Type switch
    {
        ActionType.Command => action.Command != null,
        ActionType.Audio => action.Audio != null,
        ActionType.Announcement => action.Announcement != null,
        ActionType.ExecuteScript => action.PowerShell != null,
        ActionType.SelectSignalAspect => action.SelectSignalAspect != null,
        ActionType.TrainDestinationDisplay => action.TrainDestinationDisplay != null,
        ActionType.ChangeJourneyStop => action.ChangeJourneyStop != null,
        _ => false
    };

    private static void ValidateConditionStep(
        Guid workflowId,
        WorkflowConditionStep step,
        IReadOnlyDictionary<Guid, WorkflowStep> stepsById,
        WorkflowValidationResult result)
    {
        if (step.Condition == null || !IsConditionPayloadValid(step.Condition))
            AddInvalidPayload(result, workflowId, step.Id, "condition", "Condition step must contain a supported typed condition.");

        ValidateRequiredReference(workflowId, step.Id, "trueStepId", step.TrueStepId, stepsById, result);
        ValidateRequiredReference(workflowId, step.Id, "falseStepId", step.FalseStepId, stepsById, result);

        if (step.NextStepId.HasValue)
            AddInvalidPayload(result, workflowId, step.Id, "nextStepId", "Condition step uses explicit true and false successors.");
    }

    private static bool IsConditionPayloadValid(WorkflowCondition condition) => condition switch
    {
        FeedbackSourceWorkflowCondition feedback => feedback.InPort is >= 1 and <= 512,
        CurrentJourneyWorkflowCondition journey => journey.JourneyId != Guid.Empty,
        CurrentStationWorkflowCondition station => station.StationId != Guid.Empty,
        _ => false
    };

    private static void ValidateParallelStep(
        Guid workflowId,
        WorkflowParallelStep step,
        IReadOnlyDictionary<Guid, WorkflowStep> stepsById,
        WorkflowValidationResult result)
    {
        if (step.Branches.Count == 0)
            AddError(result, WorkflowValidationCodes.EmptyParallelBranches, workflowId, step.Id, "branches", "Parallel step must contain at least one branch.");

        foreach (var branch in step.Branches)
            ValidateRequiredReference(workflowId, step.Id, "branches[].entryStepId", branch.EntryStepId, stepsById, result);

        if (step.Branches.Select(branch => branch.EntryStepId).Distinct().Count() != step.Branches.Count)
            AddError(result, WorkflowValidationCodes.DuplicateParallelBranch, workflowId, step.Id, "branches", "Parallel branch entries must be unique.");

        ValidateRequiredReference(workflowId, step.Id, "joinStepId", step.JoinStepId, stepsById, result);
        if (step.NextStepId.HasValue)
            AddInvalidPayload(result, workflowId, step.Id, "nextStepId", "Parallel step continues through its explicit join step.");
    }

    private static void ValidateErrorPolicy(
        Guid workflowId,
        Guid? stepId,
        string fieldPath,
        WorkflowErrorPolicy? policy,
        IReadOnlyDictionary<Guid, WorkflowStep> stepsById,
        WorkflowValidationResult result)
    {
        if (policy == null)
            return;

        if (policy.Behavior == WorkflowFailureBehavior.FailureBranch)
        {
            if (!policy.FailureStepId.HasValue || !stepsById.ContainsKey(policy.FailureStepId.Value))
                AddError(result, WorkflowValidationCodes.InvalidErrorPolicy, workflowId, stepId, $"{fieldPath}.failureStepId", "Failure branch must reference an existing step.");
        }
        else if (policy.FailureStepId.HasValue)
        {
            AddError(result, WorkflowValidationCodes.InvalidErrorPolicy, workflowId, stepId, $"{fieldPath}.failureStepId", "Failure step is only valid for FailureBranch behavior.");
        }

        if (policy.Retry is { } retry &&
            (retry.AdditionalAttempts is < 0 or > MaximumAdditionalAttempts || retry.DelayMs < 0))
        {
            AddError(result, WorkflowValidationCodes.InvalidRetryPolicy, workflowId, stepId, $"{fieldPath}.retry", "Retry attempts must be between 0 and 10 and delay cannot be negative.");
        }
    }

    private static void ValidateRequiredReference(
        Guid workflowId,
        Guid stepId,
        string fieldPath,
        Guid? referencedStepId,
        IReadOnlyDictionary<Guid, WorkflowStep> stepsById,
        WorkflowValidationResult result)
    {
        if (!referencedStepId.HasValue || referencedStepId == Guid.Empty || !stepsById.ContainsKey(referencedStepId.Value))
            AddError(result, WorkflowValidationCodes.MissingStepReference, workflowId, stepId, fieldPath, "Step reference must identify an existing step.");
    }

    private static void ValidateReachability(
        Workflow workflow,
        IReadOnlyDictionary<Guid, WorkflowStep> stepsById,
        WorkflowValidationResult result)
    {
        if (!workflow.EntryStepId.HasValue || !stepsById.ContainsKey(workflow.EntryStepId.Value))
            return;

        var reachable = Traverse(workflow.EntryStepId.Value, stepsById);
        foreach (var stepId in stepsById.Keys.Where(stepId => !reachable.Contains(stepId)))
            AddError(result, WorkflowValidationCodes.UnreachableStep, workflow.Id, stepId, "steps", "Step is unreachable from the workflow entry.");
    }

    private static HashSet<Guid> Traverse(Guid entryStepId, IReadOnlyDictionary<Guid, WorkflowStep> stepsById)
    {
        var visited = new HashSet<Guid>();
        var pending = new Stack<Guid>();
        pending.Push(entryStepId);
        while (pending.TryPop(out var stepId))
        {
            if (!visited.Add(stepId) || !stepsById.TryGetValue(stepId, out var step))
                continue;

            foreach (var successor in GetSuccessors(step).Where(stepsById.ContainsKey))
                pending.Push(successor);
        }

        return visited;
    }

    private static void ValidateGraphCycles(
        Workflow workflow,
        IReadOnlyDictionary<Guid, WorkflowStep> stepsById,
        WorkflowValidationResult result)
    {
        var visiting = new HashSet<Guid>();
        var visited = new HashSet<Guid>();
        foreach (var stepId in stepsById.Keys)
        {
            if (HasCycle(stepId, stepsById, visiting, visited))
            {
                AddError(result, WorkflowValidationCodes.GraphCycle, workflow.Id, stepId, "steps", "Workflow graph cannot contain cycles.");
                return;
            }
        }
    }

    private static bool HasCycle(
        Guid stepId,
        IReadOnlyDictionary<Guid, WorkflowStep> stepsById,
        ISet<Guid> visiting,
        ISet<Guid> visited)
    {
        if (visiting.Contains(stepId))
            return true;
        if (!visited.Add(stepId) || !stepsById.TryGetValue(stepId, out var step))
            return false;

        visiting.Add(stepId);
        var hasCycle = GetSuccessors(step)
            .Where(stepsById.ContainsKey)
            .Any(successor => HasCycle(successor, stepsById, visiting, visited));
        visiting.Remove(stepId);
        return hasCycle;
    }

    private static void ValidateParallelBranches(
        Workflow workflow,
        IReadOnlyDictionary<Guid, WorkflowStep> stepsById,
        WorkflowValidationResult result)
    {
        foreach (var parallel in stepsById.Values.OfType<WorkflowParallelStep>())
        {
            if (!stepsById.ContainsKey(parallel.JoinStepId))
                continue;

            var branchOwnership = new Dictionary<Guid, int>();
            for (var branchIndex = 0; branchIndex < parallel.Branches.Count; branchIndex++)
            {
                var entryStepId = parallel.Branches[branchIndex].EntryStepId;
                if (!stepsById.ContainsKey(entryStepId))
                    continue;

                if (!CanReach(entryStepId, parallel.JoinStepId, stepsById, new HashSet<Guid>()))
                {
                    AddError(result, WorkflowValidationCodes.InvalidParallelJoin, workflow.Id, parallel.Id, "joinStepId", "Every parallel branch must reach the declared join step.");
                    continue;
                }

                foreach (var ownedStepId in CollectUntilJoin(entryStepId, parallel.JoinStepId, stepsById))
                {
                    if (branchOwnership.TryGetValue(ownedStepId, out var owner) && owner != branchIndex)
                    {
                        AddError(result, WorkflowValidationCodes.OverlappingParallelBranches, workflow.Id, parallel.Id, "branches", "Parallel branches cannot own the same step before their join.");
                        break;
                    }

                    branchOwnership[ownedStepId] = branchIndex;
                }
            }
        }
    }

    private static bool CanReach(
        Guid current,
        Guid target,
        IReadOnlyDictionary<Guid, WorkflowStep> stepsById,
        ISet<Guid> visited)
    {
        if (current == target)
            return true;
        if (!visited.Add(current) || !stepsById.TryGetValue(current, out var step))
            return false;
        return GetSuccessors(step).Any(successor => CanReach(successor, target, stepsById, visited));
    }

    private static HashSet<Guid> CollectUntilJoin(
        Guid entry,
        Guid join,
        IReadOnlyDictionary<Guid, WorkflowStep> stepsById)
    {
        var collected = new HashSet<Guid>();
        var pending = new Stack<Guid>();
        pending.Push(entry);
        while (pending.TryPop(out var current))
        {
            if (current == join || !collected.Add(current) || !stepsById.TryGetValue(current, out var step))
                continue;
            foreach (var successor in GetSuccessors(step))
                pending.Push(successor);
        }
        return collected;
    }

    private static IEnumerable<Guid> GetSuccessors(WorkflowStep step)
    {
        if (step.ErrorPolicy?.Behavior == WorkflowFailureBehavior.FailureBranch && step.ErrorPolicy.FailureStepId.HasValue)
            yield return step.ErrorPolicy.FailureStepId.Value;

        switch (step)
        {
            case WorkflowConditionStep condition:
                yield return condition.TrueStepId;
                yield return condition.FalseStepId;
                break;
            case WorkflowParallelStep parallel:
                foreach (var branch in parallel.Branches)
                    yield return branch.EntryStepId;
                break;
            case WorkflowTerminateStep:
                break;
            default:
                if (step.NextStepId.HasValue)
                    yield return step.NextStepId.Value;
                break;
        }
    }

    private static void ValidateNestedWorkflowCycles(
        Project project,
        IReadOnlyDictionary<Guid, Workflow> workflowsById,
        WorkflowValidationResult result)
    {
        var graph = project.Workflows
            .Where(workflow => workflow.Steps != null && workflow.Id != Guid.Empty)
            .ToDictionary(
                workflow => workflow.Id,
                workflow => workflow.Steps!
                    .OfType<WorkflowNestedStep>()
                    .Select(step => step.WorkflowId)
                    .Where(workflowsById.ContainsKey)
                    .Distinct()
                    .ToArray());
        var visiting = new HashSet<Guid>();
        var visited = new HashSet<Guid>();
        foreach (var workflowId in graph.Keys)
        {
            if (HasNestedCycle(workflowId, graph, visiting, visited))
            {
                AddError(result, WorkflowValidationCodes.NestedWorkflowCycle, workflowId, null, "steps", "Nested workflow calls cannot be recursive.");
                return;
            }
        }
    }

    private static bool HasNestedCycle(
        Guid workflowId,
        IReadOnlyDictionary<Guid, Guid[]> graph,
        ISet<Guid> visiting,
        ISet<Guid> visited)
    {
        if (visiting.Contains(workflowId))
            return true;
        if (!visited.Add(workflowId) || !graph.TryGetValue(workflowId, out var children))
            return false;

        visiting.Add(workflowId);
        var hasCycle = children.Any(child => HasNestedCycle(child, graph, visiting, visited));
        visiting.Remove(workflowId);
        return hasCycle;
    }

    private static void AddInvalidPayload(
        WorkflowValidationResult result,
        Guid workflowId,
        Guid stepId,
        string fieldPath,
        string message) => AddError(result, WorkflowValidationCodes.InvalidStepPayload, workflowId, stepId, fieldPath, message);

    private static void AddError(
        WorkflowValidationResult result,
        string code,
        Guid workflowId,
        Guid? stepId,
        string fieldPath,
        string message) => result.Add(new WorkflowValidationIssue(
            code,
            WorkflowValidationSeverity.Error,
            workflowId,
            stepId,
            fieldPath,
            message));
}
