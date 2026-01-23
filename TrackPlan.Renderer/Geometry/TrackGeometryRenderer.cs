// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.TrackPlan.Renderer.Geometry;

using Moba.TrackPlan.Renderer.World;
using Moba.TrackPlan.TrackSystem;

/// <summary>
/// Port information for track connection points.
/// </summary>
public sealed record TrackPortInfo(
    string PortName,
    Point2D Position,
    double ExitAngleDeg);

public sealed class TrackGeometryRenderer
{
    public IEnumerable<IGeometryPrimitive> Render(
        TrackTemplate template,
        Point2D start,
        double startAngleDeg)
    {
        var spec = template.Geometry;

        return spec.GeometryKind switch
        {
            TrackGeometryKind.Straight
                => StraightGeometry.Render(start, startAngleDeg, spec.LengthMm!.Value),

            TrackGeometryKind.Curve
                => CurveGeometry.Render(start, startAngleDeg, spec),

            TrackGeometryKind.Switch
                => RenderSwitch(template, spec, start, startAngleDeg),

            TrackGeometryKind.ThreeWaySwitch
                => ThreeWaySwitchGeometry.Render(start, startAngleDeg, spec),

            _ => Array.Empty<IGeometryPrimitive>()
        };
    }

    /// <summary>
    /// Gets port information for a track (positions and exit angles).
    /// </summary>
    public IReadOnlyList<TrackPortInfo> GetPorts(
        TrackTemplate template,
        Point2D start,
        double startAngleDeg)
    {
        var prims = Render(template, start, startAngleDeg).ToList();
        var spec = template.Geometry;

        return spec.GeometryKind switch
        {
            TrackGeometryKind.Straight
                => GetStraightPorts(prims, startAngleDeg),

            TrackGeometryKind.Curve
                => GetCurvePorts(prims, startAngleDeg),

            TrackGeometryKind.Switch
                => GetSwitchPorts(prims, startAngleDeg),

            TrackGeometryKind.ThreeWaySwitch
                => GetThreeWaySwitchPorts(prims, startAngleDeg),

            _ => Array.Empty<TrackPortInfo>()
        };
    }

    private IReadOnlyList<TrackPortInfo> GetStraightPorts(List<IGeometryPrimitive> prims, double startAngleDeg)
    {
        var line = prims.OfType<LinePrimitive>().FirstOrDefault();
        if (line == null) return Array.Empty<TrackPortInfo>();

        return new[]
        {
            new TrackPortInfo("A", line.From, startAngleDeg),
            new TrackPortInfo("B", line.To, startAngleDeg)
        };
    }

    private IReadOnlyList<TrackPortInfo> GetCurvePorts(List<IGeometryPrimitive> prims, double startAngleDeg)
    {
        var arc = prims.OfType<ArcPrimitive>().FirstOrDefault();
        if (arc == null) return Array.Empty<TrackPortInfo>();

        var portA = new Point2D(
            arc.Center.X + arc.Radius * Math.Cos(arc.StartAngleRad),
            arc.Center.Y + arc.Radius * Math.Sin(arc.StartAngleRad));
        
        var portB = new Point2D(
            arc.Center.X + arc.Radius * Math.Cos(arc.StartAngleRad + arc.SweepAngleRad),
            arc.Center.Y + arc.Radius * Math.Sin(arc.StartAngleRad + arc.SweepAngleRad));

        var exitAngleDeg = startAngleDeg + (arc.SweepAngleRad * 180 / Math.PI);

        return new[]
        {
            new TrackPortInfo("A", portA, startAngleDeg),
            new TrackPortInfo("B", portB, exitAngleDeg)
        };
    }

    private IReadOnlyList<TrackPortInfo> GetSwitchPorts(List<IGeometryPrimitive> prims, double startAngleDeg)
    {
        var line = prims.OfType<LinePrimitive>().FirstOrDefault();
        var arc = prims.OfType<ArcPrimitive>().FirstOrDefault();

        if (line == null || arc == null) return Array.Empty<TrackPortInfo>();

        var portC = new Point2D(
            arc.Center.X + arc.Radius * Math.Cos(arc.StartAngleRad + arc.SweepAngleRad),
            arc.Center.Y + arc.Radius * Math.Sin(arc.StartAngleRad + arc.SweepAngleRad));

        var exitAngleDeg = startAngleDeg + (arc.SweepAngleRad * 180 / Math.PI);

        return new[]
        {
            new TrackPortInfo("A", line.From, startAngleDeg),
            new TrackPortInfo("B", line.To, startAngleDeg),
            new TrackPortInfo("C", portC, exitAngleDeg)
        };
    }

    private IReadOnlyList<TrackPortInfo> GetThreeWaySwitchPorts(List<IGeometryPrimitive> prims, double startAngleDeg)
    {
        var line = prims.OfType<LinePrimitive>().FirstOrDefault();
        var arcs = prims.OfType<ArcPrimitive>().ToList();

        if (line == null || arcs.Count < 2) return Array.Empty<TrackPortInfo>();

        var arcLeft = arcs[0];   // Left diverging (Port C)
        var arcRight = arcs[1];  // Right diverging (Port D)

        var portC = new Point2D(
            arcLeft.Center.X + arcLeft.Radius * Math.Cos(arcLeft.StartAngleRad + arcLeft.SweepAngleRad),
            arcLeft.Center.Y + arcLeft.Radius * Math.Sin(arcLeft.StartAngleRad + arcLeft.SweepAngleRad));

        var portD = new Point2D(
            arcRight.Center.X + arcRight.Radius * Math.Cos(arcRight.StartAngleRad + arcRight.SweepAngleRad),
            arcRight.Center.Y + arcRight.Radius * Math.Sin(arcRight.StartAngleRad + arcRight.SweepAngleRad));

        var exitAngleCDeg = startAngleDeg + (arcLeft.SweepAngleRad * 180 / Math.PI);
        var exitAngleDDeg = startAngleDeg + (arcRight.SweepAngleRad * 180 / Math.PI);

        return new[]
        {
            new TrackPortInfo("A", line.From, startAngleDeg),
            new TrackPortInfo("B", line.To, startAngleDeg),
            new TrackPortInfo("C", portC, exitAngleCDeg),
            new TrackPortInfo("D", portD, exitAngleDDeg)
        };
    }

    private IEnumerable<IGeometryPrimitive> RenderSwitch(
        TrackTemplate template,
        TrackGeometrySpec spec,
        Point2D start,
        double startAngleDeg)
    {
        bool isLeft = template.Id.EndsWith('L');
        return SwitchGeometry.Render(start, startAngleDeg, spec, template.Routing!, isLeft);
    }
}