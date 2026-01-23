// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.TrackPlan.Renderer.Service;

using Moba.TrackPlan.Renderer.Geometry;
using Moba.TrackPlan.Renderer.World;

using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Orchestrates the topology-first rendering pipeline:
/// Topology → Geometry Calculation → Rendering
/// 
/// This is the main entry point for converting a TopologyGraph into a rendered TrackPlan.
/// </summary>
public sealed class TrackPlanLayoutEngine
{
    private readonly ITrackCatalog _catalog;
    private readonly TopologyResolver _resolver;
    private readonly GeometryCalculationEngine _geometryEngine;
    private readonly SkiaSharpCanvasRenderer _canvasRenderer;

    public TrackPlanLayoutEngine(ITrackCatalog catalog)
    {
        _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        _resolver = new TopologyResolver(_catalog);
        _geometryEngine = new GeometryCalculationEngine(_catalog, _resolver);
        _canvasRenderer = new SkiaSharpCanvasRenderer();
    }

    /// <summary>
    /// Processes a topology and generates a complete layout with geometry and rendered output.
    /// </summary>
    public TrackPlanLayout Process(
        TopologyGraph topology,
        Point2D startPosition = default,
        double startAngleDeg = 0)
    {
        ArgumentNullException.ThrowIfNull(topology);

        if (startPosition == default)
            startPosition = new Point2D(0, 0);

        // Step 1: Analyze topology
        _resolver.Build(topology);
        var analysis = _resolver.Analyze(topology);

        // Step 2: Validate topology
        var violations = topology.Validate().ToList();

        // Step 3: Calculate geometry
        _geometryEngine.Calculate(topology, startPosition.X, startPosition.Y, startAngleDeg);
        var geometryErrors = _geometryEngine.ValidateConnections(topology).ToList();

        // Step 4: Render primitives
        var primitives = RenderAllPrimitives(topology, startPosition, startAngleDeg).ToList();

        // Step 5: Collect feedback points and signals
        var feedbackPoints = CollectFeedbackPoints(topology).ToList();
        var signals = CollectSignals(topology).ToList();

        // Calculate bounds
        var bounds = _canvasRenderer.CalculateBounds(primitives);

        return new TrackPlanLayout(
            analysis: analysis,
            primitives: primitives,
            nodePositions: _geometryEngine.GetAllNodePositions().ToList(),
            constraintViolations: violations,
            geometryErrors: geometryErrors,
            feedbackPoints: feedbackPoints,
            signals: signals,
            bounds: bounds);
    }

    /// <summary>
    /// Renders all track primitives from the topology.
    /// </summary>
    public IEnumerable<IGeometryPrimitive> RenderAllPrimitives(
        TopologyGraph topology,
        Point2D startPosition,
        double startAngleDeg)
    {
        var primitives = new List<IGeometryPrimitive>();
        var processed = new HashSet<Guid>();

        var startNode = topology.Nodes.FirstOrDefault();
        if (startNode == null)
            yield break;

        RenderRecursive(
            topology,
            startNode,
            startPosition,
            startAngleDeg,
            processed,
            primitives);

        foreach (var primitive in primitives)
            yield return primitive;
    }

    /// <summary>
    /// Exports the layout to a PNG file.
    /// </summary>
    public void ExportToPng(TrackPlanLayout layout, string filePath)
    {
        ArgumentNullException.ThrowIfNull(layout);
        ArgumentNullException.ThrowIfNull(filePath);

        _canvasRenderer.ExportToPng(filePath, layout.Primitives, layout.Bounds);
    }

    /// <summary>
    /// Exports the layout as SVG (using existing SvgExporter).
    /// </summary>
    public string ExportToSvg(TrackPlanLayout layout)
    {
        ArgumentNullException.ThrowIfNull(layout);

        // SvgExporter is static - use it directly
        var svg = SvgExporter.Export(layout.Primitives, 800, 600);
        return svg;
    }

    private void RenderRecursive(
        TopologyGraph topology,
        TrackNode currentNode,
        Point2D currentPosition,
        double currentAngleDeg,
        HashSet<Guid> processed,
        List<IGeometryPrimitive> primitives)
    {
        if (!processed.Add(currentNode.Id))
            return;

        var outgoing = _resolver.GetOutgoing(currentNode);

        foreach (var edge in outgoing)
        {
            var template = _catalog.GetById(edge.TemplateId);
            if (template == null)
                continue;

            // Render this track using geometry specifications
            var trackPrimitives = RenderTrackTemplate(template, currentPosition, currentAngleDeg);
            primitives.AddRange(trackPrimitives);

            // Get exit position and angle from last primitive
            var (exitPosition, exitAngleDeg) = GetExitPortInfo(trackPrimitives, currentAngleDeg);

            // Resolve end node from edge
            var endNode = ResolveNodeFromEdge(topology, edge, "B");
            if (endNode != null)
            {
                // Continue recursively
                RenderRecursive(
                    topology,
                    endNode,
                    exitPosition,
                    exitAngleDeg,
                    processed,
                    primitives);
            }
        }
    }

    /// <summary>
    /// Renders primitives for a track template at given position and angle.
    /// </summary>
    private IEnumerable<IGeometryPrimitive> RenderTrackTemplate(
        TrackTemplate template,
        Point2D start,
        double startAngleDeg)
    {
        var spec = template.Geometry;

        return spec.GeometryKind switch
        {
            TrackGeometryKind.Straight
                => StraightGeometry.Render(template, start, startAngleDeg),

            TrackGeometryKind.Curve
                => CurveGeometry.Render(template, start, startAngleDeg),

            TrackGeometryKind.Switch
                => RenderSwitch(template, start, startAngleDeg),

            TrackGeometryKind.ThreeWaySwitch
                => ThreeWaySwitchGeometry.Render(start, startAngleDeg, spec),

            _ => Array.Empty<IGeometryPrimitive>()
        };
    }

    /// <summary>
    /// Renders switch geometry (common for switch templates).
    /// </summary>
    private IEnumerable<IGeometryPrimitive> RenderSwitch(
        TrackTemplate template,
        Point2D start,
        double startAngleDeg)
    {
        return SwitchGeometry.Render(template, start, startAngleDeg);
    }

    /// <summary>
    /// Extracts exit port position and angle from rendered primitives.
    /// </summary>
    private (Point2D Position, double AngleDeg) GetExitPortInfo(
        IEnumerable<IGeometryPrimitive> primitives,
        double startAngleDeg)
    {
        var lastPrimitive = primitives.LastOrDefault();
        if (lastPrimitive == null)
            return (new Point2D(0, 0), startAngleDeg);

        return lastPrimitive switch
        {
            LinePrimitive line => (line.To, startAngleDeg),
            ArcPrimitive arc => (GetArcEndPoint(arc), startAngleDeg + arc.SweepAngleRad * (180.0 / Math.PI)),
            _ => (new Point2D(0, 0), startAngleDeg)
        };
    }

    /// <summary>
    /// Calculates the end point of an arc given its center, radius, start angle, and sweep angle.
    /// </summary>
    private Point2D GetArcEndPoint(ArcPrimitive arc)
    {
        double endAngleRad = arc.StartAngleRad + arc.SweepAngleRad;
        return new Point2D(
            arc.Center.X + arc.Radius * Math.Cos(endAngleRad),
            arc.Center.Y + arc.Radius * Math.Sin(endAngleRad)
        );
    }

    private TrackNode? ResolveNodeFromEdge(TopologyGraph topology, TrackEdge edge, string portKey)
    {
        if (!edge.Connections.TryGetValue(portKey, out var endpoint))
            return null;

        return topology.Nodes.FirstOrDefault(n => n.Id == endpoint.NodeId);
    }

    private IEnumerable<FeedbackPointInfo> CollectFeedbackPoints(TopologyGraph topology)
    {
        var feedbackPoints = new List<FeedbackPointInfo>();
        return feedbackPoints;
    }

    private IEnumerable<SignalInfo> CollectSignals(TopologyGraph topology)
    {
        return Enumerable.Empty<SignalInfo>();
    }
}

/// <summary>
/// Complete layout result from processing a topology.
/// </summary>
public sealed class TrackPlanLayout
{
    public TrackPlanLayout(
        TopologyAnalysis analysis,
        IList<IGeometryPrimitive> primitives,
        IList<CalculatedNodePosition> nodePositions,
        IList<ConstraintViolation> constraintViolations,
        IList<GeometryValidationError> geometryErrors,
        IList<FeedbackPointInfo> feedbackPoints,
        IList<SignalInfo> signals,
        SKRect bounds)
    {
        Analysis = analysis;
        Primitives = primitives;
        NodePositions = nodePositions;
        ConstraintViolations = constraintViolations;
        GeometryErrors = geometryErrors;
        FeedbackPoints = feedbackPoints;
        Signals = signals;
        Bounds = bounds;
    }

    public TopologyAnalysis Analysis { get; }
    public IList<IGeometryPrimitive> Primitives { get; }
    public IList<CalculatedNodePosition> NodePositions { get; }
    public IList<ConstraintViolation> ConstraintViolations { get; }
    public IList<GeometryValidationError> GeometryErrors { get; }
    public IList<FeedbackPointInfo> FeedbackPoints { get; }
    public IList<SignalInfo> Signals { get; }
    public SKRect Bounds { get; }

    public bool IsValid => !ConstraintViolations.Any() && !GeometryErrors.Any();
}

/// <summary>
/// Information about a feedback point.
/// </summary>
public sealed record FeedbackPointInfo(
    int FeedbackNumber,
    Guid EdgeId,
    double OffsetMm);

/// <summary>
/// Information about a signal.
/// </summary>
public sealed record SignalInfo(
    int Address,
    string SignalType,
    Guid? EdgeId = null);
