// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.TrackPlan.Renderer.Service;

using Moba.TrackLibrary.Base.TrackSystem;
using Moba.TrackPlan.Geometry;
using Moba.TrackPlan.Graph;

/// <summary>
/// Renders a complete TopologyGraph to geometry primitives with optional port tracking.
/// Handles mixed templates (curves, switches, etc.) automatically by selecting appropriate renderers.
/// </summary>
public class TopologyGraphRenderer
{
    private readonly ITrackCatalog _catalog;

    /// <summary>
    /// Represents a port on a rendered track edge.
    /// </summary>
    public sealed record RenderedPort(
        string PortId,
        Point2D Position,
        double AngleDeg,
        int EdgeIndex);

    private List<RenderedPort> _renderedPorts = new();

    public TopologyGraphRenderer(ITrackCatalog catalog)
    {
        _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
    }

    /// <summary>
    /// Gets the ports that were rendered in the last Render() call.
    /// </summary>
    public IReadOnlyList<RenderedPort> RenderedPorts => _renderedPorts.AsReadOnly();

    /// <summary>
    /// Renders all edges in a topology graph starting from a given position and angle.
    /// Automatically selects the appropriate renderer (CurveGeometry, SwitchGeometry, etc.) for each template.
    /// Tracks ALL port positions (connected and unconnected) for visualization.
    /// Optionally logs rendering details for debugging.
    /// </summary>
    public IEnumerable<IGeometryPrimitive> Render(
        TopologyGraph topology,
        double startX,
        double startY,
        double startAngleDeg,
        Action<string>? logger = null)
    {
        ArgumentNullException.ThrowIfNull(topology);

        _renderedPorts.Clear();
        var primitives = new List<IGeometryPrimitive>();

        double currentX = startX;
        double currentY = startY;
        double currentAngleDeg = startAngleDeg;

        int edgeIndex = 0;
        foreach (var edge in topology.Edges)
        {
            var template = _catalog.GetById(edge.TemplateId)
                ?? throw new InvalidOperationException($"Template '{edge.TemplateId}' not found in catalog");

            logger?.Invoke($"\nEdge {edgeIndex}: {edge.TemplateId}");
            logger?.Invoke($"  Ports: {edge.StartPortId} → {edge.EndPortId}");
            logger?.Invoke($"  Position: ({currentX:F2}, {currentY:F2}), Angle: {currentAngleDeg:F2}°");
            logger?.Invoke($"  Radius: {template.Geometry.RadiusMm}mm, Sweep: {template.Geometry.AngleDeg}°");

            var position = new Point2D(currentX, currentY);

            // Render all ports of this template (including diverging ports)
            RenderAllPorts(template, position, currentAngleDeg, edgeIndex, edge, logger);

            // Select appropriate renderer based on geometry type
            var edgePrimitives = template.Geometry.GeometryKind switch
            {
                TrackGeometryKind.Curve => CurveGeometry.Render(template, position, currentAngleDeg),
                TrackGeometryKind.Straight => StraightGeometry.Render(template, position, currentAngleDeg),
                TrackGeometryKind.Switch => SwitchGeometry.Render(template, position, currentAngleDeg),
                TrackGeometryKind.ThreeWaySwitch => ThreeWaySwitchGeometry.Render(position, currentAngleDeg, template.Geometry),
                _ => Enumerable.Empty<IGeometryPrimitive>()
            };

            var primitivesList = edgePrimitives.ToList();
            logger?.Invoke($"  Rendered {primitivesList.Count} primitives");
            
            primitives.AddRange(primitivesList);

            // Calculate exit point and angle for next edge
            (currentX, currentY, currentAngleDeg) = CalculateNextPosition(
                template,
                currentX,
                currentY,
                currentAngleDeg
            );

            edgeIndex++;
        }

        logger?.Invoke($"\n✓ Total: {primitives.Count} primitives, {_renderedPorts.Count} ports");
        return primitives;
    }

    /// <summary>
    /// Calculates the exit position and angle for the next edge in the sequence.
    /// </summary>
    private (double X, double Y, double AngleDeg) CalculateNextPosition(
        TrackTemplate template,
        double currentX,
        double currentY,
        double currentAngleDeg)
    {
        var spec = template.Geometry;
        
        return spec.GeometryKind switch
        {
            TrackGeometryKind.Curve => CalculateCurveExit(template, currentX, currentY, currentAngleDeg),
            TrackGeometryKind.Straight => CalculateStraightExit(template, currentX, currentY, currentAngleDeg),
            TrackGeometryKind.Switch => CalculateSwitchExit(template, currentX, currentY, currentAngleDeg),
            TrackGeometryKind.ThreeWaySwitch => CalculateSwitchExit(template, currentX, currentY, currentAngleDeg),
            _ => (currentX, currentY, currentAngleDeg)
        };
    }

    /// <summary>
    /// Calculates exit position for a curve track.
    /// </summary>
    private (double X, double Y, double AngleDeg) CalculateCurveExit(
        TrackTemplate template,
        double startX,
        double startY,
        double startAngleDeg)
    {
        double radius = template.Geometry.RadiusMm ?? 0;
        double sweepDeg = template.Geometry.AngleDeg ?? 0;

        double startRad = startAngleDeg * Math.PI / 180.0;
        double sweepRad = sweepDeg * Math.PI / 180.0;

        int normalDir = sweepRad >= 0 ? 1 : -1;
        var normal = new Point2D(
            normalDir * -Math.Sin(startRad),
            normalDir * Math.Cos(startRad)
        );

        var center = new Point2D(startX, startY) + normal * radius;

        // Exit point is at the arc end
        double arcStartRad = startRad - normalDir * Math.PI / 2.0;
        double arcEndRad = arcStartRad + sweepRad;

        var exitX = center.X + radius * Math.Cos(arcEndRad);
        var exitY = center.Y + radius * Math.Sin(arcEndRad);
        var exitAngleDeg = startAngleDeg + sweepDeg;

        return (exitX, exitY, exitAngleDeg);
    }

    /// <summary>
    /// Calculates exit position for a straight track.
    /// </summary>
    private (double X, double Y, double AngleDeg) CalculateStraightExit(
        TrackTemplate template,
        double startX,
        double startY,
        double startAngleDeg)
    {
        double length = template.Geometry.LengthMm ?? 0;
        double startRad = startAngleDeg * Math.PI / 180.0;

        var exitX = startX + length * Math.Cos(startRad);
        var exitY = startY + length * Math.Sin(startRad);

        return (exitX, exitY, startAngleDeg);
    }

    /// <summary>
    /// Calculates exit position for a switch track (uses straight path A→B, not diverging).
    /// </summary>
    private (double X, double Y, double AngleDeg) CalculateSwitchExit(
        TrackTemplate template,
        double startX,
        double startY,
        double startAngleDeg)
    {
        // For switches in topology, we follow the main path (usually A→B)
        // Diverging path (C) is optional and doesn't affect topology flow
        double length = template.Geometry.LengthMm ?? 0;
        double startRad = startAngleDeg * Math.PI / 180.0;

        var exitX = startX + length * Math.Cos(startRad);
        var exitY = startY + length * Math.Sin(startRad);

        // Exit angle remains unchanged (no sweep on main path)
        return (exitX, exitY, startAngleDeg);
    }

    /// <summary>
    /// Renders all ports of a template at their calculated positions.
    /// For switches/three-way switches, includes diverging ports (C, D, etc.).
    /// </summary>
    private void RenderAllPorts(
        TrackTemplate template,
        Point2D startPos,
        double startAngleDeg,
        int edgeIndex,
        TrackEdge edge,
        Action<string>? logger)
    {
        var templateEnds = template.Ends ?? new List<TrackEnd>();

        foreach (var trackEnd in templateEnds)
        {
            var portPosition = CalculatePortPosition(template, trackEnd, startPos, startAngleDeg);
            var portAngle = startAngleDeg + trackEnd.AngleDeg;

            _renderedPorts.Add(new RenderedPort(
                trackEnd.Id,
                portPosition,
                portAngle,
                edgeIndex
            ));

            logger?.Invoke($"    Port {trackEnd.Id}: ({portPosition.X:F2}, {portPosition.Y:F2}), Angle: {portAngle:F2}°");
        }
    }

    /// <summary>
    /// Calculates the world position of a port on a template.
    /// </summary>
    private Point2D CalculatePortPosition(
        TrackTemplate template,
        TrackEnd trackEnd,
        Point2D startPos,
        double startAngleDeg)
    {
        var templateEnds = template.Ends ?? new List<TrackEnd>();

        // Port A is at start position
        if (trackEnd.Id == templateEnds.FirstOrDefault()?.Id)
        {
            return startPos;
        }

        // Port B (and main exit) position
        var (exitX, exitY, _) = CalculateNextPosition(template, startPos.X, startPos.Y, startAngleDeg);
        if (trackEnd.Id == templateEnds.ElementAtOrDefault(1)?.Id)
        {
            return new Point2D(exitX, exitY);
        }

        // Diverging ports (C, D, etc.) on switches
        return CalculateDivergingPortPosition(template, trackEnd, startPos, startAngleDeg);
    }

    /// <summary>
    /// Calculates the world position of a diverging port (e.g., Port C on WR switch).
    /// </summary>
    private Point2D CalculateDivergingPortPosition(
        TrackTemplate template,
        TrackEnd trackEnd,
        Point2D startPos,
        double startAngleDeg)
    {
        var spec = template.Geometry;

        // For switches: calculate diverging port position
        if (spec.GeometryKind == TrackGeometryKind.Switch)
        {
            double junctionOffsetMm = spec.JunctionOffsetMm ?? 0;
            double radiusMm = spec.RadiusMm ?? 0;
            double angleDeg = spec.AngleDeg ?? 0;

            double startRad = startAngleDeg * Math.PI / 180.0;

            // Junction point on straight path
            var junctionX = startPos.X + junctionOffsetMm * Math.Cos(startRad);
            var junctionY = startPos.Y + junctionOffsetMm * Math.Sin(startRad);

            // Determine if left or right switch based on port angle
            bool isLeftSwitch = trackEnd.AngleDeg > 0;
            double side = isLeftSwitch ? +1.0 : -1.0;

            // Normal vector perpendicular to start direction
            var normalX = -Math.Sin(startRad) * side;
            var normalY = Math.Cos(startRad) * side;

            // Arc center for diverging path
            var centerX = junctionX + normalX * radiusMm;
            var centerY = junctionY + normalY * radiusMm;

            // Calculate arc end point (diverging port position)
            var arcStartRad = Math.Atan2(junctionY - centerY, junctionX - centerX);
            var arcEndRad = arcStartRad + (angleDeg * Math.PI / 180.0) * side;

            var branchEndX = centerX + radiusMm * Math.Cos(arcEndRad);
            var branchEndY = centerY + radiusMm * Math.Sin(arcEndRad);

            return new Point2D(branchEndX, branchEndY);
        }

        // For three-way switches: similar logic with two diverging ports
        return startPos;
    }
}
