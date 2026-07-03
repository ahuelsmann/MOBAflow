// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.Controls;

using Moba.WinUI.View;

using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

using Windows.UI;

/// <summary>
/// Shared Fluent gauge design rules for WinUI arc gauges (Speedometer, Amperemeter).
/// Thresholds and opacities mirror <c>MOBAsmart.Controls.SpeedGaugeView</c> / <c>SpeedGaugeDrawable</c>.
/// </summary>
internal static class GaugeVisualRules
{
    // Normalized value thresholds (parity with MOBAsmart SpeedGaugeView)
    public const double DangerNormalizedThreshold = 0.88;
    public const double CautionBlendStart = 0.7;
    public const double CautionBlendRange = 0.18;
    public const double CurrentCautionBlendStart = 0.5;
    public const double CurrentCautionBlendRange = 0.2;
    public const double VmaxOverLimitMargin = 0.05;

    // Gauge layout (WinUI canvas; center on canvas so outer labels stay on-canvas at 9 and 3 o'clock)
    public const double GaugeCanvasWidth = 342;
    public const double GaugeCanvasHeight = 200;
    public const double GaugeCenterX = GaugeCanvasWidth / 2;
    public const double GaugeCenterY = 130;
    public const double OuterArcRadius = 108;
    public const double OuterMarkerLabelDistance = 142;
    /// <summary>Inner DCC step labels sit closer to center than outer km/h / mA labels.</summary>
    public const double SecondaryMarkerLabelDistance = 65;

    // Marker label positioning: box centered on radial anchor, clamped to canvas edges
    public const double OuterMarkerLabelHeight = 18;
    public const double SecondaryMarkerLabelHeight = 16;
    public const double LabelCanvasEdgeInset = 2;
    public const double LabelArcClearance = 4;

    // Outer scale label typography (km/h, mA)
    public const double OuterMajorMarkerFontSize = 12.5;
    public const double OuterMinorMarkerFontSize = 10.5;
    public const double SecondaryMarkerFontSize = 11;

    // Opacity factors (tuned for arm's-length readability in light and dark theme)
    public const double ArcAccentAlpha = 0.95;
    public const double ArcGlowAlpha = 0.38;
    public const double NeedleIdleAlpha = 0.72;
    public const double HubIdleAlpha = 0.55;
    public const double HubAccentAlpha = 0.90;
    public const double NeedleActiveAlpha = 1.0;
    public const double PrimaryMarkerAlpha = 0.95;
    public const double SecondaryMarkerAlpha = 0.72;
    public const double TrackAlphaDark = 0.50;
    public const double TrackAlphaLight = 0.55;

    public readonly record struct NeedleBrushes(SolidColorBrush Needle, SolidColorBrush HubRing);

    public static NeedleBrushes ResolveNeedleBrushes(
        FrameworkElement element,
        bool isIdle,
        bool isDanger,
        Color? accentColorOverride)
    {
        if (isIdle)
        {
            var secondary = ThemeResourceResolver.ResolveColor(element, "TextFillColorSecondaryBrush", Colors.Gray);
            return new NeedleBrushes(
                new SolidColorBrush(WithAlpha(secondary, NeedleIdleAlpha)),
                new SolidColorBrush(WithAlpha(secondary, HubIdleAlpha)));
        }

        if (isDanger)
        {
            var danger = ThemeResourceResolver.ResolveColor(
                element, "SystemFillColorCriticalBrush", Color.FromArgb(255, 232, 17, 35));
            return new NeedleBrushes(
                new SolidColorBrush(danger),
                new SolidColorBrush(WithAlpha(danger, HubAccentAlpha)));
        }

        if (accentColorOverride is { } accent)
        {
            return new NeedleBrushes(
                new SolidColorBrush(accent),
                new SolidColorBrush(WithAlpha(accent, HubAccentAlpha)));
        }

        var accentColor = ThemeResourceResolver.ResolveColor(
            element, "AccentFillColorDefaultBrush", Color.FromArgb(255, 0, 120, 215));
        return new NeedleBrushes(
            new SolidColorBrush(WithAlpha(accentColor, NeedleActiveAlpha)),
            new SolidColorBrush(WithAlpha(accentColor, HubAccentAlpha)));
    }

    /// <summary>
    /// Speed gauge arc: accent → danger blend, with optional Vmax over-limit check.
    /// </summary>
    public static Color ResolveSpeedArcColor(FrameworkElement element, double normalizedValue, double vmaxRatio)
    {
        var accent = ThemeResourceResolver.ResolveColor(
            element, "AccentFillColorDefaultBrush", Color.FromArgb(255, 0, 120, 215));
        var danger = ThemeResourceResolver.ResolveColor(
            element, "SystemFillColorCriticalBrush", Color.FromArgb(255, 232, 17, 35));

        if (normalizedValue > DangerNormalizedThreshold
            || (vmaxRatio > 0 && normalizedValue > vmaxRatio + VmaxOverLimitMargin))
        {
            return danger;
        }

        if (normalizedValue > CautionBlendStart)
        {
            var t = Math.Clamp((normalizedValue - CautionBlendStart) / CautionBlendRange, 0, 1);
            return LerpColor(accent, WithAlpha(danger, ArcAccentAlpha), t);
        }

        return WithAlpha(accent, ArcAccentAlpha);
    }

    /// <summary>
    /// Current gauge arc: accent → caution → danger three-stage blend.
    /// </summary>
    public static Color ResolveCurrentArcColor(FrameworkElement element, double normalizedValue)
    {
        var accent = ThemeResourceResolver.ResolveColor(
            element, "AccentFillColorDefaultBrush", Color.FromArgb(255, 0, 120, 215));
        var caution = ThemeResourceResolver.ResolveColor(
            element, "SystemFillColorCautionBrush", Color.FromArgb(255, 255, 185, 0));
        var danger = ThemeResourceResolver.ResolveColor(
            element, "SystemFillColorCriticalBrush", Color.FromArgb(255, 232, 17, 35));

        if (normalizedValue > DangerNormalizedThreshold)
        {
            return danger;
        }

        if (normalizedValue > CautionBlendStart)
        {
            var t = Math.Clamp((normalizedValue - CautionBlendStart) / CautionBlendRange, 0, 1);
            return LerpColor(caution, WithAlpha(danger, ArcAccentAlpha), t);
        }

        if (normalizedValue > CurrentCautionBlendStart)
        {
            var t = Math.Clamp(
                (normalizedValue - CurrentCautionBlendStart) / CurrentCautionBlendRange, 0, 1);
            return LerpColor(accent, caution, t);
        }

        return WithAlpha(accent, ArcAccentAlpha);
    }

    public static SolidColorBrush CreatePrimaryMarkerBrush(FrameworkElement element, double opacity = PrimaryMarkerAlpha)
    {
        var primary = ThemeResourceResolver.ResolveColor(element, "TextFillColorPrimaryBrush", Colors.White);
        return new SolidColorBrush(WithAlpha(primary, opacity));
    }

    public static SolidColorBrush CreateSecondaryMarkerBrush(FrameworkElement element)
    {
        var secondary = ThemeResourceResolver.ResolveColor(element, "TextFillColorSecondaryBrush", Colors.Gray);
        return new SolidColorBrush(WithAlpha(secondary, SecondaryMarkerAlpha));
    }

    public static SolidColorBrush CreateAccentSecondaryMarkerBrush(FrameworkElement element)
    {
        var accent = ThemeResourceResolver.ResolveColor(
            element, "AccentFillColorDefaultBrush", Color.FromArgb(255, 0, 120, 215));
        return new SolidColorBrush(WithAlpha(accent, SecondaryMarkerAlpha + 0.08));
    }

    public static int CalculateMarkerLabelWidth(string labelText) => labelText.Length switch
    {
        >= 4 => 52,
        3 => 54,
        2 => 32,
        _ => 24
    };

    /// <summary>
    /// Top edge so the label box is vertically centered on the radial anchor Y.
    /// </summary>
    public static double CalculateMarkerLabelTop(double labelY, double labelHeight) =>
        labelY - (labelHeight / 2);

    /// <summary>
    /// Positions the label box so its horizontal center sits on the radial anchor, clamped to canvas bounds.
    /// Matches MOBAsmart <c>SpeedGaugeDrawable.CalculateKmhLabelLeft</c> (center on anchor, no horizontal nudging).
    /// </summary>
    public static double CalculateMarkerLabelLeft(
        double labelX,
        double labelWidth,
        double canvasWidth = GaugeCanvasWidth)
    {
        var centered = labelX - (labelWidth / 2);
        var maxLeft = canvasWidth - LabelCanvasEdgeInset - labelWidth;
        return Math.Clamp(centered, LabelCanvasEdgeInset, maxLeft);
    }

    /// <summary>
    /// Radial anchor and canvas position for an outer scale label at <paramref name="angleDeg"/>.
    /// </summary>
    public static (double AnchorX, double AnchorY, double Left, double Top) CalculateOuterScaleLabelPosition(
        double angleDeg,
        double labelWidth,
        double labelHeight,
        double labelDistance = OuterMarkerLabelDistance,
        double canvasWidth = GaugeCanvasWidth)
    {
        var angleRad = angleDeg * Math.PI / 180;
        var radialX = Math.Cos(angleRad);
        var radialY = -Math.Sin(angleRad);
        var anchorX = GaugeCenterX + (labelDistance * radialX);
        var anchorY = GaugeCenterY + (labelDistance * radialY);
        var left = CalculateMarkerLabelLeft(anchorX, labelWidth, canvasWidth);
        var top = CalculateMarkerLabelTop(anchorY, labelHeight);
        return (anchorX, anchorY, left, top);
    }

    /// <summary>
    /// Horizontal center of the label box after canvas positioning.
    /// </summary>
    public static double CalculateMarkerLabelCenterX(double left, double labelWidth) =>
        left + (labelWidth / 2);

    public static Color WithAlpha(Color color, double alpha) =>
        Color.FromArgb((byte)(alpha * 255), color.R, color.G, color.B);

    public static Color LerpColor(Color from, Color to, double t) =>
        Color.FromArgb(
            (byte)(from.A + ((to.A - from.A) * t)),
            (byte)(from.R + ((to.R - from.R) * t)),
            (byte)(from.G + ((to.G - from.G) * t)),
            (byte)(from.B + ((to.B - from.B) * t)));
}
