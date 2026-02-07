// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.WinUI.Service;

using System.Collections.Concurrent;

/// <summary>
/// Categories for grouping navigation items with separators.
/// </summary>
public enum NavigationCategory
{
    Core = 0,
    TrainControl = 1,
    Journey = 2,
    Solution = 3,
    TrackManagement = 4,
    Monitoring = 5,
    Plugins = 6,
    Help = 7
}

/// <summary>
/// Registry for all navigable pages in the application (Core + Plugins).
/// Provides a central lookup for page types by navigation tag.
/// Supports dynamic NavigationView generation with Feature Toggle integration.
/// </summary>
public sealed class NavigationRegistry
{
    private readonly ConcurrentDictionary<string, PageRegistration> _pages = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// All registered pages ordered by Category then Order.
    /// </summary>
    public IEnumerable<PageRegistration> Pages => _pages.Values
        .OrderBy(p => (int)p.Category)
        .ThenBy(p => p.Order);

    /// <summary>
    /// Tries to get a page registration by navigation tag.
    /// </summary>
    public bool TryGetPage(string tag, out PageRegistration registration)
    {
        return _pages.TryGetValue(tag, out registration!);
    }

    /// <summary>
    /// Registers or updates a page with full configuration.
    /// </summary>
    /// <param name="tag">Navigation tag (e.g., "overview", "traincontrol")</param>
    /// <param name="title">Display title for navigation menu</param>
    /// <param name="iconGlyph">Segoe MDL2 icon glyph (e.g., "\uE700") or null for PathIcon</param>
    /// <param name="pageType">The Type of the Page class</param>
    /// <param name="source">"Shell" for built-in pages, plugin name for external plugins</param>
    /// <param name="category">Navigation category for grouping</param>
    /// <param name="order">Order within category (lower = higher)</param>
    /// <param name="featureToggleKey">Key in FeatureToggleSettings (e.g., "IsTrainControlPageAvailable")</param>
    /// <param name="badgeLabelKey">Key for badge label (e.g., "TrainControlPageLabel") or null</param>
    /// <param name="pathIconData">PathIcon geometry data (if iconGlyph is null)</param>
    /// <param name="isBold">Whether the title should be bold</param>
    public void Register(
        string tag,
        string title,
        string? iconGlyph,
        Type pageType,
        string source,
        NavigationCategory category = NavigationCategory.Core,
        int order = 100,
        string? featureToggleKey = null,
        string? badgeLabelKey = null,
        string? pathIconData = null,
        bool isBold = false)
    {
        ArgumentNullException.ThrowIfNull(tag);
        ArgumentNullException.ThrowIfNull(pageType);

        _pages[tag] = new PageRegistration(
            tag, title, iconGlyph, pageType, source,
            category, order, featureToggleKey, badgeLabelKey, pathIconData, isBold);
    }

    /// <summary>
    /// Legacy register method for backwards compatibility.
    /// </summary>
    public void Register(string tag, string title, string? iconGlyph, Type pageType, string source)
    {
        Register(tag, title, iconGlyph, pageType, source, NavigationCategory.Core);
    }
}

/// <summary>
/// Registration information for a navigable page.
/// </summary>
/// <param name="Tag">Navigation tag used for routing</param>
/// <param name="Title">Display title in navigation menu</param>
/// <param name="IconGlyph">Segoe MDL2 icon glyph (null if using PathIcon)</param>
/// <param name="PageType">The Type of the Page class</param>
/// <param name="Source">"Shell" for built-in pages, plugin name for external plugins</param>
/// <param name="Category">Category for grouping with separators</param>
/// <param name="Order">Order within category</param>
/// <param name="FeatureToggleKey">Property name in FeatureToggleSettings for visibility</param>
/// <param name="BadgeLabelKey">Property name for badge text (e.g., "Preview")</param>
/// <param name="PathIconData">Geometry data for PathIcon (if IconGlyph is null)</param>
/// <param name="IsBold">Whether title text should be bold</param>
public sealed record PageRegistration(
    string Tag,
    string Title,
    string? IconGlyph,
    Type PageType,
    string Source,
    NavigationCategory Category,
    int Order,
    string? FeatureToggleKey,
    string? BadgeLabelKey,
    string? PathIconData,
    bool IsBold);

