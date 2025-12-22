// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Domain.TrackPlan;

/// <summary>
/// Represents a connection between two track segment endpoints.
/// When two segments are snapped together, a connection is created.
/// Connected segments move together as a unit.
/// 
/// Supports multiple connections per segment:
/// - Simple tracks (2 endpoints): index 0 (start), 1 (end)
/// - Turnouts (3-4 endpoints): index 0 (main), 1 (branch), 2 (branch2), 3 (branch3)
/// </summary>
public class TrackConnection
{
    /// <summary>
    /// Unique identifier for this connection.
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// ID of the first connected segment.
    /// </summary>
    public string Segment1Id { get; set; } = string.Empty;

    /// <summary>
    /// Index of the endpoint on segment 1 (0 = start/main, 1+ = other endpoints for turnouts).
    /// </summary>
    public int Segment1EndpointIndex { get; set; }

    /// <summary>
    /// ID of the second connected segment.
    /// </summary>
    public string Segment2Id { get; set; } = string.Empty;

    /// <summary>
    /// Index of the endpoint on segment 2 (0 = start/main, 1+ = other endpoints for turnouts).
    /// </summary>
    public int Segment2EndpointIndex { get; set; } = 1;

    /// <summary>
    /// X coordinate of the connection point.
    /// </summary>
    public double ConnectionX { get; set; }

    /// <summary>
    /// Y coordinate of the connection point.
    /// </summary>
    public double ConnectionY { get; set; }
}