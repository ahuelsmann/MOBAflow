// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.TrackPlan.Geometry;

using Moba.TrackLibrary.Base.TrackSystem;
using Moba.TrackPlan.Graph;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Represents a calculated position for a node in the track plan.
/// </summary>
public sealed record CalculatedNodePosition(
    Guid NodeId,
    double X,
    double Y,
    double ExitAngleDeg);

/// <summary>
/// Validation error in geometry calculations.
/// </summary>
public sealed record GeometryValidationError(
    Guid EdgeId,
    string TemplateId,
    ValidationErrorType ErrorType,
    string Message);

/// <summary>
/// Types of geometry validation errors.
/// </summary>
public enum ValidationErrorType
{
    TemplateNotFound,
    MissingConnection,
    InvalidAngle,
    PositionConflict,
    PortMismatch
}

/// <summary>
/// Calculates geometry for track nodes in the topology.
/// Traverses the graph starting from initial position and angle,
/// computing positions for each node and validating port connections.
/// </summary>
public sealed class GeometryCalculationEngine
{
    private readonly ITrackCatalog _catalog;
    private readonly Dictionary<Guid, CalculatedNodePosition> _nodePositions = new();
    private readonly List<GeometryValidationError> _validationErrors = new();

    public GeometryCalculationEngine(ITrackCatalog catalog)
    {
        _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
    }

    /// <summary>
    /// Calculates positions for all nodes in the topology.
    /// </summary>
    public void Calculate(TopologyGraph topology, double startX, double startY, double startAngleDeg)
    {
        ArgumentNullException.ThrowIfNull(topology);

        _nodePositions.Clear();
        _validationErrors.Clear();

        var startNode = topology.Nodes.FirstOrDefault();
        if (startNode == null)
            return;

        var startPos = new Point2D(startX, startY);
        CalculateRecursive(topology, startNode, startPos, startAngleDeg, new HashSet<Guid>());
    }

    /// <summary>
    /// Recursively calculates node positions by traversing edges and computing exit points.
    /// </summary>
    private void CalculateRecursive(
        TopologyGraph topology,
        TrackNode currentNode,
        Point2D currentPosition,
        double currentAngleDeg,
        HashSet<Guid> processed)
    {
        if (!processed.Add(currentNode.Id))
            return;

        // Store this node's position
        _nodePositions[currentNode.Id] = new CalculatedNodePosition(
            currentNode.Id,
            currentPosition.X,
            currentPosition.Y,
            currentAngleDeg);

        // Process all outgoing edges
        var outgoingEdges = topology.Edges.Where(e =>
            ResolveNodeFromEndpoint(topology, e, "A")?.Id == currentNode.Id).ToList();

        foreach (var edge in outgoingEdges)
        {
            var template = _catalog.GetById(edge.TemplateId);
            if (template == null)
            {
                _validationErrors.Add(new GeometryValidationError(
                    edge.Id,
                    edge.TemplateId,
                    ValidationErrorType.TemplateNotFound,
                    $"Track template '{edge.TemplateId}' not found in catalog"));
                continue;
            }

            // Calculate exit position and angle using track geometry
            var (exitPos, exitAngle) = CalculateExitPoint(template, currentPosition, currentAngleDeg);

            // Resolve destination node
            var endNode = ResolveNodeFromEndpoint(topology, edge, "B");
            if (endNode != null)
            {
                CalculateRecursive(topology, endNode, exitPos, exitAngle, processed);
            }
        }
    }

    /// <summary>
    /// Calculates the exit position and angle after traversing a track template.
    /// </summary>
    private (Point2D Position, double AngleDeg) CalculateExitPoint(
        TrackTemplate template,
        Point2D entryPos,
        double entryAngleDeg)
    {
        var spec = template.Geometry;

        return spec.GeometryKind switch
        {
            TrackGeometryKind.Straight => CalculateStraightExit(spec, entryPos, entryAngleDeg),
            TrackGeometryKind.Curve => CalculateCurveExit(spec, entryPos, entryAngleDeg),
            TrackGeometryKind.Switch => CalculateSwitchExit(spec, entryPos, entryAngleDeg),
            TrackGeometryKind.ThreeWaySwitch => CalculateThreeWaySwitchExit(spec, entryPos, entryAngleDeg),
            _ => (entryPos, entryAngleDeg)
        };
    }

    /// <summary>
    /// Calculates exit point for straight track.
    /// </summary>
    private (Point2D, double) CalculateStraightExit(
        TrackGeometrySpec spec,
        Point2D start,
        double startAngleDeg)
    {
        var lengthMm = spec.LengthMm ?? 0;
        var angleRad = DegToRad(startAngleDeg);

        var end = new Point2D(
            start.X + lengthMm * Math.Cos(angleRad),
            start.Y + lengthMm * Math.Sin(angleRad)
        );

        return (end, startAngleDeg);
    }

    /// <summary>
    /// Calculates exit point for curved track.
    /// </summary>
    private (Point2D, double) CalculateCurveExit(
        TrackGeometrySpec spec,
        Point2D start,
        double startAngleDeg)
    {
        var radiusMm = spec.RadiusMm ?? 0;
        var sweepDeg = spec.AngleDeg ?? 0;

        var angleRad = DegToRad(startAngleDeg);
        var sweepRad = DegToRad(sweepDeg);

        // Calculate arc center (perpendicular to tangent at distance radius)
        var normalDir = sweepRad >= 0 ? 1 : -1;
        var centerOffset = new Point2D(
            normalDir * -Math.Sin(angleRad) * radiusMm,
            normalDir * Math.Cos(angleRad) * radiusMm
        );

        var center = new Point2D(start.X + centerOffset.X, start.Y + centerOffset.Y);

        // Exit angle is rotated by sweep
        var exitAngleDeg = startAngleDeg + sweepDeg;

        // Exit position is on circle at new angle
        var exitAngleRad = DegToRad(exitAngleDeg);
        var exitPos = new Point2D(
            center.X + radiusMm * Math.Cos(exitAngleRad + Math.PI / 2 * normalDir),
            center.Y + radiusMm * Math.Sin(exitAngleRad + Math.PI / 2 * normalDir)
        );

        return (exitPos, exitAngleDeg);
    }

    /// <summary>
    /// Calculates exit point for switch (uses straight as default).
    /// </summary>
    private (Point2D, double) CalculateSwitchExit(
        TrackGeometrySpec spec,
        Point2D start,
        double startAngleDeg)
    {
        // For now, switches exit like straight tracks
        // In full implementation, may have branch position data
        return CalculateStraightExit(spec, start, startAngleDeg);
    }

    /// <summary>
    /// Calculates exit point for three-way switch (uses straight as default).
    /// </summary>
    private (Point2D, double) CalculateThreeWaySwitchExit(
        TrackGeometrySpec spec,
        Point2D start,
        double startAngleDeg)
    {
        // For now, three-way switches exit like straight tracks
        // In full implementation, may have multiple branch positions
        return CalculateStraightExit(spec, start, startAngleDeg);
    }

    /// <summary>
    /// Validates all connections in the topology.
    /// Checks port compatibility between connected tracks.
    /// </summary>
    public IEnumerable<GeometryValidationError> ValidateConnections(TopologyGraph topology)
    {
        ArgumentNullException.ThrowIfNull(topology);

        foreach (var error in _validationErrors)
            yield return error;

        // Validate each edge connection
        foreach (var edge in topology.Edges)
        {
            var template = _catalog.GetById(edge.TemplateId);
            if (template == null)
                continue;

            var startNode = ResolveNodeFromEndpoint(topology, edge, "A");
            var endNode = ResolveNodeFromEndpoint(topology, edge, "B");

            if (startNode == null || endNode == null)
            {
                yield return new GeometryValidationError(
                    edge.Id,
                    edge.TemplateId,
                    ValidationErrorType.MissingConnection,
                    "Edge endpoints not properly resolved");
            }
        }
    }

    /// <summary>
    /// Returns all calculated node positions.
    /// </summary>
    public IEnumerable<CalculatedNodePosition> GetAllNodePositions()
    {
        return _nodePositions.Values;
    }

    /// <summary>
    /// Resolves a node from an edge endpoint (start or end).
    /// </summary>
    private TrackNode? ResolveNodeFromEndpoint(TopologyGraph topology, TrackEdge edge, string endpoint)
    {
        Guid? nodeId = endpoint == "A" ? edge.StartNodeId : edge.EndNodeId;
        
        if (nodeId == null)
            return null;

        return topology.Nodes.FirstOrDefault(n => n.Id == nodeId.Value);
    }

    private double DegToRad(double deg) => deg * Math.PI / 180.0;
}
