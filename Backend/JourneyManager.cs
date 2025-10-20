using Moba.Backend.Model;
using Moba.Backend.Model.Enum;

using System.Diagnostics;

namespace Moba.Backend;

/// <summary>
/// Verwaltet die Ausführung von Reisen basierend auf Feedback-Ereignissen (Gleisrückmeldestellen).
/// </summary>
public class JourneyManager : IDisposable
{
    private readonly Z21 _z21;
    private readonly List<Journey> _journeys;
    private readonly Dictionary<uint, DateTime> _lastFeedbackTime = new();
    private bool _isProcessing;
    private bool _disposed;

    public JourneyManager(Z21 z21, List<Journey> journeys)
    {
        _z21 = z21;
        _journeys = journeys;
        _z21.Received += OnFeedbackReceived;
    }

    private async void OnFeedbackReceived(FeedbackResult feedback)
    {
        if (_isProcessing)
        {
            Debug.WriteLine("⏸ Feedback ignoriert - Verarbeitung läuft bereits");
            return;
        }

        Debug.WriteLine($"📡 Feedback empfangen: InPort {feedback.InPort}");

        foreach (var journey in _journeys)
        {
            if (journey.InPort == feedback.InPort)
            {
                if (ShouldIgnoreFeedback(journey))
                {
                    Debug.WriteLine($"⏭ Feedback für Journey '{journey.Name}' ignoriert (Timer aktiv)");
                    continue;
                }

                UpdateLastFeedbackTime(journey.InPort);
                await HandleJourneyFeedbackAsync(journey);
            }
        }
    }

    private bool ShouldIgnoreFeedback(Journey journey)
    {
        if (!journey.IsUsingTimerToIgnoreFeedbacks)
        {
            return false;
        }

        if (_lastFeedbackTime.TryGetValue(journey.InPort, out DateTime lastTime))
        {
            var elapsed = (DateTime.Now - lastTime).TotalSeconds;
            return elapsed < journey.IntervalForTimerToIgnoreFeedbacks;
        }

        return false;
    }

    private void UpdateLastFeedbackTime(uint inPort)
    {
        _lastFeedbackTime[inPort] = DateTime.Now;
    }

    private async Task HandleJourneyFeedbackAsync(Journey journey)
    {
        _isProcessing = true;

        try
        {
            journey.CurrentCounter++;
            Debug.WriteLine($"🔄 Journey '{journey.Name}': Runde {journey.CurrentCounter}, Position {journey.CurrentPos}");

            if (journey.CurrentPos >= journey.Stations.Count)
            {
                Debug.WriteLine($"⚠ CurrentPos außerhalb der Stations-Liste");
                return;
            }

            var currentStation = journey.Stations[(int)journey.CurrentPos];

            if (journey.CurrentCounter >= currentStation.NumberOfLapsToStop)
            {
                Debug.WriteLine($"🚉 Station erreicht: {currentStation.Name}");

                // Workflow der Station ausführen, falls vorhanden
                if (currentStation.Flow != null)
                {
                    await currentStation.Flow.StartAsync();
                }

                journey.CurrentCounter = 0;

                bool isLastStation = journey.CurrentPos == journey.Stations.Count - 1;

                if (isLastStation)
                {
                    await HandleLastStationAsync(journey);
                }
                else
                {
                    journey.CurrentPos++;
                }
            }
        }
        finally
        {
            _isProcessing = false;
        }
    }

    private async Task HandleLastStationAsync(Journey journey)
    {
        Debug.WriteLine($"🏁 Letzte Station von Journey '{journey.Name}' erreicht");

        switch (journey.OnLastStop)
        {
            case BehaviorOnLastStop.BeginAgainFromFistStop:
                Debug.WriteLine("🔄 Journey wird von vorne gestartet");
                journey.CurrentPos = 0;
                break;

            case BehaviorOnLastStop.GotoJourney:
                Debug.WriteLine($"➡ Wechsel zu Journey: {journey.NextJourney}");
                var nextJourney = _journeys.FirstOrDefault(j => j.Name == journey.NextJourney);
                if (nextJourney != null)
                {
                    nextJourney.CurrentPos = nextJourney.FirstPos;
                    Debug.WriteLine($"✅ Journey '{nextJourney.Name}' aktiviert bei Position {nextJourney.FirstPos}");
                }
                else
                {
                    Debug.WriteLine($"⚠ Journey '{journey.NextJourney}' nicht gefunden");
                }
                break;

            case BehaviorOnLastStop.None:
            default:
                Debug.WriteLine("⏹ Journey stoppt");
                break;
        }

        await Task.CompletedTask;
    }

    public static void Reset(Journey journey)
    {
        journey.CurrentCounter = 0;
        journey.CurrentPos = journey.FirstPos;
        Debug.WriteLine($"🔄 Journey '{journey.Name}' zurückgesetzt");
    }

    public void ResetAll()
    {
        foreach (var journey in _journeys)
        {
            Reset(journey);
        }
        _lastFeedbackTime.Clear();
        Debug.WriteLine("🔄 Alle Journeys zurückgesetzt");
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