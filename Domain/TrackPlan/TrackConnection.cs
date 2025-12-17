// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Domain.TrackPlan;

/// <summary>
/// Represents a connection between two track segment endpoints.
/// When two segments are snapped together, a connection is created.
/// Connected segments move together as a unit.
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
    /// Which endpoint of segment 1 is connected (true = start, false = end).
    /// </summary>
    public bool Segment1IsStart { get; set; }

    /// <summary>
    /// ID of the second connected segment.
    /// </summary>
    public string Segment2Id { get; set; } = string.Empty;

    /// <summary>
    /// Which endpoint of segment 2 is connected (true = start, false = end).
    /// </summary>
    public bool Segment2IsStart { get; set; }

    /// <summary>
    /// X coordinate of the connection point.
    /// </summary>
    public double ConnectionX { get; set; }

    /// <summary>
    /// Y coordinate of the connection point.
    /// </summary>
    public double ConnectionY { get; set; }
}
