// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.TrackPlan.Editor.Service;

using Moba.TrackLibrary.Base.TrackSystem;
using Moba.TrackPlan.Geometry;
using Moba.TrackPlan.Graph;

using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Service for detecting and managing snap-to-connect operations between tracks.
/// Provides proximity-based snap detection, validation, and multi-port snap candidate discovery.
/// </summary>
public sealed class SnapToConnectService
{
    private readonly TrackConnectionService _connectionService;
    private readonly TopologyGraph _graph;
    private readonly ITrackCatalog _catalog;

    /// <summary>
    /// Default snap detection radius in millimeters.
    /// </summary>
    public const double DefaultSnapRadiusMm = 5.0;

    public SnapToConnectService(
        TrackConnectionService connectionService,
        TopologyGraph graph,
        ITrackCatalog catalog)
    {
        _connectionService = connectionService ?? throw new ArgumentNullException(nameof(connectionService));
        _graph = graph ?? throw new ArgumentNullException(nameof(graph));
        _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
    }

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
    /// Finds all snap candidates within the given radius for a dragging track.
    /// </summary>
    /// <param name="draggedEdgeId">The edge being dragged</param>
    /// <param name="draggedPortId">The port on the dragged edge to snap (e.g., "A" or "B")</param>
    /// <param name="worldPortLocation">World space location of the dragged port</param>
    /// <param name="snapRadiusMm">Detection radius in millimeters (default: 5mm)</param>
    /// <returns>List of snap candidates sorted by distance (nearest first)</returns>
    public List<SnapCandidate> FindSnapCandidates(
        Guid draggedEdgeId,
        string draggedPortId,
        Point2D worldPortLocation,
        double snapRadiusMm = DefaultSnapRadiusMm)
    {
        ArgumentNullException.ThrowIfNull(worldPortLocation);

        var candidates = new List<SnapCandidate>();

        // Get dragged edge template
        var draggedEdge = _graph.Edges.FirstOrDefault(e => e.Id == draggedEdgeId);
        if (draggedEdge is null)
            return candidates;

        var draggedTemplate = _catalog.GetById(draggedEdge.TemplateId);
        if (draggedTemplate is null)
            return candidates;

        // Iterate through all edges except the dragged one
        foreach (var targetEdge in _graph.Edges.Where(e => e.Id != draggedEdgeId))
        {
            // Get target edge's track template
            var targetTemplate = _catalog.GetById(targetEdge.TemplateId);
            if (targetTemplate is null)
                continue;

            // Check each port on the target edge
            foreach (var targetEnd in targetTemplate.Ends)
            {
                // Skip port IDs that don't match expected ends (A, B, C, D)
                if (!IsValidPortId(targetEnd.Id))
                    continue;

                // Skip if target port is already connected
                if (IsPortConnected(targetEdge.Id, targetEnd.Id))
                    continue;

                // For now, calculate estimated position (simplified - assumes port at template location)
                // In production, would calculate actual rotated/translated position
                var targetPortWorldPos = EstimatePortWorldLocation(
                    targetEdge.Id,
                    targetEnd.Id,
                    targetEdge.RotationDeg);

                if (targetPortWorldPos is null)
                    continue;

                // Calculate distance
                var distance = CalculateDistance(worldPortLocation, targetPortWorldPos.Value);

                // Check if within snap radius
                if (distance > snapRadiusMm)
                    continue;

                // Validate geometric compatibility
                var validation = ValidateSnapConnection(
                    draggedEdgeId,
                    draggedPortId,
                    targetEdge.Id,
                    targetEnd.Id,
                    draggedTemplate,
                    targetTemplate);

                candidates.Add(new SnapCandidate(
                    TargetEdgeId: targetEdge.Id,
                    TargetPortId: targetEnd.Id,
                    TargetPortLocation: targetPortWorldPos.Value,
                    TargetPortAngleDeg: targetEnd.AngleDeg,
                    DistanceMm: distance,
                    ValidationResult: validation));
            }
        }

        // Sort by distance (nearest first), then by validation status (valid first)
        return candidates
            .OrderBy(c => c.ValidationResult.IsValid ? 0 : 1)
            .ThenBy(c => c.DistanceMm)
            .ToList();
    }

    /// <summary>
    /// Validates geometric and topological compatibility between two ports.
    /// </summary>
    private SnapValidationResult ValidateSnapConnection(
        Guid sourceEdgeId,
        string sourcePortId,
        Guid targetEdgeId,
        string targetPortId,
        TrackTemplate sourceTemplate,
        TrackTemplate targetTemplate)
    {
        // Get ends
        var sourceEnd = sourceTemplate.Ends.FirstOrDefault(e => e.Id == sourcePortId);
        var targetEnd = targetTemplate.Ends.FirstOrDefault(e => e.Id == targetPortId);

        if (sourceEnd is null || targetEnd is null)
            return new SnapValidationResult(false, "End not found on template");

        // Get edges
        var sourceEdge = _graph.Edges.FirstOrDefault(e => e.Id == sourceEdgeId);
        var targetEdge = _graph.Edges.FirstOrDefault(e => e.Id == targetEdgeId);

        if (sourceEdge is null || targetEdge is null)
            return new SnapValidationResult(false, "Edge not found");

        // Check if ports are already connected
        if (IsPortConnected(sourceEdgeId, sourcePortId))
            return new SnapValidationResult(false, "Source port already connected");

        if (IsPortConnected(targetEdgeId, targetPortId))
            return new SnapValidationResult(false, "Target port already connected");

        // NOTE: Angle validation removed - all port combinations (A-A, A-B, B-A, B-B) 
        // are geometrically valid. User controls track rotation to achieve physical fit.
        // Only position proximity (snap radius) and port availability matter.

        // Check for topology cycles (optional)
        if (WouldCreateInvalidCycle(sourceEdgeId, targetEdgeId))
            return new SnapValidationResult(false, "Connection would create invalid cycle");

        return new SnapValidationResult(true, "Valid");
    }

    /// <summary>
    /// Checks if connecting these edges would create an invalid cycle.
    /// </summary>
    private bool WouldCreateInvalidCycle(Guid sourceEdgeId, Guid targetEdgeId)
    {
        var visited = new HashSet<Guid>();
        return HasPathBetween(targetEdgeId, sourceEdgeId, visited);
    }

    /// <summary>
    /// Depth-first search to detect if there's a path between two edges in the graph.
    /// </summary>
    private bool HasPathBetween(Guid fromEdgeId, Guid toEdgeId, HashSet<Guid> visited)
    {
        if (fromEdgeId == toEdgeId)
            return true;

        if (visited.Contains(fromEdgeId))
            return false;

        visited.Add(fromEdgeId);

        var fromEdge = _graph.Edges.FirstOrDefault(e => e.Id == fromEdgeId);
        if (fromEdge is null)
            return false;

        // Check connected nodes via start/end node IDs
        var connectedNodeIds = new List<Guid>();
        if (fromEdge.StartNodeId.HasValue)
            connectedNodeIds.Add(fromEdge.StartNodeId.Value);
        if (fromEdge.EndNodeId.HasValue)
            connectedNodeIds.Add(fromEdge.EndNodeId.Value);

        // Find other edges connected to these nodes
        foreach (var nodeId in connectedNodeIds)
        {
            foreach (var otherEdge in _graph.Edges.Where(e => e.Id != fromEdgeId))
            {
                if (otherEdge.StartNodeId == nodeId || otherEdge.EndNodeId == nodeId)
                {
                    if (HasPathBetween(otherEdge.Id, toEdgeId, visited))
                        return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Estimates port location in world space (simplified version).
    /// </summary>
    private Point2D? EstimatePortWorldLocation(Guid edgeId, string portId, double edgeRotationDeg)
    {
        var edge = _graph.Edges.FirstOrDefault(e => e.Id == edgeId);
        if (edge is null)
            return null;

        // Simplified: assume port is at edge position
        // In production, would apply rotation and offset based on port definition
        return new Point2D(0, 0);
    }

    /// <summary>
    /// Gets the best snap candidate (highest priority = valid and closest).
    /// </summary>
    public SnapCandidate? GetBestSnapCandidate(
        Guid draggedEdgeId,
        string draggedPortId,
        Point2D worldPortLocation,
        double snapRadiusMm = DefaultSnapRadiusMm)
    {
        var candidates = FindSnapCandidates(draggedEdgeId, draggedPortId, worldPortLocation, snapRadiusMm);
        return candidates.FirstOrDefault();
    }

    /// <summary>
    /// Calculates Euclidean distance between two points in millimeters.
    /// </summary>
    private double CalculateDistance(Point2D p1, Point2D p2)
    {
        var dx = p1.X - p2.X;
        var dy = p1.Y - p2.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    /// <summary>
    /// Checks if a port is already connected.
    /// </summary>
    private bool IsPortConnected(Guid edgeId, string portId)
    {
        var edge = _graph.Edges.FirstOrDefault(e => e.Id == edgeId);
        if (edge is null)
            return false;

        // Check if port corresponds to start or end
        if (portId == edge.StartPortId)
            return edge.StartNodeId.HasValue;

        if (portId == edge.EndPortId)
            return edge.EndNodeId.HasValue;

        return false;
    }

    /// <summary>
    /// Validates that a port ID is valid (A, B, C, D, etc.).
    /// </summary>
    private bool IsValidPortId(string portId)
    {
        return portId.Length == 1 && char.IsUpper(portId[0]);
    }
}
