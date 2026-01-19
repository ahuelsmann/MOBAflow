// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.WinUI.Service;

/// <summary>
/// Defines the available application skins (color themes).
/// Named by their dominant accent color to avoid trademark issues.
/// </summary>
public enum AppSkin
{
    /// <summary>
    /// System: Uses Windows system accent color.
    /// Default MOBAflow skin, respects user's Windows personalization.
    /// </summary>
    System,

    /// <summary>
    /// Blue: Sleek flat design with blue accents.
    /// Contemporary, clean, Fluent Design compliant.
    /// </summary>
    Blue,

    /// <summary>
    /// Green: Professional green accents.
    /// Familiar to model railroad enthusiasts.
    /// </summary>
    Green,

    /// <summary>
    /// Violet: Night-friendly dark theme with violet accents.
    /// Reduces eye strain in dark environments.
    /// </summary>
    Violet,

    /// <summary>
    /// Orange: Vibrant orange/amber accents.
    /// High contrast, good visibility.
    /// </summary>
    Orange,

    /// <summary>
    /// DarkOrange: Dark background with deep orange accents.
    /// Minimalist, modern look.
    /// </summary>
    DarkOrange,

    /// <summary>
    /// Red: Classic red accents.
    /// Bold, traditional color scheme.
    /// </summary>
    Red
}

/// <summary>
/// Provides skin (color theme) management and color resources for the application.
/// Supports multiple predefined skins with Fluent Design System compliance.
/// </summary>
public interface ISkinProvider
{
    /// <summary>
    /// Gets the currently active skin.
    /// </summary>
    AppSkin CurrentSkin { get; }

    /// <summary>
    /// Gets or sets whether dark mode is enabled.
    /// </summary>
    bool IsDarkMode { get; set; }

    /// <summary>
    /// Initializes the skin provider with saved settings.
    /// Should be called once at application startup.
    /// </summary>
    void Initialize(Common.Configuration.AppSettings settings);

    /// <summary>
    /// Sets the application skin and updates all UI resources.
    /// </summary>
    void SetSkin(AppSkin skin);

    /// <summary>
    /// Gets the resource URI for the skin's ResourceDictionary.
    /// </summary>
    Uri GetSkinResourceUri(AppSkin skin);

    /// <summary>
    /// Occurs when the skin changes.
    /// </summary>
    event EventHandler<SkinChangedEventArgs>? SkinChanged;

    /// <summary>
    /// Occurs when the dark mode setting changes.
    /// </summary>
    event EventHandler? DarkModeChanged;
}

/// <summary>
/// Event arguments for skin changes.
/// </summary>
public class SkinChangedEventArgs : EventArgs
{
    public SkinChangedEventArgs(AppSkin oldSkin, AppSkin newSkin)
    {
        OldSkin = oldSkin;
        NewSkin = newSkin;
    }

    public AppSkin OldSkin { get; }
    public AppSkin NewSkin { get; }
}
