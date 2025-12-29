// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.Service;

using Domain.TrackPlan;
using System.Diagnostics;
using System.Globalization;
using System.Text;

/// <summary>
/// Renders track layouts from stored endpoint coordinates.
/// Endpoints are imported from AnyRail and scaled during import.
/// The renderer simply generates SVG paths from these coordinates.
/// </summary>
public class TrackLayoutRenderer
{
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
    /// Render a track layout from Lines/Arcs stored in domain model.
    /// </summary>
    public List<RenderedSegment> Render(TrackLayout layout, double scale = 1.0)
    {
        _ = scale; // Reserved for future zoom support
        
        var result = new List<RenderedSegment>();
        if (layout.Segments.Count == 0)
            return result;

        Debug.WriteLine($"🔧 Renderer: {layout.Segments.Count} segments, {layout.Connections.Count} connections");

        foreach (var segment in layout.Segments)
        {
            if (segment.Endpoints.Count == 0 && segment.Lines.Count == 0 && segment.Arcs.Count == 0)
            {
                // Fallback for segments without any geometry
                result.Add(CreateFallbackSegment(segment, result.Count));
                continue;
            }

            // Calculate center position from all drawing elements
            var (centerX, centerY) = CalculateCenter(segment);

            // Calculate rotation from first two endpoints if available
            var rotation = 0.0;
            if (segment.Endpoints.Count >= 2)
            {
                var dx = segment.Endpoints[1].X - segment.Endpoints[0].X;
                var dy = segment.Endpoints[1].Y - segment.Endpoints[0].Y;
                rotation = Math.Atan2(dy, dx) * 180 / Math.PI;
            }

            // Generate SVG path from Lines and Arcs
            var pathData = GeneratePathData(segment);

            result.Add(new RenderedSegment(
                segment.Id,
                segment.ArticleCode,
                centerX,
                centerY,
                rotation,
                pathData,
                segment.AssignedInPort));
        }


        Debug.WriteLine($"🔧 Rendered: {result.Count} segments");
        return result;
    }

    /// <summary>
    /// Calculate center position from all drawing elements.
    /// </summary>
    private static (double X, double Y) CalculateCenter(TrackSegment segment)
    {
        var points = new List<(double X, double Y)>();
        
        foreach (var line in segment.Lines)
        {
            points.Add((line.X1, line.Y1));
            points.Add((line.X2, line.Y2));
        }
        
        foreach (var arc in segment.Arcs)
        {
            points.Add((arc.X1, arc.Y1));
            points.Add((arc.X2, arc.Y2));
        }
        
        foreach (var ep in segment.Endpoints)
        {
            points.Add((ep.X, ep.Y));
        }
        
        if (points.Count == 0)
            return (0, 0);
            
        return (points.Average(p => p.X), points.Average(p => p.Y));
    }

    /// <summary>
    /// Generate SVG path data from Lines and Arcs stored in the segment.
    /// </summary>
    private static string GeneratePathData(TrackSegment segment)
    {
        var ic = CultureInfo.InvariantCulture;
        var sb = new StringBuilder();

        // Draw all lines
        foreach (var line in segment.Lines)
        {
            if (sb.Length > 0) sb.Append(' ');
            sb.AppendFormat(ic, "M {0:F1} {1:F1} L {2:F1} {3:F1}",
                line.X1, line.Y1, line.X2, line.Y2);
        }

        // Draw all arcs
        foreach (var arc in segment.Arcs)
        {
            if (sb.Length > 0) sb.Append(' ');
            sb.AppendFormat(ic, "M {0:F1} {1:F1} A {2:F1} {3:F1} 0 0 {4} {5:F1} {6:F1}",
                arc.X1, arc.Y1, arc.Radius, arc.Radius, arc.Sweep, arc.X2, arc.Y2);
        }

        return sb.ToString();
    }

    /// <summary>
    /// Create fallback segment for those without any geometry.
    /// </summary>
    private static RenderedSegment CreateFallbackSegment(TrackSegment segment, int index)
    {
        var x = 50 + index * 80;
        var y = 550;
        var pathData = $"M {x} {y} L {x + 30} {y}";

        return new RenderedSegment(
            segment.Id,
            segment.ArticleCode,
            x,
            y,
            0,
            pathData,
            segment.AssignedInPort);
    }
}
