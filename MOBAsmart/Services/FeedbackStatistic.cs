namespace MOBAsmart.Services;

/// <summary>
/// Feedback statistics data model for UI binding.
/// Represents the count and timing information for a specific InPort (track feedback point).
/// </summary>
public class FeedbackStatistic
{
    /// <summary>
    /// The InPort number (0-255) - represents a specific track feedback point.
    /// </summary>
    public uint InPort { get; set; }

    /// <summary>
    /// Total number of times this InPort has been triggered.
    /// Represents the number of train passes on this track.
    /// </summary>
    public long TotalCount { get; set; }

    /// <summary>
    /// Timestamp of the last feedback trigger.
    /// </summary>
    public DateTime LastTrigger { get; set; }

    /// <summary>
    /// Optional: Name of the associated entity (e.g., "Main Station Track 1").
    /// </summary>
    public string? EntityName { get; set; }

    /// <summary>
    /// Optional: Type of the entity (e.g., "Station", "Track", "InPort").
    /// </summary>
    public string? EntityType { get; set; }

    /// <summary>
    /// Display text for UI binding.
    /// </summary>
    public string DisplayText => $"InPort {InPort}: {TotalCount} passes";

    /// <summary>
    /// Formatted last trigger time for UI display.
    /// </summary>
    public string LastTriggerFormatted => LastTrigger.ToString("HH:mm:ss");
}
