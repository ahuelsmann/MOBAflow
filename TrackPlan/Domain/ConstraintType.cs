// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.TrackPlan.Domain;

/// <summary>
/// Defines the type of geometric constraint between two connected segments.
/// Track-Graph Architecture: Constraints define how world transforms are calculated.
/// </summary>
public enum ConstraintType
{
    /// <summary>
    /// Rigid constraint: Position AND heading must align exactly (±180° for opposite direction).
    /// Used for: Standard tracks, straight and curved segments, switch main lines.
    /// Calculation: Parent endpoint position + heading → Child positioned to align connectors.
    /// </summary>
    Rigid,

    /// <summary>
    /// Rotational constraint: Position fixed, heading can rotate freely.
    /// Used for: Turntables, rotating bridges.
    /// Calculation: Parent endpoint position → Child centered at position, heading independent.
    /// </summary>
    Rotational,

    /// <summary>
    /// Parametric constraint: Position and heading depend on a parameter (e.g., switch angle).
    /// Used for: Switch branches, three-way switches, curved turnouts.
    /// Calculation: Parent endpoint + parameter (branch angle) → Child positioned with offset.
    /// </summary>
    Parametric
}
