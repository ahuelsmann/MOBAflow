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
/// - Ports als farbliche Punkte: schwarz=A, rot=B, grün=C
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
            // Port B: Gerade nach vorne
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

        // Port A (Eingang) - schwarzer Punkt
        double portAX = x;
        double portAY = y;
        _svg.AppendLine($"  <circle cx=\"{portAX.ToString("F2", CultureInfo.InvariantCulture)}\" cy=\"{portAY.ToString("F2", CultureInfo.InvariantCulture)}\" r=\"10\" fill=\"black\" />");
        _svg.AppendLine($"  <text x=\"{portAX.ToString("F2", CultureInfo.InvariantCulture)}\" y=\"{(portAY - 30).ToString("F2", CultureInfo.InvariantCulture)}\" font-size=\"16\" font-weight=\"bold\" fill=\"black\" text-anchor=\"middle\" dominant-baseline=\"middle\">A</text>");

        // Port B (Gerade) - roter Punkt am Ende der Geraden
        double portBX = x + straightLength * Math.Cos(angle * Math.PI / 180);
        double portBY = y + straightLength * Math.Sin(angle * Math.PI / 180);

        // Gerade zeichnen (Port A -> Port B)
        _svg.AppendLine($"  <path d=\"M {portAX.ToString("F2", CultureInfo.InvariantCulture)},{portAY.ToString("F2", CultureInfo.InvariantCulture)} L {portBX.ToString("F2", CultureInfo.InvariantCulture)},{portBY.ToString("F2", CultureInfo.InvariantCulture)}\" " +
                       $"stroke=\"#333\" stroke-width=\"4\" fill=\"none\" />");

        _svg.AppendLine($"  <circle cx=\"{portBX.ToString("F2", CultureInfo.InvariantCulture)}\" cy=\"{portBY.ToString("F2", CultureInfo.InvariantCulture)}\" r=\"10\" fill=\"red\" />");
        _svg.AppendLine($"  <text x=\"{portBX.ToString("F2", CultureInfo.InvariantCulture)}\" y=\"{(portBY - 30).ToString("F2", CultureInfo.InvariantCulture)}\" font-size=\"16\" font-weight=\"bold\" fill=\"red\" text-anchor=\"middle\" dominant-baseline=\"middle\">B</text>");

        // Port C (Kurve) - grüner Punkt am Ende der Kurve
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

        _svg.AppendLine($"  <circle cx=\"{portCX.ToString("F2", CultureInfo.InvariantCulture)}\" cy=\"{portCY.ToString("F2", CultureInfo.InvariantCulture)}\" r=\"10\" fill=\"green\" />");
        _svg.AppendLine($"  <text x=\"{portCX.ToString("F2", CultureInfo.InvariantCulture)}\" y=\"{(portCY - 30).ToString("F2", CultureInfo.InvariantCulture)}\" font-size=\"16\" font-weight=\"bold\" fill=\"green\" text-anchor=\"middle\" dominant-baseline=\"middle\">C</text>");

        // Bounding box aktualisieren
        UpdateBounds(portAX, portAY);
        UpdateBounds(portBX, portBY);
        UpdateBounds(portCX, portCY);
        UpdateBounds(portAX, portAY - 30);
        UpdateBounds(portBX, portBY - 30);
        UpdateBounds(portCX, portCY - 30);

        // Für Weiterzeichnen: Position und Winkel nach Port B setzen
        x = portBX;
        y = portBY;
        angle = angle; // Winkel bleibt gleich
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

        // Port A/B-Label basierend auf Entry-Port
        // Wenn Entry A ist: Start ist Port A, Ende ist Port B
        // Wenn Entry B ist: Start ist Port B, Ende ist Port A
        char startPortLabel = entryPort == 'A' ? 'A' : 'B';
        char endPortLabel = entryPort == 'A' ? 'B' : 'A';

        // Start-Port
        var startColor = startPortLabel == 'A' ? "black" : "red";
        _svg.AppendLine($"  <circle cx=\"{startX.ToString("F2", CultureInfo.InvariantCulture)}\" cy=\"{startY.ToString("F2", CultureInfo.InvariantCulture)}\" r=\"10\" fill=\"{startColor}\" />");
        double labelStartY = startY - 30;
        _svg.AppendLine($"  <text x=\"{startX.ToString("F2", CultureInfo.InvariantCulture)}\" y=\"{labelStartY.ToString("F2", CultureInfo.InvariantCulture)}\" font-size=\"16\" font-weight=\"bold\" fill=\"{startColor}\" text-anchor=\"middle\" dominant-baseline=\"middle\">{startPortLabel}</text>");

        // End-Port
        var endColor = endPortLabel == 'A' ? "black" : "red";
        _svg.AppendLine($"  <circle cx=\"{endX.ToString("F2", CultureInfo.InvariantCulture)}\" cy=\"{endY.ToString("F2", CultureInfo.InvariantCulture)}\" r=\"10\" fill=\"{endColor}\" />");
        double labelEndY = endY - 30;
        _svg.AppendLine($"  <text x=\"{endX.ToString("F2", CultureInfo.InvariantCulture)}\" y=\"{labelEndY.ToString("F2", CultureInfo.InvariantCulture)}\" font-size=\"16\" font-weight=\"bold\" fill=\"{endColor}\" text-anchor=\"middle\" dominant-baseline=\"middle\">{endPortLabel}</text>");

        // Bounding box aktualisieren
        UpdateBounds(startX, startY);
        UpdateBounds(endX, endY);
        UpdateBounds(startX, labelStartY);
        UpdateBounds(endX, labelEndY);

        // Position für nächstes Gleis aktualisieren
        x = endX;
        y = endY;
        angle = endAngle;
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
}