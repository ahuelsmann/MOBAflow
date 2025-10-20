using System.Diagnostics;

namespace Moba.Backend.Model;

public class Workflow
{
    public Workflow()
    {
        Name = "New Flow";
        Actions = [];
    }

    public string Name { get; set; }

    public List<Action.Base> Actions { get; set; }

    /// <summary>
    /// Zuordnung R-BUS-Port zu Workflow.
    /// </summary>
    public uint InPort { get; set; }

    /// <summary>
    /// Wiederholte Feedbacks ignorieren.
    /// </summary>
    public bool IsUsingTimerToIgnoreFeedbacks { get; set; }

    /// <summary>
    /// Wiederholte Feedbacks ignorieren für x Sekunden.
    /// </summary>
    public double IntervalForTimerToIgnoreFeedbacks { get; set; }

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

        Debug.WriteLine($"▶ Workflow '{Name}' wird gestartet ({Actions.Count} Actions)");

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
}