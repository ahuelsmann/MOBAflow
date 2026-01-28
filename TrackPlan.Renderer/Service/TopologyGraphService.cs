// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.TrackPlan.Renderer.Service;

using Moba.TrackPlan.Graph;
using System;
using System.Linq;

/// <summary>
/// Service for managing TopologyGraph operations.
/// Provides methods for adding/removing nodes and edges, querying graph structure.
/// 
/// This service was extracted from TopologyGraph to keep the domain model as pure POCO.
/// </summary>
public sealed class TopologyGraphService
{
    private readonly TopologyGraph _graph;

    public TopologyGraphService(TopologyGraph graph)
    {
        _graph = graph ?? throw new ArgumentNullException(nameof(graph));
    }

    /// <summary>
    /// Add a node to the topology.
    /// </summary>
    public void AddNode(TrackNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        if (!_graph.Nodes.Any(n => n.Id == node.Id))
        {
            _graph.Nodes.Add(node);
        }
    }

    /// <summary>
    /// Add an edge to the topology.
    /// </summary>
    public void AddEdge(TrackEdge edge)
    {
        ArgumentNullException.ThrowIfNull(edge);
        if (!_graph.Edges.Any(e => e.Id == edge.Id))
        {
            _graph.Edges.Add(edge);
        }
    }

    /// <summary>
    /// Get an edge by its ID.
    /// </summary>
    public TrackEdge? GetEdge(Guid edgeId) => _graph.Edges.FirstOrDefault(e => e.Id == edgeId);

    /// <summary>
    /// Get a node by its ID.
    /// </summary>
    public TrackNode? GetNode(Guid nodeId) => _graph.Nodes.FirstOrDefault(n => n.Id == nodeId);

    /// <summary>
    /// Remove a node by its ID.
    /// </summary>
    public bool RemoveNode(Guid nodeId) => _graph.Nodes.RemoveAll(n => n.Id == nodeId) > 0;

    /// <summary>
    /// Remove an edge by its ID.
    /// </summary>
    public bool RemoveEdge(Guid edgeId) => _graph.Edges.RemoveAll(e => e.Id == edgeId) > 0;

    /// <summary>
    /// Clear all nodes and edges.
    /// </summary>
    public void Clear()
    {
        _graph.Nodes.Clear();
        _graph.Edges.Clear();
    }
}
