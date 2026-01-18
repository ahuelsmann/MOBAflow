// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.WinUI.Service;

using Microsoft.UI.Xaml.Media;
using Windows.UI;

/// <summary>
/// Centralized theme color definitions for all manufacturer-inspired skins.
/// Each theme supports both Light and Dark mode.
/// Colors extracted from official product documentation:
/// - ESU CabControl Betriebsanleitung (Mai 2025)
/// - Märklin Central Station 2 (60215) Anleitung
/// - Roco Z21 App Screenshots
/// </summary>
public static class ThemeColors
{
    /// <summary>
    /// Gets the complete color palette for a given theme and mode.
    /// </summary>
    /// <param name="theme">The theme (Classic, Modern, ESU, etc.)</param>
    /// <param name="isDarkMode">True for dark mode, false for light mode</param>
    public static ThemePalette GetPalette(ApplicationTheme theme, bool isDarkMode)
    {
        return (theme, isDarkMode) switch
        {
            (ApplicationTheme.Modern, false) => ModernLight,
            (ApplicationTheme.Modern, true) => ModernDark,
            (ApplicationTheme.Classic, false) => ClassicLight,
            (ApplicationTheme.Classic, true) => ClassicDark,
            (ApplicationTheme.Dark, _) => DarkMode,  // Dark theme is always dark
            (ApplicationTheme.EsuCabControl, false) => EsuLight,
            (ApplicationTheme.EsuCabControl, true) => EsuDark,
            (ApplicationTheme.RocoZ21, false) => RocoLight,
            (ApplicationTheme.RocoZ21, true) => RocoDark,
            (ApplicationTheme.MaerklinCS, false) => MaerklinLight,
            (ApplicationTheme.MaerklinCS, true) => MaerklinDark,
            (ApplicationTheme.Original, false) => OriginalLight,
            (ApplicationTheme.Original, true) => OriginalDark,
            _ => ModernLight
        };
    }

    // MODERN THEME
    public static ThemePalette ModernLight { get; } = new()
    {
        Name = "Modern Light",
        Accent = Color.FromArgb(255, 0, 120, 212),
        AccentDark = Color.FromArgb(255, 0, 90, 158),
        AccentLight = Color.FromArgb(255, 80, 180, 247),
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

    public static ThemePalette ModernDark { get; } = new()
    {
        Name = "Modern Dark",
        Accent = Color.FromArgb(255, 96, 205, 255),  // Lighter blue for dark mode
        AccentDark = Color.FromArgb(255, 0, 120, 212),
        AccentLight = Color.FromArgb(255, 150, 220, 255),
        HeaderBackground = Color.FromArgb(255, 0, 90, 158),
        HeaderForeground = Color.FromArgb(255, 255, 255, 255),
        PanelBackground = Color.FromArgb(255, 32, 32, 32),
        PanelBorder = Color.FromArgb(255, 60, 60, 60),
        ButtonActive = Color.FromArgb(255, 96, 205, 255),
        ButtonInactive = Color.FromArgb(255, 80, 80, 80),
        TextPrimary = Color.FromArgb(255, 255, 255, 255),
        TextSecondary = Color.FromArgb(255, 180, 180, 180),
        IsDarkTheme = true
    };

    // CLASSIC THEME
    public static ThemePalette ClassicLight { get; } = new()
    {
        Name = "Classic Light",
        Accent = Color.FromArgb(255, 42, 164, 55),
        AccentDark = Color.FromArgb(255, 30, 125, 45),
        AccentLight = Color.FromArgb(255, 94, 200, 103),
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

    public static ThemePalette ClassicDark { get; } = new()
    {
        Name = "Classic Dark",
        Accent = Color.FromArgb(255, 94, 200, 103),  // Lighter green for dark mode
        AccentDark = Color.FromArgb(255, 42, 164, 55),
        AccentLight = Color.FromArgb(255, 150, 230, 160),
        HeaderBackground = Color.FromArgb(255, 30, 125, 45),
        HeaderForeground = Color.FromArgb(255, 255, 255, 255),
        PanelBackground = Color.FromArgb(255, 28, 28, 28),
        PanelBorder = Color.FromArgb(255, 60, 60, 60),
        ButtonActive = Color.FromArgb(255, 94, 200, 103),
        ButtonInactive = Color.FromArgb(255, 80, 80, 80),
        TextPrimary = Color.FromArgb(255, 255, 255, 255),
        TextSecondary = Color.FromArgb(255, 180, 180, 180),
        IsDarkTheme = true
    };

    // DARK THEME (always dark, regardless of mode)
    public static ThemePalette DarkMode { get; } = new()
    {
        Name = "Dark",
        Accent = Color.FromArgb(255, 155, 95, 255),
        AccentDark = Color.FromArgb(255, 122, 71, 212),
        AccentLight = Color.FromArgb(255, 185, 141, 255),
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

    // ESU CABCONTROL THEME
    public static ThemePalette EsuLight { get; } = new()
    {
        Name = "ESU Light",
        Accent = Color.FromArgb(255, 255, 140, 0),
        AccentDark = Color.FromArgb(255, 204, 112, 0),
        AccentLight = Color.FromArgb(255, 255, 179, 71),
        HeaderBackground = Color.FromArgb(255, 255, 140, 0),
        HeaderForeground = Color.FromArgb(255, 0, 0, 0),  // Black text on orange
        PanelBackground = Color.FromArgb(255, 250, 250, 250),
        PanelBorder = Color.FromArgb(255, 220, 220, 220),
        ButtonActive = Color.FromArgb(255, 255, 140, 0),
        ButtonInactive = Color.FromArgb(255, 200, 200, 200),
        TextPrimary = Color.FromArgb(255, 0, 0, 0),
        TextSecondary = Color.FromArgb(255, 96, 96, 96),
        IsDarkTheme = false
    };

    public static ThemePalette EsuDark { get; } = new()
    {
        Name = "ESU Dark",
        Accent = Color.FromArgb(255, 255, 140, 0),
        AccentDark = Color.FromArgb(255, 204, 112, 0),
        AccentLight = Color.FromArgb(255, 255, 179, 71),
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

    // ROCO Z21 THEME
    public static ThemePalette RocoLight { get; } = new()
    {
        Name = "Roco Light",
        Accent = Color.FromArgb(255, 255, 102, 0),
        AccentDark = Color.FromArgb(255, 204, 82, 0),
        AccentLight = Color.FromArgb(255, 255, 153, 51),
        HeaderBackground = Color.FromArgb(255, 255, 102, 0),
        HeaderForeground = Color.FromArgb(255, 255, 255, 255),
        PanelBackground = Color.FromArgb(255, 248, 248, 248),
        PanelBorder = Color.FromArgb(255, 215, 215, 215),
        ButtonActive = Color.FromArgb(255, 255, 102, 0),
        ButtonInactive = Color.FromArgb(255, 200, 200, 200),
        TextPrimary = Color.FromArgb(255, 0, 0, 0),
        TextSecondary = Color.FromArgb(255, 96, 96, 96),
        IsDarkTheme = false
    };

    public static ThemePalette RocoDark { get; } = new()
    {
        Name = "Roco Dark",
        Accent = Color.FromArgb(255, 255, 102, 0),
        AccentDark = Color.FromArgb(255, 204, 82, 0),
        AccentLight = Color.FromArgb(255, 255, 153, 51),
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

    // MÄRKLIN CS THEME
    public static ThemePalette MaerklinLight { get; } = new()
    {
        Name = "Märklin Light",
        Accent = Color.FromArgb(255, 200, 0, 0),
        AccentDark = Color.FromArgb(255, 160, 0, 0),
        AccentLight = Color.FromArgb(255, 230, 80, 80),
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

    public static ThemePalette MaerklinDark { get; } = new()
    {
        Name = "Märklin Dark",
        Accent = Color.FromArgb(255, 230, 80, 80),  // Lighter red for dark mode
        AccentDark = Color.FromArgb(255, 200, 0, 0),
        AccentLight = Color.FromArgb(255, 255, 120, 120),
        HeaderBackground = Color.FromArgb(255, 160, 0, 0),
        HeaderForeground = Color.FromArgb(255, 255, 255, 255),
        PanelBackground = Color.FromArgb(255, 30, 30, 30),
        PanelBorder = Color.FromArgb(255, 60, 60, 60),
        ButtonActive = Color.FromArgb(255, 230, 80, 80),
        ButtonInactive = Color.FromArgb(255, 80, 80, 80),
        TextPrimary = Color.FromArgb(255, 255, 255, 255),
        TextSecondary = Color.FromArgb(255, 180, 180, 180),
        IsDarkTheme = true
    };

    // ORIGINAL THEME
    /// <summary>
    /// Original: MOBAflow TrainControlPage default theme.
    /// Pure WinUI 3 Fluent Design without custom header colors.
    /// Matches the classic TrainControlPage.xaml design (no colored header strip).
    /// </summary>
    public static ThemePalette OriginalLight { get; } = new()
    {
        Name = "Original Light",
        Accent = Color.FromArgb(255, 0, 95, 184),          // #005FB8 - Fluent Blue (darker than Modern)
        AccentDark = Color.FromArgb(255, 0, 75, 145),      // #004B91
        AccentLight = Color.FromArgb(255, 51, 153, 255),   // #3399FF
        HeaderBackground = Color.FromArgb(0, 0, 0, 0),     // Transparent (no colored header bar!)
        HeaderForeground = Color.FromArgb(255, 0, 0, 0),   // Black text
        PanelBackground = Color.FromArgb(0, 0, 0, 0),      // Transparent (uses default CardBackground)
        PanelBorder = Color.FromArgb(0, 0, 0, 0),          // Transparent (uses default CardStroke)
        ButtonActive = Color.FromArgb(255, 51, 153, 255),
        ButtonInactive = Color.FromArgb(255, 200, 200, 200),
        TextPrimary = Color.FromArgb(255, 0, 0, 0),
        TextSecondary = Color.FromArgb(255, 96, 96, 96),
        IsDarkTheme = false
    };

    public static ThemePalette OriginalDark { get; } = new()
    {
        Name = "Original Dark",
        Accent = Color.FromArgb(255, 100, 181, 246),       // Lighter blue for dark mode
        AccentDark = Color.FromArgb(255, 0, 95, 184),
        AccentLight = Color.FromArgb(255, 144, 202, 249),
        HeaderBackground = Color.FromArgb(0, 0, 0, 0),     // Transparent (no colored header bar!)
        HeaderForeground = Color.FromArgb(255, 255, 255, 255),  // White text in dark mode
        PanelBackground = Color.FromArgb(0, 0, 0, 0),      // Transparent (uses default CardBackground)
        PanelBorder = Color.FromArgb(0, 0, 0, 0),          // Transparent (uses default CardStroke)
        ButtonActive = Color.FromArgb(255, 100, 181, 246),
        ButtonInactive = Color.FromArgb(255, 80, 80, 80),
        TextPrimary = Color.FromArgb(255, 255, 255, 255),
        TextSecondary = Color.FromArgb(255, 180, 180, 180),
        IsDarkTheme = true
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
