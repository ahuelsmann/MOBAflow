// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.TrackPlan.Graph;

/// <summary>
/// Fluent builder for connecting track edges.
/// Enables chained syntax like: topology.Add(edge1).Port("A").ConnectTo(edge2).Port("B").Then(edge3)
/// </summary>
public sealed class TrackPlanBuilder
{
    private TrackEdge _currentEdge;
    private readonly HashSet<TrackEdge> _createdEdges;

    internal TrackPlanBuilder(TrackEdge edge, HashSet<TrackEdge>? createdEdges = null)
    {
        _currentEdge = edge;
        _createdEdges = createdEdges ?? new();
        _createdEdges.Add(edge);
    }

    /// <summary>
    /// Get all edges created during this builder chain.
    /// </summary>
    public IReadOnlyCollection<TrackEdge> CreatedEdges => _createdEdges;

    /// <summary>
    /// Get a port on the current edge.
    /// Example: builder.Port("A")
    /// </summary>
    public PortConnectionBuilder Port(string portId)
    {
        ArgumentException.ThrowIfNullOrEmpty(portId);
        return new PortConnectionBuilder(_currentEdge, portId, _createdEdges);
    }

    /// <summary>
    /// Switch to a new edge for continued chaining.
    /// Example: builder.Then(otherEdge).Port("B")
    /// </summary>
    public TrackPlanBuilder Then(TrackEdge nextEdge)
    {
        ArgumentNullException.ThrowIfNull(nextEdge);
        _currentEdge = nextEdge;
        _createdEdges.Add(nextEdge);
        return this;
    }

    /// <summary>
    /// Create the TopologyGraph with all edges built in this chain.
    /// Example: var topology = builder.Port("A").ConnectTo(other).Port("B").Create();
    /// </summary>
    public TopologyGraph Create()
    {
        return new TopologyGraph
        {
            Edges = _createdEdges.ToList()
        };
    }
}

/// <summary>
/// Represents a port in the fluent connection chain.
/// Enables: Port("A").ConnectTo(otherEdge)
/// </summary>
public sealed class PortConnectionBuilder
{
    private readonly TrackEdge _edge;
    private readonly string _portId;
    private readonly HashSet<TrackEdge> _createdEdges;

    internal PortConnectionBuilder(TrackEdge edge, string portId, HashSet<TrackEdge> createdEdges)
    {
        _edge = edge;
        _portId = portId;
        _createdEdges = createdEdges;
    }

    /// <summary>
    /// Connect this port to a node.
    /// Returns builder to continue chaining to other edges.
    /// </summary>
    public TrackPlanBuilder ConnectTo(TrackNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        _edge.Connections[_portId] = (node.Id, null, null);
        return new TrackPlanBuilder(_edge, _createdEdges);
    }

    /// <summary>
    /// Connect this port to another edge's port.
    /// Returns a port builder so you can immediately specify the target port.
    /// Example: .Port("A").ConnectTo(otherEdge).Port("B")
    /// </summary>
    public TargetPortBuilder ConnectTo(TrackEdge targetEdge)
    {
        ArgumentNullException.ThrowIfNull(targetEdge);
        return new TargetPortBuilder(_edge, _portId, targetEdge, _createdEdges);
    }

    /// <summary>
    /// Connect this port to a track type factory (auto-creates new edge).
    /// Enables fluent chaining: .Port("A").ConnectTo(PikoA.R9).Port("B")
    /// </summary>
    public TargetPortBuilder ConnectTo(ITrackTypeFactory factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        var targetEdge = factory.CreateEdge();
        _createdEdges.Add(targetEdge);
        return new TargetPortBuilder(_edge, _portId, targetEdge, _createdEdges);
    }
}

/// <summary>
/// Represents the target edge in a port-to-port connection.
/// Requires calling Port() to complete the connection.
/// </summary>
public sealed class TargetPortBuilder
{
    private readonly TrackEdge _sourceEdge;
    private readonly string _sourcePortId;
    private readonly TrackEdge _targetEdge;
    private readonly HashSet<TrackEdge> _createdEdges;

    internal TargetPortBuilder(TrackEdge sourceEdge, string sourcePortId, TrackEdge targetEdge, HashSet<TrackEdge> createdEdges)
    {
        _sourceEdge = sourceEdge;
        _sourcePortId = sourcePortId;
        _targetEdge = targetEdge;
        _createdEdges = createdEdges;
    }

    /// <summary>
    /// Specify the target port and complete the connection.
    /// Returns builder to continue chaining.
    /// Example: .Port("A").ConnectTo(otherEdge).Port("B").Then(...)
    /// </summary>
    public TrackPlanBuilder Port(string targetPortId)
    {
        ArgumentException.ThrowIfNullOrEmpty(targetPortId);

        // Create a shared node for this connection
        var sharedNode = new TrackNode(Guid.NewGuid());

        // Both ports connect to the same node
        _sourceEdge.Connections[_sourcePortId] = (sharedNode.Id, _targetEdge.Id.ToString(), targetPortId);
        _targetEdge.Connections[targetPortId] = (sharedNode.Id, _sourceEdge.Id.ToString(), _sourcePortId);

        return new TrackPlanBuilder(_targetEdge, _createdEdges);
    }
}
