// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.TrackPlan.Editor.Service;

using Moba.TrackPlan.Graph;

/// <summary>
/// Service for managing connections between track edges in a topology.
/// Provides methods to connect, disconnect, and query edge relationships.
/// </summary>
public class TrackConnectionService
{
    private readonly TopologyGraph _graph;

    public TrackConnectionService(TopologyGraph graph)
    {
        _graph = graph;
    }

    /// <summary>
    /// Connects two ports from different edges by merging their nodes.
    /// After connection, both ports share the same node.
    /// </summary>
    /// <param name="edge1Id">First edge ID</param>
    /// <param name="port1Id">Port ID on first edge (e.g., "A", "B")</param>
    /// <param name="edge2Id">Second edge ID</param>
    /// <param name="port2Id">Port ID on second edge</param>
    /// <returns>True if connection was successful</returns>
    public bool TryConnect(Guid edge1Id, string port1Id, Guid edge2Id, string port2Id)
    {
        var edge1 = _graph.Edges.FirstOrDefault(e => e.Id == edge1Id);
        var edge2 = _graph.Edges.FirstOrDefault(e => e.Id == edge2Id);

        if (edge1 is null || edge2 is null)
            return false;

        if (!edge1.Connections.TryGetValue(port1Id, out var endpoint1))
            return false;

        if (!edge2.Connections.TryGetValue(port2Id, out var endpoint2))
            return false;

        // Don't connect if already connected to the same node
        if (endpoint1.NodeId == endpoint2.NodeId)
            return true; // Already connected

        var node1 = _graph.Nodes.FirstOrDefault(n => n.Id == endpoint1.NodeId);
        var node2 = _graph.Nodes.FirstOrDefault(n => n.Id == endpoint2.NodeId);

        if (node1 is null || node2 is null)
            return false;

        // Merge: Add port2 to node1
        if (!node1.Ports.Contains(port2Id))
        {
            node1.Ports.Add(port2Id);
        }

        // Update edge2's connection to point to node1
        edge2.Connections[port2Id] = (node1.Id, null, null);

        // Remove orphaned node2 (only if it has no other connections)
        var hasOtherConnections = _graph.Edges
            .Where(e => e.Id != edge2Id)
            .Any(e => e.Connections.Values.Any(ep => ep.NodeId == node2.Id));

        if (!hasOtherConnections)
        {
            // Remove node from graph using internal method if available
            // For now, we'll track this as a limitation - nodes are removed when cleared
        }

        return true;
    }

    /// <summary>
    /// Disconnects a port from its current node, creating a new isolated node.
    /// </summary>
    /// <param name="edgeId">Edge ID</param>
    /// <param name="portId">Port ID to disconnect</param>
    /// <returns>True if disconnection was successful</returns>
    public bool Disconnect(Guid edgeId, string portId)
    {
        var edge = _graph.Edges.FirstOrDefault(e => e.Id == edgeId);
        if (edge is null)
            return false;

        if (!edge.Connections.TryGetValue(portId, out var endpoint))
            return false;

        var currentNode = _graph.Nodes.FirstOrDefault(n => n.Id == endpoint.NodeId);
        if (currentNode is null)
            return false;

        // Check if this port is actually shared (connected to another edge)
        var otherEdgesUsingNode = _graph.Edges
            .Where(e => e.Id != edgeId)
            .Where(e => e.Connections.Values.Any(ep => ep.NodeId == currentNode.Id))
            .ToList();

        if (otherEdgesUsingNode.Count == 0)
        {
            // Not connected to anything else - nothing to disconnect
            return true;
        }

        // Create a new isolated node for this port
        var newNode = new TrackNode(Guid.NewGuid());
        newNode.Ports.Add(portId);
        _graph.Nodes.Add(newNode);

        // Update edge's connection to point to new node
        edge.Connections[portId] = (newNode.Id, null, null);

        // Remove port from old node if it was there
        if (currentNode.Ports.Contains(portId))
        {
            currentNode.Ports.Remove(portId);
        }

        return true;
    }

    /// <summary>
    /// Finds the edge and port connected to a given port on an edge.
    /// Returns (otherEdgeId, otherPortId) or null if not connected.
    /// </summary>
    public (Guid EdgeId, string PortId)? GetConnectedPort(Guid edgeId, string portId)
    {
        var edge = _graph.Edges.FirstOrDefault(e => e.Id == edgeId);
        if (edge is null)
            return null;

        if (!edge.Connections.TryGetValue(portId, out var endpoint))
            return null;

        // Find another edge that shares this node
        foreach (var otherEdge in _graph.Edges.Where(e => e.Id != edgeId))
        {
            foreach (var kvp in otherEdge.Connections)
            {
                if (kvp.Value.NodeId == endpoint.NodeId)
                {
                    return (otherEdge.Id, kvp.Key);
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Checks if a port is connected to another port.
    /// </summary>
    public bool IsConnected(Guid edgeId, string portId)
    {
        return GetConnectedPort(edgeId, portId) != null;
    }

    /// <summary>
    /// Gets all ports connected to a given node.
    /// </summary>
    public IEnumerable<(Guid EdgeId, string PortId)> GetPortsForNode(Guid nodeId)
    {
        foreach (var edge in _graph.Edges)
        {
            foreach (var kvp in edge.Connections)
            {
                if (kvp.Value.NodeId == nodeId)
                {
                    yield return (edge.Id, kvp.Key);
                }
            }
        }
    }

    /// <summary>
    /// Gets all edges connected to a given edge (including the edge itself).
    /// </summary>
    public IEnumerable<Guid> GetConnectedGroup(Guid edgeId)
    {
        var visited = new HashSet<Guid>();
        var stack = new Stack<Guid>();
        stack.Push(edgeId);
        visited.Add(edgeId);

        while (stack.Count > 0)
        {
            var currentEdgeId = stack.Pop();
            yield return currentEdgeId;

            var currentEdge = _graph.Edges.FirstOrDefault(e => e.Id == currentEdgeId);
            if (currentEdge is null)
                continue;

            // Get all nodes this edge connects to
            var nodeIds = currentEdge.Connections.Values.Select(ep => ep.NodeId).Distinct();

            // Find all other edges that share these nodes
            foreach (var nodeId in nodeIds)
            {
                foreach (var otherEdge in _graph.Edges.Where(e => e.Id != currentEdgeId))
                {
                    if (otherEdge.Connections.Values.Any(ep => ep.NodeId == nodeId) && visited.Add(otherEdge.Id))
                    {
                        stack.Push(otherEdge.Id);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Finds the shortest path between two edges using BFS.
    /// </summary>
    public IEnumerable<Guid> FindShortestPath(Guid fromEdgeId, Guid toEdgeId)
    {
        if (fromEdgeId == toEdgeId)
        {
            yield return fromEdgeId;
            yield break;
        }

        var queue = new Queue<(Guid EdgeId, List<Guid> Path)>();
        var visited = new HashSet<Guid> { fromEdgeId };

        queue.Enqueue((fromEdgeId, new List<Guid> { fromEdgeId }));

        while (queue.Count > 0)
        {
            var (currentEdgeId, path) = queue.Dequeue();

            var currentEdge = _graph.Edges.FirstOrDefault(e => e.Id == currentEdgeId);
            if (currentEdge is null)
                continue;

            var nodeIds = currentEdge.Connections.Values.Select(ep => ep.NodeId).Distinct();

            foreach (var nodeId in nodeIds)
            {
                foreach (var otherEdge in _graph.Edges.Where(e => e.Id != currentEdgeId))
                {
                    if (!otherEdge.Connections.Values.Any(ep => ep.NodeId == nodeId))
                        continue;

                    if (otherEdge.Id == toEdgeId)
                    {
                        // Found the target
                        foreach (var edgeId in path)
                            yield return edgeId;
                        yield return toEdgeId;
                        yield break;
                    }

                    if (visited.Add(otherEdge.Id))
                    {
                        var newPath = new List<Guid>(path) { otherEdge.Id };
                        queue.Enqueue((otherEdge.Id, newPath));
                    }
                }
            }
        }
    }
}
