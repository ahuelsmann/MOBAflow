// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Moba.Backend.Interface;
using Moba.Domain;
using Moba.Domain.Enum;
using Moba.SharedUI.Enum;
using Moba.SharedUI.Interface;

using Sound;

using System.Collections.ObjectModel;
using System.Linq;

public partial class WorkflowViewModel : ObservableObject, IViewModelWrapper<Workflow>
{
    private readonly ISpeakerEngine? _speakerEngine;
    private readonly Project? _project;
    private readonly IZ21? _z21;

    public MobaType EntityType => MobaType.Workflow;

    [ObservableProperty]
    private Workflow model;

    public WorkflowViewModel(Workflow model, ISpeakerEngine? speakerEngine = null, Project? project = null, IZ21? z21 = null)
    {
        Model = model;
        _speakerEngine = speakerEngine;
        _project = project;
        _z21 = z21;

        Actions = new ObservableCollection<object>(
            model.Actions.Select(CreateViewModelForAction)
        );
    }

    public string Name
    {
        get => Model.Name;
        set => SetProperty(Model.Name, value, Model, (m, v) => m.Name = v);
    }

    public string Description
    {
        get => Model.Description ?? string.Empty;
        set => SetProperty(Model.Description, value, Model, (m, v) => m.Description = v);
    }

    public uint InPort
    {
        get => Model.InPort;
        set => SetProperty(Model.InPort, value, Model, (m, v) => m.InPort = v);
    }

    public bool IsUsingTimerToIgnoreFeedbacks
    {
        get => Model.IsUsingTimerToIgnoreFeedbacks;
        set => SetProperty(Model.IsUsingTimerToIgnoreFeedbacks, value, Model, (m, v) => m.IsUsingTimerToIgnoreFeedbacks = v);
    }

    public double IntervalForTimerToIgnoreFeedbacks
    {
        get => Model.IntervalForTimerToIgnoreFeedbacks;
        set => SetProperty(Model.IntervalForTimerToIgnoreFeedbacks, value, Model, (m, v) => m.IntervalForTimerToIgnoreFeedbacks = v);
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
                Number = (uint)(Model.Actions.Count + 1),
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
                Number = (uint)(Model.Actions.Count + 1),
                Type = ActionType.Audio,
                Parameters = new Dictionary<string, object>
                {
                    ["FilePath"] = "path/to/sound.wav"
                }
            },
            ActionType.Command => new WorkflowAction
            {
                Name = "New Command",
                Number = (uint)(Model.Actions.Count + 1),
                Type = ActionType.Command,
                Parameters = new Dictionary<string, object>
                {
                    ["Bytes"] = Array.Empty<byte>()
                }
            },
            _ => throw new ArgumentException($"Unsupported action type: {actionType}")
        };

        Model.Actions.Add(newAction);

        var actionVM = CreateViewModelForAction(newAction);
        Actions.Add(actionVM);
    }

    [RelayCommand]
    private void DeleteAction(object actionVM)
    {
        if (actionVM == null) return;

        WorkflowAction? actionModel = actionVM switch
        {
            Action.AnnouncementViewModel avm => avm.ToWorkflowAction(),
            Action.AudioViewModel audvm => audvm.ToWorkflowAction(),
            Action.CommandViewModel cvm => cvm.ToWorkflowAction(),
            _ => null
        };

        if (actionModel != null)
        {
            Model.Actions.Remove(actionModel);
            Actions.Remove(actionVM);
        }
    }

    public async Task StartAsync(Journey journey, Station station)
    {
        System.Diagnostics.Debug.WriteLine($"▶ Starte Workflow '{Model.Name}' für Station '{station.Name}'");

        if (Actions.Count == 0)
        {
            System.Diagnostics.Debug.WriteLine($"⚠ Workflow '{Model.Name}' enthält keine Actions");
            return;
        }

        foreach (var actionVM in Actions)
        {
            switch (actionVM)
            {
                case Action.AnnouncementViewModel announcement:
                    System.Diagnostics.Debug.WriteLine($"🎬 Führe Action aus: Announcement - {announcement.Name}");
                    // Note: ExecuteAsync removed - execution now handled by WorkflowService
                    break;

                case Action.AudioViewModel audio:
                    System.Diagnostics.Debug.WriteLine($"🎬 Führe Action aus: Audio - {audio.Name}");
                    // Note: ExecuteAsync removed - execution now handled by WorkflowService
                    break;

                case Action.CommandViewModel command:
                    System.Diagnostics.Debug.WriteLine($"🎬 Führe Action aus: Command - {command.Name}");
                    // Note: ExecuteAsync removed - execution now handled by WorkflowService
                    break;

                default:
                    System.Diagnostics.Debug.WriteLine($"⚠ Unbekannter Action-ViewModel-Typ: {actionVM.GetType().Name}");
                    break;
            }
        }

        System.Diagnostics.Debug.WriteLine($"✅ Workflow '{Model.Name}' abgeschlossen");
    }

    private object CreateViewModelForAction(WorkflowAction action)
    {
        return action.Type switch
        {
            ActionType.Announcement => new Action.AnnouncementViewModel(action),
            ActionType.Audio => new Action.AudioViewModel(action),
            ActionType.Command => new Action.CommandViewModel(action),
            _ => throw new NotSupportedException($"Action-Typ {action.Type} wird nicht unterstützt")
        };
    }
}