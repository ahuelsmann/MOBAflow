// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.Service;

using Common.Configuration;
using Common.Navigation;

using System.Reflection;

/// <summary>
/// Provides the list of feature-toggle pages based on NavigationRegistration.
/// Filters to only pages whose FeatureToggleKey exists in FeatureToggleSettings.
/// </summary>
internal sealed class FeatureTogglePageProvider : IFeatureTogglePageProvider
{
    private readonly List<PageMetadata> _pages;
    private readonly AppSettings _appSettings;
    private readonly Lazy<IReadOnlyList<FeatureTogglePageInfo>> _toggleablePages;
    private static readonly HashSet<string> ValidFeatureToggleKeys = GetValidFeatureToggleKeys();

    public FeatureTogglePageProvider(List<PageMetadata> pages, AppSettings appSettings)
    {
        ArgumentNullException.ThrowIfNull(pages);
        ArgumentNullException.ThrowIfNull(appSettings);
        _pages = pages;
        _appSettings = appSettings;
        _toggleablePages = new Lazy<IReadOnlyList<FeatureTogglePageInfo>>(BuildToggleablePages);
    }

    /// <inheritdoc />
    public IReadOnlyList<FeatureTogglePageInfo> GetToggleablePages() => _toggleablePages.Value;

    private static HashSet<string> GetValidFeatureToggleKeys()
    {
        var props = typeof(FeatureToggleSettings)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.PropertyType == typeof(bool) && p.Name.EndsWith("Available"))
            .Select(p => p.Name)
            .ToHashSet();
        return props;
    }

    private IReadOnlyList<FeatureTogglePageInfo> BuildToggleablePages()
    {
        return _pages
            .Where(p => !string.IsNullOrEmpty(p.FeatureToggleKey) && ValidFeatureToggleKeys.Contains(p.FeatureToggleKey))
            .Select(p => new FeatureTogglePageInfo(
                Title: p.Title,
                FeatureToggleKey: p.FeatureToggleKey!,
                BadgeLabel: GetBadgeLabel(p),
                Category: p.Category,
                Order: p.Order))
            .OrderBy(p => (int)p.Category)
            .ThenBy(p => p.Order)
            .ToList();
    }

    private string? GetBadgeLabel(PageMetadata page)
    {
        if (string.IsNullOrEmpty(page.BadgeLabelKey)) return null;
        var prop = typeof(FeatureToggleSettings).GetProperty(page.BadgeLabelKey);
        if (prop == null) return null;
        var value = prop.GetValue(_appSettings.FeatureToggles) as string;
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }
}
