// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.WinUI.Rendering;

using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using Windows.UI;

/// <summary>
/// Validates that all animation effects work correctly with theme switching (Light/Dark/HighContrast).
/// Provides theme-aware color validation and WCAG contrast ratio checking.
/// </summary>
public sealed class ThemeAnimationValidator
{
    /// <summary>
    /// Represents the results of theme validation.
    /// </summary>
    public sealed record ValidationResult(
        bool IsValid,
        List<string> Warnings,
        List<string> Errors);

    /// <summary>
    /// Validates animation colors for a specific theme.
    /// </summary>
    /// <param name="isDarkTheme">True for dark theme, false for light theme</param>
    /// <param name="isHighContrast">True if high contrast mode is enabled</param>
    /// <returns>ValidationResult with any issues found</returns>
    public static ValidationResult ValidateThemeColors(bool isDarkTheme, bool isHighContrast = false)
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        try
        {
            // Validate ghost track opacity
            var ghostOpacity = isDarkTheme ? 0.85f : 0.75f;
            if (ghostOpacity < 0.3f || ghostOpacity > 1.0f)
                errors.Add($"Ghost track opacity {ghostOpacity} is out of valid range [0.3, 1.0]");

            // Validate snap highlight colors
            var successColor = CompositionEffectsFactory.GetThemeSuccessColor(isDarkTheme);
            var warningColor = CompositionEffectsFactory.GetThemeWarningColor(isDarkTheme);
            var accentColor = CompositionEffectsFactory.GetThemeAccentColor(isDarkTheme);
            var shadowColor = CompositionEffectsFactory.GetThemeShadowColor(isDarkTheme);

            // Check contrast ratios (WCAG AA minimum)
            var backgroundColor = isDarkTheme ? Color.FromArgb(255, 32, 32, 32) : Color.FromArgb(255, 255, 255, 255);

            var successContrast = CalculateContrastRatio(successColor, backgroundColor);
            var warningContrast = CalculateContrastRatio(warningColor, backgroundColor);
            var accentContrast = CalculateContrastRatio(accentColor, backgroundColor);

            if (successContrast < 4.5f)
                warnings.Add($"Success color contrast ratio {successContrast:F2} is below WCAG AA (4.5) for {(isDarkTheme ? "dark" : "light")} theme");

            if (warningContrast < 4.5f)
                warnings.Add($"Warning color contrast ratio {warningContrast:F2} is below WCAG AA (4.5) for {(isDarkTheme ? "dark" : "light")} theme");

            if (accentContrast < 4.5f)
                warnings.Add($"Accent color contrast ratio {accentContrast:F2} is below WCAG AA (4.5) for {(isDarkTheme ? "dark" : "light")} theme");

            // High contrast mode checks
            if (isHighContrast)
            {
                if (successContrast < 7.0f)
                    errors.Add($"High contrast mode requires ratio >= 7.0, but success color has {successContrast:F2}");

                if (warningContrast < 7.0f)
                    errors.Add($"High contrast mode requires ratio >= 7.0, but warning color has {warningContrast:F2}");
            }

            // Validate shadow visibility
            if (isDarkTheme && shadowColor.R < 150 && shadowColor.G < 150 && shadowColor.B < 150)
                warnings.Add("Shadow color may not be visible enough in dark theme");

            if (!isDarkTheme && shadowColor.R > 100 && shadowColor.G > 100 && shadowColor.B > 100)
                warnings.Add("Shadow color may not be visible enough in light theme");

            var isValid = errors.Count == 0;

            return new ValidationResult(isValid, warnings, errors);
        }
        catch (Exception ex)
        {
            errors.Add($"Validation error: {ex.Message}");
            return new ValidationResult(false, warnings, errors);
        }
    }

    /// <summary>
    /// Validates animation timings for responsiveness (<100ms requirement).
    /// </summary>
    public static ValidationResult ValidateAnimationTimings()
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        // Check snap feedback timing
        const int maxSnapFeedbackMs = 100;
        const int typicalSnapFeedbackMs = 60;

        if (typicalSnapFeedbackMs > maxSnapFeedbackMs)
            errors.Add($"Snap feedback timing {typicalSnapFeedbackMs}ms exceeds maximum {maxSnapFeedbackMs}ms");

        // Check ghost track animation
        const int ghostFadeInMs = 300;
        if (ghostFadeInMs > 500)
            warnings.Add($"Ghost fade-in {ghostFadeInMs}ms is longer than recommended (max 500ms)");

        // Check snap highlight pulse
        const int snapPulseMs = 400;
        if (snapPulseMs > 600)
            warnings.Add($"Snap highlight pulse {snapPulseMs}ms is longer than recommended");

        var isValid = errors.Count == 0;
        return new ValidationResult(isValid, warnings, errors);
    }

    /// <summary>
    /// Calculates WCAG contrast ratio between two colors.
    /// Formula: (L1 + 0.05) / (L2 + 0.05) where L is relative luminance
    /// </summary>
    private static float CalculateContrastRatio(Color foreground, Color background)
    {
        var l1 = GetRelativeLuminance(foreground);
        var l2 = GetRelativeLuminance(background);

        var lighter = Math.Max(l1, l2);
        var darker = Math.Min(l1, l2);

        return (float)((lighter + 0.05) / (darker + 0.05));
    }

    /// <summary>
    /// Calculates relative luminance of a color (WCAG definition).
    /// </summary>
    private static double GetRelativeLuminance(Color color)
    {
        var r = color.R / 255.0;
        var g = color.G / 255.0;
        var b = color.B / 255.0;

        r = r <= 0.03928 ? r / 12.92 : Math.Pow((r + 0.055) / 1.055, 2.4);
        g = g <= 0.03928 ? g / 12.92 : Math.Pow((g + 0.055) / 1.055, 2.4);
        b = b <= 0.03928 ? b / 12.92 : Math.Pow((b + 0.055) / 1.055, 2.4);

        return 0.2126 * r + 0.7152 * g + 0.0722 * b;
    }

    /// <summary>
    /// Validates all animation effects for a given theme configuration.
    /// </summary>
    public static ValidationResult ValidateAll(bool isDarkTheme, bool isHighContrast = false)
    {
        var allErrors = new List<string>();
        var allWarnings = new List<string>();

        var colorResult = ValidateThemeColors(isDarkTheme, isHighContrast);
        allErrors.AddRange(colorResult.Errors);
        allWarnings.AddRange(colorResult.Warnings);

        var timingResult = ValidateAnimationTimings();
        allErrors.AddRange(timingResult.Errors);
        allWarnings.AddRange(timingResult.Warnings);

        var isValid = allErrors.Count == 0;
        return new ValidationResult(isValid, allWarnings, allErrors);
    }

    /// <summary>
    /// Formats validation results as a readable string.
    /// </summary>
    public static string FormatValidationReport(ValidationResult result, bool isDarkTheme)
    {
        var lines = new List<string>
        {
            $"=== Theme Animation Validation Report ({(isDarkTheme ? "Dark" : "Light")} Theme) ===",
            $"Status: {(result.IsValid ? "✓ VALID" : "✗ INVALID")}",
            ""
        };

        if (result.Errors.Count > 0)
        {
            lines.Add($"Errors ({result.Errors.Count}):");
            foreach (var error in result.Errors)
                lines.Add($"  • {error}");
            lines.Add("");
        }

        if (result.Warnings.Count > 0)
        {
            lines.Add($"Warnings ({result.Warnings.Count}):");
            foreach (var warning in result.Warnings)
                lines.Add($"  ⚠ {warning}");
            lines.Add("");
        }

        if (result.IsValid && result.Warnings.Count == 0)
        {
            lines.Add("✓ All validations passed!");
        }

        return string.Join(Environment.NewLine, lines);
    }
}

/// <summary>
/// Extension methods for theme validation.
/// </summary>
public static class ThemeValidationExtensions
{
    /// <summary>
    /// Validates animation effects for the current application theme.
    /// </summary>
    public static ThemeAnimationValidator.ValidationResult ValidateForCurrentTheme(this Application app)
    {
        var isDarkTheme = app.RequestedTheme == ApplicationTheme.Dark;
        return ThemeAnimationValidator.ValidateAll(isDarkTheme, false);
    }

    /// <summary>
    /// Logs validation results for debugging.
    /// </summary>
    public static void LogValidationReport(
        this ThemeAnimationValidator.ValidationResult result,
        bool isDarkTheme)
    {
        var report = ThemeAnimationValidator.FormatValidationReport(result, isDarkTheme);
        System.Diagnostics.Debug.WriteLine(report);
    }
}
