// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Domain;

/// <summary>
/// Represents a complete track plan with topology, positions, rotations, and metadata.
/// Serializable to JSON for persistence in solution files.
/// </summary>
public sealed record TrackPlanData
{
    /// <summary>
    /// Unique identifier for this track plan.
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// User-friendly name for the track plan.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Description of the track plan.
    /// </summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// ID of the track catalog used (e.g., "PikoA", "Roco", "Peco").
    /// </summary>
    public required string CatalogId { get; init; }

    /// <summary>
    /// All track nodes in the topology (junction points).
    /// </summary>
    public required List<TrackPlanNode> Nodes { get; init; } = [];

    /// <summary>
    /// All track edges in the topology (segments).
    /// </summary>
    public required List<TrackPlanEdge> Edges { get; init; } = [];

    /// <summary>
    /// Track positions: EdgeId → (X, Y) in millimeters.
    /// </summary>
    public Dictionary<string, (double X, double Y)> Positions { get; init; } = new();

    /// <summary>
    /// Track rotations: EdgeId → rotation in degrees (0-359).
    /// </summary>
    public Dictionary<string, double> Rotations { get; init; } = new();

    /// <summary>
    /// Sections (logical groups of tracks for train control).
    /// </summary>
    public List<TrackPlanSection> Sections { get; init; } = [];

    /// <summary>
    /// Isolators (electrical breaks at specific ports).
    /// </summary>
    public List<TrackPlanIsolator> Isolators { get; init; } = [];

    /// <summary>
    /// Endcaps (track terminators).
    /// </summary>
    public List<TrackPlanEndcap> Endcaps { get; init; } = [];

    /// <summary>
    /// Timestamp of last modification.
    /// </summary>
    public DateTime LastModified { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Represents a node (junction/connection point) in the track plan topology.
/// </summary>
public sealed record TrackPlanNode
{
    /// <summary>
    /// Unique identifier for this node.
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// Port IDs connected to this node.
    /// </summary>
    public required List<string> Ports { get; init; } = [];
}

/// <summary>
/// Represents an edge (track segment) in the track plan topology.
/// </summary>
public sealed record TrackPlanEdge
{
    /// <summary>
    /// Unique identifier for this edge.
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// ID of the template this edge uses (e.g., "G231", "R9", "WL").
    /// </summary>
    public required string TemplateId { get; init; }

    /// <summary>
    /// Rotation angle in degrees (0-359).
    /// </summary>
    public double RotationDeg { get; init; } = 0;

    /// <summary>
    /// Start port identifier.
    /// </summary>
    public string StartPortId { get; init; } = string.Empty;

    /// <summary>
    /// End port identifier.
    /// </summary>
    public string EndPortId { get; init; } = string.Empty;

    /// <summary>
    /// Node connected to start port (if any).
    /// </summary>
    public Guid? StartNodeId { get; init; }

    /// <summary>
    /// Node connected to end port (if any).
    /// </summary>
    public Guid? EndNodeId { get; init; }

    /// <summary>
    /// Optional feedback point number assigned to this edge.
    /// </summary>
    public int? FeedbackPointNumber { get; init; }

    /// <summary>
    /// Port connections: key is port ID, value contains connection info.
    /// </summary>
    public Dictionary<string, TrackPlanConnection> Connections { get; init; } = new();
}

/// <summary>
/// Represents a connection at a specific port.
/// </summary>
public sealed record TrackPlanConnection
{
    /// <summary>
    /// Node ID at this port.
    /// </summary>
    public required Guid NodeId { get; init; }

    /// <summary>
    /// Edge ID on the other side of the connection (if any).
    /// </summary>
    public Guid? ConnectedEdgeId { get; init; }

    /// <summary>
    /// Port ID on the other side of the connection (if any).
    /// </summary>
    public string? ConnectedPortId { get; init; }
}

/// <summary>
/// Represents a section (logical group of tracks for train control).
/// </summary>
public sealed record TrackPlanSection
{
    /// <summary>
    /// Unique identifier for this section.
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// User-friendly name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Color code for visualization.
    /// </summary>
    public string Color { get; init; } = string.Empty;

    /// <summary>
    /// Track IDs in this section.
    /// </summary>
    public required List<Guid> TrackIds { get; init; } = [];
}

/// <summary>
/// Represents an isolator (electrical break) at a specific port.
/// </summary>
public sealed record TrackPlanIsolator
{
    /// <summary>
    /// Unique identifier.
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// Edge ID where isolator is placed.
    /// </summary>
    public required Guid EdgeId { get; init; }

    /// <summary>
    /// Port ID (e.g., "A", "B", "C").
    /// </summary>
    public required string PortId { get; init; }
}

/// <summary>
/// Represents an endcap (track terminator).
/// </summary>
public sealed record TrackPlanEndcap
{
    /// <summary>
    /// Unique identifier.
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// Edge ID where endcap is placed.
    /// </summary>
    public required Guid EdgeId { get; init; }

    /// <summary>
    /// Port ID (e.g., "A", "B", "C").
    /// </summary>
    public required string PortId { get; init; }
}
