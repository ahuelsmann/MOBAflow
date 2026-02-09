// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using Action;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Domain;
using Domain.Enum;
using Interface;
using Service;
using Sound;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;

public partial class WorkflowViewModel : ObservableObject, IViewModelWrapper<Workflow>
{
    #region Fields
    // Model
    private readonly Workflow _model;

    // Services
    private readonly IIoService _ioService;
    private readonly ISoundPlayer? _soundPlayer;
    #endregion

    public WorkflowViewModel(Workflow model, IIoService? ioService = null, ISoundPlayer? soundPlayer = null)
    {
        _model = model;
        _ioService = ioService ?? new NullIoService();
        _soundPlayer = soundPlayer;

        Actions = new ObservableCollection<object>(
            model.Actions
                .OrderBy(a => a.Number)
                .Select(CreateViewModelForAction)
        );

        foreach (var actionVm in Actions.OfType<WorkflowActionViewModel>())
        {
            actionVm.PropertyChanged += OnActionPropertyChanged;
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

    public string Name
    {
        get => _model.Name;
        set => SetProperty(_model.Name, value, _model, (m, v) => m.Name = v);
    }

    public string Description
    {
        get => _model.Description;
        set => SetProperty(_model.Description, value, _model, (m, v) => m.Description = v);
    }

    public uint InPort
    {
        get => _model.InPort;
        set => SetProperty(_model.InPort, value, _model, (m, v) => m.InPort = v);
    }

    public bool IsUsingTimerToIgnoreFeedbacks
    {
        get => _model.IsUsingTimerToIgnoreFeedbacks;
        set => SetProperty(_model.IsUsingTimerToIgnoreFeedbacks, value, _model, (m, v) => m.IsUsingTimerToIgnoreFeedbacks = v);
    }

    public double IntervalForTimerToIgnoreFeedbacks
    {
        get => _model.IntervalForTimerToIgnoreFeedbacks;
        set => SetProperty(_model.IntervalForTimerToIgnoreFeedbacks, value, _model, (m, v) => m.IntervalForTimerToIgnoreFeedbacks = v);
    }

    public WorkflowExecutionMode ExecutionMode
    {
        get => _model.ExecutionMode;
        set => SetProperty(_model.ExecutionMode, value, _model, (m, v) => m.ExecutionMode = v);
    }

    /// <summary>
    /// Gets all possible WorkflowExecutionMode values for ComboBox binding.
    /// </summary>
    public IEnumerable<WorkflowExecutionMode> ExecutionModeValues => Enum.GetValues<WorkflowExecutionMode>();

    public ObservableCollection<object> Actions { get; }

    [RelayCommand]
    private void AddAction(ActionType actionType)
    {
        WorkflowAction newAction = actionType switch
        {
            ActionType.Announcement => new WorkflowAction
            {
                Name = "New Announcement",
                Number = (uint)(_model.Actions.Count + 1),
                Type = ActionType.Announcement,
                Parameters = new Dictionary<string, object>
                {
                    ["Message"] = "New announcement text",
                    ["VoiceName"] = "de-DE-KatjaNeural"
                }
            },
            ActionType.Audio => new WorkflowAction
            {
                Name = "New Audio",
                Number = (uint)(_model.Actions.Count + 1),
                Type = ActionType.Audio,
                Parameters = new Dictionary<string, object>
                {
                    ["FilePath"] = "path/to/sound.wav"
                }
            },
            ActionType.Command => new WorkflowAction
            {
                Name = "New Command",
                Number = (uint)(_model.Actions.Count + 1),
                Type = ActionType.Command,
                Parameters = new Dictionary<string, object>
                {
                    ["Bytes"] = Array.Empty<byte>()
                }
            },
            _ => throw new ArgumentException($"Unsupported action type: {actionType}")
        };

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
        WorkflowAction? actionModel = actionVm switch
        {
            AnnouncementViewModel avm => avm.ToWorkflowAction(),
            AudioViewModel audvm => audvm.ToWorkflowAction(),
            CommandViewModel cvm => cvm.ToWorkflowAction(),
            _ => null
        };

        if (actionModel != null)
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
    /// Call this after reordering actions via drag & drop.
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

    public Task StartAsync(Journey journey, Station station)
    {
        Debug.WriteLine($"[START] Workflow '{_model.Name}' for station '{station.Name}'");

        if (Actions.Count == 0)
        {
            Debug.WriteLine($"[WARN] Workflow '{_model.Name}' has no actions");
            return Task.CompletedTask;
        }

        foreach (var actionVm in Actions)
        {
            switch (actionVm)
            {
                case AnnouncementViewModel announcement:
                    Debug.WriteLine($"[ACTION] Announcement - {announcement.Name}");
                    break;

                case AudioViewModel audio:
                    Debug.WriteLine($"[ACTION] Audio - {audio.Name}");
                    break;

                case CommandViewModel command:
                    Debug.WriteLine($"[ACTION] Command - {command.Name}");
                    break;

                default:
                    Debug.WriteLine($"[WARN] Unknown action view model type: {actionVm.GetType().Name}");
                    break;
            }
        }

        return Task.CompletedTask;
    }

    private object CreateViewModelForAction(WorkflowAction action)
    {
        return action.Type switch
        {
            ActionType.Announcement => new AnnouncementViewModel(action),
            ActionType.Audio => new AudioViewModel(action, _ioService, _soundPlayer),
            ActionType.Command => new CommandViewModel(action),
            _ => throw new NotSupportedException($"Action type {action.Type} is not supported")
        };
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
        {
            Debug.WriteLine("[SKIP] Action.Number changed - internal property, no save needed");
            return;
        }

        Debug.WriteLine($"[INFO] Action property '{e.PropertyName}' changed, propagating as PropertyChanged(Actions)");
        OnPropertyChanged(nameof(Actions));
    }
}
