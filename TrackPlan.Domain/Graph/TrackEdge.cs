// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.TrackPlan.Graph;

/// <summary>
/// Represents a track segment (straight, curve, or switch) in the topology.
/// </summary>
public sealed record TrackEdge(Guid Id, string TemplateId)
{
    /// <summary>
    /// Unique identifier for this edge.
    /// </summary>
    public Guid Id { get; } = Id;

    /// <summary>
    /// Reference to the track template this edge uses.
    /// </summary>
    public string TemplateId { get; } = TemplateId;

    /// <summary>
    /// Rotation angle in degrees (0-359).
    /// </summary>
    public double RotationDeg { get; set; } = 0;

    /// <summary>
    /// Start port identifier.
    /// </summary>
    public string StartPortId { get; set; } = string.Empty;

    /// <summary>
    /// End port identifier.
    /// </summary>
    public string EndPortId { get; set; } = string.Empty;

    /// <summary>
    /// Node connected to start port (if any).
    /// </summary>
    public Guid? StartNodeId { get; set; }

    /// <summary>
    /// Node connected to end port (if any).
    /// </summary>
    public Guid? EndNodeId { get; set; }

    /// <summary>
    /// Optional feedback point number assigned to this edge.
    /// </summary>
    public int? FeedbackPointNumber { get; set; }

    /// <summary>
    /// Port connections: key is port ID, value contains connection info (NodeId).
    /// </summary>
    public Dictionary<string, (Guid NodeId, string? ConnectedEdgeId, string? ConnectedPortId)> Connections { get; } = [];
}
