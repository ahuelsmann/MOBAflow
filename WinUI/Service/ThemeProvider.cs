// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.WinUI.Service;

using Common.Configuration;

using Microsoft.UI.Xaml;

/// <summary>
/// WinUI-specific implementation of theme provider.
/// Manages ResourceDictionary switching and theme events.
/// Uses programmatically-built themes to avoid XAML parsing issues in WinUI 3.
/// </summary>
public class ThemeProvider : IThemeProvider
{
    private ApplicationTheme _currentTheme = ApplicationTheme.Modern;

    public ThemeProvider()
    {
    }

    /// <summary>
    /// Initializes the theme provider with saved settings.
    /// </summary>
    public void Initialize(AppSettings settings)
    {
        if (!string.IsNullOrEmpty(settings.Application.SelectedSkin) &&
            Enum.TryParse<ApplicationTheme>(settings.Application.SelectedSkin, out var savedTheme))
        {
            SetTheme(savedTheme);
        }
    }

    public ApplicationTheme CurrentTheme
    {
        get => _currentTheme;
    }

    public event EventHandler<ThemeChangedEventArgs>? ThemeChanged;

    /// <summary>
    /// Sets the application theme by switching ResourceDictionaries.
    /// Uses programmatically-built themes for WinUI 3 compatibility.
    /// </summary>
    public void SetTheme(ApplicationTheme theme)
    {
        if (_currentTheme == theme)
            return;

        var oldTheme = _currentTheme;
        _currentTheme = theme;

        // Remove old theme resource dictionary
        var oldDict = FindThemeDictionary(oldTheme);
        if (oldDict != null)
        {
            Application.Current.Resources.MergedDictionaries.Remove(oldDict);
        }

        // Build and add new theme resource dictionary
        var newDict = theme switch
        {
            ApplicationTheme.Modern => ThemeResourceBuilder.BuildModernTheme(),
            ApplicationTheme.Classic => ThemeResourceBuilder.BuildClassicTheme(),
            ApplicationTheme.Dark => ThemeResourceBuilder.BuildDarkTheme(),
            ApplicationTheme.EsuCabControl => ThemeResourceBuilder.BuildEsuCabControlTheme(),
            ApplicationTheme.RocoZ21 => ThemeResourceBuilder.BuildRocoZ21Theme(),
            ApplicationTheme.MaerklinCS => ThemeResourceBuilder.BuildMaerklinCSTheme(),
            _ => ThemeResourceBuilder.BuildModernTheme()
        };

        Application.Current.Resources.MergedDictionaries.Add(newDict);

        // Raise event
        ThemeChanged?.Invoke(this, new ThemeChangedEventArgs(oldTheme, theme));
    }

    /// <summary>
    /// Gets the ResourceDictionary URI for a given theme (not used with C# builder, but kept for compatibility).
    /// </summary>
    public Uri GetThemeResourceUri(ApplicationTheme theme)
    {
        return theme switch
        {
            ApplicationTheme.Classic => new Uri("ms-appx:///WinUI/Resources/Themes/ThemeClassic.xaml"),
            ApplicationTheme.Modern => new Uri("ms-appx:///WinUI/Resources/Themes/ThemeModern.xaml"),
            ApplicationTheme.Dark => new Uri("ms-appx:///WinUI/Resources/Themes/ThemeDark.xaml"),
            _ => new Uri("ms-appx:///WinUI/Resources/Themes/ThemeModern.xaml")
        };
    }

    /// <summary>
    /// Finds and returns the theme ResourceDictionary matching the given theme type.
    /// </summary>
    private ResourceDictionary? FindThemeDictionary(ApplicationTheme theme)
    {
        // Try to find by checking if it was created by our builder
        // Since we can't directly identify the source, we remove all theme dicts
        // by checking for the presence of our custom theme colors
        foreach (var dict in Application.Current.Resources.MergedDictionaries.ToList())
        {
            if (dict.ContainsKey("ThemeAccentColor"))
            {
                return dict;
            }
        }
        return null;
    }
}
