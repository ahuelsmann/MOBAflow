// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Domain.Enum;

/// <summary>
/// Defines how a journey proceeds after reaching its final station.
/// </summary>
public enum BehaviorOnLastStop
{
    /// <summary>The journey remains at its final station.</summary>
    None,

    /// <summary>Continue from the first station.</summary>
    BeginAgainFromFistStop,
}
