// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;

using Moba.TrackLibrary.Base.TrackSystem;
using Moba.TrackPlan.Editor.ViewModel;
using Moba.TrackPlan.Geometry;
using Moba.TrackPlan.Renderer.Rendering;

using Windows.UI;

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

    /// <summary>
    /// Renders a track template as a small icon for toolbox display (40x24px viewport).
    /// Calculates scale to fit the track geometry and centers it in the canvas.
    /// Used for toolbox ItemTemplate DataTemplates to show accurate track geometries.
    /// </summary>
    public void RenderToolboxIcon(
        Canvas toolboxCanvas,
        TrackTemplate template,
        SolidColorBrush accentBrush)
    {
        ArgumentNullException.ThrowIfNull(toolboxCanvas);
        ArgumentNullException.ThrowIfNull(template);
        ArgumentNullException.ThrowIfNull(accentBrush);

        var spec = template.Geometry;

        // Calculate scale to fit in ~36px width (leaving ~2px margin per side)
        double maxDimension = spec.GeometryKind switch
        {
            TrackGeometryKind.Straight => spec.LengthMm!.Value,
            TrackGeometryKind.Curve => spec.RadiusMm!.Value * 1.8,
            TrackGeometryKind.Switch => Math.Max(spec.LengthMm!.Value, spec.RadiusMm!.Value * 1.5),
            _ => 100
        };

        // Scale factor to fit in 36px viewport
        double iconScale = 36.0 / maxDimension;
        var centerPosition = new Point2D(20, 12);

        var iconBrush = new SolidColorBrush(accentBrush.Color);
        RenderTrackWithScale(toolboxCanvas, template, centerPosition, 0, iconBrush, iconScale, 2.0);
    }

    /// <summary>
    /// Internal rendering with custom scale (used for toolbox icons).
    /// </summary>
    private static void RenderTrackWithScale(
        Canvas canvas,
        TrackTemplate template,
        Point2D position,
        double rotationDeg,
        SolidColorBrush brush,
        double scale,
        double strokeThickness)
    {
        var primitives = template.Geometry.GeometryKind switch
        {
            TrackGeometryKind.Straight => StraightGeometry.Render(template, position, rotationDeg),
            TrackGeometryKind.Curve => CurveGeometry.Render(template, position, rotationDeg),
            TrackGeometryKind.Switch => SwitchGeometry.Render(template, position, rotationDeg),
            _ => Enumerable.Empty<IGeometryPrimitive>()
        };

        foreach (var primitive in primitives)
        {
            var shape = ConvertPrimitiveToShapeWithScale(primitive, brush, scale, strokeThickness);
            if (shape != null)
            {
                Canvas.SetZIndex(shape, 10);
                canvas.Children.Add(shape);
            }
        }
    }

    private static Shape? ConvertPrimitiveToShapeWithScale(
        IGeometryPrimitive primitive,
        SolidColorBrush brush,
        double scale,
        double strokeThickness)
    {
        if (primitive is LinePrimitive line)
        {
            return new Line
            {
                X1 = line.From.X * scale,
                Y1 = line.From.Y * scale,
                X2 = line.To.X * scale,
                Y2 = line.To.Y * scale,
                Stroke = brush,
                StrokeThickness = strokeThickness
            };
        }

        if (primitive is ArcPrimitive arc)
        {
            return ConvertArcToPolylineWithScale(arc, brush, strokeThickness, scale);
        }

        return null;
    }

    private static Polyline ConvertArcToPolylineWithScale(
        ArcPrimitive arc,
        SolidColorBrush brush,
        double strokeThickness,
        double scale)
    {
        // Approximate arc as polyline with enough segments for smooth rendering
        int segments = Math.Max(6, (int)Math.Ceiling(Math.Abs(arc.SweepAngleRad * 180 / Math.PI / 3)));
        var points = new PointCollection();

        for (int i = 0; i <= segments; i++)
        {
            double t = (double)i / segments;
            double angle = arc.StartAngleRad + t * arc.SweepAngleRad;

            double x = arc.Center.X + arc.Radius * Math.Cos(angle);
            double y = arc.Center.Y + arc.Radius * Math.Sin(angle);

            points.Add(new Windows.Foundation.Point(x * scale, y * scale));
        }

        return new Polyline
        {
            Points = points,
            Stroke = brush,
            StrokeThickness = strokeThickness,
            StrokeLineJoin = PenLineJoin.Round
        };
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

    /// <summary>
    /// Phase 9.2: Renders switch type indicators (WL/WR/W3/BWL/BWR) on canvas.
    /// Shows Unicode symbols with theme-aware colors at top-left of each switch.
    /// </summary>
    public void RenderTypeIndicators(
        Canvas canvas,
        TrackPlanEditorViewModel viewModel,
        ITrackCatalog catalog,
        bool isDarkTheme)
    {
        foreach (var edge in viewModel.Graph.Edges)
        {
            var template = catalog.GetById(edge.TemplateId);
            if (template?.Geometry.GeometryKind != TrackGeometryKind.Switch)
                continue;  // Only render for switches

            if (!viewModel.Positions.TryGetValue(edge.Id, out var pos))
                continue;

            // Determine switch type variant
            var typeVariant = GetSwitchTypeVariant(template);

            // Get position for indicator (top-left of switch)
            var (indicatorX, indicatorY) = PositionStateRenderer.CalculateTypeIndicatorPosition(
                pos.X, pos.Y, 40, 8);

            // Get symbol and color
            var symbol = PositionStateRenderer.GetTypeSymbol(typeVariant);
            var (r, g, b) = PositionStateRenderer.GetTypeColor(typeVariant, isDarkTheme);

            // Create TextBlock for indicator
            var textBlock = new TextBlock
            {
                Text = symbol,
                FontSize = 10.0,
                FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromArgb(255, r, g, b)),
                TextAlignment = TextAlignment.Center,
                Opacity = isDarkTheme ? 0.75 : 0.60
            };

            Canvas.SetLeft(textBlock, indicatorX * DisplayScale - 5);
            Canvas.SetTop(textBlock, indicatorY * DisplayScale - 5);
            Canvas.SetZIndex(textBlock, 50);  // Above tracks
            canvas.Children.Add(textBlock);
        }
    }

    /// <summary>
    /// Phase 9.3: Renders hover effects for ports (scale + glow).
    /// </summary>
    public void RenderPortHoverEffects(
        Canvas canvas,
        IReadOnlySet<(System.Guid TrackId, string PortId)> highlightedPorts,
        TrackPlanEditorViewModel viewModel,
        ITrackCatalog catalog)
    {
        if (highlightedPorts.Count == 0)
            return;

        const double portRadius = 8.0;
        const double hoverScale = 1.5;  // 1.5x larger on hover

        foreach (var (trackId, portId) in highlightedPorts)
        {
            if (!viewModel.Positions.TryGetValue(trackId, out var pos))
                continue;

            var edge = viewModel.Graph.Edges.FirstOrDefault(e => e.Id == trackId);
            if (edge is null)
                continue;

            var template = catalog.GetById(edge.TemplateId);
            if (template is null)
                continue;

            // Get port offset from geometry
            var end = template.Ends.FirstOrDefault(e => e.Id == portId);
            if (end is null)
                continue;

            var rot = viewModel.Rotations.GetValueOrDefault(trackId, 0.0);
            var portOffset = GetPortOffset(template, portId, rot);

            var portX = (pos.X + portOffset.X) * DisplayScale;
            var portY = (pos.Y + portOffset.Y) * DisplayScale;

            // Draw hover halo (larger semi-transparent circle)
            var halo = new Ellipse
            {
                Width = portRadius * 2 * hoverScale,
                Height = portRadius * 2 * hoverScale,
                Fill = new SolidColorBrush(Color.FromArgb(80, 100, 200, 255)),  // Subtle blue glow
                Stroke = new SolidColorBrush(Color.FromArgb(150, 100, 150, 255)),
                StrokeThickness = 1.0
            };

            Canvas.SetLeft(halo, portX - portRadius * hoverScale);
            Canvas.SetTop(halo, portY - portRadius * hoverScale);
            Canvas.SetZIndex(halo, 40);  // Below port but above tracks
            canvas.Children.Add(halo);
        }
    }

    /// <summary>
    /// Phase 9.3: Renders hover effects for tracks (subtle outline).
    /// </summary>
    public void RenderTrackHoverEffects(
        Canvas canvas,
        IReadOnlySet<System.Guid> hoveredTracks,
        TrackPlanEditorViewModel viewModel)
    {
        if (hoveredTracks.Count == 0)
            return;

        var selectedBrush = new SolidColorBrush(Color.FromArgb(200, 255, 200, 50));  // Yellow highlight

        foreach (var trackId in hoveredTracks)
        {
            if (!viewModel.Positions.TryGetValue(trackId, out var pos))
                continue;

            // Draw subtle selection indicator
            var hoverBox = new Rectangle
            {
                Width = 120,
                Height = 120,
                Stroke = selectedBrush,
                StrokeThickness = 2.0,
                Fill = new SolidColorBrush { Color = Colors.Yellow, Opacity = 0.05 },
                StrokeDashArray = new DoubleCollection { 3, 3 }
            };

            Canvas.SetLeft(hoverBox, (pos.X - 60) * DisplayScale);
            Canvas.SetTop(hoverBox, (pos.Y - 60) * DisplayScale);
            Canvas.SetZIndex(hoverBox, 5);  // Below tracks
            canvas.Children.Add(hoverBox);
        }
    }

    /// <summary>
    /// Determines switch type variant from geometry.
    /// Maps SwitchGeometry properties to SwitchTypeVariant enum.
    /// </summary>
    private static SwitchTypeVariant GetSwitchTypeVariant(TrackTemplate template)
    {
        var geometry = template.Geometry;

        // Use geometry shape analysis to determine type
        // This is a simplified version - could be enhanced with more geometry analysis
        if (geometry.LengthMm.HasValue && geometry.RadiusMm.HasValue)
        {
            // Check if it's a back-switch (curved) based on angle
            var angle = geometry.AngleDeg ?? 0;
            var isBack = angle < 0 || geometry.JunctionOffsetMm > (geometry.LengthMm.Value * 0.6);

            // Check left vs right based on sweep direction
            var isLeft = Math.Sign(angle) > 0 || template.Id.Contains("L");
            var isRight = Math.Sign(angle) < 0 || template.Id.Contains("R");

            // Three-way check
            if (template.Id.Contains("3W") || template.Id.Contains("W3"))
                return SwitchTypeVariant.W3;

            // Back switches
            if (isBack)
                return isLeft ? SwitchTypeVariant.BWL : SwitchTypeVariant.BWR;

            // Regular left/right
            return isLeft ? SwitchTypeVariant.WL : SwitchTypeVariant.WR;
        }

        return SwitchTypeVariant.WR;  // Default
    }

    /// <summary>
    /// Helper: Get port offset in world coordinates.
    /// </summary>
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
            double sweepRad = spec.AngleDeg!.Value * Math.PI / 180.0;

            if (portId == "A")
                return Rot(new Point2D(0, 0), rotRad);

            var endLocal = new Point2D(
                radius * Math.Sin(sweepRad),
                radius - radius * Math.Cos(sweepRad));

            return Rot(endLocal, rotRad);
        }

        if (spec.GeometryKind == TrackGeometryKind.Switch)
        {
            double length = spec.LengthMm!.Value;
            double radius = spec.RadiusMm!.Value;
            double sweepRad = spec.AngleDeg!.Value * Math.PI / 180.0;
            double junction = spec.JunctionOffsetMm ?? (length / 2.0);

            if (portId == "A")
                return Rot(new Point2D(0, 0), rotRad);

            if (portId == "B")
                return Rot(new Point2D(length, 0), rotRad);

            if (portId == "C")
            {
                var j = new Point2D(junction, 0);
                var center = new Point2D(j.X, j.Y + radius);
                double startAngle = Math.Atan2(j.Y - center.Y, j.X - center.X);

                var endLocal = new Point2D(
                    center.X + radius * Math.Cos(startAngle + sweepRad),
                    center.Y + radius * Math.Sin(startAngle + sweepRad));

                var rel = new Point2D(endLocal.X - 0.0, endLocal.Y - 0.0);
                return Rot(rel, rotRad);
            }
        }

        return new Point2D(0, 0);
    }
}
