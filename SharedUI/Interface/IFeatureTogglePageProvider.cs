// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.Interface;

using Configuration;

/// <summary>
/// Information for a page with a feature toggle, used in the dynamic Settings UI.
/// </summary>
/// <param name="Title">Display name of the page</param>
/// <param name="FeatureToggleKey">Page key in <see cref="FeatureToggleRegistry"/> (e.g. Overview)</param>
/// <param name="BadgeLabel">Optional badge label (e.g. Preview)</param>
public record FeatureTogglePageInfo(
    string Title,
    string FeatureToggleKey,
    string? BadgeLabel);

/// <summary>
/// Provides the list of pages that can be enabled or disabled via feature toggles in Settings,
/// already in display order.
/// </summary>
public interface IFeatureTogglePageProvider
{
    /// <summary>
    /// Returns all pages that have a feature toggle and should be shown in Settings.
    /// Only pages whose key is registered in <see cref="FeatureToggleRegistry"/> are included.
    /// </summary>
    IReadOnlyList<FeatureTogglePageInfo> GetToggleablePages();
}
