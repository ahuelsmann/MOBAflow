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
    /// 1. Startet bei Koordinate (0,0) mit konfiguriertem Winkel
    /// 2. Iteriert über alle Segmente und ruft spezifische Renderer auf
    /// 3. Bestimmt automatisch Entry-Port basierend auf Segment-Verbindungen
    /// 4. Sammelt Bounds während Rendering
    /// 5. Generiert finales SVG mit viewBox basierend auf Bounds
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

        // Start bei Ursprung (0,0) mit konfigurierbarem Winkel
        double currentX = 0;
        double currentY = 0;
        double currentAngle = trackPlan.StartAngleDegrees; // Grad, 0 = rechts
        Segment? previousSegment = null;

        foreach (var segment in trackPlan.Segments)
        {
            if (segment is WR wr)
            {
                RenderWR(wr, ref currentX, ref currentY, ref currentAngle);
                previousSegment = segment;
            }
            else if (segment is R9 r9)
            {
                // Entry-Port wird automatisch bestimmt:
                // Falls vorheriges Segment vom Typ Curved ist und auf Port B endet -> Entry ist B
                char entryPort = 'A'; // Default

                if (previousSegment is Curved prevCurved && previousSegment is not null)
                {
                    // Welcher Port des vorherigen Segments verbindet mit diesem Segment?
                    if (prevCurved.PortB == r9.No)
                    {
                        // Der Eingang ist Port B (das Ende des vorherigen Segments)
                        entryPort = 'B';
                    }
                }

                RenderR9(r9, entryPort, ref currentX, ref currentY, ref currentAngle);
                previousSegment = segment;
            }
            // Weitere Gleistypen hier hinzufügen
        }

        return BuildSvg();
    }

    /// <summary>
    /// Rendert ein WR-Gleis (Weichenfernmeldegleis).
    /// 
    /// Struktur:
    /// - Port A: Eingang (schwarzer Punkt)
    /// - Port B: Gerade (rotes Punkt), Länge: 239mm
    /// - Port C: Kurve (grüner Punkt), Radius: 908mm, Winkel: 15°
    /// 
    /// Aktualisiert Position für Weiterzeichnen zum Port-B-Ende.
    /// </summary>
    private void RenderWR(WR wr, ref double x, ref double y, ref double angle)
    {
        var straightLength = wr.LengthInMm; // 239mm - Gerade
        var radius = wr.RadiusInMm; // 908mm
        var arcDegree = wr.ArcInDegree; // 15°

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

        // Port C (Kurve) - grüner Punkt am Ende der R9-Kurve
        double centerAngle = angle + 90; // 90° nach links vom aktuellen Winkel
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

        // Für Weiterzeichnen: Position und Winkel nach Port B setzen (längere Gerade ist Standard)
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
    /// - Kreisbogen: Radius 954mm, Winkel 9°
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

        // Port A am Start
        _svg.AppendLine($"  <circle cx=\"{startX.ToString("F2", CultureInfo.InvariantCulture)}\" cy=\"{startY.ToString("F2", CultureInfo.InvariantCulture)}\" r=\"10\" fill=\"black\" />");

        // Label für Port A - ÜBER dem schwarzen Punkt
        double labelAY = startY - 30; // 30px über dem Port-Mittelpunkt
        _svg.AppendLine($"  <text x=\"{startX.ToString("F2", CultureInfo.InvariantCulture)}\" y=\"{labelAY.ToString("F2", CultureInfo.InvariantCulture)}\" font-size=\"16\" font-weight=\"bold\" fill=\"black\" text-anchor=\"middle\" dominant-baseline=\"middle\">A</text>");

        // Port B am Ende - roter Punkt
        _svg.AppendLine($"  <circle cx=\"{endX.ToString("F2", CultureInfo.InvariantCulture)}\" cy=\"{endY.ToString("F2", CultureInfo.InvariantCulture)}\" r=\"10\" fill=\"red\" />");

        // Label für Port B - ÜBER dem roten Punkt
        double labelBY = endY - 30; // 30px über dem Port-Mittelpunkt
        _svg.AppendLine($"  <text x=\"{endX.ToString("F2", CultureInfo.InvariantCulture)}\" y=\"{labelBY.ToString("F2", CultureInfo.InvariantCulture)}\" font-size=\"16\" font-weight=\"bold\" fill=\"red\" text-anchor=\"middle\" dominant-baseline=\"middle\">B</text>");

        // Bounding box aktualisieren
        UpdateBounds(startX, startY);
        UpdateBounds(endX, endY);
        UpdateBounds(startX, labelAY);
        UpdateBounds(endX, labelBY);

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