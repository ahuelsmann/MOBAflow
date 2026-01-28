// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.TrackPlan.Renderer.Rendering;

using Moba.TrackLibrary.Base.TrackSystem;
using Moba.TrackPlan.Geometry;

using System;
using System.Collections.Generic;

/// <summary>
/// Renders visual indicators for switch position states (Straight/Diverging).
/// Provides theme-aware styling and animation support.
/// Also supports switch type indicators (WL, WR, W3, BWL, BWR) for visual pattern recognition.
/// </summary>
public sealed class PositionStateRenderer
{
    /// <summary>
    /// Visual indicator that can be rendered at a switch location.
    /// </summary>
    public sealed record PositionIndicator(
        Point2D CenterWorldPosition,
        double RotationDegrees,
        SwitchPositionState State,
        bool IsVisible);

    /// <summary>
    /// Visual indicator for switch type (WL, WR, W3, BWL, BWR).
    /// Phase 9.2: Type indicators help users quickly identify switch variants through pattern recognition.
    /// </summary>
    public sealed record TypeIndicator(
        Point2D CenterWorldPosition,
        SwitchTypeVariant TypeVariant,
        bool IsVisible,
        float Opacity = 0.6f);

    /// <summary>
    /// Gets the visual indicator symbol for a switch position state.
    /// </summary>
    /// <param name="state">Current switch state</param>
    /// <returns>Unicode symbol representing the state</returns>
    public static string GetStateSymbol(SwitchPositionState state)
        => state switch
        {
            SwitchPositionState.Straight => "—",      // Horizontal bar (straight)
            SwitchPositionState.Diverging => "↗",     // Northeast arrow (diverging)
            _ => "?"
        };

    /// <summary>
    /// Gets the theme-aware color for rendering a position state indicator.
    /// </summary>
    public static (byte R, byte G, byte B, byte A) GetStateColor(
        SwitchPositionState state,
        bool isDarkTheme)
    {
        // Light theme uses darker colors, dark theme uses brighter colors
        return state switch
        {
            SwitchPositionState.Straight => isDarkTheme
                ? ((byte)100, (byte)150, (byte)255, (byte)200)   // Blue (straight = primary route)
                : ((byte)0, (byte)51, (byte)153, (byte)200),

            SwitchPositionState.Diverging => isDarkTheme
                ? ((byte)255, (byte)180, (byte)80, (byte)200)    // Orange (diverging = alternate route)
                : ((byte)204, (byte)102, (byte)0, (byte)200),

            _ => isDarkTheme
                ? ((byte)200, (byte)200, (byte)200, (byte)100)   // Gray (unknown)
                : ((byte)128, (byte)128, (byte)128, (byte)100)
        };
    }

    /// <summary>
    /// Gets the opacity level for rendering a position state indicator.
    /// Straight state is more prominent (higher opacity), diverging is subtle.
    /// </summary>
    public static double GetStateOpacity(SwitchPositionState state, bool isDarkTheme)
        => state switch
        {
            SwitchPositionState.Straight => isDarkTheme ? 0.8 : 0.7,
            SwitchPositionState.Diverging => isDarkTheme ? 0.7 : 0.6,
            _ => 0.4
        };

    /// <summary>
    /// Calculates the indicator position relative to switch center.
    /// </summary>
    /// <param name="switchCenter">World position of switch center</param>
    /// <param name="switchRotationDeg">Current switch rotation in degrees</param>
    /// <param name="offsetDistanceMm">Offset distance from switch center in mm</param>
    /// <returns>Indicator position in world space</returns>
    public static Point2D CalculateIndicatorPosition(
        Point2D switchCenter,
        double switchRotationDeg,
        double offsetDistanceMm = 50)
    {
        // Position indicator slightly above/to-the-right of switch
        // Angle is fixed relative to switch orientation
        var angleRad = (switchRotationDeg + 45) * Math.PI / 180;
        var x = switchCenter.X + offsetDistanceMm * Math.Cos(angleRad);
        var y = switchCenter.Y + offsetDistanceMm * Math.Sin(angleRad);

        return new Point2D(x, y);
    }

    /// <summary>
    /// Creates a position indicator visual for rendering.
    /// </summary>
    public static PositionIndicator CreateIndicator(
        Point2D switchCenter,
        double switchRotationDeg,
        SwitchPositionState state,
        bool isVisible = true,
        double offsetMm = 50)
    {
        var indicatorPos = CalculateIndicatorPosition(switchCenter, switchRotationDeg, offsetMm);

        return new PositionIndicator(
            CenterWorldPosition: indicatorPos,
            RotationDegrees: switchRotationDeg,
            State: state,
            IsVisible: isVisible);
    }

    /// <summary>
    /// Gets a collection of position indicators for debugging/visualization.
    /// Shows indicators for all states at offset positions around the switch.
    /// </summary>
    public static IReadOnlyList<PositionIndicator> GetDebugIndicators(
        Point2D switchCenter,
        double switchRotationDeg,
        double radiusMm = 80)
    {
        var indicators = new List<PositionIndicator>();

        // Position 0°: Straight (always visible for debug)
        indicators.Add(new PositionIndicator(
            CenterWorldPosition: new Point2D(
                switchCenter.X + radiusMm,
                switchCenter.Y),
            RotationDegrees: switchRotationDeg,
            State: SwitchPositionState.Straight,
            IsVisible: true));

        // Position 90°: Diverging (always visible for debug)
        indicators.Add(new PositionIndicator(
            CenterWorldPosition: new Point2D(
                switchCenter.X,
                switchCenter.Y + radiusMm),
            RotationDegrees: switchRotationDeg,
            State: SwitchPositionState.Diverging,
            IsVisible: true));

        return indicators;
    }

    /// <summary>
    /// Phase 9.2: Gets visual symbol for switch type indicator.
    /// Uses Unicode directional arrows for intuitive pattern recognition.
    /// </summary>
    /// <param name="typeVariant">Switch type variant (WL, WR, W3, etc.)</param>
    /// <returns>Unicode symbol representing the type</returns>
    public static string GetTypeSymbol(SwitchTypeVariant typeVariant)
        => typeVariant switch
        {
            SwitchTypeVariant.WL => "◀",    // Left-pointing triangle (left bend)
            SwitchTypeVariant.WR => "▶",    // Right-pointing triangle (right bend)
            SwitchTypeVariant.W3 => "▼",    // Down-pointing triangle (three-way)
            SwitchTypeVariant.BWL => "↙",   // Down-left arrow (back-left)
            SwitchTypeVariant.BWR => "↘",   // Down-right arrow (back-right)
            _ => "?"
        };

    /// <summary>
    /// Phase 9.2: Gets theme-aware color for switch type indicator.
    /// Implements Gestalt Law: consistent color mapping for quick visual recognition.
    /// </summary>
    /// <param name="typeVariant">Switch type variant</param>
    /// <param name="isDarkTheme">Whether dark theme is active</param>
    /// <returns>RGB color tuple (R, G, B)</returns>
    public static (byte R, byte G, byte B) GetTypeColor(
        SwitchTypeVariant typeVariant,
        bool isDarkTheme)
    {
        // Light theme: darker saturated colors
        // Dark theme: lighter variants for visibility on dark background
        return (isDarkTheme, typeVariant) switch
        {
            // WL - Blue family
            (false, SwitchTypeVariant.WL) => (51, 102, 187),     // Dark blue
            (true, SwitchTypeVariant.WL) => (135, 180, 255),     // Light blue

            // WR - Red family  
            (false, SwitchTypeVariant.WR) => (192, 0, 0),        // Dark red
            (true, SwitchTypeVariant.WR) => (255, 102, 102),     // Light red

            // W3 - Green family (three-way)
            (false, SwitchTypeVariant.W3) => (0, 128, 0),        // Dark green
            (true, SwitchTypeVariant.W3) => (102, 255, 102),     // Light green

            // BWL - Orange family (back-left)
            (false, SwitchTypeVariant.BWL) => (204, 102, 0),     // Dark orange
            (true, SwitchTypeVariant.BWL) => (255, 180, 102),    // Light orange

            // BWR - Purple family (back-right)
            (false, SwitchTypeVariant.BWR) => (128, 0, 128),     // Dark purple
            (true, SwitchTypeVariant.BWR) => (200, 150, 255),    // Light purple

            _ => (128, 128, 128)  // Gray for unknown
        };
    }

    /// <summary>
    /// Phase 9.2: Calculates indicator position for switch type (Top-Left corner).
    /// Position follows standard reading/scanning order (top-left).
    /// </summary>
    /// <param name="switchCenterX">X coordinate of switch center (world coordinates)</param>
    /// <param name="switchCenterY">Y coordinate of switch center (world coordinates)</param>
    /// <param name="switchWidthMm">Width of switch in mm (typically 40-50)</param>
    /// <param name="offsetPixels">Pixel offset from top-left corner</param>
    /// <returns>Tuple (positionX, positionY) in world coordinates</returns>
    public static (double X, double Y) CalculateTypeIndicatorPosition(
        double switchCenterX,
        double switchCenterY,
        double switchWidthMm = 40,
        double offsetPixels = 8)
    {
        // Convert to display coordinates (accounting for rendering scale ~0.5)
        const double displayScale = 0.5;
        var displayWidth = switchWidthMm * displayScale;
        var halfWidth = displayWidth / 2;

        // Position at top-left with small offset (reading order)
        var indicatorX = switchCenterX - halfWidth + offsetPixels;
        var indicatorY = switchCenterY - halfWidth + offsetPixels;

        return (indicatorX, indicatorY);
    }

    /// <summary>
    /// Phase 9.2: Creates a type indicator for rendering.
    /// </summary>
    /// <param name="switchCenter">World position of switch center</param>
    /// <param name="typeVariant">Switch type variant</param>
    /// <param name="isVisible">Whether indicator should be rendered</param>
    /// <param name="opacity">Opacity for indicator (0.0-1.0)</param>
    /// <returns>TypeIndicator ready for rendering</returns>
    public static TypeIndicator CreateTypeIndicator(
        Point2D switchCenter,
        SwitchTypeVariant typeVariant,
        bool isVisible = true,
        float opacity = 0.6f)
    {
        var (indicatorX, indicatorY) = CalculateTypeIndicatorPosition(
            switchCenter.X,
            switchCenter.Y);

        return new TypeIndicator(
            CenterWorldPosition: new Point2D(indicatorX, indicatorY),
            TypeVariant: typeVariant,
            IsVisible: isVisible,
            Opacity: opacity);
    }
}

/// <summary>
/// Phase 9.2: Switch type variant enumeration.
/// Represents Piko A-Gleis switch types for visual identification.
/// </summary>
public enum SwitchTypeVariant
{
    /// <summary>Left-handed switch (linke Weiche)</summary>
    WL = 0,

    /// <summary>Right-handed switch (rechte Weiche)</summary>
    WR = 1,

    /// <summary>Three-way switch (Dreiwegweiche)</summary>
    W3 = 2,

    /// <summary>Back-left switch (Rückweiche links)</summary>
    BWL = 3,

    /// <summary>Back-right switch (Rückweiche rechts)</summary>
    BWR = 4
}
