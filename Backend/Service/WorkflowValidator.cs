// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Backend.Service;

using Domain;
using Interface;

/// <summary>Validates ordered actions without invoking handlers or external effects.</summary>
public sealed class WorkflowValidator(IWorkflowEffectPlanner? effectPlanner = null) : IWorkflowValidator
{
    private readonly IWorkflowEffectPlanner _effectPlanner = effectPlanner ?? new WorkflowEffectPlanner();

    /// <inheritdoc />
    public WorkflowValidationResult Validate(Project project)
    {
        ArgumentNullException.ThrowIfNull(project);
        var result = new WorkflowValidationResult();
        var ids = new HashSet<Guid>();
        foreach (var workflow in project.Workflows)
        {
            if (workflow.Id == Guid.Empty || !ids.Add(workflow.Id))
                Add(result, WorkflowValidationCodes.DuplicateWorkflowId, workflow.Id, null, "id", "Workflow identifiers must be non-empty and unique within a project.");
            ValidateActions(workflow, result);
        }
        return result;
    }

    private void ValidateActions(Workflow workflow, WorkflowValidationResult result)
    {
        if (workflow.Actions is not { Count: > 0 })
        {
            Add(result, WorkflowValidationCodes.EmptyWorkflow, workflow.Id, null, "actions", "Add at least one action before running this workflow.");
            return;
        }
        var ids = new HashSet<Guid>();
        for (var index = 0; index < workflow.Actions.Count; index++)
        {
            var action = workflow.Actions[index];
            var path = $"actions[{index}]";
            if (action == null)
            {
                Add(result, WorkflowValidationCodes.InvalidActionPayload, workflow.Id, null, path, "An action cannot be null.");
                continue;
            }
            if (action.Id == Guid.Empty)
                Add(result, WorkflowValidationCodes.EmptyActionId, workflow.Id, action.Id, $"{path}.id", "Action identifiers must not be empty.");
            else if (!ids.Add(action.Id))
                Add(result, WorkflowValidationCodes.DuplicateActionId, workflow.Id, action.Id, $"{path}.id", "Action identifiers must be unique within a workflow.");
            if (action.DelayAfterMs < 0)
                Add(result, WorkflowValidationCodes.InvalidActionPayload, workflow.Id, action.Id, $"{path}.delayAfterMs", "Delay after an action cannot be negative.");
            foreach (var issue in _effectPlanner.Plan(action).Issues)
                Add(result, WorkflowValidationCodes.InvalidActionPayload, workflow.Id, action.Id, $"{path}.{issue.FieldPath}", issue.Message);
        }
    }

    private static void Add(WorkflowValidationResult result, string code, Guid workflowId, Guid? actionId, string path, string message) =>
        result.Add(new WorkflowValidationIssue(code, WorkflowValidationSeverity.Error, workflowId, actionId, path, message));
}
