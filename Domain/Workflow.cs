// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Domain;

using Enum;

/// <summary>A reusable, ordered sequence of actions, independent of its triggering events.</summary>
public class Workflow
{
    /// <summary>Gets or sets the stable identifier shared by all event assignments.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Gets or sets the display name.</summary>
    public string Name { get; set; } = "New Flow";

    /// <summary>Gets or sets the description.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Gets or sets actions in execution order. List position is authoritative.</summary>
    public List<WorkflowAction> Actions { get; set; } = [];

    /// <summary>
    /// Lists the structural problems of this workflow in action order: no actions, a missing action,
    /// an empty or repeated action identifier, or a negative delay.
    /// </summary>
    public IReadOnlyList<WorkflowStructureIssue> FindStructureIssues()
    {
        if (Actions is not { Count: > 0 })
            return [new WorkflowStructureIssue(WorkflowStructureIssueKind.EmptyWorkflow, null, "actions")];

        var issues = new List<WorkflowStructureIssue>();
        var ids = new HashSet<Guid>();
        for (var index = 0; index < Actions.Count; index++)
        {
            var action = Actions[index];
            var path = $"actions[{index}]";
            if (action == null)
            {
                issues.Add(new WorkflowStructureIssue(WorkflowStructureIssueKind.NullAction, index, path));
                continue;
            }

            if (action.Id == Guid.Empty)
                issues.Add(new WorkflowStructureIssue(WorkflowStructureIssueKind.EmptyActionId, index, $"{path}.id"));
            else if (!ids.Add(action.Id))
                issues.Add(new WorkflowStructureIssue(WorkflowStructureIssueKind.DuplicateActionId, index, $"{path}.id"));
            if (action.DelayAfterMs < 0)
                issues.Add(new WorkflowStructureIssue(WorkflowStructureIssueKind.NegativeDelay, index, $"{path}.delayAfterMs"));
        }

        return issues;
    }
}
