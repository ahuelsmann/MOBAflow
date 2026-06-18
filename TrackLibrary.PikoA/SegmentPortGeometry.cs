// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.TrackLibrary.PikoA;

using Base;

/// <summary>
/// Helper class for calculating port positions for track segments.
/// Returns port coordinates in local segment coordinates (Port A = origin, angle 0 = +X).
/// </summary>
public static class SegmentPortGeometry
{
    /// <summary>Port position in local coordinates (X, Y) and orientation angle (degrees).</summary>
    public sealed record PortInfo(string PortName, double LocalX, double LocalY, double LocalAngleDegrees);

    /// <summary>
    /// Calculates the port positions for a segment in local coordinates.
    /// Port A is at the origin, angle 0 = positive X direction.
    /// </summary>
    public static IReadOnlyList<PortInfo> GetPorts(Segment segment)
    {
        return segment switch
        {
            Straight s => GetStraightPorts(s.LengthInMm),
            Curved c => GetCurvedPorts(c.ArcInDegree, c.RadiusInMm),
            WR wr => GetWrPorts(wr.LengthInMm, wr.ArcInDegree, wr.RadiusInMm),
            WL wl => GetWlPorts(wl.LengthInMm, wl.ArcInDegree, wl.RadiusInMm),
            BWL bwl => GetBwlPorts(bwl.ArcInDegreeR2, bwl.RadiusInMmR2, bwl.RadiusInMmR3),
            BWR bwr => GetBwrPorts(bwr.ArcInDegreeR2, bwr.RadiusInMmR2, bwr.RadiusInMmR3),
            BWLR3 b => GetBwlr3Ports(b.RadiusInMmR3),
            BWRR3 b => GetBwrr3Ports(b.RadiusInMmR3),
            W3 w3 => GetW3Ports(w3.LengthInMm, w3.ArcInDegree, w3.RadiusInMm),
            WY wy => GetWyPorts(wy.ArcInDegree, wy.RadiusInMm),
            DKW dkw => GetDkwPorts(dkw.LengthInMm, dkw.ArcInDegree, dkw.RadiusInMm),
            K15 k15 => GetK15Ports(k15.LengthInMm, k15.ArcInDegree),
            K30 k30 => GetK30Ports(k30.LengthInMm, k30.ArcInDegree),
            _ => throw new NotSupportedException($"No port geometry registered for segment type '{segment.GetType().Name}'.")
        };
    }

    /// <summary>Transforms a local position to world coordinates.</summary>
    public static (double Wx, double Wy) ToWorld(double x, double y, double segmentX, double segmentY, double rotationDegrees)
    {
        var r = rotationDegrees * Math.PI / 180;
        var cos = Math.Cos(r);
        var sin = Math.Sin(r);
        return (
            segmentX + x * cos - y * sin,
            segmentY + x * sin + y * cos
        );
    }

    /// <summary>Calculates the world position of a port for a PlacedSegment.</summary>
    public static (double X, double Y, double AngleDegrees) GetPortWorldPosition(PlacedSegment placed, string portName)
    {
        var ports = GetPorts(placed.Segment);
        var port = ports.FirstOrDefault(p => p.PortName == portName);
        if (port == null)
            return (placed.X, placed.Y, placed.RotationDegrees);

        var (wx, wy) = ToWorld(port.LocalX, port.LocalY, placed.X, placed.Y, placed.RotationDegrees);
        var angle = placed.RotationDegrees + port.LocalAngleDegrees;
        return (wx, wy, angle);
    }

    /// <summary>Calculates all port world positions for a PlacedSegment. Default is entry port A.</summary>
    public static IReadOnlyList<(string PortName, double X, double Y, double AngleDegrees)> GetAllPortWorldPositions(PlacedSegment placed)
    {
        var ports = GetPorts(placed.Segment);
        var result = new List<(string, double, double, double)>();
        foreach (var p in ports)
        {
            var (wx, wy) = ToWorld(p.LocalX, p.LocalY, placed.X, placed.Y, placed.RotationDegrees);
            result.Add((p.PortName, wx, wy, placed.RotationDegrees + p.LocalAngleDegrees));
        }

        return result;
    }

    public static PortInfo? GetPort(Segment segment, string portName)
    {
        return GetPorts(segment).FirstOrDefault(p => p.PortName == portName);
    }

    public static double GetPortOutwardLocalAngleDegrees(Segment segment, string portName)
    {
        var port = GetPort(segment, portName);
        if (port == null)
            return 0;

        var angle = port.LocalAngleDegrees;
        if (IsStartPort(segment, portName))
            angle += 180;

        return NormalizeAngle(angle);
    }

    public static double GetPortOutwardWorldAngleDegrees(PlacedSegment placed, string portName)
    {
        return NormalizeAngle(placed.RotationDegrees + GetPortOutwardLocalAngleDegrees(placed.Segment, portName));
    }

    public static (double X, double Y, double RotationDegrees) GetPlacementForPort(
        Segment segment,
        string portName,
        double worldX,
        double worldY,
        double desiredOutwardAngleDegrees)
    {
        var port = GetPort(segment, portName);
        if (port == null)
            return (worldX, worldY, NormalizeAngle(desiredOutwardAngleDegrees));

        var rotationDegrees = NormalizeAngle(desiredOutwardAngleDegrees - GetPortOutwardLocalAngleDegrees(segment, portName));
        var r = rotationDegrees * Math.PI / 180;
        var cos = Math.Cos(r);
        var sin = Math.Sin(r);
        var originX = worldX - (port.LocalX * cos - port.LocalY * sin);
        var originY = worldY - (port.LocalX * sin + port.LocalY * cos);
        return (originX, originY, rotationDegrees);
    }

    public static (double X, double Y, double RotationDegrees) TranslatePlacementForPort(
        PlacedSegment placed,
        string portName,
        double worldX,
        double worldY)
    {
        var port = GetPort(placed.Segment, portName);
        if (port == null)
            return (worldX, worldY, placed.RotationDegrees);

        var r = placed.RotationDegrees * Math.PI / 180;
        var cos = Math.Cos(r);
        var sin = Math.Sin(r);
        var originX = worldX - (port.LocalX * cos - port.LocalY * sin);
        var originY = worldY - (port.LocalX * sin + port.LocalY * cos);
        return (originX, originY, placed.RotationDegrees);
    }

    private static IReadOnlyList<PortInfo> GetStraightPorts(double length)
    {
        return
        [
            new PortInfo("PortA", 0, 0, 0),
            new PortInfo("PortB", length, 0, 0)
        ];
    }

    private static IReadOnlyList<PortInfo> GetCurvedPorts(double arcDegree, double radius)
    {
        const int curveDirection = 1;
        var centerAngle = (90 * curveDirection) * Math.PI / 180;
        var centerX = radius * Math.Cos(centerAngle);
        var centerY = radius * Math.Sin(centerAngle);
        var endAngle = arcDegree * curveDirection * Math.PI / 180;
        var endLocalAngleRad = (90 * curveDirection) * Math.PI / 180;
        var endX = centerX + radius * Math.Cos(endAngle - endLocalAngleRad);
        var endY = centerY + radius * Math.Sin(endAngle - endLocalAngleRad);
        return [new PortInfo("PortA", 0, 0, 0), new PortInfo("PortB", endX, endY, arcDegree * curveDirection)];
    }

    private static IReadOnlyList<PortInfo> GetWrPorts(double straightLength, double arcDegree, double radius)
    {
        var centerAngle = 90 * Math.PI / 180;
        var centerX = radius * Math.Cos(centerAngle);
        var centerY = radius * Math.Sin(centerAngle);
        var endAngle = (arcDegree - 90) * Math.PI / 180;
        var portCx = centerX + radius * Math.Cos(endAngle);
        var portCy = centerY + radius * Math.Sin(endAngle);
        return
        [
            new PortInfo("PortA", 0, 0, 0),
            new PortInfo("PortB", straightLength, 0, 0),
            new PortInfo("PortC", portCx, portCy, arcDegree)
        ];
    }

    private static IReadOnlyList<PortInfo> GetWlPorts(double straightLength, double arcDegree, double radius)
    {
        var centerAngle = -90 * Math.PI / 180;
        var centerX = radius * Math.Cos(centerAngle);
        var centerY = radius * Math.Sin(centerAngle);
        var endAngle = (-arcDegree + 90) * Math.PI / 180;
        var portCx = centerX + radius * Math.Cos(endAngle);
        var portCy = centerY + radius * Math.Sin(endAngle);
        return
        [
            new PortInfo("PortA", 0, 0, 0),
            new PortInfo("PortB", straightLength, 0, 0),
            new PortInfo("PortC", portCx, portCy, -arcDegree)
        ];
    }

    private const double ParallelSpacingMm = 61.88;

    /// <summary>BWL/BWR: Curved switch R2→R3. Port A origin, Port B end of R2 arc, Port C end of R3 arc.</summary>
    private static IReadOnlyList<PortInfo> GetBwlPorts(double arcR2, double radiusR2, double radiusR3)
    {
        return GetParallelArcSwitchPorts(arcR2, radiusR2, radiusR3, -1);
    }

    private static IReadOnlyList<PortInfo> GetBwrPorts(double arcR2, double radiusR2, double radiusR3)
    {
        return GetParallelArcSwitchPorts(arcR2, radiusR2, radiusR3, 1);
    }

    /// <summary>BWLR3/BWRR3: Curved switch R3→R4. Port A origin, Port B end of R3 arc, Port C end of R4 arc.</summary>
    private static IReadOnlyList<PortInfo> GetBwlr3Ports(double radiusR3)
    {
        var radiusR4 = radiusR3 + ParallelSpacingMm;
        return GetParallelArcSwitchPorts(30, radiusR3, radiusR4, -1);
    }

    private static IReadOnlyList<PortInfo> GetBwrr3Ports(double radiusR3)
    {
        var radiusR4 = radiusR3 + ParallelSpacingMm;
        return GetParallelArcSwitchPorts(30, radiusR3, radiusR4, 1);
    }

    private static IReadOnlyList<PortInfo> GetParallelArcSwitchPorts(double arcDegree, double innerRadius, double outerRadius, int curveDirection)
    {
        var centerAngle = (90 * curveDirection) * Math.PI / 180;
        var centerX = innerRadius * Math.Cos(centerAngle);
        var centerY = innerRadius * Math.Sin(centerAngle);
        var endAngleRad = arcDegree * curveDirection * Math.PI / 180;
        var endLocalAngleRad = (90 * curveDirection) * Math.PI / 180;
        var sweep = endAngleRad - endLocalAngleRad;
        var portBx = centerX + innerRadius * Math.Cos(sweep);
        var portBy = centerY + innerRadius * Math.Sin(sweep);
        var portCx = centerX + outerRadius * Math.Cos(sweep);
        var portCy = centerY + outerRadius * Math.Sin(sweep);
        var exitAngle = arcDegree * curveDirection;
        return
        [
            new PortInfo("PortA", 0, 0, 0),
            new PortInfo("PortB", portBx, portBy, exitAngle),
            new PortInfo("PortC", portCx, portCy, exitAngle)
        ];
    }

    /// <summary>W3 (Piko 55225): Port A origin, Port B straight G239, Port C = WL branch -15°, Port D = WR branch +15° (each R9).</summary>
    private static IReadOnlyList<PortInfo> GetW3Ports(double length, double arcDegree, double radius)
    {
        var halfArc = arcDegree / 2;
        // Port C: wie GetWlPorts (linker Ast)
        var centerL = -90 * Math.PI / 180;
        var centerLx = radius * Math.Cos(centerL);
        var centerLy = radius * Math.Sin(centerL);
        var endAngleL = (-halfArc + 90) * Math.PI / 180;
        var portCx = centerLx + radius * Math.Cos(endAngleL);
        var portCy = centerLy + radius * Math.Sin(endAngleL);
        // Port D: wie GetWrPorts (rechter Ast)
        var centerR = 90 * Math.PI / 180;
        var centerRx = radius * Math.Cos(centerR);
        var centerRy = radius * Math.Sin(centerR);
        var endAngleR = (halfArc - 90) * Math.PI / 180;
        var portDx = centerRx + radius * Math.Cos(endAngleR);
        var portDy = centerRy + radius * Math.Sin(endAngleR);
        return
        [
            new PortInfo("PortA", 0, 0, 0),
            new PortInfo("PortB", length, 0, 0),
            new PortInfo("PortC", portCx, portCy, -halfArc),
            new PortInfo("PortD", portDx, portDy, halfArc)
        ];
    }

    /// <summary>WY: Y-switch like W3 without straight. Port A origin, Port B = WL branch -15°, Port C = WR branch +15° (each R9).</summary>
    private static IReadOnlyList<PortInfo> GetWyPorts(double arcDegree, double radius)
    {
        var halfArc = arcDegree / 2;
        // Port B: wie GetWlPorts (linker Ast)
        var centerL = -90 * Math.PI / 180;
        var centerLx = radius * Math.Cos(centerL);
        var centerLy = radius * Math.Sin(centerL);
        var endAngleL = (-halfArc + 90) * Math.PI / 180;
        var portBx = centerLx + radius * Math.Cos(endAngleL);
        var portBy = centerLy + radius * Math.Sin(endAngleL);
        // Port C: wie GetWrPorts (rechter Ast)
        var centerR = 90 * Math.PI / 180;
        var centerRx = radius * Math.Cos(centerR);
        var centerRy = radius * Math.Sin(centerR);
        var endAngleR = (halfArc - 90) * Math.PI / 180;
        var portCx = centerRx + radius * Math.Cos(endAngleR);
        var portCy = centerRy + radius * Math.Sin(endAngleR);
        return
        [
            new PortInfo("PortA", 0, 0, 0),
            new PortInfo("PortB", portBx, portBy, -halfArc),
            new PortInfo("PortC", portCx, portCy, halfArc)
        ];
    }

    /// <summary>
    /// DKW (Piko 55224): Four ports on two straight tracks crossing at <paramref name="arcDegree"/> (15°).
    /// Port A/B lie on the horizontal main track, Port C/D on the diagonal track that crosses through the midpoint.
    /// Geometry identical to K15, but all four ports are switchable (IsStartPort treats PortC as start).
    /// </summary>
    private static IReadOnlyList<PortInfo> GetDkwPorts(double length, double arcDegree, double radius)
    {
        _ = radius;
        var rad = arcDegree * Math.PI / 180;
        var half = length / 2;
        var cos = Math.Cos(rad);
        var sin = Math.Sin(rad);
        return
        [
            new PortInfo("PortA", 0, 0, 0),
            new PortInfo("PortB", length, 0, 0),
            new PortInfo("PortC", half - half * cos, -half * sin, arcDegree),
            new PortInfo("PortD", half + half * cos, half * sin, arcDegree)
        ];
    }

    /// <summary>K15: Crossing 15°. 4 ports at the ends of the crossed straights.</summary>
    private static IReadOnlyList<PortInfo> GetK15Ports(double length, double arcDegree)
    {
        var rad = arcDegree * Math.PI / 180;
        var half = length / 2;
        var cos = Math.Cos(rad);
        var sin = Math.Sin(rad);
        return
        [
            new PortInfo("PortA", 0, 0, 0),
            new PortInfo("PortB", length, 0, 0),
            new PortInfo("PortC", half - half * cos, -half * sin, arcDegree),
            new PortInfo("PortD", half + half * cos, half * sin, arcDegree)
        ];
    }

    /// <summary>K30: Crossing 30°. 4 ports at the ends of the crossed straights.</summary>
    private static IReadOnlyList<PortInfo> GetK30Ports(double length, double arcDegree)
    {
        return GetK15Ports(length, arcDegree);
    }

    private static bool IsStartPort(Segment segment, string portName)
    {
        if (portName == "PortA")
            return true;

        return segment is DKW or K15 or K30 && portName == "PortC";
    }

    private static double NormalizeAngle(double degrees)
    {
        while (degrees >= 360)
            degrees -= 360;
        while (degrees < 0)
            degrees += 360;
        return degrees;
    }
}