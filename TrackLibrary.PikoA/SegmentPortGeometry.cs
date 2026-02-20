namespace Moba.TrackLibrary.PikoA;

using Base;

/// <summary>
/// Hilfsklasse zur Berechnung von Port-Positionen für Gleissegmente.
/// Liefert Port-Koordinaten in lokalen Segment-Koordinaten (Port A = Ursprung, Winkel 0 = +X).
/// </summary>
public static class SegmentPortGeometry
{
    /// <summary>Port-Position in lokalen Koordinaten (X, Y) und Ausrichtungswinkel (Grad).</summary>
    public sealed record PortInfo(string PortName, double LocalX, double LocalY, double LocalAngleDegrees);

    /// <summary>
    /// Berechnet die Port-Positionen für ein Segment in lokalen Koordinaten.
    /// Port A liegt am Ursprung, Winkel 0 = positive X-Richtung.
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

    /// <summary>Transformiert eine lokale Position in Weltkoordinaten.</summary>
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

    /// <summary>Berechnet die Weltposition eines Ports für ein PlacedSegment.</summary>
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

    /// <summary>Berechnet alle Port-Weltpositionen für ein PlacedSegment. Standardmäßig Entry-Port A.</summary>
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
    /// Liefert Port-Infos, bei denen (0,0) der Entry-Port ist.
    /// Wichtig für Kurven: Bei Entry B liegt Port B am Ursprung, die lokalen Koordinaten entsprechen
    /// der Pfad-Geometrie von SegmentLocalPathBuilder (curveDirection -1).
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

    /// <summary>BWL/BWR: Bogenweiche R2→R3. Port A Ursprung, Port B Ende R2-Bogen, Port C Ende R3-Bogen.</summary>
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

    /// <summary>BWLR3/BWRR3: Bogenweiche R3→R4. Port A Ursprung, Port B Ende R3-Bogen, Port C Ende R4-Bogen.</summary>
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

    /// <summary>W3: 3-Wegeweiche. Port A Ursprung, Port B gerade, Port C links, Port D rechts (2×15°).</summary>
    private static IReadOnlyList<PortInfo> GetW3Ports(double length, double arcDegree, double radius)
    {
        var halfArc = arcDegree / 2;
        var centerAngle = 90 * Math.PI / 180;
        var centerX = radius * Math.Cos(centerAngle);
        var centerY = radius * Math.Sin(centerAngle);
        var endAngleL = (halfArc - 90) * Math.PI / 180;
        var endAngleR = (-halfArc - 90) * Math.PI / 180;
        var portCx = centerX + radius * Math.Cos(endAngleL);
        var portCy = centerY + radius * Math.Sin(endAngleL);
        var portDx = centerX + radius * Math.Cos(endAngleR);
        var portDy = centerY + radius * Math.Sin(endAngleR);
        return
        [
            new PortInfo("PortA", 0, 0, 0),
            new PortInfo("PortB", length, 0, 0),
            new PortInfo("PortC", portCx, portCy, halfArc),
            new PortInfo("PortD", portDx, portDy, -halfArc)
        ];
    }

    /// <summary>WY: Y-Weiche 30°. Port A Ursprung, Port B/C symmetrisch ±15°. Länge = Bogenlänge (Radius × halber Öffnungswinkel im Bogenmaß).</summary>
    private static IReadOnlyList<PortInfo> GetWyPorts(double arcDegree, double radius)
    {
        var halfArc = arcDegree / 2;
        var halfArcRad = halfArc * Math.PI / 180;
        var len = radius * halfArcRad;
        var radL = -halfArcRad;
        var radR = halfArcRad;
        return
        [
            new PortInfo("PortA", 0, 0, 0),
            new PortInfo("PortB", len * Math.Cos(radL), len * Math.Sin(radL), -halfArc),
            new PortInfo("PortC", len * Math.Cos(radR), len * Math.Sin(radR), halfArc)
        ];
    }

    /// <summary>DKW: Doppelkreuzungsweiche. 4 Ports (Kreuzung).</summary>
    private static IReadOnlyList<PortInfo> GetDkwPorts(double length, double arcDegree, double radius)
    {
        var centerAngle = 90 * Math.PI / 180;
        var centerX = radius * Math.Cos(centerAngle);
        var centerY = radius * Math.Sin(centerAngle);
        var endAngle = (arcDegree - 90) * Math.PI / 180;
        var portCx = centerX + radius * Math.Cos(endAngle);
        var portCy = centerY + radius * Math.Sin(endAngle);
        var rad = arcDegree * Math.PI / 180;
        var portDx = length * Math.Cos(rad);
        var portDy = length * Math.Sin(rad);
        return
        [
            new PortInfo("PortA", 0, 0, 0),
            new PortInfo("PortB", length, 0, 0),
            new PortInfo("PortC", portCx, portCy, arcDegree),
            new PortInfo("PortD", portDx, portDy, arcDegree)
        ];
    }

    /// <summary>K15: Kreuzung 15°. 4 Ports an den Enden der gekreuzten Geraden.</summary>
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

    /// <summary>K30: Kreuzung 30°. 4 Ports an den Enden der gekreuzten Geraden.</summary>
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
