namespace Moba.SharedUI.Service;

using Microsoft.Extensions.Logging;
using TrackPlan.Domain;
using TrackPlan.Geometry;
using TrackPlan.Renderer;
using ViewModel;

/// <summary>
/// Computes world transforms for all segments based on the connection graph.
/// This is the missing link between imported topology and actual rendering.
/// </summary>
public class TopologySolver
{
    private readonly TrackGeometryLibrary _geometryLibrary;
    private readonly ILogger<TopologySolver> _logger;

    public TopologySolver(TrackGeometryLibrary geometryLibrary, ILogger<TopologySolver> logger)
    {
        _geometryLibrary = geometryLibrary;
        _logger = logger;
    }

    /// <summary>
    /// Computes world transforms for all segments using BFS.
    /// Only processes connected components. Unconnected segments keep their current position.
    /// The segment with the "most established" position (first in list that was already placed) is used as root.
    /// </summary>
    public void Solve(
        IList<TrackSegmentViewModel> segments,
        IList<TrackConnection> connections,
        string? preferredRootId = null)
    {
        if (segments.Count == 0 || connections.Count == 0)
            return;

        // Build lookup
        var byId = segments.ToDictionary(s => s.Id, s => s);

        // Build adjacency list
        var graph = BuildGraph(connections);

        // Find all segments that are part of the connection graph
        var connectedSegmentIds = new HashSet<string>();
        foreach (var conn in connections)
        {
            connectedSegmentIds.Add(conn.Segment1Id);
            connectedSegmentIds.Add(conn.Segment2Id);
        }

        // Only process segments that have at least one connection
        var connectedSegments = segments.Where(s => connectedSegmentIds.Contains(s.Id)).ToList();
        if (connectedSegments.Count == 0)
            return;

        // Pick root: prefer specified root, otherwise use first segment that isn't at origin
        TrackSegmentViewModel? root = null;
        
        if (preferredRootId != null && byId.TryGetValue(preferredRootId, out var preferredRoot) 
            && connectedSegmentIds.Contains(preferredRootId))
        {
            root = preferredRoot;
        }
        else
        {
            // Find a segment that already has a non-origin position (was previously placed)
            root = connectedSegments.FirstOrDefault(s => 
                Math.Abs(s.WorldTransform.TranslateX) > 1.0 || 
                Math.Abs(s.WorldTransform.TranslateY) > 1.0);
            
            // Fallback to first connected segment
            root ??= connectedSegments[0];
        }

        var visited = new HashSet<string>();
        var queue = new Queue<string>();

        visited.Add(root.Id);
        queue.Enqueue(root.Id);

        _logger.LogDebug("TopologySolver: Root = {Root} at ({X:F1}, {Y:F1})", 
            root.Id, root.WorldTransform.TranslateX, root.WorldTransform.TranslateY);

        while (queue.Count > 0)
        {
            var currentId = queue.Dequeue();
            var current = byId[currentId];

            if (!graph.TryGetValue(currentId, out var neighbors))
                continue;

            foreach (var edge in neighbors)
            {
                var childId = edge.OtherSegmentId;
                if (visited.Contains(childId))
                    continue;

                if (!byId.TryGetValue(childId, out var child))
                    continue;

                ApplyTransform(current, child, edge);

                visited.Add(childId);
                queue.Enqueue(childId);
            }
        }

        _logger.LogDebug("TopologySolver: Completed. {Count}/{Total} segments positioned.", 
            visited.Count, segments.Count);
    }

    // --------------------------------------------------------------------
    // Graph Construction
    // --------------------------------------------------------------------

    private Dictionary<string, List<ConnectionEdge>> BuildGraph(IList<TrackConnection> connections)
    {
        var graph = new Dictionary<string, List<ConnectionEdge>>();

        foreach (var c in connections)
        {
            AddEdge(graph, c.Segment1Id, c.Segment2Id, c.Segment1ConnectorIndex, c.Segment2ConnectorIndex);
            AddEdge(graph, c.Segment2Id, c.Segment1Id, c.Segment2ConnectorIndex, c.Segment1ConnectorIndex);
        }

        return graph;
    }

    private void AddEdge(
        Dictionary<string, List<ConnectionEdge>> graph,
        string from,
        string to,
        int fromConnector,
        int toConnector)
    {
        if (!graph.TryGetValue(from, out var list))
        {
            list = new List<ConnectionEdge>();
            graph[from] = list;
        }

        list.Add(new ConnectionEdge(to, fromConnector, toConnector));
    }

    private record ConnectionEdge(string OtherSegmentId, int FromConnector, int ToConnector);

    // --------------------------------------------------------------------
    // Transform Application
    // --------------------------------------------------------------------

    private void ApplyTransform(
        TrackSegmentViewModel parent,
        TrackSegmentViewModel child,
        ConnectionEdge edge)
    {
        var parentGeom = _geometryLibrary.GetGeometry(parent.ArticleCode);
        var childGeom = _geometryLibrary.GetGeometry(child.ArticleCode);

        if (parentGeom == null || childGeom == null)
        {
            _logger.LogWarning("TopologySolver: Missing geometry for {A} or {B}", parent.Id, child.Id);
            return;
        }

        var parentConn = parentGeom.GetConnectorTransform(edge.FromConnector);
        var childInvConn = childGeom.GetInverseConnectorTransform(edge.ToConnector);

        // WorldTransform(child) = World(parent) * parentConn * childInvConn
        var world =
            parent.WorldTransform
            .Multiply(parentConn)
            .Multiply(childInvConn);

        child.WorldTransform = world;

        _logger.LogDebug(
            "TopologySolver: {Child} positioned via {Parent} | parentConn={PC}, childInv={CI}, world={W}",
            child.Id, parent.Id,
            parentConn, childInvConn, world);
    }
}
