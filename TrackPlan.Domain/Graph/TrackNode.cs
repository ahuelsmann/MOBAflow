// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.TrackPlan.Graph;

/// <summary>
/// Represents a junction or connection point in the track topology.
/// </summary>
public sealed record TrackNode(Guid Id)
{
    /// <summary>
    /// Unique identifier for this node.
    /// </summary>
    public Guid Id { get; } = Id;

    /// <summary>
    /// Port labels connected to this node.
    /// </summary>
    public List<string> Ports { get; } = [];
}
