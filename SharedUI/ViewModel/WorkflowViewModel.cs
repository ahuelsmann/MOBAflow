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
                case AnnouncementViewModel announcementVM:
                    System.Diagnostics.Debug.WriteLine($"🎬 Führe Action aus: Announcement - {announcementVM.Name}");
                    await announcementVM.ExecuteAsync(journey, station);
                    break;

                case GongViewModel gongVM:
                    System.Diagnostics.Debug.WriteLine($"🎬 Führe Action aus: Gong - {gongVM.Name}");
                    await gongVM.ExecuteAsync(journey, station);
                    break;

                case CommandViewModel commandVM:
                    System.Diagnostics.Debug.WriteLine($"🎬 Führe Action aus: Command - {commandVM.Name}");
                    await commandVM.ExecuteAsync(journey, station);
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
            Gong gong => new GongViewModel(gong),
            Command command => new CommandViewModel(command, _z21),
            _ => throw new NotSupportedException($"Action-Typ {action.GetType().Name} wird nicht unterstützt")
        };
    }

    public Workflow ToModel()
    {
        Model.Actions.Clear();
        
        foreach (var actionVM in Actions)
        {
            switch (actionVM)
            {
                case AnnouncementViewModel announcementVM:
                    Model.Actions.Add(announcementVM.ToModel());
                    break;
                case GongViewModel gongVM:
                    Model.Actions.Add(gongVM.ToModel());
                    break;
                case CommandViewModel commandVM:
                    Model.Actions.Add(commandVM.ToModel());
                    break;
            }
        }

        return Model;
    }

    public static WorkflowViewModel FromModel(Workflow model, ISpeakerEngine? speakerEngine = null, Project? project = null, Z21? z21 = null)
    {
        return new WorkflowViewModel(model, speakerEngine, project, z21);
    }
}
