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

    /// <summary>Berechnet alle Port-Weltpositionen für ein PlacedSegment.</summary>
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
        var curveDirection = 1;
        var centerAngle = 90 * curveDirection * Math.PI / 180;
        var centerX = radius * Math.Cos(centerAngle);
        var centerY = radius * Math.Sin(centerAngle);
        var endAngle = arcDegree * Math.PI / 180;
        var endLocalAngle = 90 * curveDirection;
        var endX = centerX + radius * Math.Cos((endAngle - endLocalAngle * Math.PI / 180));
        var endY = centerY + radius * Math.Sin((endAngle - endLocalAngle * Math.PI / 180));
        return
        [
            new PortInfo("PortA", 0, 0, 0),
            new PortInfo("PortB", endX, endY, arcDegree)
        ];
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
