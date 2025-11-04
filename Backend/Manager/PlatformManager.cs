using Moba.Backend.Model;

using System.Diagnostics;

namespace Moba.Backend.Manager;

/// <summary>
/// Manages the execution of platform workflows based on feedback events (track feedback points).
/// Represents the platform perspective: announcements from the platform's point of view
/// (e.g., "Attention on platform 3. The InterCity from Koblenz to Hamburg is now arriving.").
/// Unlike JourneyManager (train perspective) and WorkflowManager (generic workflows),
/// PlatformManager is specifically designed for station/platform-centric announcements.
/// </summary>
public class PlatformManager : BaseFeedbackManager<Platform>
{
    private readonly SemaphoreSlim _processingLock = new(1, 1);

    /// <summary>
    /// Initializes a new instance of the PlatformManager.
    /// </summary>
    /// <param name="z21">Z21 command station for receiving feedback events</param>
    /// <param name="platforms">List of platforms to manage</param>
    /// <param name="executionContext">Optional execution context; if null, a new context with Z21 will be created</param>
    public PlatformManager(Z21 z21, List<Platform> platforms, Model.Action.ActionExecutionContext? executionContext = null)
    : base(z21, platforms, executionContext)
    {
    }

    protected override async Task ProcessFeedbackAsync(FeedbackResult feedback)
    {
        // Wait for lock (blocking) - queues feedbacks sequentially
        await _processingLock.WaitAsync();

        try
        {
            Debug.WriteLine($"📡 Feedback received: InPort {feedback.InPort}");

            foreach (var platform in Entities)
            {
                if (GetInPort(platform) == feedback.InPort)
                {
                    if (ShouldIgnoreFeedback(platform))
                    {
                        Debug.WriteLine($"⏭ Feedback for platform '{GetEntityName(platform)}' ignored (timer active)");
                        continue;
                    }

                    UpdateLastFeedbackTime(GetInPort(platform));
                    await HandleFeedbackAsync(platform);
                }
            }
        }
        finally
        {
            _processingLock.Release();
        }
    }

    /// <summary>
    /// Executes the workflow associated with the platform.
    /// </summary>
    /// <param name="platform">The platform whose workflow should be executed</param>
    private async Task HandleFeedbackAsync(Platform platform)
    {
        try
        {
            Debug.WriteLine($"▶ Executing platform workflow for '{platform.Name}' (Track {platform.Track})");

            if (platform.Flow != null)
            {
                await platform.Flow.StartAsync(ExecutionContext);
                Debug.WriteLine($"✅ Platform workflow '{platform.Name}' completed");
            }
            else
            {
                Debug.WriteLine($"⚠ Platform '{platform.Name}' has no workflow assigned");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ Error in platform workflow '{platform.Name}': {ex.Message}");
        }
    }

    public override void ResetAll()
    {
        base.ResetAll();
        Debug.WriteLine("🔄 All platform timers reset");
    }

    protected override uint GetInPort(Platform entity) => entity.InPort;

    protected override bool IsUsingTimerToIgnoreFeedbacks(Platform entity) => entity.IsUsingTimerToIgnoreFeedbacks;

    protected override double GetIntervalForTimerToIgnoreFeedbacks(Platform entity) => entity.IntervalForTimerToIgnoreFeedbacks;

    protected override string GetEntityName(Platform entity) => entity.Name;

    protected override void CleanupResources()
    {
        _processingLock.Dispose();
    }
}