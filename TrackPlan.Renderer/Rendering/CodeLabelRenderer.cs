// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.TrackPlan.Renderer.Rendering;

using Moba.TrackPlan.TrackSystem;
using Moba.TrackPlan.Renderer.World;

/// <summary>
/// Renders track code labels (e.g., "G231", "R9", "BWL") centered on track pieces.
/// Labels are toggleable and theme-aware for optimal readability.
/// </summary>
public static class CodeLabelRenderer
{
    /// <summary>
    /// Calculate center position for rendering code label on track.
    /// </summary>
    /// <param name="template">Track template containing geometry</param>
    /// <param name="edgePosition">Position of track edge</param>
    /// <param name="rotationDeg">Rotation of track in degrees</param>
    /// <returns>Center point (X, Y) in world coordinates</returns>
    public static Point2D CalculateLabelPosition(
        TrackTemplate template,
        Point2D edgePosition,
        double rotationDeg)
    {
        ArgumentNullException.ThrowIfNull(template);
        ArgumentNullException.ThrowIfNull(edgePosition);

        // For straight tracks: center is at midpoint of length
        if (template.Geometry.GeometryKind == TrackGeometryKind.Straight && template.Geometry.LengthMm.HasValue)
        {
            var halfLength = template.Geometry.LengthMm.Value / 2.0;
            var radians = rotationDeg * Math.PI / 180.0;
            
            return new Point2D(
                edgePosition.X + halfLength * Math.Cos(radians),
                edgePosition.Y + halfLength * Math.Sin(radians)
            );
        }

        // For curves: center is at arc midpoint
        if (template.Geometry.GeometryKind == TrackGeometryKind.Curve 
            && template.Geometry.RadiusMm.HasValue 
            && template.Geometry.AngleDeg.HasValue)
        {
            var radius = template.Geometry.RadiusMm.Value;
            var halfAngle = template.Geometry.AngleDeg.Value / 2.0;
            var angleRad = (rotationDeg + halfAngle) * Math.PI / 180.0;
            
            return new Point2D(
                edgePosition.X + radius * Math.Cos(angleRad),
                edgePosition.Y + radius * Math.Sin(angleRad)
            );
        }

        // For switches/crossings: use edge position as approximate center
        return edgePosition;
    }

    /// <summary>
    /// Get font size for code label based on track type and zoom level.
    /// </summary>
    /// <param name="geometryKind">Track geometry type</param>
    /// <param name="zoomFactor">Current zoom level (1.0 = 100%)</param>
    /// <returns>Font size in points</returns>
    public static double GetLabelFontSize(TrackGeometryKind geometryKind, double zoomFactor = 1.0)
    {
        var baseFontSize = geometryKind switch
        {
            TrackGeometryKind.Straight => 10.0,
            TrackGeometryKind.Curve => 9.0,
            TrackGeometryKind.Switch or TrackGeometryKind.TwoWaySwitch => 8.0,
            TrackGeometryKind.ThreeWaySwitch => 7.0,
            TrackGeometryKind.Crossing => 8.0,
            TrackGeometryKind.DoubleCrossover => 7.0,
            TrackGeometryKind.Endcap => 8.0,
            _ => 9.0
        };

        return baseFontSize * zoomFactor;
    }

    /// <summary>
    /// Get label text color based on theme.
    /// </summary>
    /// <param name="isDarkTheme">Whether dark theme is active</param>
    /// <returns>RGB tuple (R, G, B) values 0-255</returns>
    public static (byte R, byte G, byte B) GetLabelColor(bool isDarkTheme)
    {
        // Light theme: dark gray (high contrast on light background)
        // Dark theme: light gray (high contrast on dark background)
        return isDarkTheme 
            ? ((byte)220, (byte)220, (byte)220)  // Light gray
            : ((byte)40, (byte)40, (byte)40);    // Dark gray
    }

    /// <summary>
    /// Get label background color (optional, for enhanced readability).
    /// </summary>
    /// <param name="isDarkTheme">Whether dark theme is active</param>
    /// <returns>RGB tuple (R, G, B) values 0-255</returns>
    public static (byte R, byte G, byte B) GetBackgroundColor(bool isDarkTheme)
    {
        // Semi-transparent background for contrast
        return isDarkTheme 
            ? ((byte)30, (byte)30, (byte)30)     // Dark background
            : ((byte)255, (byte)255, (byte)255); // Light background
    }

    /// <summary>
    /// Get background opacity (0.0-1.0).
    /// </summary>
    /// <returns>Opacity value</returns>
    public static float GetBackgroundOpacity() => 0.7f;

    /// <summary>
    /// Check if label should be visible for given track type.
    /// Some track types (e.g., very short segments) may skip labels.
    /// </summary>
    /// <param name="template">Track template</param>
    /// <returns>True if label should be shown</returns>
    public static bool ShouldShowLabel(TrackTemplate template)
    {
        ArgumentNullException.ThrowIfNull(template);

        // Skip labels for very short straight tracks
        if (template.Geometry.GeometryKind == TrackGeometryKind.Straight 
            && template.Geometry.LengthMm.HasValue 
            && template.Geometry.LengthMm.Value < 35.0)
        {
            return false;
        }

        return true;
    }
}
