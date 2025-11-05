namespace Moba.Backend.Manager;

using Model;

using System.Diagnostics;

public class StationManager : BaseFeedbackManager<Station>
{
    private readonly SemaphoreSlim _processingLock = new(1, 1);

    /// <summary>
    /// Initializes a new instance of the StationManager.
    /// </summary>
    /// <param name="z21">Z21 command station for receiving feedback events</param>
    /// <param name="platforms">List of platforms to manage</param>
    /// <param name="executionContext">Optional execution context; if null, a new context with Z21 will be created</param>
    public StationManager(Z21 z21, List<Station> stations, Model.Action.ActionExecutionContext? executionContext = null)
        : base(z21, stations, executionContext)
    {
    }

    protected override async Task ProcessFeedbackAsync(FeedbackResult feedback)
    {
        // Wait for lock (blocking) - queues feedbacks sequentially
        await _processingLock.WaitAsync();

        try
        {
            Debug.WriteLine($"📡 Feedback received: InPort {feedback.InPort}");

            foreach (var station in Entities)
            {
                if (GetInPort(station) == feedback.InPort)
                {
                    if (ShouldIgnoreFeedback(station))
                    {
                        Debug.WriteLine($"⏭ Feedback for station '{GetEntityName(station)}' ignored (timer active)");
                        continue;
                    }

                    UpdateLastFeedbackTime(GetInPort(station));
                    await HandleFeedbackAsync(station);
                }
            }
        }
        finally
        {
            _processingLock.Release();
        }
    }

    private async Task HandleFeedbackAsync(Station station)
    {
        Debug.WriteLine($"▶ Executing station workflow for '{station.Name}'");

        if (station.Flow != null)
        {
            await station.Flow.StartAsync(ExecutionContext);
            Debug.WriteLine($"✅ Station workflow '{station.Name}' completed");
        }
        else
        {
            Debug.WriteLine($"⚠ Station '{station.Name}' has no workflow assigned");
        }
    }

    protected override uint GetInPort(Station entity) => entity.Flow.InPort;

    protected override bool IsUsingTimerToIgnoreFeedbacks(Station entity) => entity.Flow.IsUsingTimerToIgnoreFeedbacks;

    protected override double GetIntervalForTimerToIgnoreFeedbacks(Station entity) => entity.Flow.IntervalForTimerToIgnoreFeedbacks;

    protected override string GetEntityName(Station entity) => entity.Flow.Name;
}