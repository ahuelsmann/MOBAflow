using System.Collections.Concurrent;
using System.Diagnostics;

namespace Moba.Backend.Monitor;

/// <summary>
/// Central monitoring service that tracks all feedback events and maintains statistics.
/// Used to provide real-time feedback data to external clients (e.g., mobile apps).
/// </summary>
public class FeedbackMonitor
{
    private readonly ConcurrentDictionary<uint, FeedbackStatistics> _statistics = new();
    private readonly object _lock = new();

    /// <summary>
    /// Event raised when a feedback is received and statistics are updated.
    /// </summary>
    public event EventHandler<FeedbackStatistics>? FeedbackReceived;

    /// <summary>
    /// Records a feedback event for a specific InPort.
    /// </summary>
    /// <param name="inPort">The InPort number that triggered the feedback</param>
    /// <param name="entityName">Optional name of the entity (Journey, Workflow, Platform)</param>
    /// <param name="entityType">Optional type of the entity</param>
    public void RecordFeedback(uint inPort, string? entityName = null, string? entityType = null)
    {
        var stats = _statistics.AddOrUpdate(
            inPort,
            // Add new
            key => new FeedbackStatistics
            {
                InPort = inPort,
                TotalCount = 1,
                LastTrigger = DateTime.UtcNow,
                EntityName = entityName,
                EntityType = entityType
            },
            // Update existing
            (key, existing) =>
            {
                existing.TotalCount++;
                existing.LastTrigger = DateTime.UtcNow;
                if (!string.IsNullOrEmpty(entityName))
                    existing.EntityName = entityName;
                if (!string.IsNullOrEmpty(entityType))
                    existing.EntityType = entityType;
                return existing;
            }
        );

        Debug.WriteLine($"📊 FeedbackMonitor: InPort {inPort} - Count: {stats.TotalCount}, Entity: {entityName ?? "Unknown"}");

        // Notify subscribers
        FeedbackReceived?.Invoke(this, stats);
    }

    /// <summary>
    /// Gets all current feedback statistics.
    /// </summary>
    /// <returns>List of all tracked feedback statistics</returns>
    public List<FeedbackStatistics> GetAllStatistics()
    {
        return _statistics.Values.OrderBy(s => s.InPort).ToList();
    }

    /// <summary>
    /// Gets statistics for a specific InPort.
    /// </summary>
    /// <param name="inPort">The InPort to query</param>
    /// <returns>Statistics for the InPort, or null if not found</returns>
    public FeedbackStatistics? GetStatistics(uint inPort)
    {
        _statistics.TryGetValue(inPort, out var stats);
        return stats;
    }

    /// <summary>
    /// Resets all statistics.
    /// </summary>
    public void ResetAll()
    {
        lock (_lock)
        {
            _statistics.Clear();
            Debug.WriteLine("🔄 FeedbackMonitor: All statistics reset");
        }
    }

    /// <summary>
    /// Resets statistics for a specific InPort.
    /// </summary>
    /// <param name="inPort">The InPort to reset</param>
    public void Reset(uint inPort)
    {
        if (_statistics.TryRemove(inPort, out var stats))
        {
            Debug.WriteLine($"🔄 FeedbackMonitor: Statistics for InPort {inPort} reset");
        }
    }
}

/// <summary>
/// Statistics data for a single feedback point (InPort).
/// </summary>
public class FeedbackStatistics
{
    /// <summary>
    /// The InPort number.
    /// </summary>
    public uint InPort { get; set; }

    /// <summary>
    /// Total number of feedbacks received for this InPort.
    /// </summary>
    public long TotalCount { get; set; }

    /// <summary>
    /// Timestamp of the last feedback event.
    /// </summary>
    public DateTime LastTrigger { get; set; }

    /// <summary>
    /// Name of the associated entity (Journey, Workflow, Platform).
    /// </summary>
    public string? EntityName { get; set; }

    /// <summary>
    /// Type of the associated entity.
    /// </summary>
    public string? EntityType { get; set; }
}
