// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using Action;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Domain;
using Domain.Enum;
using Interface;
using Microsoft.Extensions.Logging;
using Service;
using Sound;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

/// <summary>Edits a reusable workflow in the same order in which its actions execute.</summary>
public sealed partial class WorkflowViewModel : ObservableObject, IViewModelWrapper<Workflow>
{
    private readonly Workflow _model;
    private readonly HashSet<WorkflowActionViewModel> _subscribedActions = [];
    // Preserve original entries, including JSON nulls, while providing removable editor rows.
    private readonly Dictionary<WorkflowActionViewModel, WorkflowAction> _originalActions = [];
    private readonly WorkflowActionViewModelFactory _actionViewModelFactory;

    /// <summary>Creates authoritative action wrappers in persisted list order.</summary>
    public WorkflowViewModel(Workflow model, IIoService? ioService = null, ISoundPlayer? soundPlayer = null, ILoggerFactory? loggerFactory = null)
    {
        ArgumentNullException.ThrowIfNull(model);
        _model = model;
        _model.Actions ??= [];
        _actionViewModelFactory = new WorkflowActionViewModelFactory(
            ioService ?? new NullIoService(), soundPlayer, loggerFactory?.CreateLogger<CommandViewModel>());
        Actions = [];
        foreach (var action in model.Actions)
        {
            var editor = _actionViewModelFactory.CreateViewModel(action);
            _originalActions.Add(editor, action);
            Actions.Add(editor);
        }
        RefreshActionSubscriptions();
        Actions.CollectionChanged += OnActionsChanged;
        UpdateActionNumbers();
    }

    /// <summary>Gets the reusable definition.</summary>
    public Workflow Model => _model;

    /// <summary>Gets the identifier referenced by events.</summary>
    public Guid Id => _model.Id;

    /// <summary>Gets or sets the workflow name.</summary>
    public string Name
    {
        get => _model.Name;
        set => SetProperty(_model.Name, value, _model, static (model, name) => model.Name = name);
    }

    /// <summary>Gets or sets the workflow description.</summary>
    public string Description
    {
        get => _model.Description;
        set => SetProperty(_model.Description, value, _model, static (model, description) => model.Description = description);
    }

    /// <summary>Gets actions in authoritative execution order.</summary>
    public ObservableCollection<WorkflowActionViewModel> Actions { get; }

    /// <summary>Gets a readable action count for library rows.</summary>
    public string ActionSummary => Actions.Count == 1 ? "1 action" : $"{Actions.Count} actions";

    [RelayCommand]
    private void AddAction(ActionType type)
    {
        var model = _actionViewModelFactory.CreateDefaultAction(type, (uint)Actions.Count + 1);
        Actions.Add(_actionViewModelFactory.CreateViewModel(model));
    }

    [RelayCommand]
    private void DeleteAction(WorkflowActionViewModel? action)
    {
        if (action != null)
            Actions.Remove(action);
    }

    /// <summary>Moves an action and updates persisted execution order.</summary>
    public void MoveAction(WorkflowActionViewModel action, int targetIndex)
    {
        var sourceIndex = Actions.IndexOf(action);
        if (sourceIndex < 0) return;
        targetIndex = Math.Clamp(targetIndex, 0, Actions.Count - 1);
        if (sourceIndex != targetIndex)
            Actions.Move(sourceIndex, targetIndex);
    }

    [RelayCommand]
    private void MoveActionUp(WorkflowActionViewModel? action)
    {
        if (action != null) MoveAction(action, Actions.IndexOf(action) - 1);
    }

    [RelayCommand]
    private void MoveActionDown(WorkflowActionViewModel? action)
    {
        if (action != null) MoveAction(action, Actions.IndexOf(action) + 1);
    }

    /// <summary>Synchronizes display ordinals and persisted order after list changes.</summary>
    public void UpdateActionNumbers()
    {
        for (var index = 0; index < Actions.Count; index++)
            Actions[index].Number = (uint)index + 1;
        _model.Actions = Actions.Select(action => _originalActions.GetValueOrDefault(action, action.ToWorkflowAction())).ToList();
        foreach (var removed in _originalActions.Keys.Where(action => !Actions.Contains(action)).ToArray())
            _originalActions.Remove(removed);
        OnPropertyChanged(nameof(Actions));
        OnPropertyChanged(nameof(ActionSummary));
    }

    private void OnActionsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        RefreshActionSubscriptions();
        UpdateActionNumbers();
    }

    private void RefreshActionSubscriptions()
    {
        foreach (var action in _subscribedActions)
            action.PropertyChanged -= OnActionPropertyChanged;
        _subscribedActions.Clear();
        foreach (var action in Actions)
        {
            action.PropertyChanged += OnActionPropertyChanged;
            _subscribedActions.Add(action);
        }
    }

    private void OnActionPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(WorkflowActionViewModel.Number))
            OnPropertyChanged(nameof(Actions));
    }
}
