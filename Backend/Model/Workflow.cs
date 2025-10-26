using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Moba.Backend.Model;

public partial class Workflow : ObservableObject
{
    public Workflow()
    {
        Id = Guid.NewGuid();  // ✅ Eindeutige ID generieren
        Name = "New Flow";
        Actions = [];
    }

    /// <summary>
    /// Eindeutiger Identifier für diesen Workflow
    /// </summary>
    public Guid Id { get; set; }

    [ObservableProperty]
    private string name = "New Flow";

    public List<Action.Base> Actions { get; set; }

    /// <summary>
    /// Zuordnung R-BUS-Port zu Workflow.
    /// </summary>
    [ObservableProperty]
    private uint inPort;

    /// <summary>
    /// Wiederholte Feedbacks ignorieren.
    /// </summary>
    [ObservableProperty]
    private bool isUsingTimerToIgnoreFeedbacks;

    /// <summary>
    /// Wiederholte Feedbacks ignorieren für x Sekunden.
    /// </summary>
    [ObservableProperty]
    private double intervalForTimerToIgnoreFeedbacks;

    /// <summary>
    /// Startet die Ausführung aller Actions dieses Workflows
    /// </summary>
    public async Task StartAsync()
    {
        if (Actions.Count == 0)
        {
            Debug.WriteLine($"⚠ Workflow '{Name}' hat keine Actions");
            return;
        }

        Debug.WriteLine($"▶ Workflow '{Name}' (ID: {Id}) wird gestartet ({Actions.Count} Actions)");

        try
        {
            foreach (var action in Actions)
            {
                Debug.WriteLine($"  🔧 Aktion: {action.Name} ({action.Type})");

                // Hier erfolgt die eigentliche Action-Ausführung
                // await action.ExecuteAsync();
                await Task.CompletedTask;
            }

            Debug.WriteLine($"✅ Workflow '{Name}' abgeschlossen");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ Fehler in Workflow '{Name}': {ex.Message}");
            throw;
        }
    }

    // ✅ Überschreibe Equals und GetHashCode für ComboBox-Gleichheit
    public override bool Equals(object? obj)
    {
        if (obj is Workflow other)
        {
            return Id == other.Id;  // Vergleich basierend auf GUID
        }
        return false;
    }

    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }

    public override string ToString()
    {
        return Name;
    }
}