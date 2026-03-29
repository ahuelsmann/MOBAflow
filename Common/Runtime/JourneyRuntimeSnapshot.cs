// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Common.Runtime;

/// <summary>
/// Immutable runtime state for a single journey.
/// </summary>
public sealed class JourneyRuntimeSnapshot
{
    /// <summary>
    /// Gets the unique journey identifier.
    /// </summary>
    public required Guid JourneyId { get; init; }

    /// <summary>
    /// Gets the current station name.
    /// </summary>
    public string CurrentStationName { get; init; } = string.Empty;

    /// <summary>
    /// Gets the current counter value.
    /// </summary>
    public int Counter { get; init; }

    /// <summary>
    /// Gets the current position index.
    /// </summary>
    public int CurrentPos { get; init; }

    /// <summary>
    /// Gets the timestamp of the last feedback, if any.
    /// </summary>
    public DateTime? LastFeedbackTime { get; init; }

    /// <summary>
    /// Gets a value indicating whether the journey is active.
    /// </summary>
    public bool IsActive { get; init; }
}
