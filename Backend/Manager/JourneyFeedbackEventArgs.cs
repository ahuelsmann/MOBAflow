// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Backend.Manager;

using Service;

/// <summary>
/// Event arguments for the FeedbackReceived event in JourneyManager.
/// Fired on every feedback (counter increment), not just when a station is reached.
/// </summary>
public class JourneyFeedbackEventArgs : EventArgs
{
    /// <summary>
    /// Unique identifier of the journey that received feedback.
    /// </summary>
    public required Guid JourneyId { get; init; }

    /// <summary>
    /// The runtime state of the journey at the time of the feedback.
    /// ViewModels can read this to update UI with current counter value.
    /// </summary>
    public required JourneySessionState SessionState { get; init; }
}
