// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Domain.TrackPlan;

/// <summary>
/// Represents a connection between two track segment endpoints.
/// This is an edge in the topology graph - no coordinates needed.
/// The renderer calculates positions based on these connections.
/// 
/// Endpoint indices:
/// - Simple tracks (2 endpoints): 0 = start, 1 = end
/// - Turnouts (3 endpoints): 0 = main, 1 = straight, 2 = branch
/// - Double slips (4 endpoints): 0-3 for each direction
/// </summary>
public class TrackConnection
{
    /// <summary>
    /// ID of the first connected segment.
    /// </summary>
    public string Segment1Id { get; set; } = string.Empty;

    /// <summary>
    /// Endpoint index on segment 1 (0 = start, 1 = end, 2+ for turnouts).
    /// </summary>
    public int Segment1EndpointIndex { get; set; }

    /// <summary>
    /// ID of the second connected segment.
    /// </summary>
    public string Segment2Id { get; set; } = string.Empty;

    /// <summary>
    /// Endpoint index on segment 2 (0 = start, 1 = end, 2+ for turnouts).
    /// </summary>
    public int Segment2EndpointIndex { get; set; }
}