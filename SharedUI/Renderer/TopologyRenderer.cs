// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Moba.Domain.TrackPlan;

namespace Moba.SharedUI.Renderer;

/// <summary>
/// Renders a track layout from PURE TOPOLOGY (ArticleCode + Connections + Rotation) to SVG PathData.
/// Uses BFS traversal to calculate positions based on endpoint alignment.
/// This is the NEW renderer that replaces coordinate-based rendering.
/// </summary>
public class TopologyRenderer
{
    private readonly TrackGeometryLibrary _library;

    public TopologyRenderer(TrackGeometryLibrary library)
    {
        _library = library;
    }

    /// <summary>
    /// Renders the complete track layout from topology.
    /// Returns positioned segments with calculated X, Y, PathData, and Rotation.
    /// </summary>
    public RenderResult Render(TrackLayout layout)
    {
        var rendered = new List<RenderedSegment>();
        var visitedSegments = new HashSet<string>();  // Track which segments are fully positioned
        var visitedEndpoints = new HashSet<string>();  // Track (SegmentId, EndpointIndex) to avoid duplicate connections

        Debug.WriteLine($"🚀 TopologyRenderer.Render: {layout.Segments.Count} segments, {layout.Connections.Count} connections");

        // Find best starting segment (most connected segment to ensure full traversal)
        var start = layout.Segments
            .OrderByDescending(s => layout.Connections.Count(c => c.Segment1Id == s.Id || c.Segment2Id == s.Id))
            .FirstOrDefault();
        
        if (start == null)
        {
            Debug.WriteLine("⚠️  No segments found!");
            return new RenderResult(rendered, BoundingBox.Empty);
        }

        var startConnectionCount = layout.Connections.Count(c => c.Segment1Id == start.Id || c.Segment2Id == start.Id);
        Debug.WriteLine($"📍 Starting BFS from segment: {start.Id} ({start.ArticleCode}) with {startConnectionCount} connections");

        // BFS queue: (Segment, Position X, Position Y, Accumulated Rotation)
        var queue = new Queue<(TrackSegment Segment, double X, double Y, double RotationDeg)>();
        queue.Enqueue((start, 0, 0, 0));  // Start at origin with 0° rotation
        Debug.WriteLine($"🎯 START SEGMENT: {start.Id} ({start.ArticleCode}) at (0, 0, 0°)");

        while (queue.Count > 0)
        {
            var (segment, x, y, rotationDeg) = queue.Dequeue();
            
            // Only process segment positioning ONCE (avoid duplicate rendering)
            if (visitedSegments.Contains(segment.Id)) 
            {
                Debug.WriteLine($"  ⏭️  Skipping already visited segment: {segment.Id} ({segment.ArticleCode})");
                continue;
            }
            visitedSegments.Add(segment.Id);

            // Lookup geometry from library
            var geometry = _library.GetGeometry(segment.ArticleCode);
            if (geometry == null)
            {
                Debug.WriteLine($"⚠️  No geometry found for ArticleCode '{segment.ArticleCode}' (Segment {segment.Id}) - SKIPPING");
                continue;
            }

            Debug.WriteLine($"  ✓ Segment {segment.Id} ({segment.ArticleCode}): {geometry.Endpoints.Count} endpoints defined in library");

            // Apply rotation transform to PathData
            var rotatedPathData = ApplyRotation(geometry.PathData, rotationDeg);

            // Create rendered segment
            rendered.Add(new RenderedSegment
            {
                Id = segment.Id,
                ArticleCode = segment.ArticleCode,
                PathData = rotatedPathData,
                X = x,
                Y = y,
                Rotation = rotationDeg,
                AssignedInPort = segment.AssignedInPort
            });

            Debug.WriteLine($"  ✅ Rendered segment {segment.Id} ({segment.ArticleCode}): X={x:F2}, Y={y:F2}, Rot={rotationDeg:F1}°");
            Debug.WriteLine($"     PathData: {(rotatedPathData?.Length > 60 ? rotatedPathData.Substring(0, 60) + "..." : rotatedPathData)}");

            // Find ALL connections (current segment can be Segment1 OR Segment2)
            // AnyRail connections are directionless - need to check both ends
            var connections = layout.Connections.Where(c => c.Segment1Id == segment.Id || c.Segment2Id == segment.Id);

            Debug.WriteLine($"  Segment {segment.Id}: Found {connections.Count()} outgoing connections");

            foreach (var conn in connections)
            {
                // Determine next segment and endpoint indices based on connection direction
                string nextId;
                int currentEndpointIndex, nextEndpointIndex;
                
                if (conn.Segment1Id == segment.Id)
                {
                    // Current segment is Segment1 → follow to Segment2
                    nextId = conn.Segment2Id;
                    currentEndpointIndex = conn.Segment1EndpointIndex;
                    nextEndpointIndex = conn.Segment2EndpointIndex;
                }
                else
                {
                    // Current segment is Segment2 → follow to Segment1
                    nextId = conn.Segment1Id;
                    currentEndpointIndex = conn.Segment2EndpointIndex;
                    nextEndpointIndex = conn.Segment1EndpointIndex;
                }

                // Check if this endpoint connection was already processed (avoid infinite loops)
                var endpointKey = $"{segment.Id}:{currentEndpointIndex}→{nextId}:{nextEndpointIndex}";
                if (visitedEndpoints.Contains(endpointKey)) 
                {
                    Debug.WriteLine($"    ⏭️  Skipping already visited connection: {endpointKey}");
                    continue;
                }
                visitedEndpoints.Add(endpointKey);

                var nextSegment = layout.Segments.FirstOrDefault(s => s.Id == nextId);
                if (nextSegment == null) continue;

                var nextGeometry = _library.GetGeometry(nextSegment.ArticleCode);
                if (nextGeometry == null) continue;

                // Validate endpoint indices against geometry (authoritative source)
                if (currentEndpointIndex < 0 || currentEndpointIndex >= geometry.Endpoints.Count)
                {
                    Debug.WriteLine($"⚠️ Invalid endpoint index {currentEndpointIndex} for segment {segment.Id} (has {geometry.Endpoints.Count} endpoints)");
                    continue;
                }

                if (nextEndpointIndex < 0 || nextEndpointIndex >= nextGeometry.Endpoints.Count)
                {
                    Debug.WriteLine($"⚠️ Invalid endpoint index {nextEndpointIndex} for segment {nextSegment.Id} (has {nextGeometry.Endpoints.Count} endpoints)");
                    continue;
                }

                // Calculate next transform so that endpoints match AND tangents are aligned.
                // 1) Current endpoint in WORLD coordinates
                var currentEndpointLocal = geometry.Endpoints[currentEndpointIndex];
                Debug.WriteLine($"    📍 Current segment {segment.Id} at World ({x:F2}, {y:F2}), Rot={rotationDeg:F1}°");
                Debug.WriteLine($"       Endpoint[{currentEndpointIndex}] Local: ({currentEndpointLocal.X:F2}, {currentEndpointLocal.Y:F2})");
                var currentEndpointWorld = TransformPoint(currentEndpointLocal, x, y, rotationDeg);
                Debug.WriteLine($"       Endpoint[{currentEndpointIndex}] World: ({currentEndpointWorld.X:F2}, {currentEndpointWorld.Y:F2})");

                // 2) Compute target heading for the next endpoint.
                // Use absolute Direction from segment.Endpoints if available, otherwise use geometry library
                var currentHeadingWorld = GetEndpointDirection(segment, currentEndpointIndex, geometry, rotationDeg);
                
                // Next endpoint must face OPPOSITE direction (endpoints meet head-to-head, not same direction)
                // When two tracks connect, their endpoints point TOWARD each other (180° difference)
                var desiredNextHeadingWorld = NormalizeDeg(currentHeadingWorld + 180.0);

                // 3) Determine next segment rotation so that its local endpoint heading matches desired heading.
                var nextHeadingLocal = GetEndpointDirectionLocal(nextSegment, nextEndpointIndex, nextGeometry);
                var nextRotationDeg = NormalizeDeg(desiredNextHeadingWorld - nextHeadingLocal);

                // 4) With rotation known, compute translation so the two endpoints coincide.
                var nextEndpointLocal = nextGeometry.Endpoints[nextEndpointIndex];
                Debug.WriteLine($"    🔗 Next segment {nextId}: Endpoint[{nextEndpointIndex}] Local ({nextEndpointLocal.X:F2}, {nextEndpointLocal.Y:F2})");
                var nextEndpointWorldIfAtOrigin = TransformPoint(nextEndpointLocal, 0, 0, nextRotationDeg);
                Debug.WriteLine($"       WorldIfAtOrigin: ({nextEndpointWorldIfAtOrigin.X:F2}, {nextEndpointWorldIfAtOrigin.Y:F2})");
                var nextX = currentEndpointWorld.X - nextEndpointWorldIfAtOrigin.X;
                var nextY = currentEndpointWorld.Y - nextEndpointWorldIfAtOrigin.Y;
                Debug.WriteLine($"       ➡️ FINAL Next position: ({nextX:F2}, {nextY:F2}), Rot={nextRotationDeg:F1}°");

                Debug.WriteLine($"  Connection: {segment.Id}.Ep{currentEndpointIndex} → {nextId}.Ep{nextEndpointIndex}");
                Debug.WriteLine($"    Current endpoint World: ({currentEndpointWorld.X:F1}, {currentEndpointWorld.Y:F1}), Local: ({currentEndpointLocal.X:F1}, {currentEndpointLocal.Y:F1})");
                Debug.WriteLine($"    Current heading World: {currentHeadingWorld:F1}°, Desired next: {desiredNextHeadingWorld:F1}°");
                Debug.WriteLine($"    Next heading Local: {nextHeadingLocal:F1}°, Next rotation: {nextRotationDeg:F1}°");
                Debug.WriteLine($"    Next endpoint Local: ({nextEndpointLocal.X:F1}, {nextEndpointLocal.Y:F1}), WorldIfAtOrigin: ({nextEndpointWorldIfAtOrigin.X:F1}, {nextEndpointWorldIfAtOrigin.Y:F1})");
                Debug.WriteLine($"    Next segment final pos: ({nextX:F1}, {nextY:F1}), rotation: {nextRotationDeg:F1}°");

                queue.Enqueue((nextSegment, nextX, nextY, nextRotationDeg));
            }
        }

        // Calculate bounding box to shift all segments into positive coordinate space
        if (rendered.Count > 0)
        {
            var minX = rendered.Min(s => s.X);
            var minY = rendered.Min(s => s.Y);
            
            // Apply offset with margin (50px padding from Canvas edge)
            const double margin = 50;
            var offsetX = -minX + margin;
            var offsetY = -minY + margin;

            Debug.WriteLine($"📐 Bounding box: MinX={minX:F1}, MinY={minY:F1} → Offset: X+{offsetX:F1}, Y+{offsetY:F1}");

            foreach (var segment in rendered)
            {
                segment.X += offsetX;
                segment.Y += offsetY;
            }
        }

        // Final bounding box after offset
        var finalBox = rendered.Count > 0
            ? BoundingBox.FromPoints(rendered.Select(s => (s.X, s.Y)))
            : BoundingBox.Empty;

        return new RenderResult(rendered, finalBox);
    }

    /// <summary>
    /// Apply rotation transform to SVG PathData.
    /// Rotation is applied around origin (0,0) in degrees.
    /// </summary>
    private string ApplyRotation(string pathData, double degrees)
    {
        if (Math.Abs(degrees) < 0.01) return pathData;  // No rotation needed

        // For WinUI Path rendering, we return the original PathData
        // and let WinUI's RenderTransform handle rotation (more efficient)
        // PathData stays relative to (0,0)
        return pathData;
    }

    private static TrackPoint TransformPoint(TrackPoint local, double tx, double ty, double rotationDeg)
    {
        var rad = rotationDeg * Math.PI / 180.0;
        var cos = Math.Cos(rad);
        var sin = Math.Sin(rad);

        var x = local.X * cos - local.Y * sin;
        var y = local.X * sin + local.Y * cos;

        return new TrackPoint
        {
            X = x + tx,
            Y = y + ty
        };
    }

    private static double GetEndpointHeadingDeg(TrackGeometry geometry, int endpointIndex)
    {
        // Endpoint headings are MANDATORY - no fallback logic!
        // All geometries in TrackGeometryLibrary MUST have EndpointHeadingsDeg defined.
        if (geometry.EndpointHeadingsDeg.Count != geometry.Endpoints.Count)
        {
            throw new InvalidOperationException(
                $"Geometry '{geometry.ArticleCode}' has {geometry.Endpoints.Count} endpoints " +
                $"but only {geometry.EndpointHeadingsDeg.Count} headings. All endpoints MUST have headings defined!");
        }

        if (endpointIndex < 0 || endpointIndex >= geometry.EndpointHeadingsDeg.Count)
        {
            throw new ArgumentOutOfRangeException(
                nameof(endpointIndex),
                $"Endpoint index {endpointIndex} out of range for geometry '{geometry.ArticleCode}' with {geometry.EndpointHeadingsDeg.Count} endpoints");
        }

        return geometry.EndpointHeadingsDeg[endpointIndex];
    }

    /// <summary>
    /// Get endpoint direction in world coordinates.
    /// Always uses geometry library heading + current rotation.
    /// AnyRail XML directions are absolute and cannot be used for topology alignment.
    /// </summary>
    private static double GetEndpointDirection(TrackSegment segment, int endpointIndex, TrackGeometry geometry, double rotationDeg)
    {
        // Always use geometry library heading + rotation for correct alignment
        // AnyRail Direction values are absolute world coordinates, not relative to segment
        _ = segment; // Suppress unused parameter warning
        var headingLocal = GetEndpointHeadingDeg(geometry, endpointIndex);
        return NormalizeDeg(headingLocal + rotationDeg);
    }

    /// <summary>
    /// Get endpoint direction in local coordinates (no rotation applied).
    /// Always uses geometry library heading.
    /// </summary>
    private static double GetEndpointDirectionLocal(TrackSegment segment, int endpointIndex, TrackGeometry geometry)
    {
        // Always use geometry library heading (local coordinates)
        // AnyRail Direction values are absolute and not suitable for topology rendering
        _ = segment; // Suppress unused parameter warning
        return GetEndpointHeadingDeg(geometry, endpointIndex);
    }

    private static double NormalizeDeg(double value)
    {
        var result = value % 360.0;
        if (result < 0) result += 360.0;
        return result;
    }
}

/// <summary>
/// A track segment with calculated position and PathData (ready for WinUI rendering).
/// </summary>
public class RenderedSegment
{
    public string Id { get; set; } = string.Empty;
    public string ArticleCode { get; set; } = string.Empty;
    public string PathData { get; set; } = string.Empty;  // SVG path (e.g., "M 0,0 L 231,0")
    public double X { get; set; }  // Absolute X position
    public double Y { get; set; }  // Absolute Y position
    public double Rotation { get; set; }  // Rotation in degrees (for WinUI RenderTransform)
    public uint? AssignedInPort { get; set; }
}

public record BoundingBox(double MinX, double MinY, double MaxX, double MaxY)
{
    public double Width => MaxX - MinX;
    public double Height => MaxY - MinY;

    public static BoundingBox FromPoints(IEnumerable<(double X, double Y)> points)
    {
        var minX = double.MaxValue;
        var minY = double.MaxValue;
        var maxX = double.MinValue;
        var maxY = double.MinValue;

        foreach (var (x, y) in points)
        {
            if (x < minX) minX = x;
            if (y < minY) minY = y;
            if (x > maxX) maxX = x;
            if (y > maxY) maxY = y;
        }

        return new BoundingBox(minX, minY, maxX, maxY);
    }

    public static BoundingBox Empty => new(0, 0, 0, 0);
}

public record RenderResult(IReadOnlyList<RenderedSegment> Segments, BoundingBox BoundingBox);

