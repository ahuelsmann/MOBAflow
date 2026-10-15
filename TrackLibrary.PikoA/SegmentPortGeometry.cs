// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.TrackLibrary.PikoA;

using Base;

/// <summary>
/// Piko A connector tables in local segment coordinates (mm, degrees).
/// </summary>
internal static class SegmentPortGeometry
{
    private sealed record PortInfo(string PortName, double LocalX, double LocalY, double LocalAngleDegrees);

    public static IReadOnlyList<SegmentPort> GetPorts(Segment segment) =>
        GetPortTable(segment)
            .Select(port => new SegmentPort(port.PortName, port.LocalX, port.LocalY, port.LocalAngleDegrees, IsStartPort(segment, port.PortName)))
            .ToList();

    private static IReadOnlyList<PortInfo> GetPortTable(Segment segment)
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
}