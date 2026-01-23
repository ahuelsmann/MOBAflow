// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.TrackPlan.Topology;

using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Resolves topology information from a TopologyGraph.
/// Provides graph traversal, connectivity analysis, and cycle detection.
/// 
/// Note: Uses a simple dictionary-based adjacency list instead of external graph library.
/// This keeps dependencies minimal and gives direct control over traversal logic.
/// </summary>
public sealed class TopologyResolver
{
    private readonly ITrackCatalog _catalog;
    private Dictionary<Guid, List<TrackEdge>> _outgoingEdges = null!;
    private Dictionary<Guid, List<TrackEdge>> _incomingEdges = null!;

    public TopologyResolver(ITrackCatalog catalog)
    {
        _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
    }

    /// <summary>
    /// Converts a TopologyGraph to an adjacency-list representation.
    /// </summary>
    public void Build(TopologyGraph topology)
    {
        ArgumentNullException.ThrowIfNull(topology);

        _outgoingEdges = new Dictionary<Guid, List<TrackEdge>>();
        _incomingEdges = new Dictionary<Guid, List<TrackEdge>>();

        // Initialize dictionaries for all nodes
        foreach (var node in topology.Nodes)
        {
            _outgoingEdges[node.Id] = new List<TrackEdge>();
            _incomingEdges[node.Id] = new List<TrackEdge>();
        }

        // Add all edges to adjacency lists
        foreach (var edge in topology.Edges)
        {
            var startNode = ResolveNodeFromEndpoint(topology, edge, "A");
            var endNode = ResolveNodeFromEndpoint(topology, edge, "B");

            if (startNode != null && endNode != null)
            {
                _outgoingEdges[startNode.Id].Add(edge);
                _incomingEdges[endNode.Id].Add(edge);
            }
        }
    }

    /// <summary>
    /// Gets all outgoing edges from a node.
    /// </summary>
    public IEnumerable<TrackEdge> GetOutgoing(TrackNode node)
    {
        if (!_outgoingEdges.TryGetValue(node.Id, out var edges))
            return Enumerable.Empty<TrackEdge>();

        return edges;
    }

    /// <summary>
    /// Gets all incoming edges to a node.
    /// </summary>
    public IEnumerable<TrackEdge> GetIncoming(TrackNode node)
    {
        if (!_incomingEdges.TryGetValue(node.Id, out var edges))
            return Enumerable.Empty<TrackEdge>();

        return edges;
    }

    /// <summary>
    /// Traverses the topology starting from a node following edges in order.
    /// </summary>
    public IEnumerable<TrackEdge> TraverseFromNode(TrackNode startNode, TopologyGraph topology)
    {
        var visited = new HashSet<Guid>();
        var stack = new Stack<TrackEdge>();

        var outgoing = GetOutgoing(startNode);
        foreach (var edge in outgoing)
            stack.Push(edge);

        while (stack.Count > 0)
        {
            var edge = stack.Pop();
            if (visited.Add(edge.Id))
            {
                yield return edge;

                var endNode = ResolveNodeFromEndpoint(topology, edge, "B");
                if (endNode != null)
                {
                    var nextOutgoing = GetOutgoing(endNode);
                    foreach (var nextEdge in nextOutgoing)
                        stack.Push(nextEdge);
                }
            }
        }
    }

    /// <summary>
    /// Checks if the topology contains any cycles.
    /// </summary>
    public bool HasCycles(TopologyGraph topology)
    {
        foreach (var edge in topology.Edges)
        {
            var endNode = ResolveNodeFromEndpoint(topology, edge, "B");
            var startNode = ResolveNodeFromEndpoint(topology, edge, "A");

            if (startNode != null && endNode != null)
            {
                if (CanReachNode(topology, endNode, startNode))
                    return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Gets all connected components (separate track groups).
    /// </summary>
    public IEnumerable<IEnumerable<TrackNode>> GetConnectedComponents(TopologyGraph topology)
    {
        var components = new List<List<TrackNode>>();
        var visited = new HashSet<Guid>();

        foreach (var node in topology.Nodes)
        {
            if (visited.Add(node.Id))
            {
                var component = new List<TrackNode>();
                var stack = new Stack<TrackNode>();
                stack.Push(node);

                while (stack.Count > 0)
                {
                    var current = stack.Pop();
                    component.Add(current);

                    var outgoing = GetOutgoing(current)
                        .Select(e => ResolveNodeFromEndpoint(topology, e, "B"))
                        .OfType<TrackNode>()
                        .Where(n => visited.Add(n.Id));

                    var incoming = GetIncoming(current)
                        .Select(e => ResolveNodeFromEndpoint(topology, e, "A"))
                        .OfType<TrackNode>()
                        .Where(n => visited.Add(n.Id));

                    foreach (var neighbor in outgoing.Concat(incoming))
                        stack.Push(neighbor);
                }

                components.Add(component);
            }
        }

        return components;
    }

    /// <summary>
    /// Gets all nodes reachable from a starting node.
    /// </summary>
    public IEnumerable<TrackNode> GetReachableNodes(TrackNode startNode, TopologyGraph topology)
    {
        var visited = new HashSet<Guid>();
        var stack = new Stack<TrackNode>();
        stack.Push(startNode);
        visited.Add(startNode.Id);

        while (stack.Count > 0)
        {
            var node = stack.Pop();
            yield return node;

            var outgoing = GetOutgoing(node)
                .Select(e => ResolveNodeFromEndpoint(topology, e, "B"))
                .OfType<TrackNode>()
                .Where(n => visited.Add(n.Id));

            foreach (var neighbor in outgoing)
                stack.Push(neighbor);
        }
    }

    /// <summary>
    /// Gets all edges that form cycles in the topology.
    /// </summary>
    public IEnumerable<TrackEdge> GetCycleEdges(TopologyGraph topology)
    {
        var cycleEdges = new List<TrackEdge>();

        foreach (var edge in topology.Edges)
        {
            var endNode = ResolveNodeFromEndpoint(topology, edge, "B");
            var startNode = ResolveNodeFromEndpoint(topology, edge, "A");

            if (startNode != null && endNode != null && CanReachNode(topology, endNode, startNode))
                cycleEdges.Add(edge);
        }

        return cycleEdges;
    }

    /// <summary>
    /// Analyzes the topology and returns information about structure.
    /// </summary>
    public TopologyAnalysis Analyze(TopologyGraph topology)
    {
        var nodeCount = topology.Nodes.Count;
        var edgeCount = topology.Edges.Count;
        var hasCycles = HasCycles(topology);
        var components = GetConnectedComponents(topology).ToList();
        var cycleEdges = GetCycleEdges(topology).ToList();

        return new TopologyAnalysis(
            NodeCount: nodeCount,
            EdgeCount: edgeCount,
            HasCycles: hasCycles,
            ComponentCount: components.Count,
            CycleCount: cycleEdges.Count);
    }

    private TrackNode? ResolveNodeFromEndpoint(TopologyGraph topology, TrackEdge edge, string portKey)
    {
        if (!edge.Connections.TryGetValue(portKey, out var endpoint))
            return null;

        return topology.Nodes.FirstOrDefault(n => n.Id == endpoint.NodeId);
    }

    private bool CanReachNode(TopologyGraph topology, TrackNode from, TrackNode target)
    {
        return GetReachableNodes(from, topology).Any(n => n.Id == target.Id);
    }
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
