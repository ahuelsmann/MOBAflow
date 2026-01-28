// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.TrackPlan.Graph;

/// <summary>
/// Represents the complete track topology (graph structure) with all track segments and their connections.
/// 
/// POCO (Plain Old CLR Object): Contains only data properties, no business logic.
/// All graph operations are delegated to TopologyGraphService or dedicated services.
/// </summary>
public sealed class TopologyGraph
{
    /// <summary>
    /// All nodes (junctions/connection points) in the topology.
    /// </summary>
    public List<TrackNode> Nodes { get; init; } = [];

    /// <summary>
    /// All edges (track segments) in the topology.
    /// </summary>
    public List<TrackEdge> Edges { get; init; } = [];
}
