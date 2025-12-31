// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Domain.TrackPlan;

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
}