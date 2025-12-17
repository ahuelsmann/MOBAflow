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
    /// Curved track piece (R1, R2, R3, R4, R9).
    /// </summary>
    Curve,

    /// <summary>
    /// Standard turnout/switch (WL = left, WR = right).
    /// </summary>
    Switch,

    /// <summary>
    /// Left turnout (WL).
    /// </summary>
    TurnoutLeft,

    /// <summary>
    /// Right turnout (WR).
    /// </summary>
    TurnoutRight,

    /// <summary>
    /// Curved left turnout (BWL-R2, BWL-R3).
    /// </summary>
    CurvedTurnoutLeft,

    /// <summary>
    /// Curved right turnout (BWR-R2, BWR-R3).
    /// </summary>
    CurvedTurnoutRight,

    /// <summary>
    /// Three-way switch (W3).
    /// </summary>
    ThreeWaySwitch,

    /// <summary>
    /// Double crossover switch (DKW).
    /// </summary>
    DoubleCrossover,

    /// <summary>
    /// Simple crossing without switch function (K15, K30).
    /// </summary>
    Crossing
}

/// <summary>
/// Alias for TrackSegmentType (used in PikoATrackLibrary for consistency).
/// </summary>
public enum TrackType
{
    Straight = TrackSegmentType.Straight,
    Curve = TrackSegmentType.Curve,
    TurnoutLeft = TrackSegmentType.TurnoutLeft,
    TurnoutRight = TrackSegmentType.TurnoutRight,
    CurvedTurnoutLeft = TrackSegmentType.CurvedTurnoutLeft,
    CurvedTurnoutRight = TrackSegmentType.CurvedTurnoutRight,
    DoubleCrossover = TrackSegmentType.DoubleCrossover,
    ThreeWay = TrackSegmentType.ThreeWaySwitch,
    Crossing = TrackSegmentType.Crossing
}
