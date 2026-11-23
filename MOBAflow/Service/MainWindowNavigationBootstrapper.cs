// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.WinUI.Service;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using SharedUI.Configuration;

/// <summary>
/// Computed navigation visibility for registered pages and category separators.
/// </summary>
internal sealed record NavigationMenuVisibility(
    Dictionary<string, bool> PageVisibilityByTag,
    List<bool> SeparatorVisibility);

/// <summary>
/// Builds and initializes shell navigation from registered page metadata and keeps it synchronized with feature toggles.
/// </summary>
internal sealed class MainWindowNavigationBootstrapper
{
    private readonly NavigationService _navigationService;
    private readonly List<PageMetadata> _pages;
    private readonly NavigationItemFactory _navigationItemFactory;
    private readonly UiSettings _settings;
    private readonly List<(PageMetadata Page, NavigationViewItem Item)> _menuItems = [];
    private readonly List<(int SeparatorIndex, NavigationViewItemSeparator Separator)> _separators = [];

    public MainWindowNavigationBootstrapper(
        NavigationService navigationService,
        List<PageMetadata> pages,
        NavigationItemFactory navigationItemFactory,
        UiSettings settings)
    {
        _navigationService = navigationService;
        _pages = pages;
        _navigationItemFactory = navigationItemFactory;
        _settings = settings;
    }

    public void BuildMenu(NavigationView navigationView)
    {
        ArgumentNullException.ThrowIfNull(navigationView);

        navigationView.MenuItems.Clear();
        _menuItems.Clear();
        _separators.Clear();

        NavigationCategory? lastCategory = null;
        var separatorIndex = 0;

        foreach (var page in _pages)
        {
            if (lastCategory.HasValue && page.Category != lastCategory.Value)
            {
                var separator = _navigationItemFactory.CreateSeparator();
                navigationView.MenuItems.Add(separator);
                _separators.Add((separatorIndex, separator));
                separatorIndex++;
            }

            var item = _navigationItemFactory.CreateItem(page);
            navigationView.MenuItems.Add(item);
            _menuItems.Add((page, item));
            lastCategory = page.Category;
        }

        RefreshMenuVisibility();
    }

    public void RefreshMenuVisibility()
    {
        var visibility = ComputeMenuVisibility(_pages, _settings.FeatureToggles);

        foreach (var (page, item) in _menuItems)
        {
            var isVisible = visibility.PageVisibilityByTag.GetValueOrDefault(page.Tag, true);
            item.Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
        }

        foreach (var (separatorIndex, separator) in _separators)
        {
            var isVisible = separatorIndex < visibility.SeparatorVisibility.Count
                && visibility.SeparatorVisibility[separatorIndex];
            separator.Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    public string? GetFallbackPageTagIfCurrentDisabled() =>
        ResolveFallbackPageTagIfCurrentDisabled(_pages, _settings.FeatureToggles, _navigationService.CurrentPageTag);

    public bool TrySelectNavigationItem(NavigationView navigationView, string? pageTag)
    {
        ArgumentNullException.ThrowIfNull(navigationView);
        if (string.IsNullOrWhiteSpace(pageTag))
        {
            return false;
        }

        var targetItem = _menuItems
            .Select(entry => entry.Item)
            .FirstOrDefault(item => string.Equals(item.Tag as string, pageTag, StringComparison.Ordinal))
            ?? navigationView.MenuItems
                .Concat(navigationView.FooterMenuItems)
                .OfType<NavigationViewItem>()
                .FirstOrDefault(item => string.Equals(item.Tag as string, pageTag, StringComparison.Ordinal));

        if (targetItem is null || ReferenceEquals(navigationView.SelectedItem, targetItem))
        {
            return false;
        }

        navigationView.SelectedItem = targetItem;
        return true;
    }

    internal static bool IsPageVisible(PageMetadata page, FeatureToggleSettings toggles)
    {
        ArgumentNullException.ThrowIfNull(page);
        ArgumentNullException.ThrowIfNull(toggles);

        return string.IsNullOrEmpty(page.FeatureToggleKey)
            || FeatureToggleRegistry.GetPageAvailabilityOrDefault(toggles, page.FeatureToggleKey);
    }

    internal static NavigationMenuVisibility ComputeMenuVisibility(
        IReadOnlyList<PageMetadata> pages,
        FeatureToggleSettings toggles)
    {
        ArgumentNullException.ThrowIfNull(pages);
        ArgumentNullException.ThrowIfNull(toggles);

        var pageVisibilityByTag = new Dictionary<string, bool>(StringComparer.Ordinal);
        var runHasVisible = new List<bool>();
        NavigationCategory? lastCategory = null;

        foreach (var page in pages)
        {
            var isVisible = IsPageVisible(page, toggles);
            pageVisibilityByTag[page.Tag] = isVisible;

            if (!lastCategory.HasValue || page.Category != lastCategory.Value)
            {
                runHasVisible.Add(isVisible);
                lastCategory = page.Category;
            }
            else if (isVisible)
            {
                runHasVisible[^1] = true;
            }
        }

        var separatorVisibility = new List<bool>(Math.Max(0, runHasVisible.Count - 1));
        var hasVisibleBefore = runHasVisible.Count > 0 && runHasVisible[0];
        for (var runIndex = 1; runIndex < runHasVisible.Count; runIndex++)
        {
            var currentRunHasVisible = runHasVisible[runIndex];
            separatorVisibility.Add(hasVisibleBefore && currentRunHasVisible);
            if (currentRunHasVisible)
            {
                hasVisibleBefore = true;
            }
        }

        return new NavigationMenuVisibility(pageVisibilityByTag, separatorVisibility);
    }

    internal static string? ResolveFallbackPageTagIfCurrentDisabled(
        IReadOnlyList<PageMetadata> pages,
        FeatureToggleSettings toggles,
        string? currentPageTag)
    {
        ArgumentNullException.ThrowIfNull(pages);
        ArgumentNullException.ThrowIfNull(toggles);

        if (string.IsNullOrWhiteSpace(currentPageTag))
        {
            return null;
        }

        var currentPage = pages.FirstOrDefault(p => string.Equals(p.Tag, currentPageTag, StringComparison.Ordinal));
        if (currentPage is null || IsPageVisible(currentPage, toggles))
        {
            return null;
        }

        return pages
            .FirstOrDefault(p => string.Equals(p.FeatureToggleKey, FeatureTogglePages.Overview, StringComparison.Ordinal))
            ?.Tag ?? "overview";
    }

    public async Task InitializeAsync(Frame contentFrame, Action<string?> updateActivePhotoAssignmentPageTag)
    {
        await _navigationService.InitializeAsync(contentFrame).ConfigureAwait(true);
        await _navigationService.NavigateToOverviewAsync().ConfigureAwait(true);
        updateActivePhotoAssignmentPageTag(_navigationService.CurrentPageTag);
    }
}
