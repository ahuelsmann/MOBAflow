// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.Service;

using Domain.TrackPlan;
using Domain.Geometry;
using Renderer;
using System.Diagnostics;

/// <summary>
/// Pure topology-first renderer: Computes WorldTransform for each TrackSegment.
/// NO normalization, NO canvas offsets, NO UI concerns - pure graph traversal with constraints.
/// </summary>
public sealed class TopologyRenderer
{
    private readonly TrackGeometryLibrary _geometryLibrary;
    private readonly ConstraintSolver _constraintSolver;

    public TopologyRenderer(TrackGeometryLibrary geometryLibrary)
    {
        _geometryLibrary = geometryLibrary;
        _constraintSolver = new ConstraintSolver(geometryLibrary);
    }

    /// <summary>
    /// Compute WorldTransform for all segments in the layout.
    /// Pure topology: Only domain objects are modified.
    /// Handles disconnected graphs (multiple components) by rendering each component separately.
    /// </summary>
    public void Render(TrackLayout layout)
    {
        if (layout.Segments.Count == 0)
            return;

        Debug.WriteLine($"🔧 TopologyRenderer: {layout.Segments.Count} segments, {layout.Connections.Count} connections");

        var visited = new HashSet<string>();
        int componentIndex = 0;

        // Render all connected components
        while (visited.Count < layout.Segments.Count)
        {
            // Find next unvisited segment as root for this component
            var root = layout.Segments.FirstOrDefault(s => !visited.Contains(s.Id));
            if (root == null)
                break;

            componentIndex++;
            var componentVisited = new HashSet<string>();

            // Place root at origin (or offset for multi-component visualization)
            var offsetX = componentIndex > 1 ? (componentIndex - 1) * 3000.0 : 0.0; // 3m spacing between components
            root.WorldTransform = new Transform2D 
            { 
                TranslateX = offsetX, 
                TranslateY = 0, 
                RotationDegrees = 0 
            };

            Debug.WriteLine($"🔧 Component {componentIndex} - Root segment: {root.Id} ({root.ArticleCode}) at ({offsetX:F0}, 0, 0°)");

            // Traverse this component
            TraverseComponent(root, layout, componentVisited);

            // Mark all segments in this component as visited
            foreach (var segmentId in componentVisited)
            {
                visited.Add(segmentId);
            }

            Debug.WriteLine($"🔧 Component {componentIndex}: {componentVisited.Count} segments rendered");
        }

        Debug.WriteLine($"🔧 TopologyRenderer: Computed WorldTransform for {visited.Count}/{layout.Segments.Count} segments in {componentIndex} component(s)");
        
        if (visited.Count < layout.Segments.Count)
        {
            var missing = layout.Segments.Where(s => !visited.Contains(s.Id)).ToList();
            Debug.WriteLine($"⚠️ WARNING: {missing.Count} segments NOT rendered (isolated nodes without connections):");
            foreach (var seg in missing)
            {
                Debug.WriteLine($"   - {seg.Id} ({seg.ArticleCode})");
            }
        }
    }

    /// <summary>
    /// Traverse a single connected component.
    /// </summary>
    private void TraverseComponent(TrackSegment root, TrackLayout layout, HashSet<string> visited)
    {
        Traverse(root, layout, visited);
    }

    /// <summary>
    /// Find root segment (segment without incoming connections).
    /// Fallback: First segment in list.
    /// </summary>
    private static TrackSegment FindRoot(TrackLayout layout)
    {
        var connectedAsChild = layout.Connections
            .Select(c => c.Segment2Id)
            .ToHashSet();

        return layout.Segments
            .FirstOrDefault(s => !connectedAsChild.Contains(s.Id))
            ?? layout.Segments.First();
    }

    /// <summary>
    /// Recursive BFS graph traversal.
    /// Places each child segment using ConstraintSolver.
    /// Filters connections by ActiveWhen (parametric switch control).
    /// Bidirectional: Checks both Segment1Id and Segment2Id (AnyRail connections are not directional).
    /// </summary>
    private void Traverse(TrackSegment parent, TrackLayout layout, HashSet<string> visited)
    {
        visited.Add(parent.Id);

        // Check ALL connections (bidirectional)
        foreach (var conn in layout.Connections)
        {
            TrackSegment? child = null;
            TrackConnection effectiveConn = conn;
            bool isReversed = false;

            // Forward direction: parent is Segment1
            if (conn.Segment1Id == parent.Id)
            {
                // Filter by ActiveWhen (parametric switch control)
                if (conn.ActiveWhen != null && parent.SwitchState != conn.ActiveWhen)
                {
                    Debug.WriteLine($"🔀 Skipping inactive connection: {parent.Id}[{conn.Segment1ConnectorIndex}] → requires {conn.ActiveWhen}, current: {parent.SwitchState}");
                    continue;
                }

                child = layout.Segments.FirstOrDefault(s => s.Id == conn.Segment2Id);
            }
            // Reverse direction: parent is Segment2
            else if (conn.Segment2Id == parent.Id)
            {
                // Filter by ActiveWhen (parametric switch control)
                if (conn.ActiveWhen != null && parent.SwitchState != conn.ActiveWhen)
                {
                    Debug.WriteLine($"🔀 Skipping inactive connection (reversed): {parent.Id}[{conn.Segment2ConnectorIndex}] → requires {conn.ActiveWhen}, current: {parent.SwitchState}");
                    continue;
                }

                child = layout.Segments.FirstOrDefault(s => s.Id == conn.Segment1Id);
                effectiveConn = ReverseConnection(conn);
                isReversed = true;
            }
            else
            {
                continue; // Connection doesn't involve parent
            }

            if (child == null || visited.Contains(child.Id))
                continue;

            PlaceChild(parent, child, effectiveConn, isReversed);
            Traverse(child, layout, visited);
        }
    }

    /// <summary>
    /// Reverse a connection (swap Segment1 ↔ Segment2, Connector indices).
    /// Used for bidirectional traversal when parent is Segment2.
    /// </summary>
    private static TrackConnection ReverseConnection(TrackConnection conn)
    {
        return new TrackConnection
        {
            Segment1Id = conn.Segment2Id,
            Segment1ConnectorIndex = conn.Segment2ConnectorIndex,
            Segment2Id = conn.Segment1Id,
            Segment2ConnectorIndex = conn.Segment1ConnectorIndex,
            ConstraintType = conn.ConstraintType,
            ActiveWhen = conn.ActiveWhen,
            Parameters = conn.Parameters
        };
    }

    /// <summary>
    /// Place child segment using ConstraintSolver.
    /// Pure constraint-based placement - NO snap heuristics, NO coordinate comparisons.
    /// </summary>
    private void PlaceChild(TrackSegment parent, TrackSegment child, TrackConnection conn, bool isReversed)
    {
        var parentGeom = _geometryLibrary.GetGeometry(parent.ArticleCode);
        var childGeom = _geometryLibrary.GetGeometry(child.ArticleCode);

        if (parentGeom == null || childGeom == null)
        {
            Debug.WriteLine($"⚠️ TopologyRenderer: Missing geometry for {parent.ArticleCode} or {child.ArticleCode}");
            child.WorldTransform = parent.WorldTransform; // Fallback: same as parent
            return;
        }

        // Use ConstraintSolver to compute child WorldTransform
        child.WorldTransform = _constraintSolver.CalculateWorldTransform(
            parent.WorldTransform,
            parentGeom,
            conn.Segment1ConnectorIndex,
            childGeom,
            conn.Segment2ConnectorIndex,
            conn.ConstraintType,
            conn.Parameters);

        var direction = isReversed ? "←" : "→";
        Debug.WriteLine($"🔧 Placed ({conn.ConstraintType}{(isReversed ? " reversed" : "")}): " +
            $"{parent.Id} ({parent.ArticleCode})[{conn.Segment1ConnectorIndex}] {direction} " +
            $"{child.Id} ({child.ArticleCode})[{conn.Segment2ConnectorIndex}]");
        Debug.WriteLine($"   → WorldTransform: X={child.WorldTransform.TranslateX:F1}, " +
            $"Y={child.WorldTransform.TranslateY:F1}, Rotation={child.WorldTransform.RotationDegrees:F1}°");
    }
}
