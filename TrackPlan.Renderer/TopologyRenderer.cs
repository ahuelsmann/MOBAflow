// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.TrackPlan.Renderer;

using Moba.TrackPlan.Domain;
using Moba.TrackPlan.Geometry;

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

    public void Render(TrackLayout layout)
    {
        if (layout.Segments.Count == 0)
            return;

        Debug.WriteLine($"🔧 TopologyRenderer: {layout.Segments.Count} segments, {layout.Connections.Count} connections");

        var visited = new HashSet<string>();
        int componentIndex = 0;

        while (visited.Count < layout.Segments.Count)
        {
            var root = layout.Segments.FirstOrDefault(s => !visited.Contains(s.Id));
            if (root == null)
                break;

            componentIndex++;
            var componentVisited = new HashSet<string>();

            var offsetX = componentIndex > 1 ? (componentIndex - 1) * 3000.0 : 0.0;
            root.WorldTransform = new Transform2D
            {
                TranslateX = offsetX,
                TranslateY = 0,
                RotationDegrees = 0
            };

            Debug.WriteLine($"🔧 Component {componentIndex} - Root segment: {root.Id} ({root.ArticleCode}) at ({offsetX:F0}, 0, 0°)");

            TraverseComponent(root, layout, componentVisited);

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

    private void TraverseComponent(TrackSegment root, TrackLayout layout, HashSet<string> visited)
    {
        Traverse(root, layout, visited);
    }

    private void Traverse(TrackSegment parent, TrackLayout layout, HashSet<string> visited)
    {
        visited.Add(parent.Id);

        foreach (var conn in layout.Connections)
        {
            TrackSegment? child = null;
            TrackConnection effectiveConn = conn;
            bool isReversed = false;

            if (conn.Segment1Id == parent.Id)
            {
                if (conn.ActiveWhen != null && parent.SwitchState != conn.ActiveWhen)
                {
                    Debug.WriteLine($"🔀 Skipping inactive connection: {parent.Id}[{conn.Segment1ConnectorIndex}] → requires {conn.ActiveWhen}, current: {parent.SwitchState}");
                    continue;
                }

                child = layout.Segments.FirstOrDefault(s => s.Id == conn.Segment2Id);
            }
            else if (conn.Segment2Id == parent.Id)
            {
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
                continue;
            }

            if (child == null || visited.Contains(child.Id))
                continue;

            PlaceChild(parent, child, effectiveConn, isReversed);
            Traverse(child, layout, visited);
        }
    }

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

    private void PlaceChild(TrackSegment parent, TrackSegment child, TrackConnection conn, bool isReversed)
    {
        var parentGeom = _geometryLibrary.GetGeometry(parent.ArticleCode);
        var childGeom = _geometryLibrary.GetGeometry(child.ArticleCode);

        if (parentGeom == null || childGeom == null)
        {
            Debug.WriteLine($"⚠️ TopologyRenderer: Missing geometry for {parent.ArticleCode} or {child.ArticleCode}");
            child.WorldTransform = parent.WorldTransform;
            return;
        }

        child.WorldTransform = _constraintSolver.CalculateWorldTransform(
            parent.WorldTransform,
            parentGeom,
            conn.Segment1ConnectorIndex,
            childGeom,
            conn.Segment2ConnectorIndex,
            conn.ConstraintType,
            conn.Parameters);

        var direction = isReversed ? "←" : "→";
        Debug.WriteLine($"🔧 Placed ({conn.ConstraintType}{(isReversed ? " reversed" : "")}) " +
            $"{parent.Id} ({parent.ArticleCode})[{conn.Segment1ConnectorIndex}] {direction} " +
            $"{child.Id} ({child.ArticleCode})[{conn.Segment2ConnectorIndex}]");
        Debug.WriteLine($"   → WorldTransform: X={child.WorldTransform.TranslateX:F1}, " +
            $"Y={child.WorldTransform.TranslateY:F1}, Rotation={child.WorldTransform.RotationDegrees:F1}°");
    }
}
