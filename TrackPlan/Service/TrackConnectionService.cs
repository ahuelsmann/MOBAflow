// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.TrackPlan.Service;

using Moba.TrackPlan.Graph;

/// <summary>
/// Service for managing track connections in a TopologyGraph.
/// Handles connecting and disconnecting track ports by merging/splitting nodes.
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
        if (!node1.Ports.Any(p => p.Id == port2Id))
        {
            node1.Ports.Add(new TrackPort { Id = port2Id });
        }

        // Update edge2's connection to point to node1
        edge2.Connections[port2Id] = new Endpoint(node1.Id, port2Id);

        // Remove orphaned node2 (only if it has no other connections)
        var hasOtherConnections = _graph.Edges
            .Where(e => e.Id != edge2Id)
            .Any(e => e.Connections.Values.Any(ep => ep.NodeId == node2.Id));

        if (!hasOtherConnections)
        {
            _graph.Nodes.Remove(node2);
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
        var newNode = new TrackNode
        {
            Id = Guid.NewGuid(),
            Ports = [new TrackPort { Id = portId }]
        };
        _graph.Nodes.Add(newNode);

        // Update edge's connection to point to new node
        edge.Connections[portId] = new Endpoint(newNode.Id, portId);

        // Remove port from old node if it was there
        var portToRemove = currentNode.Ports.FirstOrDefault(p => p.Id == portId);
        if (portToRemove is not null)
        {
            currentNode.Ports.Remove(portToRemove);
        }

        return true;
    }

    /// <summary>
    /// Checks if a port is connected to another edge.
    /// </summary>
    public bool IsPortConnected(Guid edgeId, string portId)
    {
        var edge = _graph.Edges.FirstOrDefault(e => e.Id == edgeId);
        if (edge is null)
            return false;

        if (!edge.Connections.TryGetValue(portId, out var endpoint))
            return false;

        // Count how many edges use this node
        var edgesUsingNode = _graph.Edges
            .Count(e => e.Connections.Values.Any(ep => ep.NodeId == endpoint.NodeId));

        // If more than one edge uses this node, the port is connected
        return edgesUsingNode > 1;
    }

    /// <summary>
    /// Gets all open (unconnected) ports in the graph.
    /// </summary>
    public IEnumerable<(Guid EdgeId, string PortId)> GetOpenPorts()
    {
        foreach (var edge in _graph.Edges)
        {
            foreach (var kvp in edge.Connections)
            {
                if (!IsPortConnected(edge.Id, kvp.Key))
                {
                    yield return (edge.Id, kvp.Key);
                }
            }
        }
    }

    /// <summary>
    /// Gets the connected edge and port for a given port, if any.
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
            /// Gets all edges that are transitively connected to the given edge.
            /// Returns a set of edge IDs including the starting edge.
            /// </summary>
            public HashSet<Guid> GetConnectedGroup(Guid startEdgeId)
            {
                var result = new HashSet<Guid>();
                var queue = new Queue<Guid>();

                queue.Enqueue(startEdgeId);

                while (queue.Count > 0)
                {
                    var currentId = queue.Dequeue();
                    if (!result.Add(currentId))
                        continue;

                    var edge = _graph.Edges.FirstOrDefault(e => e.Id == currentId);
                    if (edge is null)
                        continue;

                    foreach (var portId in edge.Connections.Keys)
                    {
                        var connected = GetConnectedPort(currentId, portId);
                        if (connected.HasValue && !result.Contains(connected.Value.EdgeId))
                        {
                            queue.Enqueue(connected.Value.EdgeId);
                        }
                    }
                }

                return result;
            }

            /// <summary>
            /// Finds the shortest path between two edges using BFS.
            /// Returns list of edge IDs from start to end (inclusive).
            /// </summary>
            public List<Guid> FindShortestPath(Guid fromEdgeId, Guid toEdgeId)
            {
                if (fromEdgeId == toEdgeId)
                    return [fromEdgeId];

                var visited = new HashSet<Guid>();
                var parent = new Dictionary<Guid, Guid>();
                var queue = new Queue<Guid>();

                queue.Enqueue(fromEdgeId);
                visited.Add(fromEdgeId);

                while (queue.Count > 0)
                {
                    var currentId = queue.Dequeue();

                    if (currentId == toEdgeId)
                    {
                        // Reconstruct path
                        var path = new List<Guid>();
                        var current = toEdgeId;
                        while (current != fromEdgeId)
                        {
                            path.Add(current);
                            current = parent[current];
                        }
                        path.Add(fromEdgeId);
                        path.Reverse();
                        return path;
                    }

                    var edge = _graph.Edges.FirstOrDefault(e => e.Id == currentId);
                    if (edge is null)
                        continue;

                    foreach (var portId in edge.Connections.Keys)
                    {
                        var connected = GetConnectedPort(currentId, portId);
                        if (connected.HasValue && !visited.Contains(connected.Value.EdgeId))
                        {
                            visited.Add(connected.Value.EdgeId);
                            parent[connected.Value.EdgeId] = currentId;
                            queue.Enqueue(connected.Value.EdgeId);
                        }
                    }
                }

                return []; // No path found
            }
        }

