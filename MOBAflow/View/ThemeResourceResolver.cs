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

    private static bool TryResolve<T>(FrameworkElement? element, string key, out T value) where T : class
    {
        var theme = element?.ActualTheme ?? ElementTheme.Default;
        if (theme == ElementTheme.Default)
            theme = Application.Current.RequestedTheme == ApplicationTheme.Dark ? ElementTheme.Dark : ElementTheme.Light;

        var themeKey = theme == ElementTheme.Dark ? "Dark" : "Light";
        var themeDictionaries = Application.Current.Resources.ThemeDictionaries;
        if (themeDictionaries.TryGetValue(themeKey, out var dictObj)
            && dictObj is ResourceDictionary dict
            && dict.TryGetValue(key, out var obj)
            && obj is T typed)
        {
            value = typed;
            return true;
        }

        if (Application.Current.Resources.TryGetValue(key, out var anyObj) && anyObj is T fallbackTyped)
        {
            value = fallbackTyped;
            return true;
        }

        value = null!;
        return false;
    }
}
