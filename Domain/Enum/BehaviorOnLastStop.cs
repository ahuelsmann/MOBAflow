// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Domain.Enum;

/// <summary>
/// Defines how a journey proceeds after reaching its final station.
/// </summary>
public enum BehaviorOnLastStop
{
    /// <summary>No follow-up action is taken at the final station.</summary>
    None,

    /// <summary>Restart the journey from the first station.</summary>
    BeginAgainFromFistStop,

    /// <summary>Switch to another journey identified by <see cref="Moba.Domain.Journey.NextJourneyId"/>.</summary>
    GotoJourney,
}