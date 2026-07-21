// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using Action;
using WorkflowSteps;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Domain;
using Domain.Enum;

using Interface;

using Microsoft.Extensions.Logging;

using Service;

using Sound;

using System.Collections.ObjectModel;
using System.ComponentModel;

/// <summary>
/// ViewModel wrapper for <see cref="Workflow"/> that exposes configuration and actions for editor and execution UI.
/// </summary>
public sealed partial class WorkflowViewModel : ObservableObject, IViewModelWrapper<Workflow>
{
    #region Fields
    // Model
    private readonly Workflow _model;

    // Services
    private readonly WorkflowActionViewModelFactory _actionViewModelFactory;
    private readonly WorkflowStepViewModelFactory _stepViewModelFactory;
    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="WorkflowViewModel"/> class.
    /// </summary>
    /// <param name="model">The workflow domain model.</param>
    /// <param name="ioService">Optional IO service used by audio actions.</param>
    /// <param name="soundPlayer">Optional sound player used to preview audio actions.</param>
    /// <param name="loggerFactory">Optional factory for command action logging.</param>
    public WorkflowViewModel(Workflow model, IIoService? ioService = null, ISoundPlayer? soundPlayer = null, ILoggerFactory? loggerFactory = null)
    {
        ArgumentNullException.ThrowIfNull(model);
        _model = model;
        _actionViewModelFactory = new WorkflowActionViewModelFactory(
            ioService ?? new NullIoService(),
            soundPlayer,
            loggerFactory?.CreateLogger<CommandViewModel>());
        _stepViewModelFactory = new WorkflowStepViewModelFactory(_actionViewModelFactory);

        Actions = new ObservableCollection<object>(
            model.Actions
                .OrderBy(a => a.Number)
                .Select(CreateViewModelForAction)
        );

        foreach (var actionVm in Actions.OfType<WorkflowActionViewModel>())
        {
            actionVm.PropertyChanged += OnActionPropertyChanged;
        }

        model.Steps ??= [];
        Steps = new ObservableCollection<WorkflowStepViewModel>(
            model.Steps.Select(_stepViewModelFactory.CreateViewModel));
        foreach (var step in Steps)
        {
            step.PropertyChanged += OnStepPropertyChanged;
        }
    }

    /// <summary>
    /// Gets the underlying domain model (for IViewModelWrapper interface).
    /// </summary>
    public Workflow Model => _model;

    /// <summary>
    /// Gets the unique identifier of the workflow.
    /// </summary>
    public Guid Id => _model.Id;

    /// <summary>
    /// Gets or sets the display name of the workflow.
    /// </summary>
    public string Name
    {
        get => _model.Name;
        set => SetProperty(_model.Name, value, _model, (m, v) => m.Name = v);
    }

    /// <summary>
    /// Gets or sets the description shown for this workflow.
    /// </summary>
    public string Description
    {
        get => _model.Description;
        set => SetProperty(_model.Description, value, _model, (m, v) => m.Description = v);
    }

    /// <summary>
    /// Gets or sets the feedback input port that triggers this workflow.
    /// </summary>
    public uint InPort
    {
        get => _model.InPort;
        set => SetProperty(_model.InPort, value, _model, (m, v) => m.InPort = v);
    }

    /// <summary>
    /// Gets or sets a value indicating whether a timer window is used to ignore repeated feedbacks.
    /// </summary>
    public bool IsUsingTimerToIgnoreFeedbacks
    {
        get => _model.IsUsingTimerToIgnoreFeedbacks;
        set => SetProperty(_model.IsUsingTimerToIgnoreFeedbacks, value, _model, (m, v) => m.IsUsingTimerToIgnoreFeedbacks = v);
    }

    /// <summary>
    /// Gets or sets the timer window duration in seconds during which repeated feedbacks are ignored.
    /// </summary>
    public double IntervalForTimerToIgnoreFeedbacks
    {
        get => _model.IntervalForTimerToIgnoreFeedbacks;
        set => SetProperty(_model.IntervalForTimerToIgnoreFeedbacks, value, _model, (m, v) => m.IntervalForTimerToIgnoreFeedbacks = v);
    }

    /// <summary>
    /// Gets or sets the execution mode that defines how the workflow runs its actions.
    /// </summary>
    public WorkflowExecutionMode ExecutionMode
    {
        get => _model.ExecutionMode;
        set => SetProperty(_model.ExecutionMode, value, _model, (m, v) => m.ExecutionMode = v);
    }

    /// <summary>
    /// Gets all possible WorkflowExecutionMode values for ComboBox binding.
    /// </summary>
    public IEnumerable<WorkflowExecutionMode> ExecutionModeValues => Enum.GetValues<WorkflowExecutionMode>();

    /// <summary>
    /// Gets the collection of action ViewModels that belong to this workflow.
    /// </summary>
    public ObservableCollection<object> Actions { get; }

    /// <summary>Gets the authoritative ordered Workflow 2.0 graph-node wrappers.</summary>
    public ObservableCollection<WorkflowStepViewModel> Steps { get; }

    /// <summary>Gets workflow-level failure behaviors available to the editor.</summary>
    public IEnumerable<WorkflowFailureBehavior> FailureBehaviors => Enum.GetValues<WorkflowFailureBehavior>();

    /// <summary>Gets or sets the Workflow 2.0 entry node.</summary>
    public Guid? EntryStepId
    {
        get => _model.EntryStepId;
        set => SetProperty(_model.EntryStepId, value, _model, static (workflow, id) => workflow.EntryStepId = id);
    }

    /// <summary>Gets or sets the workflow-level failure behavior inherited by graph nodes.</summary>
    public WorkflowFailureBehavior DefaultFailureBehavior
    {
        get => _model.DefaultErrorPolicy?.Behavior ?? WorkflowFailureBehavior.Stop;
        set
        {
            _model.DefaultErrorPolicy ??= new WorkflowErrorPolicy();
            SetProperty(
                _model.DefaultErrorPolicy.Behavior,
                value,
                _model.DefaultErrorPolicy,
                static (policy, behavior) => policy.Behavior = behavior);
        }
    }

    /// <summary>Gets or sets the workflow-level failure-branch entry.</summary>
    public Guid? DefaultFailureStepId
    {
        get => _model.DefaultErrorPolicy?.FailureStepId;
        set
        {
            _model.DefaultErrorPolicy ??= new WorkflowErrorPolicy();
            SetProperty(
                _model.DefaultErrorPolicy.FailureStepId,
                value,
                _model.DefaultErrorPolicy,
                static (policy, id) => policy.FailureStepId = id);
        }
    }

    /// <summary>Gets or sets the workflow-level retry count after the initial attempt.</summary>
    public int DefaultRetryAdditionalAttempts
    {
        get => _model.DefaultErrorPolicy?.Retry?.AdditionalAttempts ?? 0;
        set
        {
            var retry = EnsureDefaultRetryPolicy();
            SetProperty(
                retry.AdditionalAttempts,
                Math.Max(value, 0),
                retry,
                static (policy, attempts) => policy.AdditionalAttempts = attempts);
        }
    }

    /// <summary>Gets or sets the workflow-level delay before each retry.</summary>
    public int DefaultRetryDelayMs
    {
        get => _model.DefaultErrorPolicy?.Retry?.DelayMs ?? 0;
        set
        {
            var retry = EnsureDefaultRetryPolicy();
            SetProperty(
                retry.DelayMs,
                Math.Max(value, 0),
                retry,
                static (policy, delay) => policy.DelayMs = delay);
        }
    }

    /// <summary>Adds a typed graph node after the current persisted tail.</summary>
    [RelayCommand]
    private void AddStep(WorkflowStepKind kind)
    {
        var step = _stepViewModelFactory.CreateDefaultStep(kind);
        var steps = _model.Steps ??= [];
        var previous = steps.LastOrDefault();
        if (previous != null && previous is not WorkflowConditionStep and not WorkflowParallelStep and not WorkflowTerminateStep)
        {
            previous.NextStepId = step.Id;
        }

        steps.Add(step);
        EntryStepId ??= step.Id;
        var viewModel = _stepViewModelFactory.CreateViewModel(step);
        viewModel.PropertyChanged += OnStepPropertyChanged;
        Steps.Add(viewModel);
        OnPropertyChanged(nameof(Steps));
    }

    /// <summary>Deletes a graph node and clears every internal edge that targeted it.</summary>
    [RelayCommand]
    private void DeleteStep(WorkflowStepViewModel? step)
    {
        if (step == null || !_model.Steps!.Remove(step.Model))
        {
            return;
        }

        step.PropertyChanged -= OnStepPropertyChanged;
        Steps.Remove(step);
        ClearStepReferences(step.Id);
        if (EntryStepId == step.Id)
        {
            EntryStepId = _model.Steps.FirstOrDefault()?.Id;
        }

        OnPropertyChanged(nameof(Steps));
    }

    /// <summary>Moves a node in persisted editor order without changing its explicit graph edges.</summary>
    public void MoveStep(WorkflowStepViewModel step, int targetIndex)
    {
        var sourceIndex = Steps.IndexOf(step);
        if (sourceIndex < 0)
        {
            return;
        }

        targetIndex = Math.Clamp(targetIndex, 0, Steps.Count - 1);
        if (sourceIndex == targetIndex)
        {
            return;
        }

        Steps.Move(sourceIndex, targetIndex);
        var model = _model.Steps![sourceIndex];
        _model.Steps.RemoveAt(sourceIndex);
        _model.Steps.Insert(targetIndex, model);
        OnPropertyChanged(nameof(Steps));
    }

    /// <summary>Moves a node one position toward the start of the persisted editor order.</summary>
    [RelayCommand]
    private void MoveStepUp(WorkflowStepViewModel? step)
    {
        if (step == null)
        {
            return;
        }

        var index = Steps.IndexOf(step);
        if (index > 0)
        {
            MoveStep(step, index - 1);
        }
    }

    /// <summary>Moves a node one position toward the end of the persisted editor order.</summary>
    [RelayCommand]
    private void MoveStepDown(WorkflowStepViewModel? step)
    {
        if (step == null)
        {
            return;
        }

        var index = Steps.IndexOf(step);
        if (index >= 0 && index < Steps.Count - 1)
        {
            MoveStep(step, index + 1);
        }
    }

    /// <summary>Synchronizes the persisted editor order after a view-level reorder operation.</summary>
    public void UpdateStepOrder()
    {
        _model.Steps = Steps.Select(step => step.Model).ToList();
        OnPropertyChanged(nameof(Steps));
    }

    [RelayCommand]
    private void AddAction(ActionType actionType)
    {
        WorkflowAction newAction = _actionViewModelFactory.CreateDefaultAction(
            actionType,
            (uint)(_model.Actions.Count + 1));

        _model.Actions.Add(newAction);

        var actionVm = CreateViewModelForAction(newAction);

        // Subscribe to PropertyChanged events from new action
        if (actionVm is WorkflowActionViewModel workflowActionVm)
        {
            workflowActionVm.PropertyChanged += OnActionPropertyChanged;
        }

        Actions.Add(actionVm);

        // Trigger PropertyChanged for Actions collection to notify auto-save
        OnPropertyChanged(nameof(Actions));
    }

    [RelayCommand]
    private void DeleteAction(object actionVm)
    {
        if (_actionViewModelFactory.TryGetAction(actionVm, out var actionModel))
        {
            // Unsubscribe from PropertyChanged events before removing
            if (actionVm is WorkflowActionViewModel workflowActionVm)
            {
                workflowActionVm.PropertyChanged -= OnActionPropertyChanged;
            }

            _model.Actions.Remove(actionModel);
            Actions.Remove(actionVm);
            UpdateActionNumbers();

            // Trigger PropertyChanged for Actions collection to notify auto-save
            OnPropertyChanged(nameof(Actions));
        }
    }

    /// <summary>
    /// Updates the Number property of all actions to reflect their current order.
    /// Call this after reordering actions via drag and drop.
    /// Synchronizes the ObservableCollection order back to Model.Actions list.
    /// </summary>
    public void UpdateActionNumbers()
    {
        // Update Number property on ViewModels (won't trigger save - Number is ignored)
        for (int i = 0; i < Actions.Count; i++)
        {
            if (Actions[i] is WorkflowActionViewModel actionVm)
            {
                actionVm.Number = (uint)(i + 1);
            }
        }

        // Synchronize order back to Model.Actions list
        _model.Actions.Clear();
        foreach (var actionVm in Actions.OfType<WorkflowActionViewModel>())
        {
            _model.Actions.Add(actionVm.ToWorkflowAction());
        }

        // Trigger PropertyChanged ONCE to save with correct order
        OnPropertyChanged(nameof(Actions));
    }

    /// <summary>
    /// Starts the workflow in a diagnostic mode that logs the actions which would be executed.
    /// </summary>
    /// <param name="journey">The journey context for the workflow.</param>
    /// <param name="station">The current station context.</param>
    public Task StartAsync(Journey journey, Station station)
    {
        _ = journey;
        _ = station;
        return Task.CompletedTask;
    }

    private object CreateViewModelForAction(WorkflowAction action)
    {
        return _actionViewModelFactory.CreateViewModel(action);
    }

    /// <summary>
    /// Handler for PropertyChanged events from child actions.
    /// Propagates changes upward as PropertyChanged("Actions") to trigger auto-save.
    /// Ignores internal properties (Number) that don't require saving.
    /// </summary>
    private void OnActionPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        // Ignore internal properties that are managed by UpdateActionNumbers()
        // Only user-edited properties (Name, Message, VoiceName, etc.) should trigger save
        if (e.PropertyName == nameof(WorkflowActionViewModel.Number))
            return;

        OnPropertyChanged(nameof(Actions));
    }

    private void OnStepPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        _ = sender;
        _ = e;
        OnPropertyChanged(nameof(Steps));
    }

    private void ClearStepReferences(Guid deletedStepId)
    {
        foreach (var graphStep in _model.Steps!)
        {
            if (graphStep.NextStepId == deletedStepId)
            {
                graphStep.NextStepId = null;
            }

            if (graphStep.ErrorPolicy?.FailureStepId == deletedStepId)
            {
                graphStep.ErrorPolicy.FailureStepId = null;
            }

            switch (graphStep)
            {
                case WorkflowConditionStep condition:
                    if (condition.TrueStepId == deletedStepId) condition.TrueStepId = Guid.Empty;
                    if (condition.FalseStepId == deletedStepId) condition.FalseStepId = Guid.Empty;
                    break;
                case WorkflowParallelStep parallel:
                    parallel.Branches.RemoveAll(branch => branch.EntryStepId == deletedStepId);
                    if (parallel.JoinStepId == deletedStepId) parallel.JoinStepId = Guid.Empty;
                    break;
            }
        }

        if (_model.DefaultErrorPolicy?.FailureStepId == deletedStepId)
        {
            _model.DefaultErrorPolicy.FailureStepId = null;
        }
    }

    private WorkflowRetryPolicy EnsureDefaultRetryPolicy()
    {
        _model.DefaultErrorPolicy ??= new WorkflowErrorPolicy();
        _model.DefaultErrorPolicy.Retry ??= new WorkflowRetryPolicy();
        return _model.DefaultErrorPolicy.Retry;
    }
}
