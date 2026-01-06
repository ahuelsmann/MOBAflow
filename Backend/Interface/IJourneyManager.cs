// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Backend.Interface;

using Domain;
using Manager;
using Service;

/// <summary>
/// Interface for managing journey execution based on track feedback events.
/// Enables testing and mocking of journey execution logic.
/// </summary>
public interface IJourneyManager
{
    /// <summary>
    /// Event raised when a journey reaches a new station.
    /// ViewModels can subscribe to this event to update UI.
    /// </summary>
    event EventHandler<StationChangedEventArgs>? StationChanged;

    /// <summary>
    /// Event raised when a journey receives a feedback (counter incremented).
    /// Fired on every feedback, not just when a station is reached.
    /// </summary>
    event EventHandler<JourneyFeedbackEventArgs>? FeedbackReceived;

    /// <summary>
    /// Resets a specific journey to its initial state.
    /// </summary>
    /// <param name="journey">The journey to reset</param>
    void Reset(Journey journey);

    /// <summary>
    /// Gets the current session state for a specific journey.
    /// </summary>
    /// <param name="journeyId">The journey ID</param>
    /// <returns>The journey session state, or null if not found</returns>
    JourneySessionState? GetState(Guid journeyId);
}
