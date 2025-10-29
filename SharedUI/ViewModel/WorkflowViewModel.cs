namespace Moba.SharedUI.ViewModel;

using Action;

using Backend;
using Backend.Model;
using Backend.Model.Action;

using CommunityToolkit.Mvvm.ComponentModel;

using Sound;

using System.Collections.ObjectModel;
using System.Linq;

public partial class WorkflowViewModel : ObservableObject
{
    private readonly ISpeakerEngine? _speakerEngine;
    private readonly Project? _project;
    private readonly Z21? _z21;

    [ObservableProperty]
    private Workflow model;

    public WorkflowViewModel(Workflow model, ISpeakerEngine? speakerEngine = null, Project? project = null, Z21? z21 = null)
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
                case AnnouncementViewModel announcement:
                    System.Diagnostics.Debug.WriteLine($"🎬 Führe Action aus: Announcement - {announcement.Name}");
                    await announcement.ExecuteAsync(journey, station);
                    break;

                case AudioViewModel audio:
                    System.Diagnostics.Debug.WriteLine($"🎬 Führe Action aus: Sound - {audio.Name}");
                    await audio.ExecuteAsync(journey, station);
                    break;

                case CommandViewModel command:
                    System.Diagnostics.Debug.WriteLine($"🎬 Führe Action aus: Command - {command.Name}");
                    await command.ExecuteAsync(journey, station);
                    break;

                default:
                    System.Diagnostics.Debug.WriteLine($"⚠ Unbekannter Action-ViewModel-Typ: {actionVM.GetType().Name}");
                    break;
            }
        }

        System.Diagnostics.Debug.WriteLine($"✅ Workflow '{Model.Name}' abgeschlossen");
    }

    private object CreateViewModelForAction(Base action)
    {
        return action switch
        {
            Announcement announcement => new AnnouncementViewModel(announcement, _speakerEngine, _project),
            Audio gong => new AudioViewModel(gong),
            Command command => new CommandViewModel(command, _z21),
            _ => throw new NotSupportedException($"Action-Typ {action.GetType().Name} wird nicht unterstützt")
        };
    }
}