// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.TrackPlan.Renderer.Rendering;

/// <summary>
/// Renders visual type indicators for different switch variants.
/// 
/// Implements neuroscience "Gestalt Law" - visual pattern recognition through
/// consistent symbols and colors. Users quickly learn: WL=◀ Blue, WR=▶ Red, W3=▼ Green.
/// 
/// This reduces cognitive load by providing instant visual feedback about switch type
/// without requiring text labels or detailed inspection.
/// </summary>
public static class SwitchTypeIndicatorRenderer
{
    /// <summary>
    /// Get the visual symbol for a switch type.
    /// Uses Unicode directional arrows for intuitive representation.
    /// </summary>
    /// <param name="variant">The switch variant type</param>
    /// <returns>Unicode symbol: "◀" (WL), "▶" (WR), "▼" (W3), "↙" (BWL), "↘" (BWR)</returns>
    public static string GetTypeSymbol(SwitchVariant variant)
    {
        return variant switch
        {
            SwitchVariant.WL => "◀",   // Left-pointing triangle (left bend)
            SwitchVariant.WR => "▶",   // Right-pointing triangle (right bend)
            SwitchVariant.W3 => "▼",   // Down-pointing triangle (three-way)
            SwitchVariant.BWL => "↙",  // Down-left arrow (back-left)
            SwitchVariant.BWR => "↘",  // Down-right arrow (back-right)
            _ => "?"                     // Unknown
        };
    }

    /// <summary>
    /// Get theme-aware RGB color for the switch type indicator.
    /// Colors are consistent with Fluent Design System and accessible.
    /// Format: (R, G, B) where each is 0-255.
    /// </summary>
    /// <param name="variant">The switch variant type</param>
    /// <param name="isDarkTheme">Whether dark theme is active</param>
    /// <returns>RGB tuple (R, G, B) values 0-255</returns>
    public static (byte R, byte G, byte B) GetTypeColor(SwitchVariant variant, bool isDarkTheme)
    {
        // Light theme uses darker saturated colors, Dark theme uses lighter variants
        return (isDarkTheme, variant) switch
        {
            // WL - Blue family
            (false, SwitchVariant.WL) => (51, 102, 187),    // Dark blue
            (true, SwitchVariant.WL) => (135, 180, 255),    // Light blue

            // WR - Red family  
            (false, SwitchVariant.WR) => (192, 0, 0),       // Dark red
            (true, SwitchVariant.WR) => (255, 102, 102),    // Light red

            // W3 - Green family (three-way)
            (false, SwitchVariant.W3) => (0, 128, 0),       // Dark green
            (true, SwitchVariant.W3) => (102, 255, 102),    // Light green

            // BWL - Orange family (back-left)
            (false, SwitchVariant.BWL) => (204, 102, 0),    // Dark orange
            (true, SwitchVariant.BWL) => (255, 180, 102),   // Light orange

            // BWR - Purple family (back-right)
            (false, SwitchVariant.BWR) => (128, 0, 128),    // Dark purple
            (true, SwitchVariant.BWR) => (200, 150, 255),   // Light purple

            _ => (128, 128, 128)  // Gray for unknown
        };
    }

    /// <summary>
    /// Get display opacity for type indicator.
    /// Slightly transparent to avoid visual clutter while remaining visible.
    /// </summary>
    /// <param name="isDarkTheme">Whether dark theme is active</param>
    /// <returns>Opacity (0.0-1.0)</returns>
    public static float GetIndicatorOpacity(bool isDarkTheme)
    {
        // Light theme: lower opacity (indicator blends slightly with light background)
        // Dark theme: higher opacity (indicator stands out more on dark background)
        return isDarkTheme ? 0.75f : 0.60f;
    }

    /// <summary>
    /// Calculate position for rendering type indicator on switch.
    /// Position is Top-Left of the switch bounds (follows reading/scanning order).
    /// </summary>
    /// <param name="switchCenterX">X coordinate of switch center (world coordinates)</param>
    /// <param name="switchCenterY">Y coordinate of switch center (world coordinates)</param>
    /// <param name="switchWidthMm">Width of switch in mm</param>
    /// <param name="offsetPixels">Pixel offset from top-left corner (for spacing)</param>
    /// <returns>Tuple (positionX, positionY) in world coordinates</returns>
    public static (double X, double Y) CalculateIndicatorPosition(
        double switchCenterX,
        double switchCenterY,
        double switchWidthMm = 40,
        double offsetPixels = 8)
    {
        // Convert to display coordinates (accounting for rendering scale)
        const double displayScale = 0.5;
        var displayWidth = switchWidthMm * displayScale;
        var halfWidth = displayWidth / 2;

        // Position at top-left with small offset
        var indicatorX = switchCenterX - halfWidth + offsetPixels;
        var indicatorY = switchCenterY - halfWidth + offsetPixels;

        return (indicatorX, indicatorY);
    }

    /// <summary>
    /// Suggested font size for type indicator symbol (in points).
    /// Scale-independent - renderer should apply based on theme/zoom.
    /// </summary>
    public const double IndicatorFontSizePt = 10.0;

    /// <summary>
    /// Get human-readable description of switch type (for accessibility/tooltips).
    /// </summary>
    /// <param name="variant">The switch variant type</param>
    /// <returns>Descriptive text like "Left-handed switch", "Three-way switch"</returns>
    public static string GetTypeDescription(SwitchVariant variant)
    {
        return variant switch
        {
            SwitchVariant.WL => "Left-handed switch (WL) - curves to the left",
            SwitchVariant.WR => "Right-handed switch (WR) - curves to the right",
            SwitchVariant.W3 => "Three-way switch (W3) - three branches",
            SwitchVariant.BWL => "Back-left switch (BWL) - back diverging left",
            SwitchVariant.BWR => "Back-right switch (BWR) - back diverging right",
            _ => "Unknown switch variant"
        };
    }

    /// <summary>
    /// Check if variant is a left-hand type (WL or BWL).
    /// </summary>
    public static bool IsLeftVariant(SwitchVariant variant)
    {
        return variant is SwitchVariant.WL or SwitchVariant.BWL;
    }

    /// <summary>
    /// Check if variant is a right-hand type (WR or BWR).
    /// </summary>
    public static bool IsRightVariant(SwitchVariant variant)
    {
        return variant is SwitchVariant.WR or SwitchVariant.BWR;
    }

    /// <summary>
    /// Check if variant is a three-way switch (W3).
    /// </summary>
    public static bool IsThreeWay(SwitchVariant variant)
    {
        return variant == SwitchVariant.W3;
    }
}

/// <summary>
/// Switch variant enumeration matching Piko A-Gleis types.
/// Used by SwitchTypeIndicatorRenderer for visual identification.
/// </summary>
public enum SwitchVariant
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
