// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.TrackPlan.Renderer.Service;

using Moba.TrackPlan.Graph;

/// <summary>
/// Interface for topology graph analysis and traversal.
/// Provides graph structure analysis, cycle detection, and connectivity queries.
/// </summary>
public interface ITopologyResolver
{
    /// <summary>
    /// Converts a TopologyGraph to an adjacency-list representation.
    /// </summary>
    void Build(TopologyGraph topology);

    /// <summary>
    /// Gets all outgoing edges from a node.
    /// </summary>
    IEnumerable<TrackEdge> GetOutgoing(TrackNode node);

    /// <summary>
    /// Gets all incoming edges to a node.
    /// </summary>
    IEnumerable<TrackEdge> GetIncoming(TrackNode node);

    /// <summary>
    /// Checks if topology has cycles.
    /// </summary>
    bool HasCycles(TopologyGraph topology);

    /// <summary>
    /// Analyzes the topology and returns structure information.
    /// </summary>
    TopologyAnalysis Analyze(TopologyGraph topology);

    /// <summary>
    /// Gets all nodes reachable from a starting node.
    /// </summary>
    IEnumerable<TrackNode> GetReachableNodes(TrackNode startNode, TopologyGraph topology);
}

/// <summary>
/// Topology analysis results.
/// </summary>
public sealed record TopologyAnalysis(
    int NodeCount,
    int EdgeCount,
    bool HasCycles,
    int ComponentCount,
    int CycleCount);
