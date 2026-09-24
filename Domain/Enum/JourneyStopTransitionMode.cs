// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Domain.Enum;

/// <summary>Defines how a workflow action changes the current journey stop.</summary>
public enum JourneyStopTransitionMode
{
    None,
    Next,
    SpecificStation
}
