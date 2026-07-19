// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Backend.Service;

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

    /// <summary>Identifier of the current stop. This is stable when stop names or ordering change.</summary>
    public Guid? CurrentStationId { get; set; }

    /// <summary>Zero-based index of the next expected feedback sequence step.</summary>
    public int CurrentFeedbackIndex { get; set; }

    /// <summary>Number of matching activations already accepted by the current feedback step.</summary>
    public uint CurrentStepOccurrence { get; set; }

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

    /// <summary>Signals that a next-stop action reached the end of the stop list.</summary>
    public bool IsJourneyCompletionRequested { get; set; }

    /// <summary>
    /// Resets the session state to initial values.
    /// </summary>
    /// <param name="firstPos">The initial position to reset to (from Journey.FirstPos)</param>
    public void Reset(int firstPos = 0)
    {
        CurrentPos = firstPos;
        CurrentStationName = string.Empty;
        CurrentStationId = null;
        CurrentFeedbackIndex = 0;
        CurrentStepOccurrence = 0;
        LastFeedbackTime = null;
        IsActive = true;
        IsJourneyCompletionRequested = false;
    }
}
