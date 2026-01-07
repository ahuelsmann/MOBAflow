// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

using System.Collections.Concurrent;

namespace Moba.WinUI.Service;

/// <summary>
/// Registry for all navigable pages in the application (Core + Plugins).
/// Provides a central lookup for page types by navigation tag.
/// </summary>
public sealed class NavigationRegistry
{
    private readonly ConcurrentDictionary<string, PageRegistration> _pages = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// All registered pages.
    /// </summary>
    public ICollection<PageRegistration> Pages => _pages.Values;

    /// <summary>
    /// Tries to get a page registration by navigation tag.
    /// </summary>
    public bool TryGetPage(string tag, out PageRegistration registration)
    {
        return _pages.TryGetValue(tag, out registration!);
    }

    /// <summary>
    /// Registers or updates a page.
    /// </summary>
    /// <param name="tag">Navigation tag (e.g., "overview", "traincontrol")</param>
    /// <param name="title">Display title for navigation menu</param>
    /// <param name="iconGlyph">Segoe MDL2 icon glyph (e.g., "\uE700")</param>
    /// <param name="pageType">The Type of the Page class</param>
    /// <param name="source">"Shell" for built-in pages, plugin name for external plugins</param>
    public void Register(string tag, string title, string? iconGlyph, Type pageType, string source)
    {
        ArgumentNullException.ThrowIfNull(tag);
        ArgumentNullException.ThrowIfNull(pageType);

        _pages[tag] = new PageRegistration(tag, title, iconGlyph, pageType, source);
    }
}

/// <summary>
/// Registration information for a navigable page.
/// </summary>
/// <param name="Tag">Navigation tag used for routing</param>
/// <param name="Title">Display title in navigation menu</param>
/// <param name="IconGlyph">Segoe MDL2 icon glyph</param>
/// <param name="PageType">The Type of the Page class</param>
/// <param name="Source">"Shell" for built-in pages, plugin name for external plugins</param>
public sealed record PageRegistration(string Tag, string Title, string? IconGlyph, Type PageType, string Source);
