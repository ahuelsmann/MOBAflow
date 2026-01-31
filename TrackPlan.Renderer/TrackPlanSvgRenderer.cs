namespace Moba.TrackPlan.Renderer;

using System.Globalization;
using System.Text;

using TrackLibrary.Base;
using TrackLibrary.PikoA;

/// <summary>
/// SVG-Renderer für TrackPlan-Visualisierung.
/// 
/// Konvertiert ein TrackPlanResult mit Gleissegmenten in skalierbare SVG-Grafik.
/// 
/// Features:
/// - Nutzt Port-Verbindungen für korrekte Segment-Verkettung
/// - Automatische Bestimmung von Entry-Ports basierend auf Verbindungen
/// - Automatische Berechnung des Zeichnungsbereichs basierend auf echtem Inhalt
/// - Unterstützung beliebiger Start-Winkel (0°, 90°, 180°, 270°)
/// - Ports als senkrechte Striche (im rechten Winkel zur Fahrtrichtung): schwarz=A, rot=B, grün=C
/// - Responsive SVG mit viewBox für automatische Skalierung
/// - 50px Padding um alle Elemente
/// </summary>
public class TrackPlanSvgRenderer
{
    private readonly StringBuilder _svg = new();
    private double _minX = double.MaxValue;
    private double _minY = double.MaxValue;
    private double _maxX = double.MinValue;
    private double _maxY = double.MinValue;

    /// <summary>
    /// Rendert einen TrackPlan in SVG-Format.
    /// 
    /// Prozess:
    /// 1. Erstellt eine Rendering-Queue mit dem ersten Segment
    /// 2. Verarbeitet Segmente in logischer Verkettungsreihenfolge (Depth-First)
    /// 3. Für jedes Segment: findet eingehende Verbindung → bestimmt Entry-Port
    /// 4. Ruft spezifischen Renderer auf (RenderWR, RenderR9, etc.)
    /// 5. Fügt nachfolgende Segmente zur Queue hinzu
    /// 6. Sammelt Bounds während Rendering
    /// 7. Generiert finales SVG mit viewBox basierend auf Bounds
    /// </summary>
    /// <param name="trackPlan">Das zu rendernde TrackPlanResult</param>
    /// <returns>SVG-String (W3C-Standard)</returns>
    public string Render(TrackPlanResult trackPlan)
    {
        _svg.Clear();
        _minX = double.MaxValue;
        _minY = double.MaxValue;
        _maxX = double.MinValue;
        _maxY = double.MinValue;

        // Finde das erste Segment (keinen eingehenden Connection)
        var firstSegment = FindFirstSegment(trackPlan);
        if (firstSegment == null && trackPlan.Segments.Any())
        {
            firstSegment = trackPlan.Segments.First();
        }

        if (firstSegment == null)
        {
            return "<svg></svg>";
        }

        // Rendering starten bei Ursprung mit konfiguriertem Winkel
        double currentX = 0;
        double currentY = 0;
        double currentAngle = trackPlan.StartAngleDegrees;
        var renderedSegments = new HashSet<Guid>();

        RenderSegmentRecursive(firstSegment, null, currentX, currentY, currentAngle, trackPlan, renderedSegments);

        return BuildSvg();
    }

    /// <summary>
    /// Findet das erste Segment: eines ohne eingehende Verbindung.
    /// Falls alle Segmente verbunden sind, wird das erste zurückgegeben.
    /// </summary>
    private Segment? FindFirstSegment(TrackPlanResult trackPlan)
    {
        var segmentsWithIncoming = new HashSet<Guid>();
        foreach (var conn in trackPlan.Connections)
        {
            segmentsWithIncoming.Add(conn.TargetSegment);
        }

        return trackPlan.Segments.FirstOrDefault(s => !segmentsWithIncoming.Contains(s.No));
    }

    /// <summary>
    /// Rekursives Rendering von Segmenten basierend auf Verbindungen.
    /// </summary>
    private void RenderSegmentRecursive(Segment segment, PortConnection? incomingConnection, double x, double y, double angle, TrackPlanResult trackPlan, HashSet<Guid> renderedSegments)
    {
        if (renderedSegments.Contains(segment.No))
        {
            return;
        }

        renderedSegments.Add(segment.No);

        // Bestimme Entry-Port basierend auf eingehender Verbindung
        char entryPort = 'A'; // Default
        if (incomingConnection != null)
        {
            // Die Target-Port der Verbindung ist unsere Entry-Port
            entryPort = ExtractPortChar(incomingConnection.TargetPort);
        }

        double nextX = x;
        double nextY = y;
        double nextAngle = angle;

        // Rendera dieses Segment
        if (segment is WR wr)
        {
            RenderWR(wr, ref nextX, ref nextY, ref nextAngle);
        }
        else if (segment is R9 r9)
        {
            RenderR9(r9, entryPort, ref nextX, ref nextY, ref nextAngle);
        }
        else if (segment is R1 r1)
        {
            RenderR1(r1, entryPort, ref nextX, ref nextY, ref nextAngle);
        }
        else if (segment is R2 r2)
        {
            RenderR2(r2, entryPort, ref nextX, ref nextY, ref nextAngle);
        }
        else if (segment is R3 r3)
        {
            RenderR3(r3, entryPort, ref nextX, ref nextY, ref nextAngle);
        }
        else if (segment is R4 r4)
        {
            RenderR4(r4, entryPort, ref nextX, ref nextY, ref nextAngle);
        }
        else if (segment is G239 g239)
        {
            RenderG239(g239, entryPort, ref nextX, ref nextY, ref nextAngle);
        }
        else if (segment is G231 g231)
        {
            RenderG231(g231, entryPort, ref nextX, ref nextY, ref nextAngle);
        }
        else if (segment is G62 g62)
        {
            RenderG62(g62, entryPort, ref nextX, ref nextY, ref nextAngle);
        }
        else
        {
            // Weitere Gleistypen hier hinzufügen
        }

        // Finde alle ausgehenden Verbindungen von diesem Segment
        var outgoingConnections = trackPlan.Connections
            .Where(c => c.SourceSegment == segment.No)
            .ToList();

        // Rendere alle nachfolgenden Segmente
        foreach (var outgoing in outgoingConnections)
        {
            var nextSegment = trackPlan.Segments.FirstOrDefault(s => s.No == outgoing.TargetSegment);
            if (nextSegment != null && !renderedSegments.Contains(nextSegment.No))
            {
                // Bestimme neue Position/Winkel basierend auf Ausgangs-Port
                var outgoingPort = ExtractPortChar(outgoing.SourcePort);

                // Berechne Position für dieses Segment aus dem Ausgangs-Port
                // Für alle Ports außer dem aktuellen Haupt-Ausgang: neue Rendering-Position berechnen
                double branchX = nextX;
                double branchY = nextY;
                double branchAngle = nextAngle;

                if (segment is WR wrSegment)
                {
                    CalculateWRPortPosition(wrSegment, outgoingPort, x, y, angle, out branchX, out branchY, out branchAngle);
                }
                else if (segment is R9)
                {
                    // Für R9: Standard-Ausgang ist Port B
                    branchX = nextX;
                    branchY = nextY;
                    branchAngle = nextAngle;
                }

                RenderSegmentRecursive(nextSegment, outgoing, branchX, branchY, branchAngle, trackPlan, renderedSegments);
            }
        }
    }

    /// <summary>
    /// Extrahiert den Port-Character aus einem Property-Namen (z.B. "PortA" → 'A').
    /// </summary>
    private char ExtractPortChar(string portProperty)
    {
        return portProperty.Last();
    }

    /// <summary>
    /// Berechnet die Ausgangsposition für einen bestimmten Port der WR.
    /// </summary>
    private void CalculateWRPortPosition(WR wr, char outgoingPort, double x, double y, double angle, out double outX, out double outY, out double outAngle)
    {
        var straightLength = wr.LengthInMm;
        var radius = wr.RadiusInMm;
        var arcDegree = wr.ArcInDegree;

        outX = x;
        outY = y;
        outAngle = angle;

        if (outgoingPort == 'A')
        {
            // Port A: Rückwärts, Winkel + 180°
            outX = x;
            outY = y;
            outAngle = angle + 180;
        }
        else if (outgoingPort == 'B')
        {
            // Port B: Gerade nach vorne, am Ende der Geraden
            outX = x + straightLength * Math.Cos(angle * Math.PI / 180);
            outY = y + straightLength * Math.Sin(angle * Math.PI / 180);
            outAngle = angle;
        }
        else if (outgoingPort == 'C')
        {
            // Port C: Ende der Kurve
            double centerAngle = angle + 90;
            double centerX = x + radius * Math.Cos(centerAngle * Math.PI / 180);
            double centerY = y + radius * Math.Sin(centerAngle * Math.PI / 180);

            double endAngle = angle + arcDegree;
            outX = centerX + radius * Math.Cos((endAngle - 90) * Math.PI / 180);
            outY = centerY + radius * Math.Sin((endAngle - 90) * Math.PI / 180);
            outAngle = endAngle;
        }
    }

    /// <summary>
    /// Rendert ein WR-Gleis (Weichenfernmeldegleis).
    /// Struktur:
    /// - Port A: Eingang (schwarzer Punkt)
    /// - Port B: Gerade (roter Punkt), Länge: 239mm
    /// - Port C: Kurve (grüner Punkt), Radius: 908mm, Winkel: 15°
    /// 
    /// Aktualisiert Position für Weiterzeichnen zum Port-B-Ende.
    /// </summary>
    private void RenderWR(WR wr, ref double x, ref double y, ref double angle)
    {
        var straightLength = wr.LengthInMm;
        var radius = wr.RadiusInMm;
        var arcDegree = wr.ArcInDegree;

        // Port A (Eingang) - physischer Port A (schwarz)
        double portAX = x;
        double portAY = y;
        DrawPortStroke(portAX, portAY, angle, GetPortColor('A'), 'A');
        UpdateBounds(portAX, portAY);

        // Port B (Gerade) - physischer Port B (rot) am Ende der Geraden
        double portBX = x + straightLength * Math.Cos(angle * Math.PI / 180);
        double portBY = y + straightLength * Math.Sin(angle * Math.PI / 180);

        // Gerade zeichnen (Port A -> Port B)
        _svg.AppendLine($"  <path d=\"M {portAX.ToString("F2", CultureInfo.InvariantCulture)},{portAY.ToString("F2", CultureInfo.InvariantCulture)} L {portBX.ToString("F2", CultureInfo.InvariantCulture)},{portBY.ToString("F2", CultureInfo.InvariantCulture)}\" " +
                       $"stroke=\"#333\" stroke-width=\"4\" fill=\"none\" />");

        DrawPortStroke(portBX, portBY, angle, GetPortColor('B'), 'B');
        UpdateBounds(portBX, portBY);

        // Port C (Kurve) - physischer Port C (grün) am Ende der Kurve
        double centerAngle = angle + 90;
        double centerX = x + radius * Math.Cos(centerAngle * Math.PI / 180);
        double centerY = y + radius * Math.Sin(centerAngle * Math.PI / 180);

        double endAngle = angle + arcDegree;
        double portCX = centerX + radius * Math.Cos((endAngle - 90) * Math.PI / 180);
        double portCY = centerY + radius * Math.Sin((endAngle - 90) * Math.PI / 180);

        // Kurve zeichnen (Port A -> Port C)
        int largeArc = arcDegree > 180 ? 1 : 0;
        int sweep = 1;

        _svg.AppendLine($"  <path d=\"M {portAX.ToString("F2", CultureInfo.InvariantCulture)},{portAY.ToString("F2", CultureInfo.InvariantCulture)} A {radius},{radius} 0 {largeArc},{sweep} {portCX.ToString("F2", CultureInfo.InvariantCulture)},{portCY.ToString("F2", CultureInfo.InvariantCulture)}\" " +
                       $"stroke=\"#333\" stroke-width=\"4\" fill=\"none\" />");

        DrawPortStroke(portCX, portCY, endAngle, GetPortColor('C'), 'C');
        UpdateBounds(portCX, portCY);

        // Position für nächstes Gleis aktualisieren
        x = portBX;
        y = portBY;
    }

    /// <summary>
    /// Rendert ein R9-Gleis (Kurvengleis).
    /// 
    /// Struktur:
    /// - Port A: Eingang (schwarzer Punkt)
    /// - Port B: Ausgang (roter Punkt)
    /// - Kreisbogen: Radius 908mm, Winkel 9°
    /// 
    /// Kurvenrichtung wird automatisch basierend auf Entry-Port angepasst:
    /// - Entry A: Kurve nach links (curveDirection = 1)
    /// - Entry B: Kurve nach rechts (curveDirection = -1)
    /// </summary>
    private void RenderR9(R9 r9, char entryPort, ref double x, ref double y, ref double angle)
    {
        var radius = r9.RadiusInMm;
        var arcDegree = r9.ArcInDegree;

        // Bei Port B-Eingang: Kurve in entgegengesetzte Richtung
        var curveDirection = entryPort == 'B' ? -1 : 1;

        // Kreisbogen zeichnen
        // Startpunkt
        double startX = x;
        double startY = y;

        // Mittelpunkt des Kreises (90° nach links vom aktuellen Winkel)
        double centerAngle = angle + (90 * curveDirection);
        double centerX = x + radius * Math.Cos(centerAngle * Math.PI / 180);
        double centerY = y + radius * Math.Sin(centerAngle * Math.PI / 180);

        // Endpunkt (nach Drehung um arcDegree)
        double endAngle = angle + (arcDegree * curveDirection);
        double endX = centerX + radius * Math.Cos((endAngle - (90 * curveDirection)) * Math.PI / 180);
        double endY = centerY + radius * Math.Sin((endAngle - (90 * curveDirection)) * Math.PI / 180);

        // Large arc flag: 0 für kleine Bögen (< 180°)
        int largeArc = arcDegree > 180 ? 1 : 0;

        // Sweep flag: abhängig von Kurvenrichtung
        int sweep = curveDirection == 1 ? 1 : 0;

        // SVG Path: M = MoveTo, A = Arc
        _svg.AppendLine($"  <path d=\"M {startX.ToString("F2", CultureInfo.InvariantCulture)},{startY.ToString("F2", CultureInfo.InvariantCulture)} A {radius},{radius} 0 {largeArc},{sweep} {endX.ToString("F2", CultureInfo.InvariantCulture)},{endY.ToString("F2", CultureInfo.InvariantCulture)}\" " +
                       $"stroke=\"#333\" stroke-width=\"4\" fill=\"none\" />");

        // Start-Port - physischer Port A oder B abhängig vom Entry
        char startPort = entryPort == 'A' ? 'A' : 'B';
        DrawPortStroke(startX, startY, angle, GetPortColor(startPort), startPort);

        // End-Port - physischer Port B oder A abhängig vom Entry
        char endPort = entryPort == 'A' ? 'B' : 'A';
        DrawPortStroke(endX, endY, endAngle, GetPortColor(endPort), endPort);

        // Bounding box aktualisieren
        UpdateBounds(startX, startY);
        UpdateBounds(endX, endY);

        // Position für nächstes Gleis aktualisieren
        x = endX;
        y = endY;
        angle = endAngle;
    }

    /// <summary>
    /// Rendert ein R1-Gleis (Kurvengleis 30°, Radius 360mm).
    /// 
    /// Struktur:
    /// - Port A: Eingang (schwarzer Punkt)
    /// - Port B: Ausgang (roter Punkt)
    /// - Kreisbogen: Radius 360mm, Winkel 30°
    /// 
    /// Kurvenrichtung wird automatisch basierend auf Entry-Port angepasst.
    /// </summary>
    private void RenderR1(R1 r1, char entryPort, ref double x, ref double y, ref double angle)
    {
        var radius = r1.RadiusInMm;
        var arcDegree = r1.ArcInDegree;

        var curveDirection = entryPort == 'B' ? -1 : 1;

        double startX = x;
        double startY = y;

        double centerAngle = angle + (90 * curveDirection);
        double centerX = x + radius * Math.Cos(centerAngle * Math.PI / 180);
        double centerY = y + radius * Math.Sin(centerAngle * Math.PI / 180);

        double endAngle = angle + (arcDegree * curveDirection);
        double endX = centerX + radius * Math.Cos((endAngle - (90 * curveDirection)) * Math.PI / 180);
        double endY = centerY + radius * Math.Sin((endAngle - (90 * curveDirection)) * Math.PI / 180);

        int largeArc = arcDegree > 180 ? 1 : 0;
        int sweep = curveDirection == 1 ? 1 : 0;

        _svg.AppendLine($"  <path d=\"M {startX.ToString("F2", CultureInfo.InvariantCulture)},{startY.ToString("F2", CultureInfo.InvariantCulture)} A {radius},{radius} 0 {largeArc},{sweep} {endX.ToString("F2", CultureInfo.InvariantCulture)},{endY.ToString("F2", CultureInfo.InvariantCulture)}\" " +
                       $"stroke=\"#333\" stroke-width=\"4\" fill=\"none\" />");

        char startPort = entryPort == 'A' ? 'A' : 'B';
        DrawPortStroke(startX, startY, angle, GetPortColor(startPort), startPort);

        char endPort = entryPort == 'A' ? 'B' : 'A';
        DrawPortStroke(endX, endY, endAngle, GetPortColor(endPort), endPort);

        UpdateBounds(startX, startY);
        UpdateBounds(endX, endY);

        x = endX;
        y = endY;
        angle = endAngle;
    }

    /// <summary>
    /// Rendert ein R2-Gleis (Kurvengleis 30°, Radius 422mm).
    /// 
    /// Struktur:
    /// - Port A: Eingang (schwarzer Punkt)
    /// - Port B: Ausgang (roter Punkt)
    /// - Kreisbogen: Radius 422mm, Winkel 30°
    /// 
    /// Kurvenrichtung wird automatisch basierend auf Entry-Port angepasst.
    /// </summary>
    private void RenderR2(R2 r2, char entryPort, ref double x, ref double y, ref double angle)
    {
        var radius = r2.RadiusInMm;
        var arcDegree = r2.ArcInDegree;

        var curveDirection = entryPort == 'B' ? -1 : 1;

        double startX = x;
        double startY = y;

        double centerAngle = angle + (90 * curveDirection);
        double centerX = x + radius * Math.Cos(centerAngle * Math.PI / 180);
        double centerY = y + radius * Math.Sin(centerAngle * Math.PI / 180);

        double endAngle = angle + (arcDegree * curveDirection);
        double endX = centerX + radius * Math.Cos((endAngle - (90 * curveDirection)) * Math.PI / 180);
        double endY = centerY + radius * Math.Sin((endAngle - (90 * curveDirection)) * Math.PI / 180);

        int largeArc = arcDegree > 180 ? 1 : 0;
        int sweep = curveDirection == 1 ? 1 : 0;

        _svg.AppendLine($"  <path d=\"M {startX.ToString("F2", CultureInfo.InvariantCulture)},{startY.ToString("F2", CultureInfo.InvariantCulture)} A {radius},{radius} 0 {largeArc},{sweep} {endX.ToString("F2",CultureInfo.InvariantCulture)},{endY.ToString("F2", CultureInfo.InvariantCulture)}\" " +
                       $"stroke=\"#333\" stroke-width=\"4\" fill=\"none\" />");

        char startPort = entryPort == 'A' ? 'A' : 'B';
        DrawPortStroke(startX, startY, angle, GetPortColor(startPort), startPort);

        char endPort = entryPort == 'A' ? 'B' : 'A';
        DrawPortStroke(endX, endY, endAngle, GetPortColor(endPort), endPort);

        UpdateBounds(startX, startY);
        UpdateBounds(endX, endY);

        x = endX;
        y = endY;
        angle = endAngle;
    }

    /// <summary>
    /// Rendert ein R3-Gleis (Kurvengleis 30°, Radius 484mm).
    /// 
    /// Struktur:
    /// - Port A: Eingang (schwarzer Punkt)
    /// - Port B: Ausgang (roter Punkt)
    /// - Kreisbogen: Radius 484mm, Winkel 30°
    /// 
    /// Kurvenrichtung wird automatisch basierend auf Entry-Port angepasst.
    /// </summary>
    private void RenderR3(R3 r3, char entryPort, ref double x, ref double y, ref double angle)
    {
        var radius = r3.RadiusInMm;
        var arcDegree = r3.ArcInDegree;

        var curveDirection = entryPort == 'B' ? -1 : 1;

        double startX = x;
        double startY = y;

        double centerAngle = angle + (90 * curveDirection);
        double centerX = x + radius * Math.Cos(centerAngle * Math.PI / 180);
        double centerY = y + radius * Math.Sin(centerAngle * Math.PI / 180);

        double endAngle = angle + (arcDegree * curveDirection);
        double endX = centerX + radius * Math.Cos((endAngle - (90 * curveDirection)) * Math.PI / 180);
        double endY = centerY + radius * Math.Sin((endAngle - (90 * curveDirection)) * Math.PI / 180);

        int largeArc = arcDegree > 180 ? 1 : 0;
        int sweep = curveDirection == 1 ? 1 : 0;

        _svg.AppendLine($"  <path d=\"M {startX.ToString("F2", CultureInfo.InvariantCulture)},{startY.ToString("F2", CultureInfo.InvariantCulture)} A {radius},{radius} 0 {largeArc},{sweep} {endX.ToString("F2", CultureInfo.InvariantCulture)},{endY.ToString("F2", CultureInfo.InvariantCulture)}\" " +
                       $"stroke=\"#333\" stroke-width=\"4\" fill=\"none\" />");

        char startPort = entryPort == 'A' ? 'A' : 'B';
        DrawPortStroke(startX, startY, angle, GetPortColor(startPort), startPort);

        char endPort = entryPort == 'A' ? 'B' : 'A';
        DrawPortStroke(endX, endY, endAngle, GetPortColor(endPort), endPort);

        UpdateBounds(startX, startY);
        UpdateBounds(endX, endY);

        x = endX;
        y = endY;
        angle = endAngle;
    }

    /// <summary>
    /// Rendert ein R4-Gleis (Kurvengleis 30°, Radius 546mm).
    /// 
    /// Struktur:
    /// - Port A: Eingang (schwarzer Punkt)
    /// - Port B: Ausgang (roter Punkt)
    /// - Kreisbogen: Radius 546mm, Winkel 30°
    /// 
    /// Kurvenrichtung wird automatisch basierend auf Entry-Port angepasst.
    /// </summary>
    private void RenderR4(R4 r4, char entryPort, ref double x, ref double y, ref double angle)
    {
        var radius = r4.RadiusInMm;
        var arcDegree = r4.ArcInDegree;

        var curveDirection = entryPort == 'B' ? -1 : 1;

        double startX = x;
        double startY = y;

        double centerAngle = angle + (90 * curveDirection);
        double centerX = x + radius * Math.Cos(centerAngle * Math.PI / 180);
        double centerY = y + radius * Math.Sin(centerAngle * Math.PI / 180);

        double endAngle = angle + (arcDegree * curveDirection);
        double endX = centerX + radius * Math.Cos((endAngle - (90 * curveDirection)) * Math.PI / 180);
        double endY = centerY + radius * Math.Sin((endAngle - (90 * curveDirection)) * Math.PI / 180);

        int largeArc = arcDegree > 180 ? 1 : 0;
        int sweep = curveDirection == 1 ? 1 : 0;

        _svg.AppendLine($"  <path d=\"M {startX.ToString("F2", CultureInfo.InvariantCulture)},{startY.ToString("F2", CultureInfo.InvariantCulture)} A {radius},{radius} 0 {largeArc},{sweep} {endX.ToString("F2", CultureInfo.InvariantCulture)},{endY.ToString("F2", CultureInfo.InvariantCulture)}\" " +
                       $"stroke=\"#333\" stroke-width=\"4\" fill=\"none\" />");

        char startPort = entryPort == 'A' ? 'A' : 'B';
        DrawPortStroke(startX, startY, angle, GetPortColor(startPort), startPort);

        char endPort = entryPort == 'A' ? 'B' : 'A';
        DrawPortStroke(endX, endY, endAngle, GetPortColor(endPort), endPort);

        UpdateBounds(startX, startY);
        UpdateBounds(endX, endY);

        x = endX;
        y = endY;
        angle = endAngle;
    }

    /// <summary>
    /// Rendert ein G239-Gleis (Gerade 239mm).
    /// 
    /// Struktur:
    /// - Port A: Eingang (schwarzer Punkt)
    /// - Port B: Ausgang (roter Punkt)
    /// - Gerade: 239mm Länge
    /// 
    /// Aktualisiert Position für Weiterzeichnen zum Port B.
    /// </summary>
    private void RenderG239(G239 g239, char entryPort, ref double x, ref double y, ref double angle)
    {
        // Bei Entry-Port B: Gleis "umgekehrt"
        if (entryPort == 'B')
        {
            angle += 180;
        }

        // Port A (physischer Port A)
        double portAX = x;
        double portAY = y;

        // Port B (physischer Port B am Ende der Geraden)
        double portBX = x + g239.LengthInMm * Math.Cos(angle * Math.PI / 180);
        double portBY = y + g239.LengthInMm * Math.Sin(angle * Math.PI / 180);

        // Gerade zeichnen
        _svg.AppendLine($"  <path d=\"M {portAX.ToString("F2", CultureInfo.InvariantCulture)},{portAY.ToString("F2", CultureInfo.InvariantCulture)} L {portBX.ToString("F2", CultureInfo.InvariantCulture)},{portBY.ToString("F2", CultureInfo.InvariantCulture)}\" " +
                       $"stroke=\"#333\" stroke-width=\"4\" fill=\"none\" />");

        // Port A - physische Farbe schwarz
        DrawPortStroke(portAX, portAY, angle, GetPortColor('A'), 'A');

        // Port B - physische Farbe rot
        DrawPortStroke(portBX, portBY, angle, GetPortColor('B'), 'B');

        // Bounding box aktualisieren
        UpdateBounds(portAX, portAY);
        UpdateBounds(portBX, portBY);

        // Position für nächstes Gleis aktualisieren
        x = portBX;
        y = portBY;
    }

    /// <summary>
    /// Rendert ein G231-Gleis (Gerade 231mm).
    /// 
    /// Struktur:
    /// - Port A: Eingang (schwarzer Punkt)
    /// - Port B: Ausgang (roter Punkt)
    /// - Gerade: 231mm Länge
    /// 
    /// Aktualisiert Position für Weiterzeichnen zum Port B.
    /// </summary>
    private void RenderG231(G231 g231, char entryPort, ref double x, ref double y, ref double angle)
    {
        // Bei Entry-Port B: Gleis "umgekehrt"
        if (entryPort == 'B')
        {
            angle += 180;
        }

        // Port A (physischer Port A)
        double portAX = x;
        double portAY = y;

        // Port B (physischer Port B am Ende der Geraden)
        double portBX = x + g231.LengthInMm * Math.Cos(angle * Math.PI / 180);
        double portBY = y + g231.LengthInMm * Math.Sin(angle * Math.PI / 180);

        // Gerade zeichnen
        _svg.AppendLine($"  <path d=\"M {portAX.ToString("F2", CultureInfo.InvariantCulture)},{portAY.ToString("F2", CultureInfo.InvariantCulture)} L {portBX.ToString("F2", CultureInfo.InvariantCulture)},{portBY.ToString("F2", CultureInfo.InvariantCulture)}\" " +
                       $"stroke=\"#333\" stroke-width=\"4\" fill=\"none\" />");

        // Port A - physische Farbe schwarz
        DrawPortStroke(portAX, portAY, angle, GetPortColor('A'), 'A');

        // Port B - physische Farbe rot
        DrawPortStroke(portBX, portBY, angle, GetPortColor('B'), 'B');

        // Bounding box aktualisieren
        UpdateBounds(portAX, portAY);
        UpdateBounds(portBX, portBY);

        // Position für nächstes Gleis aktualisieren
        x = portBX;
        y = portBY;
    }

    /// <summary>
    /// Rendert ein G62-Gleis (Gerade 62mm).
    /// 
    /// Struktur:
    /// - Port A: Eingang (schwarzer Punkt)
    /// - Port B: Ausgang (roter Punkt)
    /// - Gerade: 62mm Länge
    /// 
    /// Aktualisiert Position für Weiterzeichnen zum Port B.
    /// </summary>
    private void RenderG62(G62 g62, char entryPort, ref double x, ref double y, ref double angle)
    {
        // Bei Entry-Port B: Gleis "umgekehrt"
        if (entryPort == 'B')
        {
            angle += 180;
        }

        // Port A (physischer Port A)
        double portAX = x;
        double portAY = y;

        // Port B (physischer Port B am Ende der Geraden)
        double portBX = x + g62.LengthInMm * Math.Cos(angle * Math.PI / 180);
        double portBY = y + g62.LengthInMm * Math.Sin(angle * Math.PI / 180);

        // Gerade zeichnen
        _svg.AppendLine($"  <path d=\"M {portAX.ToString("F2", CultureInfo.InvariantCulture)},{portAY.ToString("F2", CultureInfo.InvariantCulture)} L {portBX.ToString("F2", CultureInfo.InvariantCulture)},{portBY.ToString("F2", CultureInfo.InvariantCulture)}\" " +
                       $"stroke=\"#333\" stroke-width=\"4\" fill=\"none\" />");

        // Port A - physische Farbe schwarz
        DrawPortStroke(portAX, portAY, angle, GetPortColor('A'), 'A');

        // Port B - physische Farbe rot
        DrawPortStroke(portBX, portBY, angle, GetPortColor('B'), 'B');

        // Bounding box aktualisieren
        UpdateBounds(portAX, portAY);
        UpdateBounds(portBX, portBY);

        // Position für nächstes Gleis aktualisieren
        x = portBX;
        y = portBY;
    }

    /// <summary>
    /// Aktualisiert Bounding-Box (min/max Koordinaten) für SVG viewBox-Berechnung.
    /// </summary>
    private void UpdateBounds(double x, double y)
    {
        _minX = Math.Min(_minX, x);
        _minY = Math.Min(_minY, y);
        _maxX = Math.Max(_maxX, x);
        _maxY = Math.Max(_maxY, y);
    }

    /// <summary>
    /// Finalisiert SVG basierend auf während Rendering gesammelten Bounds.
    /// 
    /// - Berechnet Breite/Höhe aus Bounds
    /// - Fügt 50px Margin hinzu
    /// - Generiert viewBox für responsive Skalierung
    /// - Wraps alle SVG-Inhalte in &lt;svg&gt;-Tag
    /// </summary>
    /// <returns>Komplettes SVG-Dokument als String</returns>
    private string BuildSvg()
    {
        // Margin hinzufügen
        double margin = 50;
        double width = _maxX - _minX + 2 * margin;
        double height = _maxY - _minY + 2 * margin;

        // viewBox: x, y, width, height (mit den originalen Koordinaten)
        double viewBoxX = _minX - margin;
        double viewBoxY = _minY - margin;
        double viewBoxWidth = width;
        double viewBoxHeight = height;

        var result = new StringBuilder();
        result.AppendLine($"<svg width=\"{width:F0}\" height=\"{height:F0}\" viewBox=\"{viewBoxX:F0} {viewBoxY:F0} {viewBoxWidth:F0} {viewBoxHeight:F0}\" xmlns=\"http://www.w3.org/2000/svg\">");
        result.Append(_svg);
        result.AppendLine("</svg>");

        return result.ToString();
    }

    /// <summary>
    /// Zeichnet einen Port-Strich (senkrecht zum Endpunkt).
    /// Der Strich ist 20px lang und steht im rechten Winkel zur angegebenen Richtung.
    /// </summary>
    private void DrawPortStroke(double x, double y, double angle, string color, char portLabel)
    {
        const double strokeLength = 20;
        
        // Senkrecht zum Winkel = Winkel + 90°
        double perpAngle = angle + 90;
        
        // Start und End des Strichs (zentriert auf dem Port-Punkt)
        double x1 = x - (strokeLength / 2) * Math.Cos(perpAngle * Math.PI / 180);
        double y1 = y - (strokeLength / 2) * Math.Sin(perpAngle * Math.PI / 180);
        double x2 = x + (strokeLength / 2) * Math.Cos(perpAngle * Math.PI / 180);
        double y2 = y + (strokeLength / 2) * Math.Sin(perpAngle * Math.PI / 180);

        // Strich zeichnen
        _svg.AppendLine($"  <line x1=\"{x1.ToString("F2", CultureInfo.InvariantCulture)}\" y1=\"{y1.ToString("F2", CultureInfo.InvariantCulture)}\" " +
                       $"x2=\"{x2.ToString("F2", CultureInfo.InvariantCulture)}\" y2=\"{y2.ToString("F2", CultureInfo.InvariantCulture)}\" " +
                       $"stroke=\"{color}\" stroke-width=\"3\" />");

        // Label neben dem Strich
        double labelX = x + 25 * Math.Cos(perpAngle * Math.PI / 180);
        double labelY = y + 25 * Math.Sin(perpAngle * Math.PI / 180);
        _svg.AppendLine($"  <text x=\"{labelX.ToString("F2", CultureInfo.InvariantCulture)}\" y=\"{labelY.ToString("F2", CultureInfo.InvariantCulture)}\" " +
                       $"font-size=\"14\" font-weight=\"bold\" fill=\"{color}\" text-anchor=\"middle\" dominant-baseline=\"middle\">{portLabel}</text>");
    }

    /// <summary>
    /// Gibt die Standardfarbe für einen Port basierend auf seinem Namen zurück.
    /// A=schwarz, B=rot, C=grün, D=blau
    /// </summary>
    private string GetPortColor(char port) => port switch
    {
        'A' => "black",
        'B' => "red",
        'C' => "green",
        'D' => "blue",
        _ => "gray"
    };
}