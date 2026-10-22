// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Backend.Service;

using Domain;
using Domain.Enum;
using Interface;

/// <summary>
/// Validates ordered actions without invoking handlers or external effects. The structure rules belong to
/// <see cref="Project"/> and <see cref="Workflow"/>; this validator adds the payload checks of the effect planner
/// and turns both into coded, user-facing issues.
/// </summary>
public sealed class WorkflowValidator : IWorkflowValidator
{
    // The planner is a pure rule over the action payload, used directly like the action handlers do.
    private readonly WorkflowEffectPlanner _effectPlanner = new();

    /// <inheritdoc />
    public WorkflowValidationResult Validate(Project project)
    {
        ArgumentNullException.ThrowIfNull(project);
        var result = new WorkflowValidationResult();
        var invalidIds = project.FindWorkflowsWithInvalidIds();
        for (var index = 0; index < project.Workflows.Count; index++)
        {
            var workflow = project.Workflows[index];
            if (invalidIds.Contains(index))
                Add(result, WorkflowValidationCodes.DuplicateWorkflowId, workflow.Id, null, "id", "Workflow identifiers must be non-empty and unique within a project.");
            ValidateActions(workflow, result);
        }
        return result;
    }

    private void ValidateActions(Workflow workflow, WorkflowValidationResult result)
    {
        var structure = workflow.FindStructureIssues().ToLookup(issue => issue.ActionIndex);
        foreach (var issue in structure[null])
            AddStructureIssue(result, workflow, issue);
        if (workflow.Actions is not { Count: > 0 })
            return;

        for (var index = 0; index < workflow.Actions.Count; index++)
        {
            foreach (var issue in structure[index])
                AddStructureIssue(result, workflow, issue);
            var action = workflow.Actions[index];
            if (action == null)
                continue;
            foreach (var issue in _effectPlanner.Plan(action).Issues)
                Add(result, WorkflowValidationCodes.InvalidActionPayload, workflow.Id, action.Id, $"actions[{index}].{issue.FieldPath}", issue.Message);
        }
    }

    private static void AddStructureIssue(WorkflowValidationResult result, Workflow workflow, WorkflowStructureIssue issue)
    {
        var actionId = issue.ActionIndex is { } index ? workflow.Actions[index]?.Id : null;
        var (code, message) = issue.Kind switch
        {
            WorkflowStructureIssueKind.EmptyWorkflow => (WorkflowValidationCodes.EmptyWorkflow, "Add at least one action before running this workflow."),
            WorkflowStructureIssueKind.NullAction => (WorkflowValidationCodes.InvalidActionPayload, "An action cannot be null."),
            WorkflowStructureIssueKind.EmptyActionId => (WorkflowValidationCodes.EmptyActionId, "Action identifiers must not be empty."),
            WorkflowStructureIssueKind.DuplicateActionId => (WorkflowValidationCodes.DuplicateActionId, "Action identifiers must be unique within a workflow."),
            WorkflowStructureIssueKind.NegativeDelay => (WorkflowValidationCodes.InvalidActionPayload, "Delay after an action cannot be negative."),
            _ => throw new ArgumentOutOfRangeException(nameof(issue), issue.Kind, "Unknown workflow structure issue.")
        };
        Add(result, code, workflow.Id, actionId, issue.FieldPath, message);
    }

    private static void Add(WorkflowValidationResult result, string code, Guid workflowId, Guid? actionId, string path, string message) =>
        result.Add(new WorkflowValidationIssue(code, WorkflowValidationSeverity.Error, workflowId, actionId, path, message));
}
