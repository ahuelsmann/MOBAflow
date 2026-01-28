// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.TrackLibrary.Base.TrackSystem;

/// <summary>
/// Describes the routing behavior of a switch/turnout.
/// Tracks which output is active based on the switch position.
/// </summary>
public sealed class SwitchRoutingModel
{
    /// <summary>
    /// Input port ID (e.g., "A")
    /// </summary>
    public required string InEndId { get; init; }

    /// <summary>
    /// Output port ID for straight path (e.g., "B")
    /// </summary>
    public required string StraightEndId { get; init; }

    /// <summary>
    /// Output port ID for diverging/curved path (e.g., "C")
    /// </summary>
    public required string DivergingEndId { get; init; }

    /// <summary>
    /// Current switch position state
    /// </summary>
    public SwitchPositionState PositionState { get; set; } = SwitchPositionState.Straight;

    /// <summary>
    /// Gets the currently active output port ID based on position state
    /// </summary>
    public string GetActiveOutEnd()
        => PositionState == SwitchPositionState.Straight 
            ? StraightEndId 
            : DivergingEndId;

    /// <summary>
    /// Obsolete: Use GetActiveOutEnd() instead
    /// </summary>
    [System.Obsolete("Use GetActiveOutEnd() without parameter instead")]
    public string GetActiveOutEnd(bool straight)
        => straight ? StraightEndId : DivergingEndId;
}

/// <summary>
/// Represents the physical position state of a switch/turnout.
/// </summary>
public enum SwitchPositionState
{
    /// <summary>
    /// Switch is in straight position (routes to StraightEndId)
    /// </summary>
    Straight = 0,

    /// <summary>
    /// Switch is in diverging/curved position (routes to DivergingEndId)
    /// </summary>
    Diverging = 1
}
