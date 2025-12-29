// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
using System;
using System.Collections.Generic;
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
    public List<RenderedSegment> Render(TrackLayout layout)
    {
        var rendered = new List<RenderedSegment>();
        var visited = new HashSet<string>();

        Console.WriteLine($"🚀 TopologyRenderer.Render: {layout.Segments.Count} segments, {layout.Connections.Count} connections");

        // Find starting segment
        var start = layout.Segments.FirstOrDefault();
        if (start == null)
        {
            Console.WriteLine("⚠️  No segments found!");
            return rendered;
        }

        Console.WriteLine($"📍 Starting BFS from segment: {start.Id} ({start.ArticleCode})");

        // BFS queue: (Segment, Position X, Position Y, Accumulated Rotation)
        var queue = new Queue<(TrackSegment Segment, double X, double Y, double Rotation)>();
        queue.Enqueue((start, 0, 0, 0));  // Start at origin with 0° rotation

        while (queue.Count > 0)
        {
            var (segment, x, y, rotation) = queue.Dequeue();
            if (visited.Contains(segment.Id)) continue;
            visited.Add(segment.Id);

            // Lookup geometry from library
            var geometry = _library.GetGeometry(segment.ArticleCode);
            if (geometry == null)
            {
                Console.WriteLine($"Warning: No geometry found for ArticleCode '{segment.ArticleCode}'");
                continue;
            }

            // Apply rotation transform to PathData
            var rotatedPathData = ApplyRotation(geometry.PathData, rotation);

            // Create rendered segment
            rendered.Add(new RenderedSegment
            {
                Id = segment.Id,
                ArticleCode = segment.ArticleCode,
                PathData = rotatedPathData,
                X = x,
                Y = y,
                Rotation = rotation,
                AssignedInPort = segment.AssignedInPort
            });

            Console.WriteLine($"  Rendered segment {segment.Id} ({segment.ArticleCode}): X={x:F1}, Y={y:F1}, PathData={(rotatedPathData?.Length > 50 ? rotatedPathData.Substring(0, 50) + "..." : rotatedPathData)}");

            // Find connected segments
            var connections = layout.Connections.Where(c =>
                c.Segment1Id == segment.Id || c.Segment2Id == segment.Id);

            Console.WriteLine($"  Segment {segment.Id}: Found {connections.Count()} connections");

            foreach (var conn in connections)
            {
                // Determine which segment is "next"
                var isSegment1 = conn.Segment1Id == segment.Id;
                var nextId = isSegment1 ? conn.Segment2Id : conn.Segment1Id;
                var currentEndpointIndex = isSegment1 ? conn.Segment1EndpointIndex : conn.Segment2EndpointIndex;
                var nextEndpointIndex = isSegment1 ? conn.Segment2EndpointIndex : conn.Segment1EndpointIndex;

                var nextSegment = layout.Segments.FirstOrDefault(s => s.Id == nextId);
                if (nextSegment == null || visited.Contains(nextId)) continue;

                var nextGeometry = _library.GetGeometry(nextSegment.ArticleCode);
                if (nextGeometry == null) continue;

                // Calculate next position based on endpoint alignment
                // Current segment's endpoint in WORLD coordinates
                var currentEndpointLocal = geometry.Endpoints[currentEndpointIndex];
                var currentEndpointWorld = new TrackPoint
                {
                    X = x + currentEndpointLocal.X,
                    Y = y + currentEndpointLocal.Y
                };

                // Next segment's connecting endpoint in LOCAL coordinates
                var nextEndpointLocal = nextGeometry.Endpoints[nextEndpointIndex];

                // Position next segment so its endpoint aligns with current endpoint
                var nextX = currentEndpointWorld.X - nextEndpointLocal.X;
                var nextY = currentEndpointWorld.Y - nextEndpointLocal.Y;

                // Inherit rotation (could be extended to support per-segment rotation)
                var nextRotation = rotation;

                Console.WriteLine($"  Connection: {segment.Id}.Ep{currentEndpointIndex} → {nextId}.Ep{nextEndpointIndex}");
                Console.WriteLine($"    Current endpoint: ({currentEndpointWorld.X:F1}, {currentEndpointWorld.Y:F1})");
                Console.WriteLine($"    Next segment pos: ({nextX:F1}, {nextY:F1})");

                queue.Enqueue((nextSegment, nextX, nextY, nextRotation));
            }
        }

        return rendered;
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
