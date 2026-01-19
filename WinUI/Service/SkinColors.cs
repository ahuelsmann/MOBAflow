// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.WinUI.Service;

using Microsoft.UI.Xaml.Media;
using Windows.UI;

/// <summary>
/// Centralized skin color definitions for all application skins.
/// Each skin supports both Light and Dark mode.
/// Colors designed for optimal readability and Fluent Design compliance.
/// </summary>
public static class SkinColors
{
    /// <summary>
    /// Gets the complete color palette for a given skin and mode.
    /// </summary>
    /// <param name="skin">The skin (Blue, Green, Red, etc.)</param>
    /// <param name="isDarkMode">True for dark mode, false for light mode</param>
    public static SkinPalette GetPalette(AppSkin skin, bool isDarkMode)
    {
        return (skin, isDarkMode) switch
        {
            (AppSkin.Blue, false) => BlueLight,
            (AppSkin.Blue, true) => BlueDark,
            (AppSkin.Green, false) => GreenLight,
            (AppSkin.Green, true) => GreenDark,
            (AppSkin.Violet, _) => VioletMode,  // Violet skin is always dark
            (AppSkin.Orange, false) => OrangeLight,
            (AppSkin.Orange, true) => OrangeDark,
            (AppSkin.DarkOrange, false) => DarkOrangeLight,
            (AppSkin.DarkOrange, true) => DarkOrangeDark,
            (AppSkin.Red, false) => RedLight,
            (AppSkin.Red, true) => RedDark,
            (AppSkin.System, false) => SystemLight,
            (AppSkin.System, true) => SystemDark,
            _ => BlueLight
        };
    }

    // BLUE SKIN
    public static SkinPalette BlueLight { get; } = new()
    {
        Name = "Blue Light",
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

    public static SkinPalette BlueDark { get; } = new()
    {
        Name = "Blue Dark",
        Accent = Color.FromArgb(255, 96, 205, 255),
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

    // GREEN SKIN
    public static SkinPalette GreenLight { get; } = new()
    {
        Name = "Green Light",
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

    public static SkinPalette GreenDark { get; } = new()
    {
        Name = "Green Dark",
        Accent = Color.FromArgb(255, 94, 200, 103),
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

    // VIOLET SKIN (always dark, regardless of mode)
    public static SkinPalette VioletMode { get; } = new()
    {
        Name = "Violet",
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

    // ORANGE SKIN
    public static SkinPalette OrangeLight { get; } = new()
    {
        Name = "Orange Light",
        Accent = Color.FromArgb(255, 255, 140, 0),
        AccentDark = Color.FromArgb(255, 204, 112, 0),
        AccentLight = Color.FromArgb(255, 255, 179, 71),
        HeaderBackground = Color.FromArgb(255, 255, 140, 0),
        HeaderForeground = Color.FromArgb(255, 0, 0, 0),
        PanelBackground = Color.FromArgb(255, 250, 250, 250),
        PanelBorder = Color.FromArgb(255, 220, 220, 220),
        ButtonActive = Color.FromArgb(255, 255, 140, 0),
        ButtonInactive = Color.FromArgb(255, 200, 200, 200),
        TextPrimary = Color.FromArgb(255, 0, 0, 0),
        TextSecondary = Color.FromArgb(255, 96, 96, 96),
        IsDarkTheme = false
    };

    public static SkinPalette OrangeDark { get; } = new()
    {
        Name = "Orange Dark",
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

    // DARK ORANGE SKIN
    public static SkinPalette DarkOrangeLight { get; } = new()
    {
        Name = "DarkOrange Light",
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

    public static SkinPalette DarkOrangeDark { get; } = new()
    {
        Name = "DarkOrange Dark",
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

    // RED SKIN
    public static SkinPalette RedLight { get; } = new()
    {
        Name = "Red Light",
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

    public static SkinPalette RedDark { get; } = new()
    {
        Name = "Red Dark",
        Accent = Color.FromArgb(255, 230, 80, 80),
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

    // SYSTEM SKIN (uses Windows accent color)
    public static SkinPalette SystemLight { get; } = new()
    {
        Name = "System Light",
        Accent = Color.FromArgb(255, 0, 95, 184),
        AccentDark = Color.FromArgb(255, 0, 75, 145),
        AccentLight = Color.FromArgb(255, 51, 153, 255),
        HeaderBackground = Color.FromArgb(0, 0, 0, 0),  // Transparent (no colored header bar)
        HeaderForeground = Color.FromArgb(255, 0, 0, 0),
        PanelBackground = Color.FromArgb(0, 0, 0, 0),   // Transparent (uses default CardBackground)
        PanelBorder = Color.FromArgb(0, 0, 0, 0),       // Transparent (uses default CardStroke)
        ButtonActive = Color.FromArgb(255, 51, 153, 255),
        ButtonInactive = Color.FromArgb(255, 200, 200, 200),
        TextPrimary = Color.FromArgb(255, 0, 0, 0),
        TextSecondary = Color.FromArgb(255, 96, 96, 96),
        IsDarkTheme = false
    };

    public static SkinPalette SystemDark { get; } = new()
    {
        Name = "System Dark",
        Accent = Color.FromArgb(255, 100, 181, 246),
        AccentDark = Color.FromArgb(255, 0, 95, 184),
        AccentLight = Color.FromArgb(255, 144, 202, 249),
        HeaderBackground = Color.FromArgb(0, 0, 0, 0),  // Transparent (no colored header bar)
        HeaderForeground = Color.FromArgb(255, 255, 255, 255),
        PanelBackground = Color.FromArgb(0, 0, 0, 0),   // Transparent (uses default CardBackground)
        PanelBorder = Color.FromArgb(0, 0, 0, 0),       // Transparent (uses default CardStroke)
        ButtonActive = Color.FromArgb(255, 100, 181, 246),
        ButtonInactive = Color.FromArgb(255, 80, 80, 80),
        TextPrimary = Color.FromArgb(255, 255, 255, 255),
        TextSecondary = Color.FromArgb(255, 180, 180, 180),
        IsDarkTheme = true
    };
}

/// <summary>
/// Complete color palette for a skin.
/// </summary>
public class SkinPalette
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
