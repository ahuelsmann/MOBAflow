// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Common.Navigation;

/// <summary>
/// Information for a page with a feature toggle, used in the dynamic Settings UI.
/// </summary>
/// <param name="Title">Display name of the page</param>
/// <param name="FeatureToggleKey">Property name in FeatureToggleSettings (e.g. IsOverviewPageAvailable)</param>
/// <param name="BadgeLabel">Optional badge label (e.g. Preview)</param>
/// <param name="Category">Navigation category for grouping</param>
/// <param name="Order">Order within the category</param>
public record FeatureTogglePageInfo(
    string Title,
    string FeatureToggleKey,
    string? BadgeLabel,
    NavigationCategory Category,
    int Order);

/// <summary>
/// Provides the list of pages that can be enabled or disabled via feature toggles in Settings.
/// Enables dynamic building of the feature toggle list based on NavigationRegistration.
/// </summary>
public interface IFeatureTogglePageProvider
{
    /// <summary>
    /// Returns all pages that have a feature toggle and should be shown in Settings.
    /// Only pages whose FeatureToggleKey exists in FeatureToggleSettings are included.
    /// </summary>
    IReadOnlyList<FeatureTogglePageInfo> GetToggleablePages();
}
