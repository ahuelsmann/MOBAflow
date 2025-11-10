using System.Collections.Concurrent;

namespace MOBAsmart.Services;

/// <summary>
/// Manages feedback statistics per InPort (track feedback point).
/// Thread-safe implementation for counting train passes per track.
/// </summary>
public class FeedbackStatisticsManager
{
    private readonly ConcurrentDictionary<uint, FeedbackStatistic> _statistics = new();

    /// <summary>
    /// Records a feedback event for a specific InPort and returns the updated statistic.
    /// </summary>
    /// <param name="inPort">The InPort number (0-255)</param>
    /// <returns>Updated statistic for the InPort</returns>
    public FeedbackStatistic RecordFeedback(uint inPort)
    {
        var now = DateTime.Now;

        var statistic = _statistics.AddOrUpdate(
            inPort,
            // Add new statistic
            _ => new FeedbackStatistic
            {
                InPort = inPort,
                TotalCount = 1,
                LastTrigger = now,
                EntityName = $"Track {inPort}",
                EntityType = "InPort"
            },
            // Update existing statistic
            (_, existing) => new FeedbackStatistic
            {
                InPort = inPort,
                TotalCount = existing.TotalCount + 1,
                LastTrigger = now,
                EntityName = existing.EntityName ?? $"Track {inPort}",
                EntityType = existing.EntityType ?? "InPort"
            }
        );

        System.Diagnostics.Debug.WriteLine($"📊 InPort {inPort}: Count={statistic.TotalCount}, Last={statistic.LastTrigger:HH:mm:ss}");

        return statistic;
    }

    /// <summary>
    /// Gets the statistic for a specific InPort, or null if not recorded yet.
    /// </summary>
    public FeedbackStatistic? GetStatistic(uint inPort)
    {
        _statistics.TryGetValue(inPort, out var statistic);
        return statistic;
    }

    /// <summary>
    /// Gets all statistics as a sorted list (by InPort).
    /// </summary>
    public List<FeedbackStatistic> GetAllStatistics()
    {
        return _statistics.Values
            .OrderBy(s => s.InPort)
            .ToList();
    }

    /// <summary>
    /// Resets all statistics counters.
    /// </summary>
    public void Reset()
    {
        _statistics.Clear();
        System.Diagnostics.Debug.WriteLine("🔄 All feedback statistics reset");
    }

    /// <summary>
    /// Gets the number of tracked InPorts.
    /// </summary>
    public int Count => _statistics.Count;
}
