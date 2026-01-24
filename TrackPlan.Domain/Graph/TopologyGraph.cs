// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.TrackPlan.Graph;

using Moba.TrackPlan.Constraint;

/// <summary>
/// Represents the complete track topology (graph structure) with all track segments and their connections.
/// </summary>
public sealed class TopologyGraph
{
    private readonly List<TrackNode> _nodes = [];
    private readonly List<TrackEdge> _edges = [];

    /// <summary>
    /// All nodes (junctions/connection points) in the topology.
    /// </summary>
    public IReadOnlyList<TrackNode> Nodes => _nodes.AsReadOnly();

    /// <summary>
    /// All edges (track segments) in the topology.
    /// </summary>
    public IReadOnlyList<TrackEdge> Edges => _edges.AsReadOnly();

    /// <summary>
    /// Add a node to the topology.
    /// </summary>
    public void AddNode(TrackNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        if (!_nodes.Any(n => n.Id == node.Id))
        {
            _nodes.Add(node);
        }
    }

    /// <summary>
    /// Add an edge to the topology.
    /// </summary>
    public void AddEdge(TrackEdge edge)
    {
        ArgumentNullException.ThrowIfNull(edge);
        if (!_edges.Any(e => e.Id == edge.Id))
        {
            _edges.Add(edge);
        }
    }

    /// <summary>
    /// Get an edge by its ID.
    /// </summary>
    public TrackEdge? GetEdge(Guid edgeId) => _edges.FirstOrDefault(e => e.Id == edgeId);

    /// <summary>
    /// Get a node by its ID.
    /// </summary>
    public TrackNode? GetNode(Guid nodeId) => _nodes.FirstOrDefault(n => n.Id == nodeId);

    /// <summary>
    /// Remove a node by its ID.
    /// </summary>
    public bool RemoveNode(Guid nodeId) => _nodes.RemoveAll(n => n.Id == nodeId) > 0;

    /// <summary>
    /// Remove an edge by its ID.
    /// </summary>
    public bool RemoveEdge(Guid edgeId) => _edges.RemoveAll(e => e.Id == edgeId) > 0;

    /// <summary>
    /// Clear all nodes and edges.
    /// </summary>
    public void Clear()
    {
        _nodes.Clear();
        _edges.Clear();
    }

    /// <summary>
    /// Validates the topology using provided constraints. Yields constraint violations.
    /// </summary>
    public IEnumerable<ConstraintViolation> Validate(params ITopologyConstraint[] constraints)
    {
        foreach (var constraint in constraints)
        {
            foreach (var violation in constraint.Validate(this))
            {
                yield return violation;
            }
        }
    }
}
