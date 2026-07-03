// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.WinUI.View;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

using Windows.UI;

/// <summary>
/// Resolves theme-aware resources based on a <see cref="FrameworkElement"/>'s
/// <see cref="FrameworkElement.ActualTheme"/>. This is necessary because
/// <see cref="Application.Current"/>.<see cref="Application.Resources"/> indexer uses the
/// app-level <see cref="ApplicationTheme"/> set at startup. When the UI theme is toggled at
/// runtime via <c>RequestedTheme</c> on a root element, a plain indexer lookup still returns
/// the initial theme's brush. Resolving through <c>ThemeDictionaries</c> by the element's
/// actual theme fixes that.
/// </summary>
internal static class ThemeResourceResolver
{
    /// <summary>
    /// Returns the brush registered under <paramref name="key"/> in the theme dictionary that
    /// matches <paramref name="element"/>'s <see cref="FrameworkElement.ActualTheme"/>.
    /// Falls back to the top-level resource and then to <paramref name="fallback"/>.
    /// </summary>
    public static Brush ResolveBrush(FrameworkElement? element, string key, Color fallback)
    {
        if (TryResolve<SolidColorBrush>(element, key, out var brush))
            return brush;
        return new SolidColorBrush(fallback);
    }

    /// <summary>
    /// Returns the color of the brush registered under <paramref name="key"/> in the theme
    /// dictionary that matches <paramref name="element"/>'s actual theme.
    /// </summary>
    public static Color ResolveColor(FrameworkElement? element, string key, Color fallback)
    {
        if (TryResolve<SolidColorBrush>(element, key, out var brush))
            return brush.Color;
        return fallback;
    }

    /// <summary>
    /// Returns an accent brush with the given alpha (0-255), resolved for the element's theme.
    /// </summary>
    public static SolidColorBrush ResolveAccentWithAlpha(FrameworkElement? element, byte alpha)
    {
        var accent = ResolveColor(element, "AccentFillColorDefaultBrush", Color.FromArgb(255, 0, 120, 212));
        return new SolidColorBrush(Color.FromArgb(alpha, accent.R, accent.G, accent.B));
    }

    /// <summary>
    /// Maps a <see cref="FrameworkElement"/>'s effective theme to light/dark appearance.
    /// </summary>
    public static bool IsLightTheme(FrameworkElement? element)
        => GetEffectiveTheme(element) == ElementTheme.Light;

    /// <summary>
    /// Returns the fallback theme root when no element is available in a converter binding.
    /// </summary>
    public static FrameworkElement? GetDefaultThemeRoot()
        => App.MainWindow?.Content as FrameworkElement;

    private static ElementTheme GetEffectiveTheme(FrameworkElement? element)
    {
        var theme = element?.ActualTheme ?? ElementTheme.Default;
        if (theme != ElementTheme.Default)
            return theme;

        if (Application.Current == null)
            return ElementTheme.Dark;

        return Application.Current.RequestedTheme == ApplicationTheme.Dark
            ? ElementTheme.Dark
            : ElementTheme.Light;
    }

    private static bool TryResolve<T>(FrameworkElement? element, string key, out T value) where T : class
    {
        if (Application.Current == null)
        {
            value = null!;
            return false;
        }

        var themeKey = GetEffectiveTheme(element) == ElementTheme.Dark ? "Dark" : "Light";

        if (TryResolveFromDictionary(Application.Current.Resources, themeKey, key, out value))
            return true;

        value = null!;
        return false;
    }

    private static bool TryResolveFromDictionary<T>(
        ResourceDictionary dictionary,
        string themeKey,
        string key,
        out T value) where T : class
    {
        if (dictionary.ThemeDictionaries.TryGetValue(themeKey, out var dictObj)
            && dictObj is ResourceDictionary themeDict
            && themeDict.TryGetValue(key, out var themedObj)
            && themedObj is T themedTyped)
        {
            value = themedTyped;
            return true;
        }

        foreach (var merged in dictionary.MergedDictionaries)
        {
            if (TryResolveFromDictionary(merged, themeKey, key, out value))
                return true;
        }

        if (dictionary.TryGetValue(key, out var anyObj) && anyObj is T fallbackTyped)
        {
            value = fallbackTyped;
            return true;
        }

        value = null!;
        return false;
    }
}
