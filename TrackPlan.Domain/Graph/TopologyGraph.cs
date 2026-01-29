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

    /// <summary>
    /// Start building a fluent connection chain from the specified edge.
    /// Example: topology.Add(edge1).Port("A").ConnectTo(edge2).Port("B").Then(edge3).Port("C")
    /// </summary>
    public TrackPlanBuilder Add(TrackEdge edge)
    {
        ArgumentNullException.ThrowIfNull(edge);
        return new TrackPlanBuilder(edge);
    }

    /// <summary>
    /// Start building a fluent connection chain from a track type factory (auto-creates edge).
    /// Example: var builder = topology.Add(PikoA.R9).Port("A").ConnectTo(PikoA.WR).Port("B");
    ///          topology.Edges.AddRange(builder.CreatedEdges);
    /// </summary>
    public TrackPlanBuilder Add(ITrackTypeFactory factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        var edge = factory.CreateEdge();
        return new TrackPlanBuilder(edge);
    }
}
