using System.Diagnostics;

using Moba.Backend.Model;

namespace Moba.Backend;

/// <summary>
/// Verwaltet die Ausführung von Workflows basierend auf Feedback-Ereignissen (Gleisrückmeldestellen).
/// </summary>
public class WorkflowManager : IDisposable
{
    private readonly Z21 _z21;
    private readonly List<Workflow> _workflows;
    private readonly Dictionary<uint, DateTime> _lastFeedbackTime = new();
    private bool _isProcessing;
    private bool _disposed;

    public WorkflowManager(Z21 z21, List<Workflow> workflows)
    {
        _z21 = z21;
        _workflows = workflows;
        _z21.Received += OnFeedbackReceived;
    }

    private async void OnFeedbackReceived(FeedbackResult feedback)
    {
        if (_isProcessing)
        {
            Debug.WriteLine("⏸ Workflow-Feedback ignoriert - Verarbeitung läuft bereits");
            return;
        }

        Debug.WriteLine($"📡 Workflow-Feedback empfangen: InPort {feedback.InPort}");

        foreach (var workflow in _workflows)
        {
            if (workflow.InPort == feedback.InPort)
            {
                if (ShouldIgnoreFeedback(workflow))
                {
                    Debug.WriteLine($"⏭ Feedback für Workflow '{workflow.Name}' ignoriert (Timer aktiv)");
                    continue;
                }

                UpdateLastFeedbackTime(workflow.InPort);
                await HandleWorkflowFeedbackAsync(workflow);
            }
        }
    }

    private bool ShouldIgnoreFeedback(Workflow workflow)
    {
        if (!workflow.IsUsingTimerToIgnoreFeedbacks)
        {
            return false;
        }

        if (_lastFeedbackTime.TryGetValue(workflow.InPort, out DateTime lastTime))
        {
            var elapsed = (DateTime.Now - lastTime).TotalSeconds;
            return elapsed < workflow.IntervalForTimerToIgnoreFeedbacks;
        }

        return false;
    }

    private void UpdateLastFeedbackTime(uint inPort)
    {
        _lastFeedbackTime[inPort] = DateTime.Now;
    }

    private async Task HandleWorkflowFeedbackAsync(Workflow workflow)
    {
        _isProcessing = true;

        try
        {
            Debug.WriteLine($"▶ Workflow '{workflow.Name}' wird ausgeführt");

            await workflow.StartAsync();

            Debug.WriteLine($"✅ Workflow '{workflow.Name}' abgeschlossen");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ Fehler bei Workflow '{workflow.Name}': {ex.Message}");
        }
        finally
        {
            _isProcessing = false;
        }
    }

    public void ResetAll()
    {
        _lastFeedbackTime.Clear();
        Debug.WriteLine("🔄 Alle Workflow-Timer zurückgesetzt");
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            _z21.Received -= OnFeedbackReceived;
        }

        _disposed = true;
    }
}