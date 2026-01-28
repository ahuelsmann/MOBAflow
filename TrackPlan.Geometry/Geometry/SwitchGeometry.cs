// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.TrackPlan.Geometry;

using Moba.TrackLibrary.Base.TrackSystem;

/// <summary>
/// Calculates and renders switch (turnout) track geometry.
/// Supports left and right variants (WL/WR, BWL/BWR, etc.) across all track libraries.
/// </summary>
public static class SwitchGeometry
{
    /// <summary>
    /// Renders a switch track template to primitives (straight and diverging paths).
    /// Automatically detects left (WL, BWL) vs right (WR, BWR) from template ID.
    /// Uses SwitchRoutingModel to determine port configuration.
    /// </summary>
    public static IEnumerable<IGeometryPrimitive> Render(
        TrackTemplate template,
        Point2D start,
        double startAngleDeg)
    {
        var spec = template.Geometry;
        var routing = template.Routing ?? throw new InvalidOperationException(
            $"Switch template '{template.Id}' is missing SwitchRoutingModel");

        double lengthMm = spec.LengthMm ?? throw new InvalidOperationException(
            $"Switch template '{template.Id}' is missing LengthMm");
        double radiusMm = spec.RadiusMm ?? throw new InvalidOperationException(
            $"Switch template '{template.Id}' is missing RadiusMm");
        double angleDeg = spec.AngleDeg ?? throw new InvalidOperationException(
            $"Switch template '{template.Id}' is missing AngleDeg");
        double junctionOffsetMm = spec.JunctionOffsetMm ?? throw new InvalidOperationException(
            $"Switch template '{template.Id}' is missing JunctionOffsetMm");

        // Auto-detect left vs right from template ID
        // Left: WL, BWL, or contains "Left", "L"
        // Right: WR, BWR, or contains "Right", "R"
        bool isLeftSwitch = IsLeftVariant(template.Id);

        double startRad = DegToRad(startAngleDeg);
        double sweepRad = DegToRad(angleDeg);

        // Straight path (A → B)
        var straightEnd = new Point2D(
            start.X + lengthMm * Math.Cos(startRad),
            start.Y + lengthMm * Math.Sin(startRad)
        );

        yield return new LinePrimitive(start, straightEnd);

        // Diverging path (A → C), branches off at junction
        var junction = new Point2D(
            start.X + junctionOffsetMm * Math.Cos(startRad),
            start.Y + junctionOffsetMm * Math.Sin(startRad)
        );

        double side = isLeftSwitch ? +1.0 : -1.0;

        var normal = new Point2D(
            -Math.Sin(startRad) * side,
            Math.Cos(startRad) * side
        );

        var center = new Point2D(
            junction.X + normal.X * radiusMm,
            junction.Y + normal.Y * radiusMm
        );

        double startAngleArcRad = Math.Atan2(
            junction.Y - center.Y,
            junction.X - center.X
        );

        double sweepArcRad = sweepRad * side;

        yield return new ArcPrimitive(
            Center: center,
            Radius: radiusMm,
            StartAngleRad: startAngleArcRad,
            SweepAngleRad: sweepArcRad
        );
    }

    /// <summary>
    /// Determines if a switch template is a left variant (WL, BWL) or right variant (WR, BWR).
    /// Works across all track libraries.
    /// </summary>
    private static bool IsLeftVariant(string templateId)
    {
        // Explicit check: if template ID ends with 'L' (for Left)
        if (templateId.EndsWith("L", StringComparison.OrdinalIgnoreCase))
            return true;

        // Explicit check: if template ID ends with 'R' (for Right)
        if (templateId.EndsWith("R", StringComparison.OrdinalIgnoreCase))
            return false;

        // Fallback: contains "Left" or "left"
        if (templateId.Contains("Left", StringComparison.OrdinalIgnoreCase))
            return true;

        // Fallback: contains "Right" or "right"
        if (templateId.Contains("Right", StringComparison.OrdinalIgnoreCase))
            return false;

        // Default: assume left if unsure
        return true;
    }

    private static double DegToRad(double deg) => deg * Math.PI / 180.0;
}
