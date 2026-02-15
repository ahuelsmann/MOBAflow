namespace Moba.TrackLibrary.PikoA;

using Base;

/// <summary>
/// Erzeugt Pfad-Geometrie für Gleissegmente in lokalen Koordinaten (Port A = Ursprung, Winkel 0 = +X).
/// Verwendet dieselben Formeln wie der TrackPlanSvgRenderer für konsistente Darstellung.
/// </summary>
public static class SegmentLocalPathBuilder
{
    /// <summary>Befehl im Pfad (plattformunabhängig).</summary>
    public abstract record PathCommand;

    /// <summary>Bewegt zur Position ohne zu zeichnen (Start neuer Unterpfad).</summary>
    public sealed record MoveTo(double X, double Y) : PathCommand;

    /// <summary>Linie zum angegebenen Punkt.</summary>
    public sealed record LineTo(double X, double Y) : PathCommand;

    /// <summary>Kreisbogen zum Endpunkt (Radius in mm, Clockwise = Sweep-Richtung).</summary>
    public sealed record ArcTo(double EndX, double EndY, double Radius, bool Clockwise) : PathCommand;

    /// <summary>
    /// Liefert die Pfad-Befehle für ein Segment in lokalen Koordinaten.
    /// Startpunkt ist stets (0, 0) = Port A.
    /// Verwendet dieselben Formeln wie der TrackPlanSvgRenderer.
    /// </summary>
    public static IReadOnlyList<PathCommand> GetPath(Segment segment) => GetPath(segment, 'A');

    /// <summary>
    /// Liefert die Pfad-Befehle für ein Segment in lokalen Koordinaten mit Berücksichtigung des Entry-Ports.
    /// Entry-Port B bei Kurven: Kurve rechts (curveDirection -1). Entry-Port B bei Geraden: Linie rückwärts.
    /// </summary>
    public static IReadOnlyList<PathCommand> GetPath(Segment segment, char entryPort)
    {
        var curveDirection = entryPort == 'B' ? -1 : 1;

        return segment switch
        {
            Straight s => GetStraightPath(s.LengthInMm, curveDirection),
            Curved c => GetCurvedPath(c.ArcInDegree, c.RadiusInMm, curveDirection),
            WR wr => GetWrPath(wr.LengthInMm, wr.ArcInDegree, wr.RadiusInMm),
            WL wl => GetWlPath(wl.LengthInMm, wl.ArcInDegree, wl.RadiusInMm),
            WY wy => GetWyPath(wy.ArcInDegree, wy.RadiusInMm),
            W3 w3 => GetW3Path(w3.LengthInMm, w3.ArcInDegree),
            BWL bwl => GetBwlPath(bwl.ArcInDegreeR2, bwl.RadiusInMmR2, bwl.RadiusInMmR3),
            BWR bwr => GetBwrPath(bwr.ArcInDegreeR2, bwr.RadiusInMmR2, bwr.RadiusInMmR3),
            BWLR3 bwlr3 => GetBwlr3Path(bwlr3.RadiusInMmR3),
            BWRR3 bwrr3 => GetBwrr3Path(bwrr3.RadiusInMmR3),
            DKW _ => GetDkwPath(),
            K15 k15 => GetCrossingPath(k15.ArcInDegree, k15.LengthInMm),
            K30 k30 => GetCrossingPath(k30.ArcInDegree, k30.LengthInMm),
            _ => GetStraightPath(100, 1)
        };
    }

    /// <summary>Liefert die Bounding-Box des Pfads (MinX, MinY, MaxX, MaxY) in mm.</summary>
    public static (double MinX, double MinY, double MaxX, double MaxY) GetBounds(IReadOnlyList<PathCommand> path)
    {
        double minX = 0, minY = 0, maxX = 0, maxY = 0;
        double x = 0, y = 0;
        var hasPoint = false;

        foreach (var cmd in path)
        {
            if (cmd is MoveTo move)
            {
                x = move.X;
                y = move.Y;
            }
            else if (cmd is LineTo line)
            {
                x = line.X;
                y = line.Y;
            }
            else if (cmd is ArcTo arc)
            {
                x = arc.EndX;
                y = arc.EndY;
            }

            hasPoint = true;
            minX = Math.Min(minX, x);
            minY = Math.Min(minY, y);
            maxX = Math.Max(maxX, x);
            maxY = Math.Max(maxY, y);
        }

        if (!hasPoint)
            return (0, 0, 1, 1);

        return (minX, minY, maxX, maxY);
    }

    private static IReadOnlyList<PathCommand> GetStraightPath(double length, int direction = 1)
    {
        return [new LineTo(length * direction, 0)];
    }

    private static IReadOnlyList<PathCommand> GetCurvedPath(double arcDegree, double radius, int curveDirection = 1)
    {
        var centerAngleRad = (90 * curveDirection) * Math.PI / 180;
        var centerX = radius * Math.Cos(centerAngleRad);
        var centerY = radius * Math.Sin(centerAngleRad);
        var endAngle = arcDegree * curveDirection * Math.PI / 180;
        var endLocalAngleRad = (90 * curveDirection) * Math.PI / 180;
        var endX = centerX + radius * Math.Cos(endAngle - endLocalAngleRad);
        var endY = centerY + radius * Math.Sin(endAngle - endLocalAngleRad);
        var clockwise = curveDirection == 1;
        return [new ArcTo(endX, endY, radius, clockwise)];
    }

    private static IReadOnlyList<PathCommand> GetWrPath(double straightLength, double arcDegree, double radius)
    {
        var centerAngleRad = 90 * Math.PI / 180;
        var centerX = radius * Math.Cos(centerAngleRad);
        var centerY = radius * Math.Sin(centerAngleRad);
        var endAngleRad = (arcDegree - 90) * Math.PI / 180;
        var portCx = centerX + radius * Math.Cos(endAngleRad);
        var portCy = centerY + radius * Math.Sin(endAngleRad);
        return
        [
            new LineTo(straightLength, 0),
            new MoveTo(0, 0),
            new ArcTo(portCx, portCy, radius, true)
        ];
    }

    private static IReadOnlyList<PathCommand> GetWlPath(double straightLength, double arcDegree, double radius)
    {
        var centerAngleRad = -90 * Math.PI / 180;
        var centerX = radius * Math.Cos(centerAngleRad);
        var centerY = radius * Math.Sin(centerAngleRad);
        var endAngleRad = (-arcDegree + 90) * Math.PI / 180;
        var portCx = centerX + radius * Math.Cos(endAngleRad);
        var portCy = centerY + radius * Math.Sin(endAngleRad);
        return
        [
            new LineTo(straightLength, 0),
            new MoveTo(0, 0),
            new ArcTo(portCx, portCy, radius, false)
        ];
    }

    private static IReadOnlyList<PathCommand> GetWyPath(double arcDegree, double radius)
    {
        var len = radius * 0.15;
        var a = -arcDegree / 2 * Math.PI / 180;
        var b = arcDegree / 2 * Math.PI / 180;
        return
        [
            new LineTo(len, 0),
            new MoveTo(0, 0),
            new LineTo(len * Math.Cos(a), len * Math.Sin(a)),
            new MoveTo(0, 0),
            new LineTo(len * Math.Cos(b), len * Math.Sin(b))
        ];
    }

    private static IReadOnlyList<PathCommand> GetW3Path(double length, double arcDegree)
    {
        var len = length * 0.4;
        var a = -arcDegree / 2 * Math.PI / 180;
        var b = 0.0;
        var c = arcDegree / 2 * Math.PI / 180;
        return
        [
            new LineTo(len * Math.Cos(a), len * Math.Sin(a)),
            new MoveTo(0, 0),
            new LineTo(len, 0),
            new MoveTo(0, 0),
            new LineTo(len * Math.Cos(c), len * Math.Sin(c))
        ];
    }

    /// <summary>BWL: Bogenweiche links R2→R3. Stammgleis R2-Bogen, Abzweig R3-Bogen.</summary>
    private static IReadOnlyList<PathCommand> GetBwlPath(double arcR2, double radiusR2, double radiusR3)
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
        return
        [
            new ArcTo(portBx, portBy, radiusR2, false),
            new MoveTo(0, 0),
            new ArcTo(portCx, portCy, radiusR3, false)
        ];
    }

    /// <summary>BWR: Bogenweiche rechts R2→R3. Stammgleis R2-Bogen, Abzweig R3-Bogen.</summary>
    private static IReadOnlyList<PathCommand> GetBwrPath(double arcR2, double radiusR2, double radiusR3)
    {
        var curveDir = 1;
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
        return
        [
            new ArcTo(portBx, portBy, radiusR2, true),
            new MoveTo(0, 0),
            new ArcTo(portCx, portCy, radiusR3, true)
        ];
    }

    /// <summary>BWLR3: Bogenweiche links R3→R4.</summary>
    private static IReadOnlyList<PathCommand> GetBwlr3Path(double radiusR3)
    {
        var radiusR4 = radiusR3 + 61.88;
        var curveDir = -1;
        var centerAngle = (90 * curveDir) * Math.PI / 180;
        var centerX = radiusR3 * Math.Cos(centerAngle);
        var centerY = radiusR3 * Math.Sin(centerAngle);
        const double arcDeg = 30;
        var sweep = (arcDeg * curveDir * Math.PI / 180) - ((90 * curveDir) * Math.PI / 180);
        var portBx = centerX + radiusR3 * Math.Cos(sweep);
        var portBy = centerY + radiusR3 * Math.Sin(sweep);
        var portCx = centerX + radiusR4 * Math.Cos(sweep);
        var portCy = centerY + radiusR4 * Math.Sin(sweep);
        return
        [
            new ArcTo(portBx, portBy, radiusR3, false),
            new MoveTo(0, 0),
            new ArcTo(portCx, portCy, radiusR4, false)
        ];
    }

    /// <summary>BWRR3: Bogenweiche rechts R3→R4.</summary>
    private static IReadOnlyList<PathCommand> GetBwrr3Path(double radiusR3)
    {
        var radiusR4 = radiusR3 + 61.88;
        var curveDir = 1;
        var centerAngle = (90 * curveDir) * Math.PI / 180;
        var centerX = radiusR3 * Math.Cos(centerAngle);
        var centerY = radiusR3 * Math.Sin(centerAngle);
        const double arcDeg = 30;
        var sweep = (arcDeg * curveDir * Math.PI / 180) - ((90 * curveDir) * Math.PI / 180);
        var portBx = centerX + radiusR3 * Math.Cos(sweep);
        var portBy = centerY + radiusR3 * Math.Sin(sweep);
        var portCx = centerX + radiusR4 * Math.Cos(sweep);
        var portCy = centerY + radiusR4 * Math.Sin(sweep);
        return
        [
            new ArcTo(portBx, portBy, radiusR3, true),
            new MoveTo(0, 0),
            new ArcTo(portCx, portCy, radiusR4, true)
        ];
    }

    private static IReadOnlyList<PathCommand> GetDkwPath()
    {
        var len = 80.0;
        return
        [
            new LineTo(len, 0),
            new MoveTo(0, 0),
            new LineTo(len * 0.7, len * 0.5),
            new MoveTo(len * 0.3, -len * 0.5),
            new LineTo(len, len * 0.2)
        ];
    }

    private static IReadOnlyList<PathCommand> GetCrossingPath(double angleDeg, double length)
    {
        var len = length * 0.4;
        var rad = angleDeg * Math.PI / 180;
        return
        [
            new MoveTo(-len * Math.Cos(rad), -len * Math.Sin(rad)),
            new LineTo(len * Math.Cos(rad), len * Math.Sin(rad)),
            new MoveTo(-len * Math.Cos(-rad), -len * Math.Sin(-rad)),
            new LineTo(len * Math.Cos(-rad), len * Math.Sin(-rad))
        ];
    }
}
