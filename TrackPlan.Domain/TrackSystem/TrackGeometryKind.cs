// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.TrackPlan.TrackSystem;

/// <summary>
/// Kinds of track geometry: straight segments, curves, and switches.
/// </summary>
public enum TrackGeometryKind
{
    /// <summary>
    /// Straight track segment.
    /// </summary>
    Straight,

    /// <summary>
    /// Curved track segment with fixed radius.
    /// </summary>
    Curve,

    /// <summary>
    /// Three-way switch (Y-switch).
    /// </summary>
    ThreeWaySwitch,

    /// <summary>
    /// Two-way switch (left/right). Alias: Switch.
    /// </summary>
    TwoWaySwitch,

    /// <summary>
    /// Alias for TwoWaySwitch (for compatibility).
    /// </summary>
    Switch = TwoWaySwitch,

    /// <summary>
    /// Crossing (no switch).
    /// </summary>
    Crossing
}
