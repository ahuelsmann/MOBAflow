// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Domain.TrackPlan;

/// <summary>
/// Types of track segments based on Piko A-Gleis system.
/// </summary>
public enum TrackSegmentType
{
    /// <summary>
    /// Straight track piece (G231, G239, G119, G62, etc.).
    /// </summary>
    Straight,

    /// <summary>
    /// Curved track piece (R1, R2, R3, etc.).
    /// </summary>
    Curve,

    /// <summary>
    /// Standard turnout/switch (WL = left, WR = right).
    /// </summary>
    Switch,

    /// <summary>
    /// Three-way switch (W3).
    /// </summary>
    ThreeWaySwitch,

    /// <summary>
    /// Double crossover switch (DKW).
    /// </summary>
    DoubleCrossover,

    /// <summary>
    /// Simple crossing without switch function.
    /// </summary>
    Crossing
}
