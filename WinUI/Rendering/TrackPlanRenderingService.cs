// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.WinUI.Rendering;

using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;

using Moba.TrackPlan.Geometry;
using Moba.TrackPlan.Graph;
using Moba.TrackPlan.Renderer.Geometry;
using Moba.TrackPlan.Renderer.Service;
using Moba.TrackPlan.Renderer.World;
using Moba.TrackPlan.TrackSystem;

using System;
using System.Collections.Generic;

/// <summary>
/// Service to render track primitives to WinUI Canvas elements.
/// Bridges between the rendering engine's primitives and WinUI UI elements.
/// </summary>
public sealed class TrackPlanRenderingService
{
    private readonly ITrackCatalog _catalog;
    private readonly TrackPlanLayoutEngine _layoutEngine;
    private double _displayScale = 0.5;

    public double DisplayScale
    {
        get => _displayScale;
        set => _displayScale = value;
    }

    public TrackPlanRenderingService(ITrackCatalog catalog, TrackPlanLayoutEngine layoutEngine)
    {
        _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        _layoutEngine = layoutEngine ?? throw new ArgumentNullException(nameof(layoutEngine));
    }

    /// <summary>
    /// Renders all track geometry from a topology graph.
    /// </summary>
    public IEnumerable<Shape> RenderTrackGeometry(TopologyGraph topology)
    {
        ArgumentNullException.ThrowIfNull(topology);
        var shapes = new List<Shape>();

        if (topology.Edges.Count == 0)
            return shapes;

        try
        {
            // Process topology to get all primitives
            var layout = _layoutEngine.Process(
                topology,
                new Point2D(100, 100),  // Start position
                0);                      // Start angle

            // Convert primitives to WinUI shapes
            foreach (var primitive in layout.Primitives)
            {
                var shape = ConvertPrimitiveToShape(primitive);
                if (shape != null)
                    shapes.Add(shape);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Track rendering error: {ex.Message}");
        }

        return shapes;
    }

    /// <summary>
    /// Converts a geometry primitive to a WinUI Shape.
    /// </summary>
    private Shape? ConvertPrimitiveToShape(IGeometryPrimitive primitive)
    {
        return primitive switch
        {
            LinePrimitive line => ConvertLinePrimitive(line),
            ArcPrimitive arc => ConvertArcPrimitive(arc),
            _ => null
        };
    }

    private Shape ConvertLinePrimitive(LinePrimitive line)
    {
        return new Line
        {
            X1 = line.From.X * _displayScale,
            Y1 = line.From.Y * _displayScale,
            X2 = line.To.X * _displayScale,
            Y2 = line.To.Y * _displayScale,
            Stroke = new SolidColorBrush(Microsoft.UI.Colors.Black),
            StrokeThickness = 2
        };
    }

    private Shape ConvertArcPrimitive(ArcPrimitive arc)
    {
        // Approximate arc as polyline using 16 segments
        var segments = 16;
        var pointCollection = new PointCollection();

        for (int i = 0; i <= segments; i++)
        {
            var t = (double)i / segments;
            var angle = arc.StartAngleRad + t * arc.SweepAngleRad;

            var x = arc.Center.X + arc.Radius * Math.Cos(angle);
            var y = arc.Center.Y + arc.Radius * Math.Sin(angle);

            pointCollection.Add(new Windows.Foundation.Point(x * _displayScale, y * _displayScale));
        }

        return new Polyline
        {
            Points = pointCollection,
            Stroke = new SolidColorBrush(Microsoft.UI.Colors.Black),
            StrokeThickness = 2,
            StrokeLineJoin = PenLineJoin.Round
        };
    }
}
