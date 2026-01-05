// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Backend.Manager;

using Domain;
using Service;

/// <summary>
/// Event arguments for the StationChanged event in JourneyManager.
/// Contains the journey ID, station, and runtime state when a journey reaches a new station.
/// </summary>
public class StationChangedEventArgs : EventArgs
{
    /// <summary>
    /// Unique identifier of the journey that changed.
    /// </summary>
    public required Guid JourneyId { get; init; }

    /// <summary>
    /// The station that was reached.
    /// </summary>
    public required Station Station { get; init; }

    /// <summary>
    /// The runtime state of the journey at the time of the station change.
    /// ViewModels can read this to update UI without querying JourneyManager.
    /// </summary>
    public required JourneySessionState SessionState { get; init; }
}
