namespace Moba.SharedUI.ViewModel.Action;

using Backend.Model;
using Backend.Model.Action;

using CommunityToolkit.Mvvm.ComponentModel;

using Sound;

using System.Diagnostics;

public partial class AnnouncementViewModel : ObservableObject
{
    private readonly ISpeakerEngine? _speakerEngine;
    private readonly Project? _project;

    [ObservableProperty]
    private Announcement model;

    public AnnouncementViewModel(Announcement model, ISpeakerEngine? speakerEngine = null, Project? project = null)
    {
        Model = model;
        _speakerEngine = speakerEngine;
        _project = project;
    }

    public string? TextToSpeak
    {
        get => Model.TextToSpeak;
        set => SetProperty(Model.TextToSpeak, value, Model, (m, v) => m.TextToSpeak = v);
    }

    public string Name
    {
        get => Model.Name;
        set => SetProperty(Model.Name, value, Model, (m, v) => m.Name = v);
    }

    public async Task ExecuteAsync(Journey journey, Station station)
    {
        if (string.IsNullOrEmpty(Model.TextToSpeak))
        {
            return;
        }

        string message = ReplaceAnnouncementPlaceholders(Model.TextToSpeak, station, journey);

        Debug.WriteLine($"📢 Ansage: {message}");

        if (_speakerEngine != null)
        {
            string? voiceName = _project?.Settings.VoiceName;
            await _speakerEngine.AnnouncementAsync(message, voiceName);
        }
        else
        {
            Debug.WriteLine("⚠ ISpeakerEngine nicht verfügbar - Ansage nur als Debug-Output");
        }
    }

    private static string ReplaceAnnouncementPlaceholders(string text, Station station, Journey journey)
    {
        return text
            .Replace("{StationName}", station.Name)
            .Replace("{Track}", station.Track.ToString())
            .Replace("{ExitSide}", station.IsExitOnLeft ? "links" : "rechts")
            .Replace("{TrainName}", journey.Train?.Name ?? "Unbekannt")
            .Replace("{CurrentLap}", journey.CurrentCounter.ToString());
    }
}