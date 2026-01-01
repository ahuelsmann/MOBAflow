// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.TrackPlan.Domain;

/// <summary>
/// Switch state for parametric switch control.
/// Switches are topology nodes with active/inactive constraints, NOT coordinate changes.
/// </summary>
public enum SwitchState
{
    /// <summary>
    /// Switch is set to straight (main track).
    /// </summary>
    Straight,

    /// <summary>
    /// Switch is set to left branch.
    /// </summary>
    BranchLeft,

    /// <summary>
    /// Switch is set to right branch.
    /// </summary>
    BranchRight
}
