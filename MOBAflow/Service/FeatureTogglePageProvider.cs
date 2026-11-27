// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.MOBAflow.Service;

using SharedUI.Configuration;
using SharedUI.Interface;

/// <summary>
/// Provides the list of feature-toggle pages based on NavigationRegistration, ordered by navigation category.
/// Filters to only pages whose FeatureToggleKey is toggleable in <see cref="FeatureToggleRegistry"/>.
/// </summary>
internal sealed class FeatureTogglePageProvider : IFeatureTogglePageProvider
{
    private readonly List<PageMetadata> _pages;
    private readonly UiSettings _settings;
    private readonly Lazy<IReadOnlyList<FeatureTogglePageInfo>> _toggleablePages;

    public FeatureTogglePageProvider(List<PageMetadata> pages, UiSettings settings)
    {
        ArgumentNullException.ThrowIfNull(pages);
        ArgumentNullException.ThrowIfNull(settings);
        _pages = pages;
        _settings = settings;
        _toggleablePages = new Lazy<IReadOnlyList<FeatureTogglePageInfo>>(BuildToggleablePages);
    }

    /// <inheritdoc />
    public IReadOnlyList<FeatureTogglePageInfo> GetToggleablePages() => _toggleablePages.Value;

    private IReadOnlyList<FeatureTogglePageInfo> BuildToggleablePages()
    {
        return _pages
            .Where(p => FeatureToggleRegistry.IsToggleable(p.FeatureToggleKey))
            .OrderBy(p => (int)p.Category)
            .ThenBy(p => p.Order)
            .Select(p => new FeatureTogglePageInfo(
                Title: p.Title,
                FeatureToggleKey: p.FeatureToggleKey!,
                BadgeLabel: FeatureToggleRegistry.GetBadgeLabel(_settings.FeatureToggles, p.FeatureToggleKey)))
            .ToList();
    }
}
