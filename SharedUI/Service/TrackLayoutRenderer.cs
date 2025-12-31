// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.Service;

using Domain.TrackPlan;
using ViewModel;
using Renderer;
using System.Diagnostics;

/// <summary>
/// Renders track layouts using Track-Graph Architecture with ConstraintSolver.
/// Calculates world coordinates from connections + TrackGeometryLibrary + geometric constraints.
/// Pure topology approach: No coordinate storage in Domain.
/// </summary>
public class TrackLayoutRenderer
{
    private readonly TrackGeometryLibrary _geometryLibrary;
    private readonly ConstraintSolver _constraintSolver;

    public TrackLayoutRenderer(TrackGeometryLibrary geometryLibrary)
    {
        _geometryLibrary = geometryLibrary;
        _constraintSolver = new ConstraintSolver(geometryLibrary);
    }

    /// <summary>
    /// Rendered segment with position and SVG path.
    /// </summary>
    public record RenderedSegment(
        string Id,
        string ArticleCode,
        double X,
        double Y,
        double Rotation,
        string PathData,
        uint? AssignedInPort);

    /// <summary>
    /// Result of rendering a track layout.
    /// </summary>
    public record RenderResult(
        List<RenderedSegment> Segments,
        BoundingBox BoundingBox);

    /// <summary>
    /// Bounding box for rendered segments.
    /// </summary>
    public record BoundingBox(double Width, double Height, double MinX, double MinY, double MaxX, double MaxY);

    /// <summary>
    /// Render a track layout using topology-first graph traversal.
    /// Calculates world coordinates from connections + TrackGeometryLibrary.
    /// </summary>
    public RenderResult Render(TrackLayout layout, double scale = 1.0)
    {
        _ = scale; // Reserved for future zoom support
        
        var result = new List<RenderedSegment>();
        if (layout.Segments.Count == 0)
            return new RenderResult(result, new BoundingBox(0, 0, 0, 0, 0, 0));

        Debug.WriteLine($"🔧 Renderer: {layout.Segments.Count} segments, {layout.Connections.Count} connections");

        // Track world transforms for each segment (SegmentId -> (X, Y, RotationDeg))
        var worldTransforms = new Dictionary<string, (double X, double Y, double Rotation)>();
        var visited = new HashSet<string>();
        
        // Start with first segment at origin
        var startSegment = layout.Segments[0];
        var queue = new Queue<(string SegmentId, double X, double Y, double Rotation)>();
        queue.Enqueue((startSegment.Id, 0, 0, 0));
        
        // BFS graph traversal with ConstraintSolver
        while (queue.Count > 0)
        {
            var (segmentId, x, y, rotation) = queue.Dequeue();
            
            if (visited.Contains(segmentId))
                continue;
            
            visited.Add(segmentId);
            worldTransforms[segmentId] = (x, y, rotation);
            
            var segment = layout.Segments.FirstOrDefault(s => s.Id == segmentId);
            if (segment == null)
                continue;
            
            var geometry = _geometryLibrary.GetGeometry(segment.ArticleCode);
            if (geometry == null)
            {
                Debug.WriteLine($"⚠️ Renderer: No geometry for {segment.ArticleCode}");
                continue;
            }
            
            // Find all connections from this segment
            var connections = layout.Connections.Where(c => 
                c.Segment1Id == segmentId || c.Segment2Id == segmentId).ToList();
            
            foreach (var conn in connections)
            {
                // Determine which segment is the "other" one
                var isSegment1 = conn.Segment1Id == segmentId;
                var otherSegmentId = isSegment1 ? conn.Segment2Id : conn.Segment1Id;
                var thisConnectorIndex = isSegment1 ? conn.Segment1ConnectorIndex : conn.Segment2ConnectorIndex;
                var otherConnectorIndex = isSegment1 ? conn.Segment2ConnectorIndex : conn.Segment1ConnectorIndex;
                
                if (visited.Contains(otherSegmentId))
                    continue; // Already processed
                
                // Get other segment's geometry
                var otherSegment = layout.Segments.FirstOrDefault(s => s.Id == otherSegmentId);
                if (otherSegment == null)
                    continue;
                
                var otherGeometry = _geometryLibrary.GetGeometry(otherSegment.ArticleCode);
                if (otherGeometry == null)
                    continue;
                
                // Use ConstraintSolver to calculate other segment's transform
                var otherTransform = _constraintSolver.CalculateWorldTransform(
                    (x, y, rotation),
                    geometry,
                    thisConnectorIndex,
                    otherGeometry,
                    otherConnectorIndex,
                    conn.ConstraintType,
                    conn.Parameters);
                
                queue.Enqueue((otherSegmentId, otherTransform.X, otherTransform.Y, otherTransform.Rotation));
                
                Debug.WriteLine($"🔧 Connected ({conn.ConstraintType}): {segmentId}[{thisConnectorIndex}] -> {otherSegmentId}[{otherConnectorIndex}] at ({otherTransform.X:F1}, {otherTransform.Y:F1}, {otherTransform.Rotation:F1}°)");
            }
        }
        
        // Render all segments using calculated world transforms
        foreach (var segment in layout.Segments)
        {
            var geometry = _geometryLibrary.GetGeometry(segment.ArticleCode);
            if (geometry == null)
            {
                Debug.WriteLine($"⚠️ Renderer: No geometry found for {segment.ArticleCode}");
                result.Add(CreateFallbackSegment(segment, result.Count));
                continue;
            }
            
            if (!worldTransforms.TryGetValue(segment.Id, out var transform))
            {
                Debug.WriteLine($"⚠️ Renderer: No world transform for {segment.Id} (disconnected?)");
                // Fallback: place at next available position
                transform = (50 + result.Count * 80, 550, 0);
            }
            
            // Use local PathData from library (XAML handles transformation via RenderTransform)
            result.Add(new RenderedSegment(
                segment.Id,
                segment.ArticleCode,
                transform.X,
                transform.Y,
                transform.Rotation,
                geometry.PathData, // Local coordinates, no transformation!
                segment.AssignedInPort));
            
            
            Debug.WriteLine($"🔧 Rendered: {segment.Id} at ({transform.X:F1}, {transform.Y:F1}, {transform.Rotation:F1}°)");
        }

        Debug.WriteLine($"🔧 Rendered: {result.Count} segments");
        
        // Calculate bounding box
        var boundingBox = CalculateBoundingBox(result);
        
        Debug.WriteLine($"🔧 BoundingBox: ({boundingBox.MinX:F1}, {boundingBox.MinY:F1}) to ({boundingBox.MaxX:F1}, {boundingBox.MaxY:F1})");
        Debug.WriteLine($"🔧 BoundingBox size: {boundingBox.Width:F1} x {boundingBox.Height:F1}");
        
        return new RenderResult(result, boundingBox);
    }

    /// <summary>
    /// Calculate bounding box from rendered segments.
    /// </summary>
    private static BoundingBox CalculateBoundingBox(List<RenderedSegment> segments)
    {
        if (segments.Count == 0)
            return new BoundingBox(0, 0, 0, 0, 0, 0);

        var minX = segments.Min(s => s.X);
        var minY = segments.Min(s => s.Y);
        var maxX = segments.Max(s => s.X);
        var maxY = segments.Max(s => s.Y);

        return new BoundingBox(maxX - minX, maxY - minY, minX, minY, maxX, maxY);
    }

    /// <summary>
    /// Create fallback segment for unknown ArticleCodes.
    /// Uses local coordinates (XAML handles positioning via Canvas.Left/Top).
    /// </summary>
    private static RenderedSegment CreateFallbackSegment(TrackSegment segment, int index)
    {
        var x = 50 + index * 80;
        var y = 550;
        var pathData = "M0,0 L30,0"; // Local coordinates

        return new RenderedSegment(
            segment.Id,
            segment.ArticleCode,
            x,
            y,
            0,
            pathData,
            segment.AssignedInPort);
    }
    
    /// <summary>
    /// Rotate and translate a point.
    /// </summary>
    private static (double X, double Y) RotateAndTranslate(double x, double y, double rotationDeg, double tx, double ty)
    {
        var rad = rotationDeg * Math.PI / 180.0;
        var cos = Math.Cos(rad);
        var sin = Math.Sin(rad);
        
        var rotatedX = x * cos - y * sin;
        var rotatedY = x * sin + y * cos;
        
        return (rotatedX + tx, rotatedY + ty);
    }
    
    /// <summary>
    /// Transform SVG PathData from local to world coordinates.
    /// Parses and transforms M (move), L (line), and A (arc) commands.
    /// </summary>
    private static string TransformPathData(string pathData, double x, double y, double rotationDeg)
    {
        if (string.IsNullOrEmpty(pathData))
            return "";
        
        var rad = rotationDeg * Math.PI / 180.0;
        var cos = Math.Cos(rad);
        var sin = Math.Sin(rad);
        
        var result = new System.Text.StringBuilder();
        var parts = pathData.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        
        for (int i = 0; i < parts.Length; i++)
        {
            var part = parts[i];
            
            if (part == "M" || part == "L")
            {
                // Move or Line command - next part is "x,y"
                result.Append(part).Append(' ');
                if (i + 1 < parts.Length)
                {
                    i++;
                    var coords = parts[i].Split(',');
                    if (coords.Length == 2 &&
                        double.TryParse(coords[0], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var localX) &&
                        double.TryParse(coords[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var localY))
                    {
                        var (worldX, worldY) = RotateAndTranslate(localX, localY, rotationDeg, x, y);
                        // WinUI Geometry requires space separators, not commas; also avoid negative zero
                        var cleanX = Math.Abs(worldX) < 0.01 ? 0 : worldX;
                        var cleanY = Math.Abs(worldY) < 0.01 ? 0 : worldY;
                        result.Append($"{cleanX:F2} {cleanY:F2} ");
                    }
                    else
                    {
                        result.Append(parts[i]).Append(' ');
                    }
                }
            }
            else if (part == "A")
            {
                // Arc command: A rx,ry rotation large-arc sweep x,y (SVG syntax)
                result.Append(part).Append(' ');
                
                // Copy rx,ry (radii don't change with rotation, keep comma separator)
                if (i + 1 < parts.Length)
                {
                    i++;
                    result.Append(parts[i]).Append(' '); // Keep original comma separator
                }
                
                // Copy rotation flag
                if (i + 1 < parts.Length)
                {
                    i++;
                    if (int.TryParse(parts[i], out var arcRotation))
                    {
                        // Add our rotation to arc's rotation
                        var newArcRotation = (arcRotation + (int)rotationDeg) % 360;
                        result.Append(newArcRotation).Append(' ');
                    }
                    else
                    {
                        result.Append(parts[i]).Append(' ');
                    }
                }
                
                // Copy large-arc and sweep flags
                if (i + 1 < parts.Length)
                {
                    i++;
                    result.Append(parts[i]).Append(' '); // large-arc
                }
                if (i + 1 < parts.Length)
                {
                    i++;
                    result.Append(parts[i]).Append(' '); // sweep
                }
                
                // Transform endpoint
                if (i + 1 < parts.Length)
                {
                    i++;
                    var coords = parts[i].Split(',');
                    if (coords.Length == 2 &&
                        double.TryParse(coords[0], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var localX) &&
                        double.TryParse(coords[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var localY))
                    {
                        var (worldX, worldY) = RotateAndTranslate(localX, localY, rotationDeg, x, y);
                        // SVG path syntax requires comma separator between x,y coordinates
                        var cleanX = Math.Abs(worldX) < 0.01 ? 0 : worldX;
                        var cleanY = Math.Abs(worldY) < 0.01 ? 0 : worldY;
                        result.Append(cleanX.ToString("F2", System.Globalization.CultureInfo.InvariantCulture))
                              .Append(',')
                              .Append(cleanY.ToString("F2", System.Globalization.CultureInfo.InvariantCulture))
                              .Append(' ');
                    }
                    else
                    {
                        result.Append(parts[i]).Append(' ');
                    }
                }
            }
            else
            {
                // Unknown command or parameter - copy as-is
                result.Append(part).Append(' ');
            }
        }
        
        return result.ToString().TrimEnd();
    }
}
