// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.TrackPlan.Renderer;

using System.Globalization;
using System.Text;

using TrackLibrary.Base;
using TrackLibrary.PikoA;

/// <summary>
/// Result of <see cref="TrackPlanSvgRenderer.Render"/>: SVG string and placements for Win2D.
/// </summary>
public record RenderResult(string Svg, IReadOnlyList<PlacedSegment> Placements);

/// <summary>
/// SVG renderer for TrackPlan visualization.
/// 
/// Converts a TrackPlanResult with track segments into scalable SVG graphics.
/// 
/// Features:
/// - Uses port connections for correct segment chaining
/// - Automatic determination of entry ports based on connections
/// - Automatic calculation of drawing area based on actual content
/// - Support for arbitrary start angles (0°, 90°, 180°, 270°)
/// - Ports as perpendicular strokes (at right angle to direction): black=A, red=B, green=C
/// - Responsive SVG with viewBox for automatic scaling
/// - 50px padding around all elements
/// </summary>
public class TrackPlanSvgRenderer
{
    private readonly ISegmentGeometryProvider _geometryProvider;
    private readonly StringBuilder _svg = new();
    private readonly List<PlacedSegment> _placements = [];
    private readonly TrackPlanSvgBoundsTracker _bounds = new();
    private int _segmentIndex; // Counter for alternating color scheme

    public TrackPlanSvgRenderer() : this(PikoASegmentGeometryProvider.Instance)
    {
    }

    public TrackPlanSvgRenderer(ISegmentGeometryProvider geometryProvider)
    {
        ArgumentNullException.ThrowIfNull(geometryProvider);
        _geometryProvider = geometryProvider;
    }

    /// <summary>
    /// Renders a TrackPlan in SVG format and returns placements for Win2D.
    /// 
    /// Process:
    /// 1. Creates a rendering queue with the first segment
    /// 2. Processes segments in logical chaining order (depth-first)
    /// 3. For each segment: finds incoming connection → determines entry port
    /// 4. Collects placement (x, y, angle) for Win2D
    /// 5. Calls specific renderer (RenderWR, RenderR9, etc.)
    /// 6. Generates final SVG with viewBox based on bounds
    /// </summary>
    /// <param name="trackPlan">The TrackPlanResult to render</param>
    /// <returns>SVG string and placements (identical to SVG geometry)</returns>
    public RenderResult Render(TrackPlanResult trackPlan)
    {
        _svg.Clear();
        _placements.Clear();
        _bounds.Reset();

        var firstSegment = FindFirstSegment(trackPlan);
        if (firstSegment == null && trackPlan.Segments.Any())
            firstSegment = trackPlan.Segments.First();

        if (firstSegment == null)
            return new RenderResult("<svg></svg>", []);

        double currentX = 0;
        double currentY = 0;
        double currentAngle = trackPlan.StartAngleDegrees;
        var renderedSegments = new HashSet<Guid>();

        RenderSegmentRecursive(firstSegment, null, currentX, currentY, currentAngle, trackPlan, renderedSegments);

        return new RenderResult(BuildSvg(), _placements.ToList());
    }

    /// <summary>
    /// Finds the first segment: one without incoming connection.
    /// If all segments are connected, returns the first one.
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
    /// Recursive rendering of segments based on connections.
    /// </summary>
    private void RenderSegmentRecursive(Segment segment, PortConnection? incomingConnection, double x, double y, double angle, TrackPlanResult trackPlan, HashSet<Guid> renderedSegments)
    {
        if (renderedSegments.Contains(segment.No))
        {
            return;
        }

        renderedSegments.Add(segment.No);

        // Determine entry port based on incoming connection
        char entryPort = 'A'; // Default
        if (incomingConnection != null)
            entryPort = ExtractPortChar(incomingConnection.TargetPort);

        // Placement for Win2D (identical to SVG drawing position)
        var placed = CreatePlacement(segment, incomingConnection, x, y, angle);
        _placements.Add(placed);

        // Increment segment index for color scheme
        var currentSegmentIndex = _segmentIndex++;

        RenderSegment(placed, entryPort, currentSegmentIndex);

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
                // Determine new position/angle based on exit port
                GetOutgoingPortState(placed, outgoing.SourcePort, out var branchX, out var branchY, out var branchAngle);

                RenderSegmentRecursive(nextSegment, outgoing, branchX, branchY, branchAngle, trackPlan, renderedSegments);
            }
        }
    }

    /// <summary>
    /// Extracts the port character from a property name (e.g. "PortA" → 'A').
    /// </summary>
    private char ExtractPortChar(string portProperty)
    {
        return portProperty.Last();
    }

    private PlacedSegment CreatePlacement(Segment segment, PortConnection? incomingConnection, double x, double y, double angle)
    {
        if (incomingConnection == null)
            return new PlacedSegment(segment, x, y, NormalizeAngle(angle));

        var desiredOutwardAngle = NormalizeAngle(angle + 180);
        var (originX, originY, rotationDegrees) = _geometryProvider.GetPlacementForPort(segment, incomingConnection.TargetPort, x, y, desiredOutwardAngle);
        return new PlacedSegment(segment, originX, originY, rotationDegrees);
    }

    private void GetOutgoingPortState(PlacedSegment placed, string portName, out double x, out double y, out double angle)
    {
        var (worldX, worldY, _) = _geometryProvider.GetPortWorldPosition(placed, portName);
        x = worldX;
        y = worldY;
        angle = _geometryProvider.GetPortOutwardWorldAngleDegrees(placed, portName);
    }

    private static double NormalizeAngle(double angle)
    {
        while (angle >= 360)
            angle -= 360;
        while (angle < 0)
            angle += 360;
        return angle;
    }

    private void RenderSegment(PlacedSegment placed, char entryPort, int segmentIndex)
    {
        DrawSegmentPath(placed);

        foreach (var port in _geometryProvider.GetPorts(placed.Segment))
        {
            var portChar = ExtractPortChar(port.PortName);
            var (portX, portY, portAngle) = _geometryProvider.GetPortWorldPosition(placed, port.PortName);
            DrawPortStroke(
                portX,
                portY,
                portAngle,
                TrackPlanSvgPortColorScheme.GetPortColor(portChar, segmentIndex),
                portChar,
                portChar == entryPort);
            UpdateBounds(portX, portY);
        }
    }

    /// <summary>Draws a path with shared geometry from SegmentLocalPathBuilder.</summary>
    private void DrawSegmentPath(PlacedSegment placed)
    {
        var path = _geometryProvider.GetPath(placed.Segment);
        var svgPath = PathToSvgConverter.ToSvgPath(path, placed.X, placed.Y, placed.RotationDegrees);
        _svg.AppendLine($"  <path d=\"{svgPath}\" stroke=\"#333\" stroke-width=\"4\" fill=\"none\" />");
    }

    /// <summary>
    /// Renders a WR track (switch remote indication track).
    /// Structure:
    /// - Port A: Entry (black dot)
    /// - Port B: Straight (red dot), length: 239mm
    /// - Port C: Curve (green dot), radius: 908mm, angle: 15°
    /// 
    /// Updates position for continuing drawing to Port B end.
    /// </summary>
    private (double X, double Y, double Angle) RenderWr(PlacedSegment placed, int segmentIndex)
    {
        // Port A (entry) - physical port A (black)
        double portAx = placed.X;
        double portAy = placed.Y;
        DrawPortStroke(portAx, portAy, placed.RotationDegrees, TrackPlanSvgPortColorScheme.GetPortColor('A', segmentIndex), 'A', true);
        UpdateBounds(portAx, portAy);

        // Port B (Gerade) - physischer Port B (rot) am Ende der Geraden
        var (portBx, portBy, portBAngle) = SegmentPortGeometry.GetPortWorldPosition(placed, "PortB");

        DrawSegmentPath(placed);

        DrawPortStroke(portBx, portBy, portBAngle, TrackPlanSvgPortColorScheme.GetPortColor('B', segmentIndex), 'B', false);
        UpdateBounds(portBx, portBy);

        // Port C (Kurve) - physischer Port C (grün) am Ende der Kurve
        var (portCx, portCy, portCAngle) = SegmentPortGeometry.GetPortWorldPosition(placed, "PortC");

        DrawPortStroke(portCx, portCy, portCAngle, TrackPlanSvgPortColorScheme.GetPortColor('C', segmentIndex), 'C', false);
        UpdateBounds(portCx, portCy);

        // Update position for next track
        return (portBx, portBy, SegmentPortGeometry.GetPortOutwardWorldAngleDegrees(placed, "PortB"));
    }

    /// <summary>
    /// Renders an R9 track (curved track).
    /// 
    /// Structure:
    /// - Port A: Entry (black dot)
    /// - Port B: Exit (red dot)
    /// - Arc: radius 908mm, angle 9°
    /// 
    /// Curve direction is automatically adjusted based on entry port:
    /// - Entry A: Kurve nach links (curveDirection = 1)
    /// - Entry B: Kurve nach rechts (curveDirection = -1)
    /// </summary>
    private (double X, double Y, double Angle) RenderR9(char entryPort, PlacedSegment placed, int segmentIndex)
    {
        return DrawTwoPortSegment(placed, entryPort, segmentIndex);
    }

    /// <summary>
    /// Renders an R1 track (curved track 30°, radius 360mm).
    /// 
    /// Structure:
    /// - Port A: Entry (black dot)
    /// - Port B: Exit (red dot)
    /// - Arc: radius 360mm, angle 30°
    /// 
    /// Curve direction is automatically adjusted based on entry port.
    /// </summary>
    private (double X, double Y, double Angle) RenderR1(char entryPort, PlacedSegment placed, int segmentIndex)
    {
        return DrawTwoPortSegment(placed, entryPort, segmentIndex);
    }

    /// <summary>
    /// Renders an R2 track (curved track 30°, radius 422mm).
    /// 
    /// Structure:
    /// - Port A: Entry (black dot)
    /// - Port B: Exit (red dot)
    /// - Arc: radius 422mm, angle 30°
    /// 
    /// Curve direction is automatically adjusted based on entry port.
    /// </summary>
    private (double X, double Y, double Angle) RenderR2(char entryPort, PlacedSegment placed, int segmentIndex)
    {
        return DrawTwoPortSegment(placed, entryPort, segmentIndex);
    }

    /// <summary>
    /// Renders an R3 track (curved track 30°, radius 484mm).
    /// 
    /// Structure:
    /// - Port A: Entry (black dot)
    /// - Port B: Exit (red dot)
    /// - Arc: radius 484mm, angle 30°
    /// 
    /// Curve direction is automatically adjusted based on entry port.
    /// </summary>
    private (double X, double Y, double Angle) RenderR3(char entryPort, PlacedSegment placed, int segmentIndex)
    {
        return DrawTwoPortSegment(placed, entryPort, segmentIndex);
    }

    /// <summary>
    /// Renders an R4 track (curved track 30°, radius 546mm).
    /// 
    /// Structure:
    /// - Port A: Entry (black dot)
    /// - Port B: Exit (red dot)
    /// - Arc: radius 546mm, angle 30°
    /// 
    /// Curve direction is automatically adjusted based on entry port.
    /// </summary>
    private (double X, double Y, double Angle) RenderR4(char entryPort, PlacedSegment placed, int segmentIndex)
    {
        return DrawTwoPortSegment(placed, entryPort, segmentIndex);
    }

    /// <summary>
    /// Renders a G239 track (straight 239mm).
    /// 
    /// Structure:
    /// - Port A: Entry (black dot)
    /// - Port B: Exit (red dot)
    /// - Straight: 239mm length
    /// 
    /// Updates position for continuing drawing to Port B.
    /// </summary>
    private (double X, double Y, double Angle) RenderG239(char entryPort, PlacedSegment placed, int segmentIndex)
    {
        return DrawTwoPortSegment(placed, entryPort, segmentIndex);
    }

    /// <summary>
    /// Renders a G231 track (straight 231mm).
    /// 
    /// Structure:
    /// - Port A: Entry (black dot)
    /// - Port B: Exit (red dot)
    /// - Straight: 231mm length
    /// 
    /// Updates position for continuing drawing to Port B.
    /// </summary>
    private (double X, double Y, double Angle) RenderG231(char entryPort, PlacedSegment placed, int segmentIndex)
    {
        return DrawTwoPortSegment(placed, entryPort, segmentIndex);
    }

    /// <summary>
    /// Renders a G62 track (straight 62mm).
    /// 
    /// Structure:
    /// - Port A: Entry (black dot)
    /// - Port B: Exit (red dot)
    /// - Straight: 62mm length
    /// 
    /// Updates position for continuing drawing to Port B.
    /// </summary>
    private (double X, double Y, double Angle) RenderG62(char entryPort, PlacedSegment placed, int segmentIndex)
    {
        return DrawTwoPortSegment(placed, entryPort, segmentIndex);
    }

    private (double X, double Y, double Angle) DrawTwoPortSegment(PlacedSegment placed, char entryPort, int segmentIndex)
    {
        var (portAx, portAy, _) = SegmentPortGeometry.GetPortWorldPosition(placed, "PortA");
        var (portBx, portBy, _) = SegmentPortGeometry.GetPortWorldPosition(placed, "PortB");
        var portAOutwardAngle = SegmentPortGeometry.GetPortOutwardWorldAngleDegrees(placed, "PortA");
        var portBOutwardAngle = SegmentPortGeometry.GetPortOutwardWorldAngleDegrees(placed, "PortB");
        var portAStrokeAngle = entryPort == 'A' ? NormalizeAngle(portAOutwardAngle + 180) : portAOutwardAngle;
        var portBStrokeAngle = entryPort == 'B' ? NormalizeAngle(portBOutwardAngle + 180) : portBOutwardAngle;

        DrawSegmentPath(placed);

        DrawPortStroke(portAx, portAy, portAStrokeAngle, TrackPlanSvgPortColorScheme.GetPortColor('A', segmentIndex), 'A', entryPort == 'A');
        DrawPortStroke(portBx, portBy, portBStrokeAngle, TrackPlanSvgPortColorScheme.GetPortColor('B', segmentIndex), 'B', entryPort == 'B');

        UpdateBounds(portAx, portAy);
        UpdateBounds(portBx, portBy);

        return (portBx, portBy, portBOutwardAngle);
    }

    /// <summary>
    /// Updates bounding box (min/max coordinates) for SVG viewBox calculation.
    /// </summary>
    private void UpdateBounds(double x, double y) => _bounds.Include(x, y);

    /// <summary>
    /// Finalizes SVG based on bounds collected during rendering.
    /// 
    /// - Calculates width/height from bounds
    /// - Adds 50px margin
    /// - Generates viewBox for responsive scaling
    /// - Wraps all SVG content in &lt;svg&gt; tag
    /// </summary>
    /// <returns>Complete SVG document as string</returns>
    private string BuildSvg()
    {
        // Add margin
        double margin = 50;
        double width = _bounds.MaxX - _bounds.MinX + 2 * margin;
        double height = _bounds.MaxY - _bounds.MinY + 2 * margin;

        // viewBox: x, y, width, height (mit den originalen Koordinaten)
        double viewBoxX = _bounds.MinX - margin;
        double viewBoxY = _bounds.MinY - margin;
        double viewBoxWidth = width;
        double viewBoxHeight = height;

        var result = new StringBuilder();
        result.AppendLine($"<svg width=\"{width:F0}\" height=\"{height:F0}\" viewBox=\"{viewBoxX:F0} {viewBoxY:F0} {viewBoxWidth:F0} {viewBoxHeight:F0}\" xmlns=\"http://www.w3.org/2000/svg\">");
        result.Append(_svg);
        result.AppendLine("</svg>");

        return result.ToString();
    }

    /// <summary>
    /// Draws a port stroke (perpendicular to endpoint) with overlap-free positioning.
    /// The stroke is 20px long and at right angles to the given direction.
    /// 
    /// Positioning at connections (e.g. A-R9-B + B-R9-A):
    /// - Exit port (isEntry=false): stroke -2px before connection point (backwards in travel direction)
    /// - Entry port (isEntry=true): stroke +2px after connection point (forwards in travel direction)
    /// 
    /// This prevents strokes from overlapping at connections (1-2px gap).
    /// </summary>
    /// <param name="x">Port X coordinate (connection point)</param>
    /// <param name="y">Port Y coordinate (connection point)</param>
    /// <param name="angle">Travel direction (in degrees)</param>
    /// <param name="color">Stroke color</param>
    /// <param name="portLabel">Port label (A/B/C/D)</param>
    /// <param name="isEntry">True if entry port (+2px forwards), False if exit port (-2px back)</param>
    private void DrawPortStroke(double x, double y, double angle, string color, char portLabel, bool isEntry)
    {
        const double strokeLength = 20;
        const double gap = 1; // 1px Abstand vom Verbindungspunkt (kante-an-kante)
        const double labelOffsetParallel = 8; // 8px Abstand entlang Fahrtrichtung
        const double labelOffsetPerpendicular = 12; // 12px Abstand senkrecht (ober/unterhalb Gleis)

        // Senkrecht zum Winkel = Winkel + 90°
        double perpAngle = angle + 90;

        // Strich-Positionierung: ±1px vom Verbindungspunkt
        // Exit: -1px back (in direction of travel), Entry: +1px forward (in direction of travel)
        double offset = isEntry ? gap : -gap;

        // Versetzung entlang der Fahrtrichtung
        double baseOffsetX = offset * Math.Cos(angle * Math.PI / 180);
        double baseOffsetY = offset * Math.Sin(angle * Math.PI / 180);

        // Start und End des Strichs (senkrecht zur Fahrtrichtung)
        double x1 = x + baseOffsetX - (strokeLength / 2) * Math.Cos(perpAngle * Math.PI / 180);
        double y1 = y + baseOffsetY - (strokeLength / 2) * Math.Sin(perpAngle * Math.PI / 180);
        double x2 = x + baseOffsetX + (strokeLength / 2) * Math.Cos(perpAngle * Math.PI / 180);
        double y2 = y + baseOffsetY + (strokeLength / 2) * Math.Sin(perpAngle * Math.PI / 180);

        // Strich zeichnen
        _svg.AppendLine($"  <line x1=\"{x1.ToString("F2", CultureInfo.InvariantCulture)}\" y1=\"{y1.ToString("F2", CultureInfo.InvariantCulture)}\" " +
                       $"x2=\"{x2.ToString("F2", CultureInfo.InvariantCulture)}\" y2=\"{y2.ToString("F2", CultureInfo.InvariantCulture)}\" " +
                       $"stroke=\"{color}\" stroke-width=\"2\" />");

        // Label positioning: BOTH along (±8px) AND perpendicular (12px) offset
        // Exit: -8px back, Entry: +8px forward (along direction of travel)
        double labelParallel = isEntry ? labelOffsetParallel : -labelOffsetParallel;
        double labelBaseX = x + labelParallel * Math.Cos(angle * Math.PI / 180);
        double labelBaseY = y + labelParallel * Math.Sin(angle * Math.PI / 180);

        // ADDITIONAL: 12px perpendicular offset (above/below track)
        double labelX = labelBaseX + labelOffsetPerpendicular * Math.Cos(perpAngle * Math.PI / 180);
        double labelY = labelBaseY + labelOffsetPerpendicular * Math.Sin(perpAngle * Math.PI / 180);

        _svg.AppendLine($"  <text x=\"{labelX.ToString("F2", CultureInfo.InvariantCulture)}\" y=\"{labelY.ToString("F2", CultureInfo.InvariantCulture)}\" " +
                       $"font-size=\"14\" font-weight=\"bold\" fill=\"{color}\" text-anchor=\"middle\" dominant-baseline=\"middle\">{portLabel}</text>");
    }
}