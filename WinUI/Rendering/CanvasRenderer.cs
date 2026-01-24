// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

using System.Collections.Generic;
using System.Linq;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;

using Moba.TrackPlan.Editor.ViewModel;
using Moba.TrackPlan.Geometry;
using Moba.TrackPlan.Renderer.Geometry;
using Moba.TrackPlan.Renderer.World;
using Moba.TrackPlan.TrackSystem;

namespace Moba.WinUI.Rendering;

/// <summary>
/// WinUI Canvas renderer using real geometry engine.
/// Renders each track using actual Geometry classes (StraightGeometry, CurveGeometry, SwitchGeometry).
/// </summary>
public class CanvasRenderer
{
    private const double DisplayScale = 0.5;
    private const double TrackStrokeThickness = 6.0;
    private const double RailStrokeThickness = 2.0;
    private const double GhostOpacity = 0.75;

    /// <summary>
    /// Renders all tracks from ViewModel to WinUI Canvas using real geometry primitives.
    /// </summary>
    public void Render(
        Canvas canvas,
        TrackPlanEditorViewModel viewModel,
        ITrackCatalog catalog,
        SolidColorBrush trackBrush,
        SolidColorBrush selectedBrush,
        SolidColorBrush hoverBrush)
    {
        ArgumentNullException.ThrowIfNull(canvas);
        ArgumentNullException.ThrowIfNull(viewModel);
        ArgumentNullException.ThrowIfNull(catalog);

        // Draw all edges using real geometry engine
        foreach (var edge in viewModel.Graph.Edges)
        {
            var template = catalog.GetById(edge.TemplateId);
            if (template == null)
                continue;

            if (!viewModel.Positions.TryGetValue(edge.Id, out var pos))
                continue;

            var rot = viewModel.Rotations.GetValueOrDefault(edge.Id, 0.0);
            var isSelected = viewModel.SelectedTrackIds.Contains(edge.Id);
            var brush = isSelected ? selectedBrush : trackBrush;

            DrawTrackWithGeometry(canvas, template, pos, rot, brush, isGhost: false);
        }
    }

    public void RenderGhostTrack(
        Canvas canvas,
        TrackTemplate template,
        Point2D position,
        double rotationDeg,
        SolidColorBrush ghostBrush,
        double opacity = -1)
    {
        // Use provided opacity, or default based on theme (0.75 light, 0.85 dark)
        if (opacity < 0)
            opacity = 0.75; // Default for light theme

        var brush = new SolidColorBrush(ghostBrush.Color)
        {
            Opacity = opacity
        };
        DrawTrackWithGeometry(canvas, template, position, rotationDeg, brush, isGhost: true);
    }

    private static void DrawTrackWithGeometry(
        Canvas canvas,
        TrackTemplate template,
        Point2D position,
        double rotationDeg,
        SolidColorBrush brush,
        bool isGhost)
    {
        // Generate primitives using real geometry engine
        IEnumerable<IGeometryPrimitive> primitives = template.Geometry.GeometryKind switch
        {
            TrackGeometryKind.Straight => StraightGeometry.Render(template, position, rotationDeg),
            TrackGeometryKind.Curve => CurveGeometry.Render(template, position, rotationDeg),
            TrackGeometryKind.Switch => SwitchGeometry.Render(template, position, rotationDeg),
            _ => Enumerable.Empty<IGeometryPrimitive>()
        };

        // Convert primitives to WinUI shapes
        foreach (var primitive in primitives)
        {
            var shape = ConvertPrimitiveToShape(primitive, brush, isGhost);
            if (shape != null)
            {
                Canvas.SetZIndex(shape, 10);
                canvas.Children.Add(shape);
            }
        }
    }

    private static Shape? ConvertPrimitiveToShape(IGeometryPrimitive primitive, SolidColorBrush brush, bool isGhost)
    {
        var strokeThickness = TrackStrokeThickness;

        if (primitive is LinePrimitive line)
        {
            return new Line
            {
                X1 = line.From.X * DisplayScale,
                Y1 = line.From.Y * DisplayScale,
                X2 = line.To.X * DisplayScale,
                Y2 = line.To.Y * DisplayScale,
                Stroke = brush,
                StrokeThickness = strokeThickness
            };
        }

        if (primitive is ArcPrimitive arc)
        {
            return ConvertArcToPolyline(arc, brush, strokeThickness, null);
        }

        return null;
    }

    private static Polyline ConvertArcToPolyline(ArcPrimitive arc, SolidColorBrush brush, double strokeThickness, DoubleCollection? dash)
    {
        // Approximate arc as polyline with enough segments for smooth rendering
        // Use more segments for larger sweeps
        int segments = Math.Max(6, (int)Math.Ceiling(Math.Abs(arc.SweepAngleRad * 180 / Math.PI / 3)));
        var points = new PointCollection();

        for (int i = 0; i <= segments; i++)
        {
            double t = (double)i / segments;
            double angle = arc.StartAngleRad + t * arc.SweepAngleRad;

            double x = arc.Center.X + arc.Radius * Math.Cos(angle);
            double y = arc.Center.Y + arc.Radius * Math.Sin(angle);

            points.Add(new Windows.Foundation.Point(x * DisplayScale, y * DisplayScale));
        }

        return new Polyline
        {
            Points = points,
            Stroke = brush,
            StrokeThickness = strokeThickness,
            StrokeLineJoin = PenLineJoin.Round,
            StrokeDashArray = dash
        };
    }
}
