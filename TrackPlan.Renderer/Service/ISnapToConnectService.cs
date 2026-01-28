// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.TrackPlan.Renderer.Service;

using Moba.TrackPlan.Geometry;
using Moba.TrackPlan.Graph;

/// <summary>
/// Interface for snap-to-connect service functionality.
/// Implementations provide snap detection and validation for connecting tracks.
/// </summary>
public interface ISnapToConnectService
{
    /// <summary>
    /// Represents a potential snap point for connection.
    /// </summary>
    public sealed record SnapCandidate(
        Guid TargetEdgeId,
        string TargetPortId,
        Point2D TargetPortLocation,
        double TargetPortAngleDeg,
        double DistanceMm,
        SnapValidationResult ValidationResult);

    /// <summary>
    /// Result of snap validation between two ports.
    /// </summary>
    public sealed record SnapValidationResult(
        bool IsValid,
        string Reason);

    /// <summary>
    /// Default snap detection radius in millimeters.
    /// </summary>
    public const double DefaultSnapRadiusMm = 5.0;

    /// <summary>
    /// Finds all snap candidates within the given radius for a dragging track.
    /// </summary>
    List<SnapCandidate> FindSnapCandidates(
        Guid draggedEdgeId,
        string draggedPortId,
        Point2D worldPortLocation,
        double snapRadiusMm = DefaultSnapRadiusMm);

    /// <summary>
    /// Gets the best snap candidate (highest priority = valid and closest).
    /// </summary>
    SnapCandidate? GetBestSnapCandidate(
        Guid draggedEdgeId,
        string draggedPortId,
        Point2D worldPortLocation,
        double snapRadiusMm = DefaultSnapRadiusMm);
}
