// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.Service;

using Common.Configuration;
using Common.Navigation;

using Microsoft.Extensions.DependencyInjection;

using SharedUI.Interface;
using SharedUI.ViewModel;

using View;

using ViewModel;

/// <summary>
/// Centralizes page and navigation registrations for the WinUI app.
/// Uses auto-discovery for pages with [NavigationItem] attribute.
/// Manual registrations only for pages requiring custom DI setup.
/// </summary>
public static class NavigationRegistration
{
    /// <summary>
    /// Discovers and registers all pages (auto + custom DI).
    /// Returns combined page metadata for NavigationView building.
    /// </summary>
    public static List<PageMetadata> RegisterPages(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Auto-discover pages with [NavigationItem] attribute
        var discoveredPages = PageDiscoveryService.DiscoverPages(services, typeof(NavigationRegistration).Assembly);

        // Manual registrations for pages with custom DI requirements
        var customPages = RegisterPagesWithCustomDI(services);

        // Combine and return sorted
        return discoveredPages.Concat(customPages)
            .OrderBy(p => (int)p.Category)
            .ThenBy(p => p.Order)
            .ToList();
    }

    private static List<PageMetadata> RegisterPagesWithCustomDI(IServiceCollection services)
    {
        var pages = new List<PageMetadata>();

        // JourneysPage: requires AppSettings + ISettingsService injection
        services.AddTransient<JourneysPage>(sp => new JourneysPage(
            sp.GetRequiredService<MainWindowViewModel>(),
            sp.GetRequiredService<AppSettings>(),
            sp.GetService<ISettingsService>()));
        pages.Add(new PageMetadata(
            Tag: "journeys",
            Title: "Journeys",
            Icon: "\uE7C1",
            PageType: typeof(JourneysPage),
            Category: NavigationCategory.Journey,
            Order: 10,
            FeatureToggleKey: "IsJourneysPageAvailable",
            BadgeLabelKey: "JourneysPageLabel",
            PathIconData: null,
            IsBold: true));

        // SignalBoxPage: requires custom viewmodel injection
        services.AddTransient<SignalBoxPage>(sp => new SignalBoxPage(
            sp.GetRequiredService<MainWindowViewModel>(),
            sp.GetRequiredService<ISkinProvider>(),
            sp.GetRequiredService<SkinSelectorViewModel>()));
        pages.Add(new PageMetadata(
            Tag: "signalbox",
            Title: "Signal Box",
            Icon: null,
            PageType: typeof(SignalBoxPage),
            Category: NavigationCategory.TrackManagement,
            Order: 20,
            FeatureToggleKey: null,
            BadgeLabelKey: null,
            PathIconData: "M7,2 A2,2 0 1,1 11,2 A2,2 0 1,1 7,2 M3,10 A2,2 0 1,1 7,10 A2,2 0 1,1 3,10 M11,10 A2,2 0 1,1 15,10 A2,2 0 1,1 11,10",
            IsBold: false));

        // DockingPage: requires separate ViewModel registration
        services.AddTransient<DockingPageViewModel>();
        services.AddTransient<DockingPage>();
        pages.Add(new PageMetadata(
            Tag: "docking",
            Title: "Docking",
            Icon: null,
            PageType: typeof(DockingPage),
            Category: NavigationCategory.Monitoring,
            Order: 20,
            FeatureToggleKey: "IsDockingPageAvailable",
            BadgeLabelKey: "DockingPageLabel",
            PathIconData: "M2,3 L6,3 L6,7 L2,7 Z M8,3 L12,3 L12,7 L8,7 Z M2,9 L6,9 L6,13 L2,13 Z M8,9 L12,9 L12,13 L8,13 Z",
            IsBold: false));

        return pages;
    }
}
