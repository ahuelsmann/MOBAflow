// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.WinUI.Rendering;

using Microsoft.UI.Composition;

using System;
using System.Numerics;

using Windows.UI;

/// <summary>
/// Factory for creating WinUI 3 Composition effects used in TrackPlan rendering.
/// Provides pre-configured effects for ghost tracks, snap highlights, and selections.
/// </summary>
public sealed class CompositionEffectsFactory
{
    private readonly Compositor _compositor;

    public CompositionEffectsFactory(Compositor compositor)
    {
        _compositor = compositor ?? throw new ArgumentNullException(nameof(compositor));
    }

    /// <summary>
    /// Creates a drop shadow effect for highlight/emphasis.
    /// </summary>
    /// <param name="blurRadius">Blur radius in pixels (typical: 5-10)</param>
    /// <param name="offsetX">Shadow X offset in pixels</param>
    /// <param name="offsetY">Shadow Y offset in pixels</param>
    /// <param name="opacity">Shadow opacity (0.0-1.0)</param>
    /// <param name="color">Shadow color (typically black or theme accent)</param>
    /// <returns>DropShadow CompositionShadow</returns>
    public DropShadow CreateDropShadow(
        float blurRadius = 8f,
        float offsetX = 0f,
        float offsetY = 0f,
        float opacity = 0.6f,
        Color? color = null)
    {
        var shadow = _compositor.CreateDropShadow();
        shadow.BlurRadius = blurRadius;
        shadow.Offset = new Vector3(offsetX, offsetY, 0);
        shadow.Opacity = opacity;
        shadow.Color = color ?? Color.FromArgb(255, 0, 0, 0);  // Black

        return shadow;
    }

    /// <summary>
    /// Creates a scale animation for pulse/highlight effects.
    /// </summary>
    /// <param name="startScale">Starting scale (typically 1.0)</param>
    /// <param name="endScale">Ending scale for pulse (typically 1.2-1.4)</param>
    /// <param name="durationMs">Animation duration in milliseconds (typical: 300-500)</param>
    /// <returns>ScalarKeyFrameAnimation ready for CompositionObject.Scale</returns>
    public ScalarKeyFrameAnimation CreatePulseAnimation(
        float startScale = 1.0f,
        float endScale = 1.3f,
        int durationMs = 400)
    {
        var linear = _compositor.CreateLinearEasingFunction();
        var easeInOut = _compositor.CreateCubicBezierEasingFunction(
            new Vector2(0.42f, 0f),
            new Vector2(0.58f, 1f));

        var animation = _compositor.CreateScalarKeyFrameAnimation();

        // Keyframes: 0% start, 50% peak, 100% return
        animation.InsertKeyFrame(0f, startScale, linear);
        animation.InsertKeyFrame(0.5f, endScale, easeInOut);
        animation.InsertKeyFrame(1f, startScale, easeInOut);

        animation.Duration = TimeSpan.FromMilliseconds(durationMs);

        return animation;
    }

    /// <summary>
    /// Creates a fade-in animation for smooth appearance.
    /// </summary>
    /// <param name="startOpacity">Starting opacity (typically 0.0)</param>
    /// <param name="endOpacity">Ending opacity (typically 1.0 or 0.75)</param>
    /// <param name="durationMs">Animation duration in milliseconds (typical: 200-400)</param>
    /// <returns>ScalarKeyFrameAnimation for Opacity property</returns>
    public ScalarKeyFrameAnimation CreateFadeInAnimation(
        float startOpacity = 0.0f,
        float endOpacity = 1.0f,
        int durationMs = 300)
    {
        var easeOut = _compositor.CreateCubicBezierEasingFunction(
            new Vector2(0.25f, 0.46f),
            new Vector2(0.45f, 0.94f));

        var animation = _compositor.CreateScalarKeyFrameAnimation();
        animation.InsertKeyFrame(0f, startOpacity);
        animation.InsertKeyFrame(1f, endOpacity, easeOut);

        animation.Duration = TimeSpan.FromMilliseconds(durationMs);

        return animation;
    }

    /// <summary>
    /// Creates a fade-out animation for smooth disappearance.
    /// </summary>
    /// <param name="startOpacity">Starting opacity (typically 0.75 or 1.0)</param>
    /// <param name="endOpacity">Ending opacity (typically 0.0)</param>
    /// <param name="durationMs">Animation duration in milliseconds (typical: 150-300)</param>
    /// <returns>ScalarKeyFrameAnimation for Opacity property</returns>
    public ScalarKeyFrameAnimation CreateFadeOutAnimation(
        float startOpacity = 1.0f,
        float endOpacity = 0.0f,
        int durationMs = 250)
    {
        var easeIn = _compositor.CreateCubicBezierEasingFunction(
            new Vector2(0.42f, 0f),
            new Vector2(0.58f, 1f));

        var animation = _compositor.CreateScalarKeyFrameAnimation();
        animation.InsertKeyFrame(0f, startOpacity);
        animation.InsertKeyFrame(1f, endOpacity, easeIn);

        animation.Duration = TimeSpan.FromMilliseconds(durationMs);

        return animation;
    }

    /// <summary>
    /// Creates a color animation for theme-aware transitions.
    /// </summary>
    /// <param name="startColor">Starting color</param>
    /// <param name="endColor">Ending color</param>
    /// <param name="durationMs">Animation duration in milliseconds</param>
    /// <returns>ColorKeyFrameAnimation for color properties</returns>
    public ColorKeyFrameAnimation CreateColorTransitionAnimation(
        Color startColor,
        Color endColor,
        int durationMs = 500)
    {
        var easeInOut = _compositor.CreateCubicBezierEasingFunction(
            new Vector2(0.42f, 0f),
            new Vector2(0.58f, 1f));

        var animation = _compositor.CreateColorKeyFrameAnimation();
        animation.InsertKeyFrame(0f, startColor);
        animation.InsertKeyFrame(1f, endColor, easeInOut);

        animation.Duration = TimeSpan.FromMilliseconds(durationMs);

        return animation;
    }

    /// <summary>
    /// Creates a combined fade + scale animation (used for ghost track entry).
    /// </summary>
    /// <param name="fadeDurationMs">Fade animation duration</param>
    /// <param name="scaleDurationMs">Scale animation duration</param>
    /// <returns>Tuple of (fade animation, scale animation)</returns>
    public (ScalarKeyFrameAnimation FadeAnimation, Vector3KeyFrameAnimation ScaleAnimation)
        CreateGhostTrackEntryAnimations(int fadeDurationMs = 300, int scaleDurationMs = 400)
    {
        var fadeAnim = CreateFadeInAnimation(
            startOpacity: 0.6f,
            endOpacity: 0.85f,
            durationMs: fadeDurationMs);

        var scaleAnim = CreateScaleTransitionAnimation(
            startScale: new Vector3(0.95f),
            endScale: new Vector3(1.0f),
            durationMs: scaleDurationMs);

        return (fadeAnim, scaleAnim);
    }

    /// <summary>
    /// Creates a 3D scale animation for vector scaling.
    /// </summary>
    /// <param name="startScale">Starting scale vector (e.g., Vector3(1.0))</param>
    /// <param name="endScale">Ending scale vector (e.g., Vector3(1.2))</param>
    /// <param name="durationMs">Animation duration in milliseconds</param>
    /// <returns>Vector3KeyFrameAnimation for Scale property</returns>
    public Vector3KeyFrameAnimation CreateScaleTransitionAnimation(
        Vector3 startScale,
        Vector3 endScale,
        int durationMs = 400)
    {
        var easeInOut = _compositor.CreateCubicBezierEasingFunction(
            new Vector2(0.42f, 0f),
            new Vector2(0.58f, 1f));

        var animation = _compositor.CreateVector3KeyFrameAnimation();
        animation.InsertKeyFrame(0f, startScale);
        animation.InsertKeyFrame(1f, endScale, easeInOut);

        animation.Duration = TimeSpan.FromMilliseconds(durationMs);

        return animation;
    }

    /// <summary>
    /// Gets theme-aware shadow color based on current application theme.
    /// </summary>
    /// <param name="isDarkTheme">True if dark theme is active</param>
    /// <returns>Appropriate shadow color for the theme</returns>
    public static Color GetThemeShadowColor(bool isDarkTheme)
    {
        // Dark theme: lighter shadow (visible on dark background)
        // Light theme: darker shadow (visible on light background)
        return isDarkTheme
            ? Color.FromArgb(200, 230, 230, 230)  // Light gray
            : Color.FromArgb(100, 0, 0, 0);        // Dark semi-transparent
    }

    /// <summary>
    /// Gets theme-aware accent color for snap highlights.
    /// </summary>
    /// <param name="isDarkTheme">True if dark theme is active</param>
    /// <returns>Accent color suitable for snap preview</returns>
    public static Color GetThemeAccentColor(bool isDarkTheme)
    {
        // Use Windows 11 Fluent Design accent (typically blue-ish)
        return isDarkTheme
            ? Color.FromArgb(255, 120, 180, 255)  // Light blue
            : Color.FromArgb(255, 0, 102, 204);   // Darker blue
    }

    /// <summary>
    /// Gets theme-aware success color for valid snap state.
    /// </summary>
    public static Color GetThemeSuccessColor(bool isDarkTheme)
    {
        return isDarkTheme
            ? Color.FromArgb(255, 107, 207, 107)  // Light green
            : Color.FromArgb(255, 34, 154, 34);   // Darker green
    }

    /// <summary>
    /// Gets theme-aware warning color for invalid snap state.
    /// </summary>
    public static Color GetThemeWarningColor(bool isDarkTheme)
    {
        return isDarkTheme
            ? Color.FromArgb(255, 255, 184, 82)   // Light orange
            : Color.FromArgb(255, 230, 126, 34);  // Darker orange
    }
}
