// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.SharedUI.ViewModel.WorkflowSteps;

using Domain;

/// <summary>A named journey or stop available to a workflow condition.</summary>
public sealed record WorkflowContextChoice(Guid Id, string Name);

public abstract partial class WorkflowStepViewModel
{
    internal WorkflowViewModel? EditorWorkflow { get; private set; }

    /// <summary>Gets stable wrappers used to select connections by name.</summary>
    public IEnumerable<WorkflowStepViewModel> AvailableSteps => EditorWorkflow?.Steps ?? [];

    /// <summary>Gets a named selection label that distinguishes equally named steps.</summary>
    public string SelectionLabel => EditorWorkflow != null && EditorWorkflow.Steps.Count(step => step.Name == Name) > 1
        ? $"{Name} ({EditorWorkflow.Steps.IndexOf(this) + 1})"
        : string.IsNullOrWhiteSpace(Name) ? Kind.ToString() : Name;

    /// <summary>Gets a readable node label, including its role as the workflow entry.</summary>
    public string StepCaption => EditorWorkflow?.EntryStepId == Id ? $"{Kind} · Start" : Kind.ToString();

    /// <summary>Gets the actual outgoing connections independently of the editor's list order.</summary>
    public string ConnectionsSummary => EditorWorkflow?.DescribeConnections(Model) ?? string.Empty;

    /// <summary>Gets or sets the normal successor through its authoritative wrapper.</summary>
    public WorkflowStepViewModel? NextStep
    {
        get => FindStep(NextStepId);
        set
        {
            // A ComboBox can clear its selection while its source changes. Keep unresolved IDs for validation.
            if (value != null && AvailableSteps.Contains(value)) NextStepId = value.Id;
        }
    }

    protected WorkflowStepViewModel? FindStep(Guid? id) => AvailableSteps.FirstOrDefault(step => step.Id == id);

    internal void RefreshEditor(WorkflowViewModel workflow)
    {
        var ownerChanged = EditorWorkflow != workflow;
        EditorWorkflow = workflow;
        if (ownerChanged) OnPropertyChanged(nameof(AvailableSteps));
        OnPropertyChanged(nameof(SelectionLabel));
        OnPropertyChanged(nameof(StepCaption));
        OnPropertyChanged(nameof(ConnectionsSummary));
        OnPropertyChanged(nameof(NextStep));
        switch (this)
        {
            case WorkflowConditionStepViewModel condition:
                condition.RefreshConditionEditor();
                break;
            case WorkflowParallelStepViewModel parallel:
                parallel.RefreshParallelEditor(workflow);
                break;
            case WorkflowNestedStepViewModel nested:
                nested.RefreshNestedEditor();
                break;
        }
    }
}

public sealed partial class WorkflowConditionStepViewModel
{
    /// <summary>Gets whether this condition matches a feedback input.</summary>
    public bool IsFeedbackCondition => ConditionKind == WorkflowConditionKind.FeedbackSource;

    /// <summary>Gets whether this condition selects a journey or stop.</summary>
    public bool IsContextCondition => !IsFeedbackCondition;

    /// <summary>Gets named entities appropriate to the selected condition kind.</summary>
    public IEnumerable<WorkflowContextChoice> ContextChoices => ConditionKind switch
    {
        WorkflowConditionKind.CurrentJourney => EditorWorkflow?.JourneyChoices ?? [],
        WorkflowConditionKind.CurrentStation => EditorWorkflow?.StationChoices ?? [],
        _ => []
    };

    /// <summary>Gets or sets the selected journey or stop without exposing its ID.</summary>
    public WorkflowContextChoice? ContextEntity
    {
        get => ContextChoices.FirstOrDefault(choice => choice.Id == ContextEntityId);
        set
        {
            if (value != null && ContextChoices.Contains(value)) ContextEntityId = value.Id;
        }
    }

    /// <summary>Gets or sets the successor for a matching condition.</summary>
    public WorkflowStepViewModel? TrueStep
    {
        get => FindStep(TrueStepId);
        set { if (value != null && AvailableSteps.Contains(value)) TrueStepId = value.Id; }
    }

    /// <summary>Gets or sets the successor for a nonmatching condition.</summary>
    public WorkflowStepViewModel? FalseStep
    {
        get => FindStep(FalseStepId);
        set { if (value != null && AvailableSteps.Contains(value)) FalseStepId = value.Id; }
    }

    internal void RefreshConditionEditor()
    {
        OnPropertyChanged(nameof(IsFeedbackCondition));
        OnPropertyChanged(nameof(IsContextCondition));
        OnPropertyChanged(nameof(ContextChoices));
        OnPropertyChanged(nameof(ContextEntity));
        OnPropertyChanged(nameof(TrueStep));
        OnPropertyChanged(nameof(FalseStep));
    }
}

public sealed partial class WorkflowParallelStepViewModel
{
    /// <summary>Gets or sets the step reached after all parallel branches finish.</summary>
    public WorkflowStepViewModel? JoinStep
    {
        get => FindStep(JoinStepId);
        set { if (value != null && AvailableSteps.Contains(value)) JoinStepId = value.Id; }
    }

    internal void RefreshParallelEditor(WorkflowViewModel workflow)
    {
        // Deleting a referenced step also removes its branch from the domain graph.
        foreach (var branch in Branches.Where(branch => !_model.Branches.Contains(branch.Model)).ToArray())
        {
            branch.PropertyChanged -= OnBranchPropertyChanged;
            Branches.Remove(branch);
        }
        foreach (var branch in Branches) branch.RefreshEditor(workflow);
        OnPropertyChanged(nameof(JoinStep));
    }
}

public sealed partial class WorkflowParallelBranchViewModel
{
    private WorkflowViewModel? _editorWorkflow;

    /// <summary>Gets the workflow's stable step wrappers.</summary>
    public IEnumerable<WorkflowStepViewModel> AvailableSteps => _editorWorkflow?.Steps ?? [];

    /// <summary>Gets or sets the first step of this parallel branch.</summary>
    public WorkflowStepViewModel? EntryStep
    {
        get => AvailableSteps.FirstOrDefault(step => step.Id == EntryStepId);
        set { if (value != null && AvailableSteps.Contains(value)) EntryStepId = value.Id; }
    }

    internal void RefreshEditor(WorkflowViewModel workflow)
    {
        var ownerChanged = _editorWorkflow != workflow;
        _editorWorkflow = workflow;
        if (ownerChanged) OnPropertyChanged(nameof(AvailableSteps));
        OnPropertyChanged(nameof(EntryStep));
    }
}

public sealed partial class WorkflowNestedStepViewModel
{
    /// <summary>Gets workflows from the current project's authoritative catalog.</summary>
    public IEnumerable<WorkflowViewModel> AvailableWorkflows => EditorWorkflow?.AvailableWorkflows ?? [];

    /// <summary>Gets or sets the workflow invoked by this step.</summary>
    public WorkflowViewModel? InvokedWorkflow
    {
        get => AvailableWorkflows.FirstOrDefault(workflow => workflow.Id == WorkflowId);
        set { if (value != null && AvailableWorkflows.Contains(value)) WorkflowId = value.Id; }
    }

    internal void RefreshNestedEditor()
    {
        OnPropertyChanged(nameof(AvailableWorkflows));
        OnPropertyChanged(nameof(InvokedWorkflow));
    }
}
