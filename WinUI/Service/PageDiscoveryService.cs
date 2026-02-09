// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.Service;

using Common.Navigation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using System.Reflection;

/// <summary>
/// Metadata for a discovered page with navigation information.
/// </summary>
/// <param name="Tag">Navigation tag for routing</param>
/// <param name="Title">Display title</param>
/// <param name="Icon">Icon glyph or null for PathIcon</param>
/// <param name="PageType">Page class type</param>
/// <param name="Category">Navigation category</param>
/// <param name="Order">Order within category</param>
/// <param name="FeatureToggleKey">Feature toggle key or null</param>
/// <param name="BadgeLabelKey">Badge label key or null</param>
/// <param name="PathIconData">PathIcon geometry or null</param>
/// <param name="IsBold">Bold title flag</param>
public record PageMetadata(
    string Tag,
    string Title,
    string? Icon,
    Type PageType,
    NavigationCategory Category,
    int Order,
    string? FeatureToggleKey,
    string? BadgeLabelKey,
    string? PathIconData,
    bool IsBold);

/// <summary>
/// Discovers and registers pages with [NavigationItem] attribute.
/// Convention-over-configuration approach: Pages auto-register themselves.
/// </summary>
public static class PageDiscoveryService
{
    /// <summary>
    /// Discovers all Page classes with [NavigationItem] attribute.
    /// Returns metadata list for manual NavigationView building.
    /// Also auto-registers pages in DI container as Transient.
    /// </summary>
    public static List<PageMetadata> DiscoverPages(IServiceCollection services, Assembly assembly)
    {
        var pages = new List<PageMetadata>();

        var pageTypes = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && t.IsSubclassOf(typeof(Page)))
            .Where(t => t.GetCustomAttribute<NavigationItemAttribute>() != null);

        foreach (var pageType in pageTypes)
        {
            var attr = pageType.GetCustomAttribute<NavigationItemAttribute>()!;

            // Auto-register Page as Transient in DI (if not manually registered)
            services.AddTransient(pageType);

            pages.Add(new PageMetadata(
                Tag: attr.Tag,
                Title: attr.Title,
                Icon: attr.Icon,
                PageType: pageType,
                Category: attr.Category,
                Order: attr.Order,
                FeatureToggleKey: attr.FeatureToggleKey,
                BadgeLabelKey: attr.BadgeLabelKey,
                PathIconData: attr.PathIconData,
                IsBold: attr.IsBold));
        }

        return pages.OrderBy(p => (int)p.Category).ThenBy(p => p.Order).ToList();
    }
}
