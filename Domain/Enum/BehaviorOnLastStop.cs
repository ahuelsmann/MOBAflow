// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Domain.Enum;

public enum BehaviorOnLastStop
{
    None, // Nothing happens when you reach the last stop or station.
    BeginAgainFromFistStop, // The journey simply starts again at the first stop or station.
    GotoJourney, // When reaching the last stop, a new journey is selected. (The implementation for this has not yet been fully developed.)
}