// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.TrackPlan.Domain;

using Geometry;

using System.Text.Json.Serialization;

/// <summary>
/// Represents a single track segment in the track plan.
/// Track system agnostic - supports Piko, Roco, Tillig, etc.
/// Topology-First architecture: No coordinates stored, only ArticleCode + Connections.
/// Coordinates are computed at runtime from TrackGeometryLibrary + graph traversal.
/// </summary>
public class TrackSegment
{
    /// <summary>
    /// Unique identifier for this segment.
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Track article code (e.g., "G231", "R2", "WL").
    /// Used to look up geometry from the track library (Piko, Roco, Tillig).
    /// </summary>
    public string ArticleCode { get; set; } = string.Empty;

    /// <summary>
    /// Optional: Assigned feedback sensor port (InPort 1-2048).
    /// </summary>
    public uint? AssignedInPort { get; set; }

    /// <summary>
    /// Optional: User-defined name for this segment.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Optional: Layer/group this segment belongs to.
    /// </summary>
    public string? Layer { get; set; }

    /// <summary>
    /// Optional: Switch state for parametric switch control.
    /// Only switches (WL, WR, BWL, BWR, W3, DKW) have a non-null value.
    /// Determines which connection constraint is active.
    /// </summary>
    public SwitchState? SwitchState { get; set; }

    /// <summary>
    /// Runtime-only: Track segment occupation state.
    /// Set by FeedbackStateManager when AssignedInPort feedback is triggered.
    /// Used for route monitoring and collision detection.
    /// NOT serialized - pure runtime state.
    /// </summary>
    [JsonIgnore]
    public bool IsOccupied { get; set; }

    /// <summary>
    /// Runtime-only world transformation matrix.
    /// Calculated by TopologyRenderer from connections + geometry.
    /// NOT serialized to JSON (pure topology-first).
    /// </summary>
    [JsonIgnore]
    public Transform2D WorldTransform { get; set; } = Transform2D.Identity;
}
