using Moba.Backend.Model;
using Moba.Backend.Model.Enum;

using System.Diagnostics;

namespace Moba.Backend;

/// <summary>
/// Manages the execution of a workflow and there actions related to a journey or stop (station) based on feedback events (track feedback points).
/// </summary>
public class JourneyManager : IDisposable
{
    private readonly Z21 _z21;
    private readonly List<Journey> _journeys;
    private readonly Dictionary<uint, DateTime> _lastFeedbackTime = new();
    private readonly SemaphoreSlim _processingLock = new(1, 1);
    private bool _disposed;
    private readonly Model.Action.ActionExecutionContext _executionContext;

    public JourneyManager(Z21 z21, List<Journey> journeys, Model.Action.ActionExecutionContext? executionContext = null)
    {
        _z21 = z21;
        _journeys = journeys;
        _z21.Received += OnFeedbackReceived;

        // Create execution context with Z21
        _executionContext = executionContext ?? new Model.Action.ActionExecutionContext
        {
            Z21 = z21
        };
    }

    private async void OnFeedbackReceived(FeedbackResult feedback)
    {
        // Wait for lock (blocking) - queues feedbacks sequentially
        await _processingLock.WaitAsync();

        try
        {
            Debug.WriteLine($"📡 Feedback received: InPort {feedback.InPort}");

            foreach (var journey in _journeys)
            {
                if (journey.InPort == feedback.InPort)
                {
                    if (ShouldIgnoreFeedback(journey))
                    {
                        Debug.WriteLine($"⏭ Feedback for journey '{journey.Name}' ignored (timer active)");
                        continue;
                    }

                    UpdateLastFeedbackTime(journey.InPort);
                    await HandleJourneyFeedbackAsync(journey);
                }
            }
        }
        finally
        {
            _processingLock.Release();
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
            var elapsed = (DateTime.UtcNow - lastTime).TotalSeconds;
            return elapsed < journey.IntervalForTimerToIgnoreFeedbacks;
        }

        return false;
    }

    private void UpdateLastFeedbackTime(uint inPort)
    {
        _lastFeedbackTime[inPort] = DateTime.UtcNow;
    }

    private async Task HandleJourneyFeedbackAsync(Journey journey)
    {
        journey.CurrentCounter++;
        Debug.WriteLine($"🔄 Journey '{journey.Name}': Round {journey.CurrentCounter}, Position {journey.CurrentPos}");

        if (journey.CurrentPos >= journey.Stations.Count)
        {
            Debug.WriteLine($"⚠ CurrentPos out of Stations list bounds");
            return;
        }

        var currentStation = journey.Stations[(int)journey.CurrentPos];

        if (journey.CurrentCounter >= currentStation.NumberOfLapsToStop)
        {
            Debug.WriteLine($"🚉 Station reached: {currentStation.Name}");

            // Execute station workflow if present
            if (currentStation.Flow != null)
            {
                await currentStation.Flow.StartAsync(_executionContext);
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

    private async Task HandleLastStationAsync(Journey journey)
    {
        Debug.WriteLine($"🏁 Last station of journey '{journey.Name}' reached");

        switch (journey.OnLastStop)
        {
            case BehaviorOnLastStop.BeginAgainFromFistStop:
                Debug.WriteLine("🔄 Journey will restart from beginning");
                journey.CurrentPos = 0;
                break;

            case BehaviorOnLastStop.GotoJourney:
                Debug.WriteLine($"➡ Switching to journey: {journey.NextJourney}");
                var nextJourney = _journeys.FirstOrDefault(j => j.Name == journey.NextJourney);
                if (nextJourney != null)
                {
                    nextJourney.CurrentPos = nextJourney.FirstPos;
                    Debug.WriteLine($"✅ Journey '{nextJourney.Name}' activated at position {nextJourney.FirstPos}");
                }
                else
                {
                    Debug.WriteLine($"⚠ Journey '{journey.NextJourney}' not found");
                }
                break;

            case BehaviorOnLastStop.None:
                Debug.WriteLine("⏹ Journey stops");
                break;
        }

        await Task.CompletedTask;
    }

    public static void Reset(Journey journey)
    {
        journey.CurrentCounter = 0;
        journey.CurrentPos = journey.FirstPos;
        Debug.WriteLine($"🔄 Journey '{journey.Name}' reset");
    }

    public void ResetAll()
    {
        foreach (var journey in _journeys)
        {
            Reset(journey);
        }
        _lastFeedbackTime.Clear();
        Debug.WriteLine("🔄 All journeys reset");
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
            _processingLock.Dispose();
        }

        _disposed = true;
    }
}