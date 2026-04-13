// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.Service;

using Common.Configuration;
using Common.Navigation;

/// <summary>
/// Provides the list of feature-toggle pages based on NavigationRegistration.
/// Filters to only pages whose FeatureToggleKey exists in <see cref="FeatureToggleRegistry"/>.
/// </summary>
internal sealed class FeatureTogglePageProvider : IFeatureTogglePageProvider
{
    private readonly List<PageMetadata> _pages;
    private readonly AppSettings _appSettings;
    private readonly Lazy<IReadOnlyList<FeatureTogglePageInfo>> _toggleablePages;

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

    private IReadOnlyList<FeatureTogglePageInfo> BuildToggleablePages()
    {
        return _pages
            .Where(p => !string.IsNullOrEmpty(p.FeatureToggleKey) && FeatureToggleRegistry.PageAvailabilityKeys.Contains(p.FeatureToggleKey!))
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

    private string? GetBadgeLabel(PageMetadata page) =>
        FeatureToggleRegistry.GetBadgeLabel(_appSettings.FeatureToggles, page.BadgeLabelKey);
}
