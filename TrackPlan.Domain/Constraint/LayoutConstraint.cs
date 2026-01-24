// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.TrackPlan.Constraint;

/// <summary>
/// Base class for layout constraints in track planning.
/// </summary>
public abstract record LayoutConstraint
{
    /// <summary>
    /// Unique identifier for this constraint.
    /// </summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    /// Description of what this constraint enforces.
    /// </summary>
    public string? Description { get; init; }
}
