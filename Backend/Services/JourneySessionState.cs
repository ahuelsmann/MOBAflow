// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Backend.Services;

/// <summary>
/// Runtime state for Journey execution (not persisted to disk).
/// Managed by JourneyManager during journey execution.
/// This class contains session-specific data that is separate from user project data (Domain.Journey).
/// </summary>
public class JourneySessionState
{
    /// <summary>
    /// Unique identifier of the journey this state belongs to.
    /// </summary>
    public Guid JourneyId { get; set; }

    /// <summary>
    /// Name of the current station in the journey.
    /// Empty string indicates no station has been reached yet.
    /// </summary>
    public string CurrentStationName { get; set; } = string.Empty;

    /// <summary>
    /// Counter for tracking journey progress (e.g., lap number, station visits).
    /// Incremented by JourneyManager when feedback is received.
    /// </summary>
    public int Counter { get; set; }

    /// <summary>
    /// Current position (index) in the journey's station list.
    /// Managed by JourneyManager during journey execution.
    /// </summary>
    public int CurrentPos { get; set; }

    /// <summary>
    /// Timestamp of the last feedback received for this journey.
    /// Used for debugging and timeout detection.
    /// </summary>
    public DateTime? LastFeedbackTime { get; set; }

    /// <summary>
    /// Indicates whether this journey is actively running.
    /// Set to true when journey starts, false when stopped or completed.
    /// </summary>
    public bool IsActive { get; set; }
}