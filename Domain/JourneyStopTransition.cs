// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Domain;

using Enum;

/// <summary>Describes how a workflow action changes the current journey stop.</summary>
public sealed class JourneyStopTransition
{
    public JourneyStopTransitionMode Mode { get; set; }

    public Guid? StationId { get; set; }
}
