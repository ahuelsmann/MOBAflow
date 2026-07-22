// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Common.Events;

/// <summary>
/// Identifies an authoritative journey runtime transition.
/// </summary>
public enum JourneyRuntimeTransitionKind
{
    /// <summary>A configured feedback occurrence was accepted.</summary>
    FeedbackAccepted,

    /// <summary>The current station changed.</summary>
    StopChanged,

    /// <summary>The journey run reached its terminal stop.</summary>
    Completed,

    /// <summary>The same journey restarted with a new run identity.</summary>
    Restarted,

    /// <summary>A linked journey became active.</summary>
    Activated,

    /// <summary>The journey became inactive after completion.</summary>
    Stopped,

    /// <summary>The operator reset the journey to its initial state.</summary>
    Reset
}

/// <summary>
/// Carries an immutable journey transition without retaining mutable runtime state.
/// </summary>
/// <param name="ProjectId">Owning project identifier.</param>
/// <param name="JourneyId">Journey identifier.</param>
/// <param name="JourneyRunId">Stable identity of the current journey run.</param>
/// <param name="Kind">Authoritative transition kind.</param>
/// <param name="FeedbackIndex">Zero-based feedback step being processed or expected next.</param>
/// <param name="CurrentOccurrence">Accepted occurrence count for the feedback step.</param>
/// <param name="RequiredOccurrences">Occurrences required to complete the feedback step.</param>
/// <param name="InPort">Optional one-based feedback input port.</param>
/// <param name="StationId">Optional current station identifier.</param>
/// <param name="StationIndex">Zero-based current station index, or -1 when unavailable.</param>
/// <param name="IsActive">Whether the journey remains active after the transition.</param>
public sealed record JourneyRuntimeTransitionEvent(
    Guid ProjectId,
    Guid JourneyId,
    Guid JourneyRunId,
    JourneyRuntimeTransitionKind Kind,
    int FeedbackIndex,
    uint CurrentOccurrence,
    uint RequiredOccurrences,
    int? InPort,
    Guid? StationId,
    int StationIndex,
    bool IsActive) : EventBase;