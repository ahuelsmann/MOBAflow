// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.WinUI.Service;

/// <summary>
/// Defines the available application themes.
/// </summary>
public enum ApplicationTheme
{
    /// <summary>
    /// Classic: Maerklin-inspired (Silver/Black with Green accents).
    /// Professional, familiar to model railroad enthusiasts.
    /// </summary>
    Classic,

    /// <summary>
    /// Modern: Sleek flat design (Blue accents, Fluent Design compliant).
    /// Contemporary, clean, minimal.
    /// </summary>
    Modern,

    /// <summary>
    /// Dark: Night-friendly dark theme (Dark Blue/Violet accents).
    /// Reduces eye strain in dark environments.
    /// </summary>
    Dark,

    /// <summary>
    /// ESU CabControl: Inspired by ESU CabControl DCC System.
    /// Dark background with vibrant orange/amber accents.
    /// </summary>
    EsuCabControl,

    /// <summary>
    /// Roco Z21: Inspired by Roco Z21 App Interface.
    /// Black background with orange accents, minimalist.
    /// </summary>
    RocoZ21,

    /// <summary>
    /// Maerklin CS: Inspired by Maerklin Central Station 2/3.
    /// Classic red/white/grey color scheme.
    /// </summary>
    MaerklinCS
}

/// <summary>
/// Provides theme management and color resources for the application.
/// Supports multiple predefined themes with Fluent Design System compliance.
/// </summary>
public interface IThemeProvider
{
    /// <summary>
    /// Gets the currently active theme.
    /// </summary>
    ApplicationTheme CurrentTheme { get; }

    /// <summary>
    /// Initializes the theme provider with saved settings.
    /// Should be called once at application startup.
    /// </summary>
    void Initialize(Common.Configuration.AppSettings settings);

    /// <summary>
    /// Sets the application theme and updates all UI resources.
    /// </summary>
    void SetTheme(ApplicationTheme theme);

    /// <summary>
    /// Gets the resource URI for the theme's ResourceDictionary.
    /// </summary>
    Uri GetThemeResourceUri(ApplicationTheme theme);

    /// <summary>
    /// Occurs when the theme changes.
    /// </summary>
    event EventHandler<ThemeChangedEventArgs>? ThemeChanged;
}

/// <summary>
/// Event arguments for theme changes.
/// </summary>
public class ThemeChangedEventArgs : EventArgs
{
    public ThemeChangedEventArgs(ApplicationTheme oldTheme, ApplicationTheme newTheme)
    {
        OldTheme = oldTheme;
        NewTheme = newTheme;
    }

    public ApplicationTheme OldTheme { get; }
    public ApplicationTheme NewTheme { get; }
}
