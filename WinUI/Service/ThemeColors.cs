// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.WinUI.Service;

using Microsoft.UI.Xaml.Media;
using Windows.UI;

/// <summary>
/// Centralized theme color definitions for all manufacturer-inspired skins.
/// Colors extracted from official product documentation:
/// - ESU CabControl Betriebsanleitung (Mai 2025)
/// - Märklin Central Station 2 (60215) Anleitung
/// - Roco Z21 App Screenshots
/// </summary>
public static class ThemeColors
{
    /// <summary>
    /// Gets the complete color palette for a given theme.
    /// </summary>
    public static ThemePalette GetPalette(ApplicationTheme theme)
    {
        return theme switch
        {
            ApplicationTheme.Modern => Modern,
            ApplicationTheme.Classic => Classic,
            ApplicationTheme.Dark => Dark,
            ApplicationTheme.EsuCabControl => EsuCabControl,
            ApplicationTheme.RocoZ21 => RocoZ21,
            ApplicationTheme.MaerklinCS => MaerklinCS,
            _ => Modern
        };
    }

    /// <summary>
    /// Modern: Microsoft Fluent Design - Blue accents, clean and minimal.
    /// </summary>
    public static ThemePalette Modern { get; } = new()
    {
        Name = "Modern",
        Accent = Color.FromArgb(255, 0, 120, 212),         // #0078D4 - Microsoft Blue
        AccentDark = Color.FromArgb(255, 0, 90, 158),      // #005A9E
        AccentLight = Color.FromArgb(255, 80, 180, 247),   // #50B4F7
        HeaderBackground = Color.FromArgb(255, 0, 120, 212),
        HeaderForeground = Color.FromArgb(255, 255, 255, 255),
        PanelBackground = Color.FromArgb(255, 243, 243, 243),
        PanelBorder = Color.FromArgb(255, 225, 225, 225),
        ButtonActive = Color.FromArgb(255, 80, 180, 247),
        ButtonInactive = Color.FromArgb(255, 200, 200, 200),
        TextPrimary = Color.FromArgb(255, 0, 0, 0),
        TextSecondary = Color.FromArgb(255, 96, 96, 96),
        IsDarkTheme = false
    };

    /// <summary>
    /// Classic: Märklin-inspired - Green accents, professional silver/black.
    /// Based on Märklin Digital product line styling.
    /// </summary>
    public static ThemePalette Classic { get; } = new()
    {
        Name = "Classic",
        Accent = Color.FromArgb(255, 42, 164, 55),         // #2AA437 - Märklin Green
        AccentDark = Color.FromArgb(255, 30, 125, 45),     // #1E7D2D
        AccentLight = Color.FromArgb(255, 94, 200, 103),   // #5EC867
        HeaderBackground = Color.FromArgb(255, 42, 164, 55),
        HeaderForeground = Color.FromArgb(255, 255, 255, 255),
        PanelBackground = Color.FromArgb(255, 218, 218, 218),
        PanelBorder = Color.FromArgb(255, 176, 176, 176),
        ButtonActive = Color.FromArgb(255, 94, 200, 103),
        ButtonInactive = Color.FromArgb(255, 180, 180, 180),
        TextPrimary = Color.FromArgb(255, 0, 0, 0),
        TextSecondary = Color.FromArgb(255, 80, 80, 80),
        IsDarkTheme = false
    };

    /// <summary>
    /// Dark: Night-friendly dark theme with violet accents.
    /// Reduced eye strain for evening/night operation.
    /// </summary>
    public static ThemePalette Dark { get; } = new()
    {
        Name = "Dark",
        Accent = Color.FromArgb(255, 155, 95, 255),        // #9B5FFF - Violet
        AccentDark = Color.FromArgb(255, 122, 71, 212),    // #7A47D4
        AccentLight = Color.FromArgb(255, 185, 141, 255),  // #B98DFF
        HeaderBackground = Color.FromArgb(255, 45, 45, 48),
        HeaderForeground = Color.FromArgb(255, 255, 255, 255),
        PanelBackground = Color.FromArgb(255, 37, 37, 38),
        PanelBorder = Color.FromArgb(255, 60, 60, 60),
        ButtonActive = Color.FromArgb(255, 155, 95, 255),
        ButtonInactive = Color.FromArgb(255, 80, 80, 80),
        TextPrimary = Color.FromArgb(255, 255, 255, 255),
        TextSecondary = Color.FromArgb(255, 176, 176, 176),
        IsDarkTheme = true
    };

    /// <summary>
    /// ESU CabControl: Orange/Amber accents on dark background.
    /// Inspired by ESU CabControl DCC System (Mobile Control Pro).
    /// Colors extracted from ESU Betriebsanleitung Mai 2025.
    /// </summary>
    public static ThemePalette EsuCabControl { get; } = new()
    {
        Name = "ESU CabControl",
        Accent = Color.FromArgb(255, 255, 140, 0),         // #FF8C00 - ESU Orange
        AccentDark = Color.FromArgb(255, 204, 112, 0),     // #CC7000
        AccentLight = Color.FromArgb(255, 255, 179, 71),   // #FFB347
        HeaderBackground = Color.FromArgb(255, 35, 35, 35),
        HeaderForeground = Color.FromArgb(255, 255, 140, 0),
        PanelBackground = Color.FromArgb(255, 26, 26, 26),
        PanelBorder = Color.FromArgb(255, 60, 60, 60),
        ButtonActive = Color.FromArgb(255, 255, 140, 0),
        ButtonInactive = Color.FromArgb(255, 70, 70, 70),
        TextPrimary = Color.FromArgb(255, 255, 255, 255),
        TextSecondary = Color.FromArgb(255, 176, 176, 176),
        IsDarkTheme = true
    };

    /// <summary>
    /// Roco Z21: Orange accents on black background, minimalist.
    /// Inspired by Roco Z21 App interface design.
    /// </summary>
    public static ThemePalette RocoZ21 { get; } = new()
    {
        Name = "Roco Z21",
        Accent = Color.FromArgb(255, 255, 102, 0),         // #FF6600 - Z21 Orange
        AccentDark = Color.FromArgb(255, 204, 82, 0),      // #CC5200
        AccentLight = Color.FromArgb(255, 255, 153, 51),   // #FF9933
        HeaderBackground = Color.FromArgb(255, 0, 0, 0),
        HeaderForeground = Color.FromArgb(255, 255, 102, 0),
        PanelBackground = Color.FromArgb(255, 20, 20, 20),
        PanelBorder = Color.FromArgb(255, 50, 50, 50),
        ButtonActive = Color.FromArgb(255, 255, 102, 0),
        ButtonInactive = Color.FromArgb(255, 60, 60, 60),
        TextPrimary = Color.FromArgb(255, 255, 255, 255),
        TextSecondary = Color.FromArgb(255, 160, 160, 160),
        IsDarkTheme = true
    };

    /// <summary>
    /// Märklin CS: Classic red/white/grey color scheme.
    /// Inspired by Märklin Central Station 2/3 (60215) hardware and software.
    /// Colors extracted from CS2 Bedienungsanleitung.
    /// </summary>
    public static ThemePalette MaerklinCS { get; } = new()
    {
        Name = "Märklin CS",
        Accent = Color.FromArgb(255, 200, 0, 0),           // #C80000 - Märklin Red
        AccentDark = Color.FromArgb(255, 160, 0, 0),       // #A00000
        AccentLight = Color.FromArgb(255, 230, 80, 80),    // #E65050
        HeaderBackground = Color.FromArgb(255, 200, 0, 0),
        HeaderForeground = Color.FromArgb(255, 255, 255, 255),
        PanelBackground = Color.FromArgb(255, 235, 235, 235),
        PanelBorder = Color.FromArgb(255, 180, 180, 180),
        ButtonActive = Color.FromArgb(255, 200, 0, 0),
        ButtonInactive = Color.FromArgb(255, 180, 180, 180),
        TextPrimary = Color.FromArgb(255, 0, 0, 0),
        TextSecondary = Color.FromArgb(255, 80, 80, 80),
        IsDarkTheme = false
    };
}

/// <summary>
/// Complete color palette for a theme.
/// </summary>
public class ThemePalette
{
    public required string Name { get; init; }

    // Accent colors
    public required Color Accent { get; init; }
    public required Color AccentDark { get; init; }
    public required Color AccentLight { get; init; }

    // Header
    public required Color HeaderBackground { get; init; }
    public required Color HeaderForeground { get; init; }

    // Panels
    public required Color PanelBackground { get; init; }
    public required Color PanelBorder { get; init; }

    // Buttons
    public required Color ButtonActive { get; init; }
    public required Color ButtonInactive { get; init; }

    // Text
    public required Color TextPrimary { get; init; }
    public required Color TextSecondary { get; init; }

    // Theme type
    public required bool IsDarkTheme { get; init; }

    // Brush helpers
    public SolidColorBrush AccentBrush => new(Accent);
    public SolidColorBrush AccentDarkBrush => new(AccentDark);
    public SolidColorBrush AccentLightBrush => new(AccentLight);
    public SolidColorBrush HeaderBackgroundBrush => new(HeaderBackground);
    public SolidColorBrush HeaderForegroundBrush => new(HeaderForeground);
    public SolidColorBrush PanelBackgroundBrush => new(PanelBackground);
    public SolidColorBrush PanelBorderBrush => new(PanelBorder);
    public SolidColorBrush ButtonActiveBrush => new(ButtonActive);
    public SolidColorBrush ButtonInactiveBrush => new(ButtonInactive);
    public SolidColorBrush TextPrimaryBrush => new(TextPrimary);
    public SolidColorBrush TextSecondaryBrush => new(TextSecondary);
}
