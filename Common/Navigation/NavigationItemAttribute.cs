// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Common.Navigation;

/// <summary>
/// Marks a Page class for automatic navigation registration.
/// Pages with this attribute are auto-discovered and registered in DI + NavigationView.
/// Used by both core app pages and plugin pages.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class NavigationItemAttribute : Attribute
{
    /// <summary>
    /// Navigation tag (e.g., "overview", "journeys"). Used for routing.
    /// </summary>
    public required string Tag { get; init; }

    /// <summary>
    /// Display title in navigation menu.
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// Segoe MDL2 icon glyph (e.g., "\uE80F") or null for PathIcon.
    /// </summary>
    public string? Icon { get; init; }

    /// <summary>
    /// Navigation category for grouping with separators.
    /// </summary>
    public NavigationCategory Category { get; init; } = NavigationCategory.Core;

    /// <summary>
    /// Order within category (lower = higher in list).
    /// </summary>
    public int Order { get; init; } = 100;

    /// <summary>
    /// Feature toggle key (e.g., "IsOverviewPageAvailable") or null.
    /// </summary>
    public string? FeatureToggleKey { get; init; }

    /// <summary>
    /// Badge label key (e.g., "OverviewPageLabel") or null.
    /// </summary>
    public string? BadgeLabelKey { get; init; }

    /// <summary>
    /// PathIcon geometry data (if Icon is null).
    /// </summary>
    public string? PathIconData { get; init; }

    /// <summary>
    /// Whether the title should be bold in navigation menu.
    /// </summary>
    public bool IsBold { get; init; }
}
