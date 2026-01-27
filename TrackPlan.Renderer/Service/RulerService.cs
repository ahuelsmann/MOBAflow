// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.TrackPlan.Renderer.Service;

using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Represents a single tick mark on a ruler.
/// </summary>
public sealed record RulerTick(
    double Position,           // Position in display coordinates (pixels)
    double WorldPosition,      // Position in world coordinates (mm)
    double Height,             // Height of tick mark
    string? Label = null);     // Optional label (e.g., "0cm", "100mm")

/// <summary>
/// Represents a ruler (horizontal or vertical) showing measurements.
/// Used for both fixed rulers (top/left) and movable rulers.
/// 
/// Design principle: Ruler shows both mm and cm depending on zoom level.
/// Zoom-dependent tick spacing prevents clutter.
/// </summary>
public sealed record RulerGeometry(
    IReadOnlyList<RulerTick> Ticks,
    int NumberOfLabels,
    double MinorTickHeight,
    double MajorTickHeight,
    string? Unit = null);

/// <summary>
/// Service for calculating ruler geometries based on zoom level and viewport.
/// Handles mm/cm conversion and intelligent tick mark spacing.
/// </summary>
public sealed class RulerService
{
    /// <summary>
    /// Piko A track library reference length (100mm standard segment).
    /// Used as base unit for ruler calculations.
    /// </summary>
    private const double PikoAUnitMm = 100.0;

    /// <summary>
    /// Calculate horizontal ruler ticks for a given viewport and zoom level.
    /// </summary>
    /// <param name="viewportStartX">Left edge of viewport in display coordinates</param>
    /// <param name="viewportEndX">Right edge of viewport in display coordinates</param>
    /// <param name="viewportWidth">Width of viewport in pixels</param>
    /// <param name="zoomLevel">Current zoom level (0.1 - 3.0)</param>
    /// <param name="displayScale">Display scale factor (e.g., 0.5 converts world to display)</param>
    public RulerGeometry CreateHorizontalRuler(
        double viewportStartX,
        double viewportEndX,
        double viewportWidth,
        double zoomLevel,
        double displayScale = 0.5)
    {
        // Convert viewport coordinates to world coordinates
        var worldStartX = viewportStartX / (displayScale * zoomLevel);
        var worldEndX = viewportEndX / (displayScale * zoomLevel);
        var worldRange = worldEndX - worldStartX;

        // Intelligent tick spacing: adjust based on zoom level and range
        var tickSpacing = CalculateTickSpacing(worldRange, zoomLevel);

        var ticks = GenerateTicks(
            worldStartX,
            worldRange,
            tickSpacing,
            viewportStartX,
            displayScale,
            zoomLevel,
            isHorizontal: true);

        return new RulerGeometry(
            Ticks: ticks,
            NumberOfLabels: ticks.Count(t => t.Label != null),
            MinorTickHeight: 4.0,
            MajorTickHeight: 8.0,
            Unit: GetUnitForZoom(zoomLevel));
    }

    /// <summary>
    /// Calculate vertical ruler ticks for a given viewport and zoom level.
    /// </summary>
    public RulerGeometry CreateVerticalRuler(
        double viewportStartY,
        double viewportEndY,
        double viewportHeight,
        double zoomLevel,
        double displayScale = 0.5)
    {
        // Convert viewport coordinates to world coordinates
        var worldStartY = viewportStartY / (displayScale * zoomLevel);
        var worldEndY = viewportEndY / (displayScale * zoomLevel);
        var worldRange = worldEndY - worldStartY;

        // Intelligent tick spacing
        var tickSpacing = CalculateTickSpacing(worldRange, zoomLevel);

        var ticks = GenerateTicks(
            worldStartY,
            worldRange,
            tickSpacing,
            viewportStartY,
            displayScale,
            zoomLevel,
            isHorizontal: false);

        return new RulerGeometry(
            Ticks: ticks,
            NumberOfLabels: ticks.Count(t => t.Label != null),
            MinorTickHeight: 4.0,
            MajorTickHeight: 8.0,
            Unit: GetUnitForZoom(zoomLevel));
    }

    /// <summary>
    /// Calculate optimal tick spacing based on visible world range and zoom.
    /// Returns tick spacing in world coordinates (mm).
    /// </summary>
    private double CalculateTickSpacing(double worldRange, double zoomLevel)
    {
        // At low zoom, need larger tick spacing to avoid clutter
        // At high zoom, can show finer detail

        if (zoomLevel < 0.5)
            return 500.0;   // 5cm at very low zoom
        else if (zoomLevel < 1.0)
            return 200.0;   // 2cm at low zoom
        else if (zoomLevel < 1.5)
            return 100.0;   // 1cm (standard Piko A unit)
        else if (zoomLevel < 2.0)
            return 50.0;    // 0.5cm
        else
            return 20.0;    // 2mm at high zoom
    }

    /// <summary>
    /// Generate tick marks for a ruler.
    /// </summary>
    private IReadOnlyList<RulerTick> GenerateTicks(
        double worldStart,
        double worldRange,
        double tickSpacing,
        double viewportStart,
        double displayScale,
        double zoomLevel,
        bool isHorizontal)
    {
        var ticks = new List<RulerTick>();

        // Calculate number of ticks needed
        var numTicks = (int)Math.Ceiling(worldRange / tickSpacing) + 2;

        // Determine label interval (show labels every N ticks, not all)
        var labelInterval = tickSpacing > 100 ? 1 : (tickSpacing > 50 ? 2 : 5);
        var tickCount = 0;

        for (double worldPos = Math.Floor(worldStart / tickSpacing) * tickSpacing;
             worldPos < worldStart + worldRange;
             worldPos += tickSpacing)
        {
            // Convert world position to display coordinates
            var displayPos = viewportStart + (worldPos - worldStart) * displayScale * zoomLevel;

            // Determine if this is a major or minor tick
            var isMajor = (tickCount % labelInterval) == 0;
            var height = isMajor ? 8.0 : 4.0;

            // Generate label for major ticks
            string? label = null;
            if (isMajor)
            {
                label = FormatRulerLabel(worldPos, GetUnitForZoom(zoomLevel));
            }

            ticks.Add(new RulerTick(
                Position: displayPos,
                WorldPosition: worldPos,
                Height: height,
                Label: label));

            tickCount++;
        }

        return ticks;
    }

    /// <summary>
    /// Determine unit (mm or cm) based on zoom level.
    /// </summary>
    private string GetUnitForZoom(double zoomLevel)
        => zoomLevel > 1.5 ? "mm" : "cm";

    /// <summary>
    /// Format ruler label (e.g., "0cm", "10cm", "100mm").
    /// </summary>
    private string FormatRulerLabel(double worldPositionMm, string unit)
        => unit switch
        {
            "cm" => $"{worldPositionMm / 10:F0}cm",
            "mm" => $"{worldPositionMm:F0}mm",
            _ => $"{worldPositionMm:F0}"
        };

    /// <summary>
    /// Calculate world coordinates from display coordinates using current viewport.
    /// Useful for positioning movable ruler.
    /// </summary>
    public Point2D DisplayToWorld(
        double displayX,
        double displayY,
        double viewportOffsetX,
        double viewportOffsetY,
        double displayScale,
        double zoomLevel)
    {
        var worldX = (displayX + viewportOffsetX) / (displayScale * zoomLevel);
        var worldY = (displayY + viewportOffsetY) / (displayScale * zoomLevel);
        return new Point2D(worldX, worldY);
    }

    /// <summary>
    /// Calculate display coordinates from world coordinates.
    /// </summary>
    public (double DisplayX, double DisplayY) WorldToDisplay(
        double worldX,
        double worldY,
        double viewportOffsetX,
        double viewportOffsetY,
        double displayScale,
        double zoomLevel)
    {
        var displayX = (worldX * displayScale * zoomLevel) - viewportOffsetX;
        var displayY = (worldY * displayScale * zoomLevel) - viewportOffsetY;
        return (displayX, displayY);
    }
}
