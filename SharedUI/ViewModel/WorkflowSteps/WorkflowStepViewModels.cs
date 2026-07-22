// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.SharedUI.ViewModel.WorkflowSteps;

using Action;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Domain;
using Domain.Enum;

using System.Collections.ObjectModel;
using System.ComponentModel;

/// <summary>Identifies the typed graph node created by the workflow editor.</summary>
public enum WorkflowStepKind
{
    /// <summary>Typed external or journey action.</summary>
    Action,

    /// <summary>Cancellable delay.</summary>
    Delay,

    /// <summary>Typed two-way condition.</summary>
    Condition,

    /// <summary>Ordered parallel branch launch and join.</summary>
    Parallel,

    /// <summary>Nested workflow invocation.</summary>
    NestedWorkflow,

    /// <summary>Explicit graph termination.</summary>
    Terminate
}

/// <summary>Identifies the typed condition edited on a condition step.</summary>
public enum WorkflowConditionKind
{
    /// <summary>Match the feedback input that triggered execution.</summary>
    FeedbackSource,

    /// <summary>Match the captured journey.</summary>
    CurrentJourney,

    /// <summary>Match the captured station.</summary>
    CurrentStation
}

/// <summary>Base wrapper for one Workflow 2.0 graph node.</summary>
public abstract class WorkflowStepViewModel : ObservableObject
{
    /// <summary>Creates a graph-node wrapper.</summary>
    protected WorkflowStepViewModel(WorkflowStep model)
    {
        ArgumentNullException.ThrowIfNull(model);
        Model = model;
    }

    /// <summary>Gets the wrapped graph node.</summary>
    public WorkflowStep Model { get; }

    /// <summary>Gets the stable node identifier.</summary>
    public Guid Id => Model.Id;

    /// <summary>Gets the concrete editor kind.</summary>
    public abstract WorkflowStepKind Kind { get; }

    /// <summary>Gets or sets the editor-facing node name.</summary>
    public string Name
    {
        get => Model.Name;
        set => SetProperty(Model.Name, value, Model, static (step, name) => step.Name = name);
    }

    /// <summary>Gets or sets the normal successor identifier.</summary>
    public Guid? NextStepId
    {
        get => Model.NextStepId;
        set => SetProperty(Model.NextStepId, value, Model, static (step, id) => step.NextStepId = id);
    }

    /// <summary>Gets failure behaviors available to the editor.</summary>
    public IEnumerable<WorkflowFailureBehavior> FailureBehaviors { get; } = Enum.GetValues<WorkflowFailureBehavior>();

    /// <summary>Gets whether this node overrides the workflow error policy.</summary>
    public bool HasErrorPolicy
    {
        get => Model.ErrorPolicy != null;
        set
        {
            if (value == HasErrorPolicy)
            {
                return;
            }

            Model.ErrorPolicy = value ? new WorkflowErrorPolicy() : null;
            OnPropertyChanged();
            NotifyPolicyChanged();
        }
    }

    /// <summary>Gets or sets the failure behavior for the node override.</summary>
    public WorkflowFailureBehavior FailureBehavior
    {
        get => Model.ErrorPolicy?.Behavior ?? WorkflowFailureBehavior.Stop;
        set
        {
            var policy = EnsureErrorPolicy();
            if (SetProperty(policy.Behavior, value, policy, static (target, behavior) => target.Behavior = behavior))
            {
                OnPropertyChanged(nameof(HasErrorPolicy));
            }
        }
    }

    /// <summary>Gets or sets the failure-branch entry node.</summary>
    public Guid? FailureStepId
    {
        get => Model.ErrorPolicy?.FailureStepId;
        set
        {
            var policy = EnsureErrorPolicy();
            if (SetProperty(policy.FailureStepId, value, policy, static (target, id) => target.FailureStepId = id))
            {
                OnPropertyChanged(nameof(HasErrorPolicy));
            }
        }
    }

    /// <summary>Gets or sets the number of retry attempts after the initial attempt.</summary>
    public int RetryAdditionalAttempts
    {
        get => Model.ErrorPolicy?.Retry?.AdditionalAttempts ?? 0;
        set
        {
            var retry = EnsureRetryPolicy();
            SetProperty(retry.AdditionalAttempts, Math.Max(value, 0), retry, static (target, attempts) => target.AdditionalAttempts = attempts);
            OnPropertyChanged(nameof(HasErrorPolicy));
        }
    }

    /// <summary>Gets or sets the delay before each retry.</summary>
    public int RetryDelayMs
    {
        get => Model.ErrorPolicy?.Retry?.DelayMs ?? 0;
        set
        {
            var retry = EnsureRetryPolicy();
            SetProperty(retry.DelayMs, Math.Max(value, 0), retry, static (target, delay) => target.DelayMs = delay);
            OnPropertyChanged(nameof(HasErrorPolicy));
        }
    }

    private WorkflowErrorPolicy EnsureErrorPolicy()
    {
        if (Model.ErrorPolicy != null)
        {
            return Model.ErrorPolicy;
        }

        Model.ErrorPolicy = new WorkflowErrorPolicy();
        OnPropertyChanged(nameof(HasErrorPolicy));
        return Model.ErrorPolicy;
    }

    private WorkflowRetryPolicy EnsureRetryPolicy()
    {
        var policy = EnsureErrorPolicy();
        policy.Retry ??= new WorkflowRetryPolicy();
        return policy.Retry;
    }

    private void NotifyPolicyChanged()
    {
        OnPropertyChanged(nameof(FailureBehavior));
        OnPropertyChanged(nameof(FailureStepId));
        OnPropertyChanged(nameof(RetryAdditionalAttempts));
        OnPropertyChanged(nameof(RetryDelayMs));
    }
}

/// <summary>Wraps an action graph node and its typed action editor.</summary>
public sealed class WorkflowActionStepViewModel : WorkflowStepViewModel
{
    /// <summary>Creates an action-node wrapper.</summary>
    public WorkflowActionStepViewModel(WorkflowActionStep model, WorkflowActionViewModelFactory actionFactory)
        : base(model)
    {
        Action = model.Action == null ? null : actionFactory.CreateViewModel(model.Action);
        if (Action != null)
        {
            Action.PropertyChanged += OnActionPropertyChanged;
        }
    }

    /// <inheritdoc />
    public override WorkflowStepKind Kind => WorkflowStepKind.Action;

    /// <summary>Gets the typed action payload editor.</summary>
    public WorkflowActionViewModel? Action { get; }

    private void OnActionPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        _ = sender;
        _ = e;
        OnPropertyChanged(nameof(Action));
    }
}

/// <summary>Wraps a delay graph node.</summary>
public sealed class WorkflowDelayStepViewModel(WorkflowDelayStep model) : WorkflowStepViewModel(model)
{
    /// <inheritdoc />
    public override WorkflowStepKind Kind => WorkflowStepKind.Delay;

    /// <summary>Gets or sets the non-negative delay duration.</summary>
    public int DelayMs
    {
        get => model.DelayMs;
        set => SetProperty(model.DelayMs, Math.Max(value, 0), model, static (step, delay) => step.DelayMs = delay);
    }
}

/// <summary>Wraps a typed condition and its two explicit successors.</summary>
public sealed class WorkflowConditionStepViewModel(WorkflowConditionStep model) : WorkflowStepViewModel(model)
{
    /// <inheritdoc />
    public override WorkflowStepKind Kind => WorkflowStepKind.Condition;

    /// <summary>Gets typed condition kinds available to the editor.</summary>
    public IEnumerable<WorkflowConditionKind> ConditionKinds { get; } = Enum.GetValues<WorkflowConditionKind>();

    /// <summary>Gets or sets the condition discriminator.</summary>
    public WorkflowConditionKind ConditionKind
    {
        get => model.Condition switch
        {
            CurrentJourneyWorkflowCondition => WorkflowConditionKind.CurrentJourney,
            CurrentStationWorkflowCondition => WorkflowConditionKind.CurrentStation,
            _ => WorkflowConditionKind.FeedbackSource
        };
        set
        {
            if (ConditionKind == value)
            {
                return;
            }

            model.Condition = value switch
            {
                WorkflowConditionKind.CurrentJourney => new CurrentJourneyWorkflowCondition(),
                WorkflowConditionKind.CurrentStation => new CurrentStationWorkflowCondition(),
                _ => new FeedbackSourceWorkflowCondition { InPort = 1 }
            };
            OnPropertyChanged();
            OnPropertyChanged(nameof(FeedbackInPort));
            OnPropertyChanged(nameof(ContextEntityId));
        }
    }

    /// <summary>Gets or sets the feedback input for a feedback-source condition.</summary>
    public uint FeedbackInPort
    {
        get => (model.Condition as FeedbackSourceWorkflowCondition)?.InPort ?? 1;
        set
        {
            if (model.Condition is not FeedbackSourceWorkflowCondition condition)
            {
                model.Condition = condition = new FeedbackSourceWorkflowCondition();
                OnPropertyChanged(nameof(ConditionKind));
            }

            SetProperty(condition.InPort, Math.Clamp(value, 1u, 512u), condition, static (target, port) => target.InPort = port);
        }
    }

    /// <summary>Gets or sets the journey or station identifier for a context condition.</summary>
    public Guid ContextEntityId
    {
        get => model.Condition switch
        {
            CurrentJourneyWorkflowCondition condition => condition.JourneyId,
            CurrentStationWorkflowCondition condition => condition.StationId,
            _ => Guid.Empty
        };
        set
        {
            switch (model.Condition)
            {
                case CurrentJourneyWorkflowCondition journey:
                    SetProperty(journey.JourneyId, value, journey, static (target, id) => target.JourneyId = id);
                    break;
                case CurrentStationWorkflowCondition station:
                    SetProperty(station.StationId, value, station, static (target, id) => target.StationId = id);
                    break;
            }
        }
    }

    /// <summary>Gets or sets the true successor.</summary>
    public Guid TrueStepId
    {
        get => model.TrueStepId;
        set => SetProperty(model.TrueStepId, value, model, static (step, id) => step.TrueStepId = id);
    }

    /// <summary>Gets or sets the false successor.</summary>
    public Guid FalseStepId
    {
        get => model.FalseStepId;
        set => SetProperty(model.FalseStepId, value, model, static (step, id) => step.FalseStepId = id);
    }
}

/// <summary>Wraps one persisted parallel branch.</summary>
public sealed class WorkflowParallelBranchViewModel : ObservableObject
{
    private readonly WorkflowParallelBranch _model;

    /// <summary>Creates a branch wrapper.</summary>
    public WorkflowParallelBranchViewModel(WorkflowParallelBranch model)
    {
        ArgumentNullException.ThrowIfNull(model);
        _model = model;
    }

    /// <summary>Gets the wrapped branch.</summary>
    public WorkflowParallelBranch Model => _model;

    /// <summary>Gets or sets the branch name.</summary>
    public string Name
    {
        get => _model.Name;
        set => SetProperty(_model.Name, value, _model, static (branch, name) => branch.Name = name);
    }

    /// <summary>Gets or sets the branch entry node.</summary>
    public Guid EntryStepId
    {
        get => _model.EntryStepId;
        set => SetProperty(_model.EntryStepId, value, _model, static (branch, id) => branch.EntryStepId = id);
    }
}

/// <summary>Wraps an ordered parallel launch and join node.</summary>
public sealed partial class WorkflowParallelStepViewModel : WorkflowStepViewModel
{
    private readonly WorkflowParallelStep _model;

    /// <summary>Creates a parallel-node wrapper.</summary>
    public WorkflowParallelStepViewModel(WorkflowParallelStep model)
        : base(model)
    {
        _model = model;
        Branches = new ObservableCollection<WorkflowParallelBranchViewModel>(model.Branches.Select(CreateBranch));
    }

    /// <inheritdoc />
    public override WorkflowStepKind Kind => WorkflowStepKind.Parallel;

    /// <summary>Gets ordered branch editors.</summary>
    public ObservableCollection<WorkflowParallelBranchViewModel> Branches { get; }

    /// <summary>Gets or sets the explicit join node.</summary>
    public Guid JoinStepId
    {
        get => _model.JoinStepId;
        set => SetProperty(_model.JoinStepId, value, _model, static (step, id) => step.JoinStepId = id);
    }

    /// <summary>Adds an empty branch that must be connected before validation succeeds.</summary>
    [RelayCommand]
    private void AddBranch()
    {
        var branch = new WorkflowParallelBranch { Name = $"Branch {_model.Branches.Count + 1}" };
        _model.Branches.Add(branch);
        Branches.Add(CreateBranch(branch));
        OnPropertyChanged(nameof(Branches));
    }

    /// <summary>Deletes one branch from the persisted and observable order.</summary>
    [RelayCommand]
    private void DeleteBranch(WorkflowParallelBranchViewModel? branch)
    {
        if (branch == null)
        {
            return;
        }

        branch.PropertyChanged -= OnBranchPropertyChanged;
        _model.Branches.Remove(branch.Model);
        Branches.Remove(branch);
        OnPropertyChanged(nameof(Branches));
    }

    /// <summary>Moves a branch in both persisted launch order and the observable editor collection.</summary>
    public void MoveBranch(WorkflowParallelBranchViewModel branch, int targetIndex)
    {
        var sourceIndex = Branches.IndexOf(branch);
        if (sourceIndex < 0)
        {
            return;
        }

        targetIndex = Math.Clamp(targetIndex, 0, Branches.Count - 1);
        if (sourceIndex == targetIndex)
        {
            return;
        }

        Branches.Move(sourceIndex, targetIndex);
        _model.Branches.RemoveAt(sourceIndex);
        _model.Branches.Insert(targetIndex, branch.Model);
        OnPropertyChanged(nameof(Branches));
    }

    private WorkflowParallelBranchViewModel CreateBranch(WorkflowParallelBranch branch)
    {
        var viewModel = new WorkflowParallelBranchViewModel(branch);
        viewModel.PropertyChanged += OnBranchPropertyChanged;
        return viewModel;
    }

    private void OnBranchPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        _ = sender;
        _ = e;
        OnPropertyChanged(nameof(Branches));
    }
}

/// <summary>Wraps a nested workflow invocation node.</summary>
public sealed class WorkflowNestedStepViewModel(WorkflowNestedStep model) : WorkflowStepViewModel(model)
{
    /// <inheritdoc />
    public override WorkflowStepKind Kind => WorkflowStepKind.NestedWorkflow;

    /// <summary>Gets or sets the invoked workflow identifier.</summary>
    public Guid WorkflowId
    {
        get => model.WorkflowId;
        set => SetProperty(model.WorkflowId, value, model, static (step, id) => step.WorkflowId = id);
    }
}

/// <summary>Wraps an explicit graph termination node.</summary>
public sealed class WorkflowTerminateStepViewModel(WorkflowTerminateStep model) : WorkflowStepViewModel(model)
{
    /// <inheritdoc />
    public override WorkflowStepKind Kind => WorkflowStepKind.Terminate;

    /// <summary>Gets terminal results available to the editor.</summary>
    public IEnumerable<WorkflowTerminationResult> Results { get; } = Enum.GetValues<WorkflowTerminationResult>();

    /// <summary>Gets or sets the terminal result.</summary>
    public WorkflowTerminationResult Result
    {
        get => model.Result;
        set => SetProperty(model.Result, value, model, static (step, result) => step.Result = result);
    }
}
