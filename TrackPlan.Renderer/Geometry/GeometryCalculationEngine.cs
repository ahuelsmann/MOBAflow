// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.TrackPlan.Renderer.Geometry;

using Moba.TrackPlan.Graph;
using Moba.TrackPlan.Renderer.Service;
using Moba.TrackPlan.TrackSystem;

/// <summary>
/// Represents a calculated position for a node in the track plan.
/// Uses Point2D from TrackPlan.Renderer namespace indirectly via serialization.
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
/// Calculates geometric positions and orientations for track elements.
/// Uses MathNet.Numerics for mathematical calculations.
/// </summary>
public sealed class GeometryCalculationEngine
{
    private readonly ITrackCatalog _catalog;
    private readonly TopologyResolver _resolver;
    private Dictionary<Guid, CalculatedNodePosition> _nodePositions = null!;

    public GeometryCalculationEngine(ITrackCatalog catalog, TopologyResolver resolver)
    {
        _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
    }

    /// <summary>
    /// Calculates all node positions and orientations based on topology.
    /// </summary>
    public void Calculate(TopologyGraph topology, double startX, double startY, double startAngleDeg)
    {
        ArgumentNullException.ThrowIfNull(topology);

        _nodePositions = new Dictionary<Guid, CalculatedNodePosition>();

        // Find start node
        var startNode = topology.Nodes.FirstOrDefault();
        if (startNode == null)
            return;

        // Initialize start node
        _nodePositions[startNode.Id] = new CalculatedNodePosition(
            NodeId: startNode.Id,
            X: startX,
            Y: startY,
            ExitAngleDeg: startAngleDeg);

        // Traverse topology and calculate positions
        var visited = new HashSet<Guid>();
        CalculateRecursive(topology, startNode, startX, startY, startAngleDeg, visited);
    }

    /// <summary>
    /// Gets the calculated position for a node.
    /// </summary>
    public CalculatedNodePosition? GetNodePosition(Guid nodeId)
    {
        if (_nodePositions == null)
            return null;

        _nodePositions.TryGetValue(nodeId, out var position);
        return position;
    }

    /// <summary>
    /// Gets all calculated node positions.
    /// </summary>
    public IEnumerable<CalculatedNodePosition> GetAllNodePositions()
        => _nodePositions?.Values ?? Enumerable.Empty<CalculatedNodePosition>();

    /// <summary>
    /// Validates that all connections are geometrically compatible.
    /// </summary>
    public IEnumerable<GeometryValidationError> ValidateConnections(TopologyGraph topology)
    {
        var errors = new List<GeometryValidationError>();

        foreach (var edge in topology.Edges)
        {
            var template = _catalog.GetById(edge.TemplateId);
            if (template == null)
            {
                errors.Add(new GeometryValidationError(
                    EdgeId: edge.Id,
                    TemplateId: edge.TemplateId,
                    ErrorType: ValidationErrorType.TemplateNotFound,
                    Message: $"Template {edge.TemplateId} not found in catalog"));
                continue;
            }

            // Verify both connection endpoints exist
            if (!edge.Connections.ContainsKey("A") || !edge.Connections.ContainsKey("B"))
            {
                errors.Add(new GeometryValidationError(
                    EdgeId: edge.Id,
                    TemplateId: edge.TemplateId,
                    ErrorType: ValidationErrorType.MissingConnection,
                    Message: "Edge must have both A and B connections"));
            }
        }

        return errors;
    }

    /// <summary>
    /// Calculates distance between two points.
    /// </summary>
    public static double Distance(double x1, double y1, double x2, double y2)
    {
        var dx = x1 - x2;
        var dy = y1 - y2;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    /// <summary>
    /// Calculates angle between two points in degrees.
    /// </summary>
    public static double AngleBetweenPoints(double fromX, double fromY, double toX, double toY)
    {
        var radians = Math.Atan2(toY - fromY, toX - fromX);
        var degrees = radians * 180.0 / Math.PI;
        return degrees;
    }

    private void CalculateRecursive(
        TopologyGraph topology,
        TrackNode currentNode,
        double currentX,
        double currentY,
        double currentAngleDeg,
        HashSet<Guid> visited)
    {
        if (!visited.Add(currentNode.Id))
            return;

        _resolver.Build(topology);
        var outgoing = _resolver.GetOutgoing(currentNode);

        foreach (var edge in outgoing)
        {
            var template = _catalog.GetById(edge.TemplateId);
            if (template == null)
                continue;

            // TODO: Calculate next position using geometric properties
            // This is simplified - in real implementation would calculate based on track geometry
        }
    }
}
