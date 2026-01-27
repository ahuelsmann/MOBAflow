// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.TrackPlan.Renderer.Service;

using System.Globalization;
using System.Text;

/// <summary>
/// Exports geometry primitives to SVG format for visual debugging.
/// Useful for verifying geometry calculations without rendering in WinUI.
/// 
/// CONVENTION: Rails on top, sleepers (ties) on bottom.
/// This defines track orientation - sleepers indicate the "underside" of the track.
/// 
/// Usage:
///   var primitives = CurveGeometry.Render(start, angle, spec);
///   var svg = SvgExporter.Export(primitives, width: 800, height: 600);
///   File.WriteAllText("debug.svg", svg);
/// </summary>
public static class SvgExporter
{
    /// <summary>
    /// Track segment information for rendering with sleepers and ports.
    /// </summary>
    public sealed record TrackSegment(
        Guid Id,
        string TemplateId,
        Point2D Position,
        double RotationDeg,
        IReadOnlyList<IGeometryPrimitive> Primitives,
        IReadOnlyList<PortInfo> Ports);

    /// <summary>
    /// Port information for connection visualization.
    /// </summary>
    public sealed record PortInfo(
        string PortId,
        Point2D Position,
        double AngleDeg,
        Guid? ConnectedToEdgeId = null,
        string? ConnectedToPortId = null);

    /// <summary>
    /// Exports labeled track segments with parameters matching ExportWithLabels signature.
    /// </summary>
    public static string Export(
        IEnumerable<LabeledTrack> tracks,
        int width = 1400,
        int height = 1400,
        double scale = 0.35,
        double strokeWidth = 3,
        string strokeColor = "#333333",
        bool showLabels = true,
        bool showSeparators = true,
        bool showSegmentNumbers = true,
        bool showGrid = true,
        bool showOrigin = true,
        double gridSize = 100,
        double? centerOffsetX = null,
        double? centerOffsetY = null)
    {
        return ExportWithLabels(tracks, width, height, scale, strokeWidth, strokeColor,
            showLabels, showSeparators, showSegmentNumbers, showGrid, showOrigin, gridSize,
            centerOffsetX, centerOffsetY);
    }

    /// <summary>
    /// Exports primitives to an SVG string.
    /// </summary>
    /// <param name="primitives">Geometry primitives to render.</param>
    /// <param name="width">SVG canvas width in pixels.</param>
    /// <param name="height">SVG canvas height in pixels.</param>
    /// <param name="scale">Scale factor (mm to pixels).</param>
    /// <param name="strokeWidth">Line stroke width.</param>
    /// <param name="strokeColor">Line stroke color (CSS color).</param>
    /// <param name="showOrigin">Whether to show coordinate origin marker.</param>
    /// <param name="showGrid">Whether to show grid lines.</param>
    /// <param name="gridSize">Grid spacing in mm.</param>
    /// <param name="centerOffsetX">Center offset X (for positioning geometry in canvas).</param>
    /// <param name="centerOffsetY">Center offset Y (for positioning geometry in canvas).</param>
    public static string Export(
        IEnumerable<IGeometryPrimitive> primitives,
        int width = 800,
        int height = 600,
        double scale = 0.5,
        double strokeWidth = 3,
        string strokeColor = "#333333",
        bool showOrigin = true,
        bool showGrid = true,
        double gridSize = 100,
        double? centerOffsetX = null,
        double? centerOffsetY = null)
    {
        var sb = new StringBuilder();

        // ULTRA-SIMPLE: No viewBox tricks, no complex transforms
        // Just render geometry directly with offset
        sb.AppendLine($@"<svg xmlns=""http://www.w3.org/2000/svg"" width=""{width}"" height=""{height}"" viewBox=""0 0 {width} {height}"">");
        sb.AppendLine(@"  <style>");
        sb.AppendLine(@"    .track { fill: none; stroke-linecap: round; }");
        sb.AppendLine(@"    .grid { stroke: #e0e0e0; stroke-width: 0.5; }");
        sb.AppendLine(@"    .origin { stroke: #ff0000; stroke-width: 2; }");
        sb.AppendLine(@"  </style>");

        // Canvas center
        var centerX = width / 2.0;
        var centerY = height / 2.0;

        // Apply offsets if provided (in mm, converted to pixels)
        var offsetPixelX = (centerOffsetX ?? 0) * scale;
        var offsetPixelY = (centerOffsetY ?? 0) * scale;

        sb.AppendLine($@"  <g transform=""translate({F(centerX + offsetPixelX)}, {F(centerY - offsetPixelY)}) scale({F(scale)}, {F(-scale)})"">");

        // Grid
        if (showGrid)
        {
            sb.AppendLine(@"    <!-- Grid -->");
            var gridRange = 3000;
            for (double i = -gridRange; i <= gridRange; i += gridSize)
            {
                sb.AppendLine($@"    <line class=""grid"" x1=""{F(i)}"" y1=""{F(-gridRange)}"" x2=""{F(i)}"" y2=""{F(gridRange)}""/>");
                sb.AppendLine($@"    <line class=""grid"" x1=""{F(-gridRange)}"" y1=""{F(i)}"" x2=""{F(gridRange)}"" y2=""{F(i)}""/>");
            }
        }

        // Origin
        if (showOrigin)
        {
            sb.AppendLine(@"    <!-- Origin -->");
            sb.AppendLine($@"    <line class=""origin"" x1=""-100"" y1=""0"" x2=""100"" y2=""0""/>");
            sb.AppendLine($@"    <line class=""origin"" x1=""0"" y1=""-100"" x2=""0"" y2=""100""/>");
            sb.AppendLine($@"    <circle cx=""0"" cy=""0"" r=""10"" fill=""none"" stroke=""red"" stroke-width=""2""/>");
        }

        // Primitives (in geometry space, transformed by the group)
        sb.AppendLine(@"    <!-- Primitives -->");
        foreach (var primitive in primitives)
        {
            sb.AppendLine(RenderPrimitive(primitive, strokeWidth / scale, strokeColor));
        }

        sb.AppendLine(@"  </g>");
        sb.AppendLine(@"</svg>");

        return sb.ToString();
    }

    /// <summary>
    /// Exports primitives to an SVG file.
    /// </summary>
    public static void ExportToFile(
        IEnumerable<IGeometryPrimitive> primitives,
        string filePath,
        int width = 800,
        int height = 600,
        double scale = 0.5)
    {
        var svg = Export(primitives, width, height, scale);
        File.WriteAllText(filePath, svg);
    }

    /// <summary>
    /// Creates a debug SVG with labeled start/end points and centers.
    /// Useful for debugging curve and switch calculations.
    /// </summary>
    public static string ExportDebug(
        IEnumerable<IGeometryPrimitive> primitives,
        Point2D? startPoint = null,
        double? startAngle = null,
        int width = 800,
        int height = 600,
        double scale = 0.5)
    {
        var sb = new StringBuilder();

        sb.AppendLine($@"<svg xmlns=""http://www.w3.org/2000/svg"" width=""{width}"" height=""{height}"">");
        sb.AppendLine(@"  <style>");
        sb.AppendLine(@"    .track { fill: none; stroke: #333; stroke-width: 3; stroke-linecap: round; }");
        sb.AppendLine(@"    .debug-point { fill: blue; }");
        sb.AppendLine(@"    .debug-center { fill: green; }");
        sb.AppendLine(@"    .debug-line { stroke: #999; stroke-dasharray: 5,5; stroke-width: 1; }");
        sb.AppendLine(@"    .label { font-family: Arial; font-size: 12px; }");
        sb.AppendLine(@"  </style>");

        var offsetX = width / 2.0;
        var offsetY = height / 2.0;

        // Main transform group: Y-axis flip without affecting X-axis
        // SVG: positive Y goes DOWN by default, we want positive Y UP
        // Solution: Use scaleY only without scaleX, via matrix transform
        // matrix(a,b,c,d,e,f) = [a c e]
        //                        [b d f]
        //                        [0 0 1]
        // For scale: a=scaleX, d=scaleY; translate via e,f
        sb.AppendLine($@"  <g transform=""translate({F(offsetX)},{F(offsetY)}) scale({F(scale)}, {F(-scale)})"">");

        // Render primitives with debug info
        foreach (var primitive in primitives)
        {
            sb.AppendLine(RenderPrimitive(primitive, 3 / scale, "#333333"));

            if (primitive is ArcPrimitive arc)
            {
                // Draw center point
                sb.AppendLine($@"    <circle class=""debug-center"" cx=""{F(arc.Center.X)}"" cy=""{F(arc.Center.Y)}"" r=""{F(8 / scale)}""/>");

                // Draw radius line to start
                var startX = arc.Center.X + arc.Radius * Math.Cos(arc.StartAngleRad);
                var startY = arc.Center.Y + arc.Radius * Math.Sin(arc.StartAngleRad);
                sb.AppendLine($@"    <line class=""debug-line"" x1=""{F(arc.Center.X)}"" y1=""{F(arc.Center.Y)}"" x2=""{F(startX)}"" y2=""{F(startY)}""/>");

                // Draw radius line to end
                var endX = arc.Center.X + arc.Radius * Math.Cos(arc.StartAngleRad + arc.SweepAngleRad);
                var endY = arc.Center.Y + arc.Radius * Math.Sin(arc.StartAngleRad + arc.SweepAngleRad);
                sb.AppendLine($@"    <line class=""debug-line"" x1=""{F(arc.Center.X)}"" y1=""{F(arc.Center.Y)}"" x2=""{F(endX)}"" y2=""{F(endY)}""/>");
            }

            if (primitive is LinePrimitive line)
            {
                // Draw endpoints
                sb.AppendLine($@"    <circle class=""debug-point"" cx=""{F(line.From.X)}"" cy=""{F(line.From.Y)}"" r=""{F(5 / scale)}""/>");
                sb.AppendLine($@"    <circle class=""debug-point"" cx=""{F(line.To.X)}"" cy=""{F(line.To.Y)}"" r=""{F(5 / scale)}""/>");
            }
        }

        // Draw input start point if provided
        if (startPoint.HasValue)
        {
            sb.AppendLine($@"    <circle fill=""red"" cx=""{F(startPoint.Value.X)}"" cy=""{F(startPoint.Value.Y)}"" r=""{F(10 / scale)}""/>");

            if (startAngle.HasValue)
            {
                var angleRad = startAngle.Value * Math.PI / 180.0;
                var arrowLength = 50;
                var endX = startPoint.Value.X + arrowLength * Math.Cos(angleRad);
                var endY = startPoint.Value.Y + arrowLength * Math.Sin(angleRad);
                sb.AppendLine($@"    <line stroke=""red"" stroke-width=""{F(2 / scale)}"" x1=""{F(startPoint.Value.X)}"" y1=""{F(startPoint.Value.Y)}"" x2=""{F(endX)}"" y2=""{F(endY)}""/>");
            }
        }

        sb.AppendLine(@"  </g>");
        sb.AppendLine(@"</svg>");

        return sb.ToString();
    }

    private static string RenderPrimitive(IGeometryPrimitive primitive, double strokeWidth, string strokeColor)
    {
        return primitive switch
        {
            LinePrimitive line => RenderLine(line, strokeWidth, strokeColor),
            ArcPrimitive arc => RenderArc(arc, strokeWidth, strokeColor),
            _ => $"<!-- Unknown primitive type: {primitive.GetType().Name} -->"
        };
    }

    private static string RenderLine(LinePrimitive line, double strokeWidth, string strokeColor)
    {
        return $@"    <line class=""track"" stroke=""{strokeColor}"" stroke-width=""{F(strokeWidth)}"" x1=""{F(line.From.X)}"" y1=""{F(line.From.Y)}"" x2=""{F(line.To.X)}"" y2=""{F(line.To.Y)}""/>";
    }

    private static string RenderArc(ArcPrimitive arc, double strokeWidth, string strokeColor)
    {
        var startX = arc.Center.X + arc.Radius * Math.Cos(arc.StartAngleRad);
        var startY = arc.Center.Y + arc.Radius * Math.Sin(arc.StartAngleRad);
        var endX = arc.Center.X + arc.Radius * Math.Cos(arc.StartAngleRad + arc.SweepAngleRad);
        var endY = arc.Center.Y + arc.Radius * Math.Sin(arc.StartAngleRad + arc.SweepAngleRad);

        var largeArc = Math.Abs(arc.SweepAngleRad) > Math.PI ? 1 : 0;

        // Sweep-flag: Positiver Sweep-Winkel (CCW in Y-up) → sweep=1 (CW in SVG Y-down nach scale-Flip)
        // Negativer Sweep-Winkel (CW in Y-up) → sweep=0 (CCW in SVG Y-down nach scale-Flip)
        var sweep = arc.SweepAngleRad >= 0 ? 1 : 0;

        // SVG arc path: M startX,startY A rx,ry rotation large-arc-flag,sweep-flag endX,endY
        return $@"    <path class=""track"" stroke=""{strokeColor}"" stroke-width=""{F(strokeWidth)}"" d=""M {F(startX)},{F(startY)} A {F(arc.Radius)},{F(arc.Radius)} 0 {largeArc},{sweep} {F(endX)},{F(endY)}""/>";
    }

    /// <summary>
    /// Labeled track segment for SVG export with track codes and port information.
    /// </summary>
    public sealed record LabeledTrack(
        string Label,
        IReadOnlyList<IGeometryPrimitive> Primitives,
        Point2D StartPoint,
        Point2D EndPoint,
        string? StartPortLabel = null,
        string? EndPortLabel = null,
        Point2D? MiddlePoint = null,
        string? MiddlePortLabel = null,
        Point2D? ExtraPoint1 = null,
        string? ExtraPortLabel1 = null,
        Point2D? ExtraPoint2 = null,
        string? ExtraPortLabel2 = null);

    /// <summary>
    /// Exports track primitives with labels and separator marks between segments.
    /// 
    /// Features:
    /// - Track code labels above each segment (e.g., "R9", "BWL", "G239")
    /// - Separator marks (small circles) at segment boundaries
    /// - Segment numbering
    /// </summary>
    public static string ExportWithLabels(
        IEnumerable<LabeledTrack> tracks,
        int width = 1400,
        int height = 1400,
        double scale = 0.35,
        double strokeWidth = 3,
        string strokeColor = "#333333",
        bool showLabels = true,
        bool showSeparators = true,
        bool showSegmentNumbers = true,
        bool showGrid = true,
        bool showOrigin = true,
        double gridSize = 100,
        double? centerOffsetX = null,
        double? centerOffsetY = null)
    {
        var trackList = tracks.ToList();
        var sb = new StringBuilder();

        sb.AppendLine($@"<svg xmlns=""http://www.w3.org/2000/svg"" width=""{width}"" height=""{height}"" viewBox=""0 0 {width} {height}"">");
        sb.AppendLine(@"  <style>");
        sb.AppendLine(@"    .track { fill: none; stroke-linecap: round; }");
        sb.AppendLine(@"    .grid { stroke: #e8e8e8; stroke-width: 0.5; }");
        sb.AppendLine(@"    .origin { stroke: #ff0000; stroke-width: 2; }");
        sb.AppendLine(@"    .separator { fill: #ff6600; stroke: none; }");
        sb.AppendLine(@"    .track-label { font-family: 'Consolas', monospace; font-size: 11px; fill: #0066cc; font-weight: bold; }");
        sb.AppendLine(@"    .segment-num { font-family: Arial, sans-serif; font-size: 9px; fill: #999; }");
        sb.AppendLine(@"    .legend { font-family: Arial, sans-serif; font-size: 11px; fill: #555; }");
        sb.AppendLine(@"  </style>");

        // Use provided center offset or default to canvas center
        var offsetX = centerOffsetX ?? (width / 2.0);
        var offsetY = centerOffsetY ?? (height / 2.0);

        // Main transform group: Y-axis flip without affecting X-axis
        // SVG: positive Y goes DOWN by default, we want positive Y UP
        // Solution: Use scaleY only without scaleX, via matrix transform
        // matrix(a,b,c,d,e,f) = [a c e]
        //                        [b d f]
        //                        [0 0 1]
        // For scale: a=scaleX, d=scaleY; translate via e,f
        sb.AppendLine($@"  <g transform=""translate({F(offsetX)},{F(offsetY)}) scale({F(scale)}, {F(-scale)})"">");

        // Grid
        if (showGrid)
        {
            var gridExtent = Math.Max(width, height) / scale;
            sb.AppendLine(@"    <!-- Grid -->");
            for (double x = -gridExtent; x <= gridExtent; x += gridSize)
            {
                sb.AppendLine($@"    <line class=""grid"" x1=""{F(x)}"" y1=""{F(-gridExtent)}"" x2=""{F(x)}"" y2=""{F(gridExtent)}""/>");
            }
            for (double y = -gridExtent; y <= gridExtent; y += gridSize)
            {
                sb.AppendLine($@"    <line class=""grid"" x1=""{F(-gridExtent)}"" y1=""{F(y)}"" x2=""{F(gridExtent)}"" y2=""{F(y)}""/>");
            }
        }

        // Origin marker
        if (showOrigin)
        {
            sb.AppendLine(@"    <!-- Origin -->");
            sb.AppendLine($@"    <line class=""origin"" x1=""-50"" y1=""0"" x2=""50"" y2=""0""/>");
            sb.AppendLine($@"    <line class=""origin"" x1=""0"" y1=""-50"" x2=""0"" y2=""50""/>");
            sb.AppendLine($@"    <circle cx=""0"" cy=""0"" r=""8"" fill=""red""/>");
        }

        // Track primitives
        sb.AppendLine(@"    <!-- Tracks -->");
        foreach (var track in trackList)
        {
            foreach (var primitive in track.Primitives)
            {
                sb.AppendLine(RenderPrimitive(primitive, strokeWidth / scale, strokeColor));
            }
        }

        // Separators (small circles at segment boundaries)
        if (showSeparators)
        {
            sb.AppendLine(@"    <!-- Separators -->");
            var separatorRadius = 6 / scale;
            foreach (var track in trackList)
            {
                sb.AppendLine($@"    <circle class=""separator"" cx=""{F(track.StartPoint.X)}"" cy=""{F(track.StartPoint.Y)}"" r=""{F(separatorRadius)}""/>");
            }
        }

        sb.AppendLine(@"  </g>");

        // Labels (outside transform to keep text upright)
        if (showLabels || showSegmentNumbers)
        {
            sb.AppendLine(@"  <!-- Labels -->");
            int segmentNum = 1;
            foreach (var track in trackList)
            {
                // Calculate midpoint for label placement
                var midX = (track.StartPoint.X + track.EndPoint.X) / 2;
                var midY = (track.StartPoint.Y + track.EndPoint.Y) / 2;

                // Transform to screen coordinates
                var screenX = offsetX + midX * scale;
                var screenY = offsetY - midY * scale;  // Y is flipped

                // Offset label above the track
                var labelOffsetY = -15;

                if (showLabels)
                {
                    sb.AppendLine($@"  <text class=""track-label"" x=""{F(screenX)}"" y=""{F(screenY + labelOffsetY)}"" text-anchor=""middle"">{track.Label}</text>");
                }

                if (showSegmentNumbers)
                {
                    var numOffsetY = showLabels ? -28 : -15;
                    sb.AppendLine($@"  <text class=""segment-num"" x=""{F(screenX)}"" y=""{F(screenY + numOffsetY)}"" text-anchor=""middle"">#{segmentNum}</text>");
                }

                // Port labels at start/end points
                if (!string.IsNullOrEmpty(track.StartPortLabel))
                {
                    var startScreenX = offsetX + track.StartPoint.X * scale;
                    var startScreenY = offsetY - track.StartPoint.Y * scale;
                    sb.AppendLine($@"  <circle cx=""{F(startScreenX)}"" cy=""{F(startScreenY)}"" r=""5"" fill=""#cc0000"" stroke=""#990000"" stroke-width=""1""/>");
                    sb.AppendLine($@"  <text class=""track-label"" x=""{F(startScreenX)}"" y=""{F(startScreenY - 8)}"" text-anchor=""middle"" style=""fill: #cc0000; font-size: 10px;"">{track.StartPortLabel}</text>");
                }

                if (!string.IsNullOrEmpty(track.EndPortLabel))
                {
                    var endScreenX = offsetX + track.EndPoint.X * scale;
                    var endScreenY = offsetY - track.EndPoint.Y * scale;
                    sb.AppendLine($@"  <circle cx=""{F(endScreenX)}"" cy=""{F(endScreenY)}"" r=""5"" fill=""#cc0000"" stroke=""#990000"" stroke-width=""1""/>");
                    sb.AppendLine($@"  <text class=""track-label"" x=""{F(endScreenX)}"" y=""{F(endScreenY - 8)}"" text-anchor=""middle"" style=""fill: #cc0000; font-size: 10px;"">{track.EndPortLabel}</text>");
                }

                // Middle port label (for switches, e.g., Port C)
                if (track.MiddlePoint.HasValue && !string.IsNullOrEmpty(track.MiddlePortLabel))
                {
                    var midScreenX = offsetX + track.MiddlePoint.Value.X * scale;
                    var midScreenY = offsetY - track.MiddlePoint.Value.Y * scale;
                    sb.AppendLine($@"  <circle cx=""{F(midScreenX)}"" cy=""{F(midScreenY)}"" r=""5"" fill=""#ff8800"" stroke=""#cc6600"" stroke-width=""1""/>");
                    sb.AppendLine($@"  <text class=""track-label"" x=""{F(midScreenX)}"" y=""{F(midScreenY - 8)}"" text-anchor=""middle"" style=""fill: #ff8800; font-size: 10px;"">{track.MiddlePortLabel}</text>");
                }

                // Extra port labels (for W3 switches, e.g., Ports D and E)
                if (track.ExtraPoint1.HasValue && !string.IsNullOrEmpty(track.ExtraPortLabel1))
                {
                    var extra1ScreenX = offsetX + track.ExtraPoint1.Value.X * scale;
                    var extra1ScreenY = offsetY - track.ExtraPoint1.Value.Y * scale;
                    sb.AppendLine($@"  <circle cx=""{F(extra1ScreenX)}"" cy=""{F(extra1ScreenY)}"" r=""5"" fill=""#ff8800"" stroke=""#cc6600"" stroke-width=""1""/>");
                    sb.AppendLine($@"  <text class=""track-label"" x=""{F(extra1ScreenX)}"" y=""{F(extra1ScreenY - 8)}"" text-anchor=""middle"" style=""fill: #ff8800; font-size: 10px;"">{track.ExtraPortLabel1}</text>");
                }

                if (track.ExtraPoint2.HasValue && !string.IsNullOrEmpty(track.ExtraPortLabel2))
                {
                    var extra2ScreenX = offsetX + track.ExtraPoint2.Value.X * scale;
                    var extra2ScreenY = offsetY - track.ExtraPoint2.Value.Y * scale;
                    sb.AppendLine($@"  <circle cx=""{F(extra2ScreenX)}"" cy=""{F(extra2ScreenY)}"" r=""5"" fill=""#ff8800"" stroke=""#cc6600"" stroke-width=""1""/>");
                    sb.AppendLine($@"  <text class=""track-label"" x=""{F(extra2ScreenX)}"" y=""{F(extra2ScreenY - 8)}"" text-anchor=""middle"" style=""fill: #ff8800; font-size: 10px;"">{track.ExtraPortLabel2}</text>");
                }

                segmentNum++;
            }
        }

        // Legend
        sb.AppendLine(@"  <!-- Legend -->");
        sb.AppendLine($@"  <text class=""legend"" x=""10"" y=""20"">Track Plan - {trackList.Count} Segments</text>");

        // Track type summary
        var trackCounts = trackList.GroupBy(t => t.Label)
            .Select(g => $"{g.Key}×{g.Count()}")
            .ToList();
        sb.AppendLine($@"  <text class=""legend"" x=""10"" y=""35"">{string.Join(", ", trackCounts)}</text>");

        if (showSeparators)
        {
            sb.AppendLine($@"  <circle class=""separator"" cx=""15"" cy=""52"" r=""4""/>");
            sb.AppendLine($@"  <text class=""legend"" x=""25"" y=""56"">Segment boundary</text>");
        }

        sb.AppendLine($@"  <text class=""legend"" x=""10"" y=""{height - 10}"">Scale: 1mm = {scale}px</text>");

        sb.AppendLine(@"</svg>");

        return sb.ToString();
    }

    /// <summary>
    /// Formats a double for SVG (invariant culture, no trailing zeros).
    /// </summary>
    private static string F(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);

    /// <summary>
    /// Exports a complete track plan with sleepers, ports, and connections.
    /// 
    /// CONVENTION: Rails on top, sleepers on bottom (sleepers indicate track underside).
    /// </summary>
    /// <param name="segments">Track segments with geometry and port information.</param>
    /// <param name="width">SVG canvas width in pixels.</param>
    /// <param name="height">SVG canvas height in pixels.</param>
    /// <param name="scale">Scale factor (mm to pixels).</param>
    /// <param name="showSleepers">Whether to draw sleepers (ties) under the rails.</param>
    /// <param name="showPorts">Whether to show port markers (A, B, C) at track ends.</param>
    /// <param name="showConnections">Whether to draw connection lines between connected ports.</param>
    /// <param name="sleeperSpacing">Distance between sleepers in mm.</param>
    /// <param name="sleeperLength">Length of each sleeper in mm.</param>
    public static string ExportTrackPlan(
        IEnumerable<TrackSegment> segments,
        int width = 1200,
        int height = 900,
        double scale = 0.5,
        bool showSleepers = true,
        bool showPorts = true,
        bool showConnections = true,
        double sleeperSpacing = 25.0,
        double sleeperLength = 20.0)
    {
        var segmentList = segments.ToList();
        var sb = new StringBuilder();

        sb.AppendLine($@"<svg xmlns=""http://www.w3.org/2000/svg"" width=""{width}"" height=""{height}"" viewBox=""0 0 {width} {height}"">");
        sb.AppendLine(@"  <style>");
        sb.AppendLine(@"    .rail { fill: none; stroke: #4a4a4a; stroke-width: 3; stroke-linecap: round; }");
        sb.AppendLine(@"    .sleeper { stroke: #8B4513; stroke-width: 4; stroke-linecap: round; }");
        sb.AppendLine(@"    .port-open { fill: #ff6600; stroke: #cc5500; stroke-width: 1; }");
        sb.AppendLine(@"    .port-connected { fill: #00cc00; stroke: #009900; stroke-width: 1; }");
        sb.AppendLine(@"    .port-label { font-family: Arial, sans-serif; font-size: 10px; fill: #333; font-weight: bold; }");
        sb.AppendLine(@"    .connection { stroke: #00aa00; stroke-width: 2; stroke-dasharray: 4,2; }");
        sb.AppendLine(@"    .grid { stroke: #e8e8e8; stroke-width: 0.5; }");
        sb.AppendLine(@"    .legend { font-family: Arial, sans-serif; font-size: 11px; fill: #555; }");
        sb.AppendLine(@"  </style>");

        var offsetX = width / 2.0;
        var offsetY = height / 2.0;

        // Main transform group: Y-axis flip without affecting X-axis
        // SVG: positive Y goes DOWN by default, we want positive Y UP
        // Solution: Use scaleY only without scaleX, via matrix transform
        // matrix(a,b,c,d,e,f) = [a c e]
        //                        [b d f]
        //                        [0 0 1]
        // For scale: a=scaleX, d=scaleY; translate via e,f
        sb.AppendLine($@"  <g transform=""translate({F(offsetX)},{F(offsetY)}) scale({F(scale)}, {F(-scale)})"">");

        // Grid
        var gridExtent = Math.Max(width, height) / scale;
        sb.AppendLine(@"    <!-- Grid -->");
        for (double x = -gridExtent; x <= gridExtent; x += 100)
        {
            sb.AppendLine($@"    <line class=""grid"" x1=""{F(x)}"" y1=""{F(-gridExtent)}"" x2=""{F(x)}"" y2=""{F(gridExtent)}""/>");
        }
        for (double y = -gridExtent; y <= gridExtent; y += 100)
        {
            sb.AppendLine($@"    <line class=""grid"" x1=""{F(-gridExtent)}"" y1=""{F(y)}"" x2=""{F(gridExtent)}"" y2=""{F(y)}""/>");
        }

        // 1. Draw sleepers first (bottom layer)
        if (showSleepers)
        {
            sb.AppendLine(@"    <!-- Sleepers -->");
            foreach (var segment in segmentList)
            {
                foreach (var primitive in segment.Primitives)
                {
                    RenderSleepers(sb, primitive, sleeperSpacing, sleeperLength, scale);
                }
            }
        }

        // 2. Draw rails (middle layer)
        sb.AppendLine(@"    <!-- Rails -->");
        foreach (var segment in segmentList)
        {
            sb.AppendLine($@"    <!-- Segment: {segment.TemplateId} -->");
            foreach (var primitive in segment.Primitives)
            {
                sb.AppendLine(RenderPrimitive(primitive, 3 / scale, "#4a4a4a"));
            }
        }

        // 3. Draw connections (between layers)
        if (showConnections)
        {
            sb.AppendLine(@"    <!-- Connections -->");
            var drawnConnections = new HashSet<string>();

            foreach (var segment in segmentList)
            {
                foreach (var port in segment.Ports.Where(p => p.ConnectedToEdgeId.HasValue))
                {
                    // Create unique key to avoid drawing same connection twice
                    var key1 = $"{segment.Id}:{port.PortId}-{port.ConnectedToEdgeId}:{port.ConnectedToPortId}";
                    var key2 = $"{port.ConnectedToEdgeId}:{port.ConnectedToPortId}-{segment.Id}:{port.PortId}";

                    if (drawnConnections.Contains(key1) || drawnConnections.Contains(key2))
                        continue;

                    drawnConnections.Add(key1);

                    // Find the connected port's position
                    var connectedSegment = segmentList.FirstOrDefault(s => s.Id == port.ConnectedToEdgeId);
                    var connectedPort = connectedSegment?.Ports.FirstOrDefault(p => p.PortId == port.ConnectedToPortId);

                    if (connectedPort != null)
                    {
                        sb.AppendLine($@"    <line class=""connection"" x1=""{F(port.Position.X)}"" y1=""{F(port.Position.Y)}"" x2=""{F(connectedPort.Position.X)}"" y2=""{F(connectedPort.Position.Y)}""/>");
                    }
                }
            }
        }

        // 4. Draw ports (top layer)
        if (showPorts)
        {
            sb.AppendLine(@"    <!-- Ports -->");
            foreach (var segment in segmentList)
            {
                foreach (var port in segment.Ports)
                {
                    var cssClass = port.ConnectedToEdgeId.HasValue ? "port-connected" : "port-open";
                    var radius = 6 / scale;

                    sb.AppendLine($@"    <circle class=""{cssClass}"" cx=""{F(port.Position.X)}"" cy=""{F(port.Position.Y)}"" r=""{F(radius)}""/>");

                    // Port label (offset in direction perpendicular to port angle)
                    var labelAngleRad = (port.AngleDeg + 90) * Math.PI / 180.0;
                    var labelOffset = 15 / scale;
                    var labelX = port.Position.X + labelOffset * Math.Cos(labelAngleRad);
                    var labelY = port.Position.Y + labelOffset * Math.Sin(labelAngleRad);

                    // Note: We need to counter the Y-flip for text
                    sb.AppendLine($@"    <text class=""port-label"" x=""{F(labelX)}"" y=""{F(labelY)}"" transform=""scale(1,-1) translate(0,{F(-2 * labelY)})"" text-anchor=""middle"">{port.PortId}</text>");
                }
            }
        }

        sb.AppendLine(@"  </g>");

        // Legend (outside transform)
        sb.AppendLine(@"  <!-- Legend -->");
        sb.AppendLine($@"  <text class=""legend"" x=""10"" y=""20"">Track Plan Export</text>");
        sb.AppendLine($@"  <text class=""legend"" x=""10"" y=""35"">Segments: {segmentList.Count}</text>");
        sb.AppendLine($@"  <text class=""legend"" x=""10"" y=""50"">Scale: 1mm = {scale}px</text>");

        // Port legend
        sb.AppendLine($@"  <circle class=""port-open"" cx=""15"" cy=""70"" r=""5""/>");
        sb.AppendLine($@"  <text class=""legend"" x=""25"" y=""74"">Open port</text>");
        sb.AppendLine($@"  <circle class=""port-connected"" cx=""15"" cy=""90"" r=""5""/>");
        sb.AppendLine($@"  <text class=""legend"" x=""25"" y=""94"">Connected port</text>");

        // Orientation hint
        sb.AppendLine($@"  <text class=""legend"" x=""10"" y=""{height - 30}"">CONVENTION: Rails on top, sleepers on bottom</text>");
        sb.AppendLine($@"  <text class=""legend"" x=""10"" y=""{height - 15}"">+X → (right)  |  +Y ↑ (up)</text>");

        sb.AppendLine(@"</svg>");

        return sb.ToString();
    }

    /// <summary>
    /// Renders sleepers (ties) along a primitive, offset to the "bottom" side.
    /// Sleepers indicate the underside of the track (convention: rails on top).
    /// </summary>
    private static void RenderSleepers(StringBuilder sb, IGeometryPrimitive primitive, double spacing, double length, double scale)
    {
        var sleeperWidth = 4 / scale;
        var sleeperOffset = 8 / scale;  // How far below the rail centerline

        switch (primitive)
        {
            case LinePrimitive line:
                RenderSleepersAlongLine(sb, line, spacing, length, sleeperWidth, sleeperOffset);
                break;
            case ArcPrimitive arc:
                RenderSleepersAlongArc(sb, arc, spacing, length, sleeperWidth, sleeperOffset);
                break;
        }
    }

    private static void RenderSleepersAlongLine(StringBuilder sb, LinePrimitive line, double spacing, double length, double width, double offset)
    {
        var dx = line.To.X - line.From.X;
        var dy = line.To.Y - line.From.Y;
        var lineLength = Math.Sqrt(dx * dx + dy * dy);
        var angle = Math.Atan2(dy, dx);

        // Perpendicular direction (sleepers are perpendicular to track)
        var perpX = -Math.Sin(angle);
        var perpY = Math.Cos(angle);

        // Direction along track
        var dirX = Math.Cos(angle);
        var dirY = Math.Sin(angle);

        var count = (int)(lineLength / spacing);
        for (int i = 0; i <= count; i++)
        {
            var t = i * spacing;
            if (t > lineLength) break;

            var centerX = line.From.X + dirX * t;
            var centerY = line.From.Y + dirY * t;

            // Offset sleeper to bottom side (negative perpendicular direction)
            var sleeper1X = centerX - perpX * (length / 2 + offset);
            var sleeper1Y = centerY - perpY * (length / 2 + offset);
            var sleeper2X = centerX - perpX * (-length / 2 + offset);
            var sleeper2Y = centerY - perpY * (-length / 2 + offset);

            sb.AppendLine($@"    <line class=""sleeper"" x1=""{F(sleeper1X)}"" y1=""{F(sleeper1Y)}"" x2=""{F(sleeper2X)}"" y2=""{F(sleeper2Y)}"" stroke-width=""{F(width)}""/>");
        }
    }

    private static void RenderSleepersAlongArc(StringBuilder sb, ArcPrimitive arc, double spacing, double length, double width, double offset)
    {
        var arcLength = arc.Radius * Math.Abs(arc.SweepAngleRad);
        var count = (int)(arcLength / spacing);

        for (int i = 0; i <= count; i++)
        {
            var t = (double)i / count;
            var angle = arc.StartAngleRad + t * arc.SweepAngleRad;

            var centerX = arc.Center.X + arc.Radius * Math.Cos(angle);
            var centerY = arc.Center.Y + arc.Radius * Math.Sin(angle);

            // Radial direction (perpendicular to arc at this point)
            var radialX = Math.Cos(angle);
            var radialY = Math.Sin(angle);

            // Sleepers are perpendicular to the track direction (tangent)
            // For inner radius offset, we go towards center
            // Convention: sleepers on bottom = towards center for CCW curves, away from center for CW
            var direction = arc.SweepAngleRad >= 0 ? -1 : 1;  // CCW = sleepers towards center

            var sleeper1X = centerX + radialX * (direction * offset - length / 2);
            var sleeper1Y = centerY + radialY * (direction * offset - length / 2);
            var sleeper2X = centerX + radialX * (direction * offset + length / 2);
            var sleeper2Y = centerY + radialY * (direction * offset + length / 2);

            sb.AppendLine($@"    <line class=""sleeper"" x1=""{F(sleeper1X)}"" y1=""{F(sleeper1Y)}"" x2=""{F(sleeper2X)}"" y2=""{F(sleeper2Y)}"" stroke-width=""{F(width)}""/>");
        }
    }
}
