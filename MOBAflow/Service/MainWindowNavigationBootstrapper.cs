// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.WinUI.Service;

using Common.Navigation;

using Microsoft.UI.Xaml.Controls;

/// <summary>
/// Builds and initializes shell navigation from registered page metadata.
/// </summary>
public sealed class MainWindowNavigationBootstrapper
{
    private readonly NavigationService _navigationService;
    private readonly List<PageMetadata> _pages;
    private readonly NavigationItemFactory _navigationItemFactory;

    public MainWindowNavigationBootstrapper(
        NavigationService navigationService,
        List<PageMetadata> pages,
        NavigationItemFactory navigationItemFactory)
    {
        _navigationService = navigationService;
        _pages = pages;
        _navigationItemFactory = navigationItemFactory;
    }

    public void BuildMenu(NavigationView navigationView)
    {
        navigationView.MenuItems.Clear();

        NavigationCategory? lastCategory = null;

        foreach (var page in _pages)
        {
            if (lastCategory.HasValue && page.Category != lastCategory.Value)
            {
                navigationView.MenuItems.Add(_navigationItemFactory.CreateSeparator());
            }

            navigationView.MenuItems.Add(_navigationItemFactory.CreateItem(page));
            lastCategory = page.Category;
        }
    }

    public async Task InitializeAsync(Frame contentFrame, Action<string?> updateActivePhotoAssignmentPageTag)
    {
        await _navigationService.InitializeAsync(contentFrame).ConfigureAwait(true);
        await _navigationService.NavigateToOverviewAsync().ConfigureAwait(true);
        updateActivePhotoAssignmentPageTag(_navigationService.CurrentPageTag);
    }
}
