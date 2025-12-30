// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

/// <summary>
/// Represents a potential snap target when dragging a track segment.
/// </summary>
public class SnapCandidate
{
    /// <summary>
    /// The segment to snap to.
    /// </summary>
    public required TrackSegmentViewModel TargetSegment { get; init; }

    /// <summary>
    /// The endpoint index on the target segment.
    /// </summary>
    public required int TargetEndpointIndex { get; init; }

    /// <summary>
    /// The endpoint index on the dragged segment that will connect.
    /// </summary>
    public required int DraggedEndpointIndex { get; init; }

    /// <summary>
    /// Distance between the endpoints (for ranking candidates).
    /// </summary>
    public double Distance { get; init; }

    /// <summary>
    /// X coordinate of the snap point.
    /// </summary>
    public double SnapX { get; init; }

    /// <summary>
    /// Y coordinate of the snap point.
    /// </summary>
    public double SnapY { get; init; }
}
