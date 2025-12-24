// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using Action;

using Backend.Interface;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Domain;
using Domain.Enum;

using Interface;

using Sound;

using System.Collections.ObjectModel;
using System.Diagnostics;

public partial class WorkflowViewModel : ObservableObject, IViewModelWrapper<Workflow>
{
    #region Fields
    // Model
    private readonly Workflow _model;

    // Context
    #pragma warning disable IDE0052 // Remove unread private members - Reserved for future use
    private readonly Project? _project;

    // Optional Services
    private readonly ISpeakerEngine? _speakerEngine;
    private readonly IZ21? _z21;
    #pragma warning restore IDE0052
    #endregion

    public WorkflowViewModel(Workflow model, ISpeakerEngine? speakerEngine = null, Project? project = null, IZ21? z21 = null)
    {
        _model = model;
        _speakerEngine = speakerEngine;
        _project = project;
        _z21 = z21;

        Actions = new ObservableCollection<object>(
            model.Actions.Select(CreateViewModelForAction)
        );
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

        var actionVM = CreateViewModelForAction(newAction);
        Actions.Add(actionVM);
    }

    [RelayCommand]
    private void DeleteAction(object actionVM)
    {
        WorkflowAction? actionModel = actionVM switch
        {
            AnnouncementViewModel avm => avm.ToWorkflowAction(),
            AudioViewModel audvm => audvm.ToWorkflowAction(),
            CommandViewModel cvm => cvm.ToWorkflowAction(),
            _ => null
        };

        if (actionModel != null)
        {
            _model.Actions.Remove(actionModel);
            Actions.Remove(actionVM);
        }
    }

    public Task StartAsync(Journey journey, Station station)
    {
        Debug.WriteLine($"▶ Starte Workflow '{_model.Name}' für Station '{station.Name}'");

        if (Actions.Count == 0)
        {
            Debug.WriteLine($"⚠ Workflow '{_model.Name}' enthält keine Actions");
            return Task.CompletedTask;
        }

        foreach (var actionVM in Actions)
        {
            switch (actionVM)
            {
                case AnnouncementViewModel announcement:
                    Debug.WriteLine($"🎬 Führe Action aus: Announcement - {announcement.Name}");
                    // Note: ExecuteAsync removed - execution now handled by WorkflowService
                    break;

                case AudioViewModel audio:
                    Debug.WriteLine($"🎬 Führe Action aus: Audio - {audio.Name}");
                    // Note: ExecuteAsync removed - execution now handled by WorkflowService
                    break;

                case CommandViewModel command:
                    Debug.WriteLine($"🎬 Führe Action aus: Command - {command.Name}");
                    // Note: ExecuteAsync removed - execution now handled by WorkflowService
                    break;

                default:
                    Debug.WriteLine($"⚠ Unbekannter Action-ViewModel-Typ: {actionVM.GetType().Name}");
                    break;
            }
        }

        Debug.WriteLine($"✅ Workflow '{_model.Name}' abgeschlossen");
        return Task.CompletedTask;
    }

    private object CreateViewModelForAction(WorkflowAction action)
    {
        return action.Type switch
        {
            ActionType.Announcement => new AnnouncementViewModel(action),
            ActionType.Audio => new AudioViewModel(action),
            ActionType.Command => new CommandViewModel(action),
            _ => throw new NotSupportedException($"Action-Typ {action.Type} wird nicht unterstützt")
        };
    }
}
