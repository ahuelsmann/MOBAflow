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
            _ => GetGenericPorts(segment)
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
    public static IReadOnlyList<(string PortName, double X, double Y, double AngleDegrees)> GetAllPortWorldPositions(PlacedSegment placed, char entryPort = 'A')
    {
        var ports = GetPortsWithEntry(placed.Segment, entryPort);
        var result = new List<(string, double, double, double)>();
        foreach (var p in ports)
        {
            var (wx, wy) = ToWorld(p.LocalX, p.LocalY, placed.X, placed.Y, placed.RotationDegrees);
            result.Add((p.PortName, wx, wy, placed.RotationDegrees + p.LocalAngleDegrees));
        }

        return result;
    }

    /// <summary>
    /// Returns port infos where (0,0) is the entry port.
    /// Important for curves: With entry B, Port B is at the origin; local coordinates match
    /// the path geometry from SegmentLocalPathBuilder (curveDirection -1).
    /// </summary>
    public static IReadOnlyList<PortInfo> GetPortsWithEntry(Segment segment, char entryPort)
    {
        return segment switch
        {
            Curved c => GetCurvedPortsWithEntry(c.ArcInDegree, c.RadiusInMm, entryPort),
            Straight s => GetStraightPortsWithEntry(s.LengthInMm, entryPort),
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
            _ => GetPorts(segment)
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
        return GetCurvedPortsWithEntry(arcDegree, radius, 'A');
    }

    private static IReadOnlyList<PortInfo> GetCurvedPortsWithEntry(double arcDegree, double radius, char entryPort)
    {
        var curveDirection = entryPort == 'B' ? -1 : 1;
        var centerAngle = (90 * curveDirection) * Math.PI / 180;
        var centerX = radius * Math.Cos(centerAngle);
        var centerY = radius * Math.Sin(centerAngle);
        var endAngle = arcDegree * curveDirection * Math.PI / 180;
        var endLocalAngleRad = (90 * curveDirection) * Math.PI / 180;
        var endX = centerX + radius * Math.Cos(endAngle - endLocalAngleRad);
        var endY = centerY + radius * Math.Sin(endAngle - endLocalAngleRad);

        if (entryPort == 'A')
            return [new PortInfo("PortA", 0, 0, 0), new PortInfo("PortB", endX, endY, arcDegree * curveDirection)];

        return [new PortInfo("PortB", 0, 0, 0), new PortInfo("PortA", endX, endY, -arcDegree * curveDirection)];
    }

    private static IReadOnlyList<PortInfo> GetStraightPortsWithEntry(double length, char entryPort)
    {
        if (entryPort == 'A')
            return [new PortInfo("PortA", 0, 0, 0), new PortInfo("PortB", length, 0, 0)];
        return [new PortInfo("PortB", 0, 0, 0), new PortInfo("PortA", length, 0, 0)];
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
        var curveDir = -1;
        var centerAngle = (90 * curveDir) * Math.PI / 180;
        var centerX = radiusR2 * Math.Cos(centerAngle);
        var centerY = radiusR2 * Math.Sin(centerAngle);
        var endAngleRad = arcR2 * curveDir * Math.PI / 180;
        var endLocalAngleRad = (90 * curveDir) * Math.PI / 180;
        var sweep = endAngleRad - endLocalAngleRad;
        var portBx = centerX + radiusR2 * Math.Cos(sweep);
        var portBy = centerY + radiusR2 * Math.Sin(sweep);
        var portCx = centerX + radiusR3 * Math.Cos(sweep);
        var portCy = centerY + radiusR3 * Math.Sin(sweep);
        var exitAngle = arcR2 * curveDir;
        return
        [
            new PortInfo("PortA", 0, 0, 0),
            new PortInfo("PortB", portBx, portBy, exitAngle),
            new PortInfo("PortC", portCx, portCy, exitAngle)
        ];
    }

    private static IReadOnlyList<PortInfo> GetBwrPorts(double arcR2, double radiusR2, double radiusR3)
    {
        const int curveDir = 1;
        var centerAngle = 90 * Math.PI / 180;
        var centerX = radiusR2 * Math.Cos(centerAngle);
        var centerY = radiusR2 * Math.Sin(centerAngle);
        var endAngleRad = arcR2 * curveDir * Math.PI / 180;
        var endLocalAngleRad = 90 * Math.PI / 180;
        var sweep = endAngleRad - endLocalAngleRad;
        var portBx = centerX + radiusR2 * Math.Cos(sweep);
        var portBy = centerY + radiusR2 * Math.Sin(sweep);
        var portCx = centerX + radiusR3 * Math.Cos(sweep);
        var portCy = centerY + radiusR3 * Math.Sin(sweep);
        var exitAngle = arcR2 * curveDir;
        return
        [
            new PortInfo("PortA", 0, 0, 0),
            new PortInfo("PortB", portBx, portBy, exitAngle),
            new PortInfo("PortC", portCx, portCy, exitAngle)
        ];
    }

    /// <summary>BWLR3/BWRR3: Curved switch R3→R4. Port A origin, Port B end of R3 arc, Port C end of R4 arc.</summary>
    private static IReadOnlyList<PortInfo> GetBwlr3Ports(double radiusR3)
    {
        var radiusR4 = radiusR3 + ParallelSpacingMm;
        const int curveDir = -1;
        var centerAngle = -90 * Math.PI / 180;
        var centerX = radiusR3 * Math.Cos(centerAngle);
        var centerY = radiusR3 * Math.Sin(centerAngle);
        const double arcDeg = 30;
        var endAngleRad = arcDeg * curveDir * Math.PI / 180;
        var endLocalAngleRad = -90 * Math.PI / 180;
        var sweep = endAngleRad - endLocalAngleRad;
        var portBx = centerX + radiusR3 * Math.Cos(sweep);
        var portBy = centerY + radiusR3 * Math.Sin(sweep);
        var portCx = centerX + radiusR4 * Math.Cos(sweep);
        var portCy = centerY + radiusR4 * Math.Sin(sweep);
        var exitAngle = arcDeg * curveDir;
        return
        [
            new PortInfo("PortA", 0, 0, 0),
            new PortInfo("PortB", portBx, portBy, exitAngle),
            new PortInfo("PortC", portCx, portCy, exitAngle)
        ];
    }

    private static IReadOnlyList<PortInfo> GetBwrr3Ports(double radiusR3)
    {
        var radiusR4 = radiusR3 + ParallelSpacingMm;
        const int curveDir = 1;
        var centerAngle = 90 * Math.PI / 180;
        var centerX = radiusR3 * Math.Cos(centerAngle);
        var centerY = radiusR3 * Math.Sin(centerAngle);
        const double arcDeg = 30;
        var endAngleRad = arcDeg * curveDir * Math.PI / 180;
        var endLocalAngleRad = 90 * Math.PI / 180;
        var sweep = endAngleRad - endLocalAngleRad;
        var portBx = centerX + radiusR3 * Math.Cos(sweep);
        var portBy = centerY + radiusR3 * Math.Sin(sweep);
        var portCx = centerX + radiusR4 * Math.Cos(sweep);
        var portCy = centerY + radiusR4 * Math.Sin(sweep);
        var exitAngle = arcDeg * curveDir;
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

    /// <summary>DKW (Piko 55224) – AnyRail reference: Four ports at the ends of the two parallel tracks – A/B top track, C/D bottom track.</summary>
    private static IReadOnlyList<PortInfo> GetDkwPorts(double length, double arcDegree, double radius)
    {
        var half = length / 2;
        var rad = arcDegree * Math.PI / 180;
        var sin = Math.Sin(rad);
        var trackOffset = half * sin;
        return
        [
            new PortInfo("PortA", 0, trackOffset, 0),
            new PortInfo("PortB", length, trackOffset, 0),
            new PortInfo("PortC", 0, -trackOffset, 0),
            new PortInfo("PortD", length, -trackOffset, 0)
        ];
    }

    /// <summary>K15: Crossing 15°. 4 ports at the ends of the crossed straights.</summary>
    private static IReadOnlyList<PortInfo> GetK15Ports(double length, double arcDegree)
    {
        var rad = arcDegree * Math.PI / 180;
        var half = length / 2;
        return
        [
            new PortInfo("PortA", -half * Math.Cos(rad), -half * Math.Sin(rad), 0),
            new PortInfo("PortB", half * Math.Cos(rad), half * Math.Sin(rad), 0),
            new PortInfo("PortC", -half * Math.Cos(-rad), -half * Math.Sin(-rad), arcDegree),
            new PortInfo("PortD", half * Math.Cos(-rad), half * Math.Sin(-rad), arcDegree)
        ];
    }

    /// <summary>K30: Crossing 30°. 4 ports at the ends of the crossed straights.</summary>
    private static IReadOnlyList<PortInfo> GetK30Ports(double length, double arcDegree)
    {
        return GetK15Ports(length, arcDegree);
    }

    private static IReadOnlyList<PortInfo> GetGenericPorts(Segment segment)
    {
        var result = new List<PortInfo>();
        var type = segment.GetType();
        foreach (var prop in type.GetProperties())
        {
            if (prop.Name.StartsWith("Port") && prop.PropertyType == typeof(Guid?))
                result.Add(new PortInfo(prop.Name, 0, 0, 0));
        }

        return result;
    }
}
