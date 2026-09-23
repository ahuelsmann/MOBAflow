// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.SharedUI.ViewModel;

using Domain;
using WorkflowSteps;

public sealed partial class WorkflowViewModel
{
    private ProjectViewModel? _editorProject;
    private bool _refreshingEditor;

    /// <summary>Gets the available workflows without creating competing model wrappers.</summary>
    public IEnumerable<WorkflowViewModel> AvailableWorkflows => _editorProject?.Workflows ?? [];

    internal IReadOnlyList<WorkflowContextChoice> JourneyChoices { get; private set; } = [];
    internal IReadOnlyList<WorkflowContextChoice> StationChoices { get; private set; } = [];

    /// <summary>Gets or sets the starting step by name.</summary>
    public WorkflowStepViewModel? EntryStep
    {
        get => Steps.FirstOrDefault(step => step.Id == EntryStepId);
        set { if (value != null && Steps.Contains(value)) EntryStepId = value.Id; }
    }

    /// <summary>Gets or sets the workflow's default failure target by name.</summary>
    public WorkflowStepViewModel? DefaultFailureStep
    {
        get => Steps.FirstOrDefault(step => step.Id == DefaultFailureStepId);
        set { if (value != null && Steps.Contains(value)) DefaultFailureStepId = value.Id; }
    }

    internal void ConfigureEditor(ProjectViewModel? project)
    {
        _editorProject = project;
        JourneyChoices = project?.Model.Journeys
            .Select(journey => new WorkflowContextChoice(journey.Id, journey.Name)).ToArray() ?? [];
        StationChoices = project?.Model.Journeys.SelectMany(journey => journey.Stations)
            .DistinctBy(station => station.Id)
            .Select(station => new WorkflowContextChoice(station.Id, station.Name)).ToArray() ?? [];
        RefreshStepPresentation();
    }

    internal void RefreshStepPresentation()
    {
        if (_refreshingEditor) return;
        _refreshingEditor = true;
        try
        {
            foreach (var step in Steps) step.RefreshEditor(this);
        }
        finally
        {
            _refreshingEditor = false;
        }
    }

    internal string DescribeConnections(WorkflowStep step)
    {
        var description = step switch
        {
            WorkflowConditionStep condition => $"True → {StepName(condition.TrueStepId)}\nFalse → {StepName(condition.FalseStepId)}",
            WorkflowParallelStep parallel => DescribeParallel(parallel),
            WorkflowNestedStep nested => $"Call: {AvailableWorkflows.FirstOrDefault(workflow => workflow.Id == nested.WorkflowId)?.Name ?? "Missing workflow"}\nNext → {StepName(step.NextStepId)}",
            WorkflowTerminateStep terminate => $"End: {terminate.Result}",
            _ => $"Next → {StepName(step.NextStepId)}"
        };
        var policy = step.ErrorPolicy ?? Model.DefaultErrorPolicy;
        return policy?.Behavior == WorkflowFailureBehavior.FailureBranch
            ? $"{description}\nOn failure → {StepName(policy.FailureStepId)}"
            : description;
    }

    private string DescribeParallel(WorkflowParallelStep step)
    {
        var branches = step.Branches.Count == 0 ? "No branches connected" : string.Join("\n",
            step.Branches.Select(branch => $"{branch.Name} → {StepName(branch.EntryStepId)}"));
        return $"In parallel:\n{branches}\nJoin → {StepName(step.JoinStepId)}";
    }

    private string StepName(Guid? id) => !id.HasValue || id == Guid.Empty
        ? "Not connected"
        : Steps.FirstOrDefault(step => step.Id == id)?.SelectionLabel ?? "Missing step";
}
