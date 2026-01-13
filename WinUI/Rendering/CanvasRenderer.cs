// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.WinUI.Rendering;

using Microsoft.UI;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

using Moba.TrackPlan.Editor.ViewModel;
using Moba.TrackPlan.TrackSystem;

using System.Globalization;
using Windows.UI;

/// <summary>
/// Renders track plan edges onto a WinUI Canvas.
/// Converts world coordinates (mm) to screen coordinates (px) using displayScale.
/// </summary>
public sealed class CanvasRenderer
{
    /// <summary>
    /// Renders all edges from the view model onto the canvas.
    /// </summary>
    /// <param name="canvas">The target WinUI Canvas.</param>
    /// <param name="viewModel">The TrackPlanEditorViewModel containing edges and positions.</param>
    /// <param name="catalog">The track catalog for template lookup.</param>
    /// <param name="trackBrush">Default track color.</param>
    /// <param name="selectedBrush">Selected track color.</param>
    /// <param name="hoverBrush">Hovered track color.</param>
    /// <param name="displayScale">Scale factor: 1mm = displayScale pixels (e.g., 0.5).</param>
    public void Render(
        Canvas canvas,
        TrackPlanEditorViewModel viewModel,
        ITrackCatalog catalog,
        SolidColorBrush trackBrush,
        SolidColorBrush selectedBrush,
        SolidColorBrush hoverBrush,
        double displayScale = 0.5)
    {
        foreach (var edge in viewModel.Graph.Edges)
        {
            if (!viewModel.Positions.TryGetValue(edge.Id, out _))
                continue;

            var primitives = viewModel.RenderEdge(edge.Id);

            var isSelected = viewModel.SelectedTrackId == edge.Id
                          || viewModel.SelectedTrackIds.Contains(edge.Id);
            var isHovered = viewModel.HoveredTrackId == edge.Id;

            // Determine brush: selection > hover > section color > default
            SolidColorBrush brush;
            if (isSelected)
            {
                brush = selectedBrush;
            }
            else if (isHovered)
            {
                brush = hoverBrush;
            }
            else
            {
                var section = viewModel.GetSectionForTrack(edge.Id);
                brush = section is not null
                    ? new SolidColorBrush(ParseColor(section.Color))
                    : trackBrush;
            }

            foreach (var primitive in primitives)
            {
                var shape = PrimitiveShapeFactory.CreateShape(primitive, displayScale);
                shape.Stroke = brush;
                canvas.Children.Add(shape);
            }
        }
    }

    private static Color ParseColor(string hex)
    {
        hex = hex.TrimStart('#');
        if (hex.Length == 6)
        {
            return Color.FromArgb(255,
                byte.Parse(hex[..2], NumberStyles.HexNumber),
                byte.Parse(hex[2..4], NumberStyles.HexNumber),
                byte.Parse(hex[4..6], NumberStyles.HexNumber));
        }
        return Colors.Gray;
    }
}