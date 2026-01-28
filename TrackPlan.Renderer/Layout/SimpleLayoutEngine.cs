// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.TrackPlan.Renderer.Layout;

using Moba.TrackPlan.Geometry;

/// <summary>
/// Layout engine that positions track edges based on their geometric connections.
/// Uses BFS traversal from a starting edge and calculates positions based on track geometry.
/// </summary>
public sealed class SimpleLayoutEngine : ILayoutEngine
{
    private readonly ITrackCatalog _catalog;

    public SimpleLayoutEngine(ITrackCatalog catalog)
    {
        _catalog = catalog;
    }

    // ILayoutEngine verlangt: Dictionary<Guid, Point2D>
    public Dictionary<Guid, Point2D> Layout(TopologyGraph graph)
    {
        var positions = new Dictionary<Guid, Point2D>();

        if (graph.Edges.Count == 0)
            return positions;

        // Startpunkt
        var start = graph.Edges[0];
        positions[start.Id] = new Point2D(0, 0);

        var queue = new Queue<Guid>();
        queue.Enqueue(start.Id);

        while (queue.Count > 0)
        {
            var currentId = queue.Dequeue();
            var currentEdge = graph.Edges.First(e => e.Id == currentId);
            var currentTemplate = _catalog.GetById(currentEdge.TemplateId)!;

            var currentPos = positions[currentId];

            foreach (var (localPortId, endpoint) in currentEdge.Connections)
            {
                var connected = graph.Edges
                    .SelectMany(e => e.Connections.Select(c => (edge: e, portId: c.Key, endpoint: c.Value)))
                    .FirstOrDefault(x =>
                        x.edge.Id != currentId &&
                        x.endpoint.NodeId == endpoint.NodeId);

                if (connected.edge is null)
                    continue;

                var otherEdge = connected.edge;
                if (positions.ContainsKey(otherEdge.Id))
                    continue;

                var otherTemplate = _catalog.GetById(otherEdge.TemplateId)!;

                // Wir ignorieren Rotationen – LayoutEngine liefert nur Positionen
                var offset = GetPortOffset(otherTemplate, connected.portId, rotationDeg: 0);

                var otherPos = new Point2D(
                    currentPos.X + offset.X,
                    currentPos.Y + offset.Y
                );

                positions[otherEdge.Id] = otherPos;
                queue.Enqueue(otherEdge.Id);
            }
        }

        return positions;
    }

    private static Point2D GetPortOffset(TrackTemplate template, string portId, double rotationDeg)
    {
        var spec = template.Geometry;
        double rotRad = rotationDeg * Math.PI / 180.0;

        static Point2D Rot(Point2D p, double r) =>
            new(p.X * Math.Cos(r) - p.Y * Math.Sin(r),
               p.X * Math.Sin(r) + p.Y * Math.Cos(r));

        if (spec.GeometryKind == TrackGeometryKind.Straight)
        {
            double length = spec.LengthMm!.Value;
            var local = portId == "A" ? new Point2D(0, 0) : new Point2D(length, 0);
            return Rot(local, rotRad);
        }

        if (spec.GeometryKind == TrackGeometryKind.Curve)
        {
            double radius = spec.RadiusMm!.Value;
            double sweep = spec.AngleDeg!.Value * Math.PI / 180.0;

            if (portId == "A") return Rot(new Point2D(0, 0), rotRad);

            var end = new Point2D(
                radius * Math.Sin(sweep),
                radius - radius * Math.Cos(sweep)
            );
            return Rot(end, rotRad);
        }

        if (spec.GeometryKind == TrackGeometryKind.Switch)
        {
            double length = spec.LengthMm!.Value;
            double radius = spec.RadiusMm!.Value;
            double sweep = spec.AngleDeg!.Value * Math.PI / 180.0;
            double junction = spec.JunctionOffsetMm ?? (length / 2.0);

            if (portId == "A") return Rot(new Point2D(0, 0), rotRad);
            if (portId == "B") return Rot(new Point2D(length, 0), rotRad);

            var j = new Point2D(junction, 0);
            var center = new Point2D(j.X, j.Y + radius);
            double startAngle = Math.Atan2(j.Y - center.Y, j.X - center.X);

            var end = new Point2D(
                center.X + radius * Math.Cos(startAngle + sweep),
                center.Y + radius * Math.Sin(startAngle + sweep)
            );

            return Rot(end, rotRad);
        }

        return new Point2D(0, 0);
    }
}