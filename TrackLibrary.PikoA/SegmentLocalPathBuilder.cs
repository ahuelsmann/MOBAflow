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

    /// <summary>Kreisbogen zum Endpunkt (Radius in mm, Clockwise = Sweep-Richtung, LargeArc = Bogen &gt; 180°).</summary>
    public sealed record ArcTo(double EndX, double EndY, double Radius, bool Clockwise, bool LargeArc = false) : PathCommand;

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
            W3 w3 => GetW3Path(w3.LengthInMm, w3.ArcInDegree, w3.RadiusInMm),
            BWL bwl => GetBwlPath(bwl.ArcInDegreeR2, bwl.RadiusInMmR2, bwl.RadiusInMmR3),
            BWR bwr => GetBwrPath(bwr.ArcInDegreeR2, bwr.RadiusInMmR2, bwr.RadiusInMmR3),
            BWLR3 bwlr3 => GetBwlr3Path(bwlr3.RadiusInMmR3),
            BWRR3 bwrr3 => GetBwrr3Path(bwrr3.RadiusInMmR3),
            DKW dkw => GetDkwPath(dkw.LengthInMm, dkw.ArcInDegree, dkw.RadiusInMm),
            K15 k15 => GetCrossingPath(k15.ArcInDegree, k15.LengthInMm),
            K30 k30 => GetCrossingPath(k30.ArcInDegree, k30.LengthInMm),
            _ => GetStraightPath(100)
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

    /// <summary>WY: Y-Weiche wie W3 ohne Gerade – zwei R9-Bögen je 15° (Port B links wie WL, Port C rechts wie WR).</summary>
    private static IReadOnlyList<PathCommand> GetWyPath(double arcDegree, double radius)
    {
        var halfArc = arcDegree / 2;
        // Port B: linker Ast wie WL
        var centerL = -90 * Math.PI / 180;
        var centerLx = radius * Math.Cos(centerL);
        var centerLy = radius * Math.Sin(centerL);
        var endAngleL = (-halfArc + 90) * Math.PI / 180;
        var portBx = centerLx + radius * Math.Cos(endAngleL);
        var portBy = centerLy + radius * Math.Sin(endAngleL);
        // Port C: rechter Ast wie WR
        var centerR = 90 * Math.PI / 180;
        var centerRx = radius * Math.Cos(centerR);
        var centerRy = radius * Math.Sin(centerR);
        var endAngleR = (halfArc - 90) * Math.PI / 180;
        var portCx = centerRx + radius * Math.Cos(endAngleR);
        var portCy = centerRy + radius * Math.Sin(endAngleR);
        return
        [
            new ArcTo(portBx, portBy, radius, false),
            new MoveTo(0, 0),
            new ArcTo(portCx, portCy, radius, true)
        ];
    }

    /// <summary>W3 (Piko 55225): Gerade G239 bis Port B, zwei Abzweige R9 je 15° – Port C wie WL (links), Port D wie WR (rechts).</summary>
    private static IReadOnlyList<PathCommand> GetW3Path(double length, double arcDegree, double radius)
    {
        var halfArc = arcDegree / 2;
        // Port C: linker Ast wie WL – Mittelpunkt (0, -radius), Bogen 15° gegen Uhrzeigersinn
        var centerL = -90 * Math.PI / 180;
        var centerLx = radius * Math.Cos(centerL);
        var centerLy = radius * Math.Sin(centerL);
        var endAngleL = (-halfArc + 90) * Math.PI / 180;
        var portCx = centerLx + radius * Math.Cos(endAngleL);
        var portCy = centerLy + radius * Math.Sin(endAngleL);
        // Port D: rechter Ast wie WR – Mittelpunkt (0, radius), Bogen 15° im Uhrzeigersinn
        var centerR = 90 * Math.PI / 180;
        var centerRx = radius * Math.Cos(centerR);
        var centerRy = radius * Math.Sin(centerR);
        var endAngleR = (halfArc - 90) * Math.PI / 180;
        var portDx = centerRx + radius * Math.Cos(endAngleR);
        var portDy = centerRy + radius * Math.Sin(endAngleR);
        return
        [
            new LineTo(length, 0),
            new MoveTo(0, 0),
            new ArcTo(portCx, portCy, radius, false),
            new MoveTo(0, 0),
            new ArcTo(portDx, portDy, radius, true)
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
        const int curveDir = -1;
        var centerAngle = -90 * Math.PI / 180;
        var centerX = radiusR3 * Math.Cos(centerAngle);
        var centerY = radiusR3 * Math.Sin(centerAngle);
        const double arcDeg = 30;
        var sweep = (arcDeg * curveDir * Math.PI / 180) - (-90 * Math.PI / 180);
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
        const int curveDir = 1;
        var centerAngle = 90 * Math.PI / 180;
        var centerX = radiusR3 * Math.Cos(centerAngle);
        var centerY = radiusR3 * Math.Sin(centerAngle);
        const double arcDeg = 30;
        var sweep = (arcDeg * curveDir * Math.PI / 180) - (90 * Math.PI / 180);
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

    /// <summary>DKW (Piko 55224) – AnyRail-Referenz: Zwei parallele Hauptgleise (oben A–B, unten C–D), in der Mitte X aus zwei Diagonalen (15°).</summary>
    private static IReadOnlyList<PathCommand> GetDkwPath(double length, double arcDegree, double radius)
    {
        var half = length / 2;
        var rad = arcDegree * Math.PI / 180;
        var cos = Math.Cos(rad);
        var sin = Math.Sin(rad);
        // Abstand der parallelen Gleise (Höhe der Raute)
        var trackOffset = half * sin;
        var crossHalf = half * cos;
        // Oberes Gleis: (0, trackOffset) bis (length, trackOffset)
        // Unteres Gleis: (0, -trackOffset) bis (length, -trackOffset)
        // X: Diagonale 1 von (half - crossHalf, trackOffset) nach (half + crossHalf, -trackOffset)
        //    Diagonale 2 von (half - crossHalf, -trackOffset) nach (half + crossHalf, trackOffset)
        return
        [
            new MoveTo(0, trackOffset),
            new LineTo(length, trackOffset),
            new MoveTo(0, -trackOffset),
            new LineTo(length, -trackOffset),
            new MoveTo(half - crossHalf, trackOffset),
            new LineTo(half + crossHalf, -trackOffset),
            new MoveTo(half - crossHalf, -trackOffset),
            new LineTo(half + crossHalf, trackOffset)
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
