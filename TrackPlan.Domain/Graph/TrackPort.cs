// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.TrackPlan.Graph;

/// <summary>
/// Represents a single port on a track edge.
/// Provides fluent API for connecting ports to nodes or other ports.
/// </summary>
public sealed class TrackPort
{
    private readonly TrackEdge _edge;
    private readonly string _portId;

    internal TrackPort(TrackEdge edge, string portId)
    {
        _edge = edge;
        _portId = portId;
    }

    /// <summary>
    /// Connect this port to a node (creates a connection).
    /// </summary>
    public void ConnectTo(TrackNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        _edge.Connections[_portId] = (node.Id, null, null);
    }

    /// <summary>
    /// Connect this port to another port (creates a bidirectional connection).
    /// </summary>
    public void ConnectTo(TrackPort otherPort)
    {
        ArgumentNullException.ThrowIfNull(otherPort);
        
        // Create a shared node for this connection
        var sharedNode = new TrackNode(Guid.NewGuid());
        
        // Both ports connect to the same node
        _edge.Connections[_portId] = (sharedNode.Id, otherPort._edge.Id.ToString(), otherPort._portId);
        otherPort._edge.Connections[otherPort._portId] = (sharedNode.Id, _edge.Id.ToString(), _portId);
    }
}
