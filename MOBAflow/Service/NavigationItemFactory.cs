// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.MOBAflow.Service;

using SharedUI.Configuration;

using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;

using Moba.MOBAflow.View;

/// <summary>
/// Factory for creating NavigationViewItems from PageMetadata.
/// Handles icon creation, badge rendering, and Feature Toggle visibility binding.
/// </summary>
internal sealed class NavigationItemFactory
{
    private readonly UiSettings _settings;

    public NavigationItemFactory(UiSettings settings)
    {
        _settings = settings;
    }

    /// <summary>
    /// Creates a NavigationViewItem from a PageMetadata.
    /// </summary>
    public NavigationViewItem CreateItem(PageMetadata page)
    {
        var badgeLabel = GetBadgeLabel(page);
        var item = new NavigationViewItem
        {
            Tag = page.Tag,
            Icon = CreateIcon(page),
            Content = CreateContent(page, badgeLabel)
        };

        // Bold or badged items show a panel as content, which gives Narrator no name; name every item explicitly.
        AutomationProperties.SetName(item, ComposeAccessibleName(page.Title, badgeLabel));

        ApplyVisibility(item, page);

        // Add tooltip for PathIcon pages
        if (!string.IsNullOrEmpty(page.PathIconData))
        {
            ToolTipService.SetToolTip(item, page.Title);
        }

        return item;
    }

    /// <summary>
    /// Creates a NavigationViewItemSeparator.
    /// </summary>
    public NavigationViewItemSeparator CreateSeparator() => new();

    /// <summary>
    /// Updates the visibility of <paramref name="item"/> based on the current Feature Toggle state of <paramref name="page"/>.
    /// </summary>
    public void ApplyVisibility(NavigationViewItem item, PageMetadata page)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(page);

        var isVisible = IsPageVisible(page);
        item.Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
    }

    /// <summary>
    /// Returns whether <paramref name="page"/> should be visible in the navigation pane.
    /// </summary>
    public bool IsPageVisible(PageMetadata page)
    {
        ArgumentNullException.ThrowIfNull(page);
        return string.IsNullOrEmpty(page.FeatureToggleKey)
            || GetFeatureToggleValue(page.FeatureToggleKey);
    }

    /// <summary>
    /// Composes the accessible name of a navigation item: the page title, followed by the badge when one is shown
    /// (for example "Timetable, Preview").
    /// </summary>
    internal static string ComposeAccessibleName(string title, string? badgeLabel) =>
        string.IsNullOrWhiteSpace(badgeLabel) ? title : $"{title}, {badgeLabel}";

    /// <summary>
    /// Creates the icon (FontIcon or PathIcon).
    /// </summary>
    private IconElement CreateIcon(PageMetadata page)
    {
        // Use PathIcon if PathIconData is specified
        if (!string.IsNullOrEmpty(page.PathIconData))
        {
            return new PathIcon
            {
                Data = (Geometry)XamlBindingHelper.ConvertValue(
                    typeof(Geometry), page.PathIconData)
            };
        }

        // Use FontIcon with Glyph
        if (!string.IsNullOrEmpty(page.Icon))
        {
            return new FontIcon { Glyph = page.Icon };
        }

        // Fallback icon
        return new FontIcon { Glyph = "\uE7C3" }; // Page icon
    }

    /// <summary>
    /// Creates the content (title with optional badge).
    /// </summary>
    private static object CreateContent(PageMetadata page, string badgeLabel)
    {
        var hasBadge = !string.IsNullOrEmpty(badgeLabel);

        // Simple title without badge
        if (!hasBadge && !page.IsBold)
        {
            return page.Title;
        }

        // Title with formatting or badge
        var panel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8
        };

        // Title TextBlock
        var titleBlock = new TextBlock
        {
            Text = page.Title,
            VerticalAlignment = VerticalAlignment.Center
        };

        if (page.IsBold)
        {
            titleBlock.FontWeight = FontWeights.SemiBold;
        }

        panel.Children.Add(titleBlock);

        // Badge (Preview, etc.)
        if (hasBadge)
        {
            var badge = CreateBadge(badgeLabel);
            panel.Children.Add(badge);
        }

        return panel;
    }

    /// <summary>
    /// Creates a badge Border element.
    /// </summary>
    private static Border CreateBadge(string label)
    {
        var isPreviewBadge = label.Equals("Preview", StringComparison.OrdinalIgnoreCase);

        Brush backgroundColor;
        Brush foregroundColor;

        var themeRoot = ThemeResourceResolver.GetDefaultThemeRoot();

        if (isPreviewBadge)
        {
            backgroundColor = ThemeResourceResolver.ResolveBrush(themeRoot, "AccentFillColorDefaultBrush", Microsoft.UI.Colors.Blue);
            foregroundColor = ThemeResourceResolver.ResolveBrush(themeRoot, "TextFillColorInverseBrush", Microsoft.UI.Colors.White);
        }
        else
        {
            backgroundColor = ThemeResourceResolver.ResolveBrush(themeRoot, "SystemFillColorCautionBackgroundBrush", Microsoft.UI.Colors.Orange);
            foregroundColor = ThemeResourceResolver.ResolveBrush(themeRoot, "SystemFillColorCautionBrush", Microsoft.UI.Colors.Orange);
        }

        var badge = new Border
        {
            Padding = new Thickness(3),
            VerticalAlignment = VerticalAlignment.Center,
            Background = backgroundColor,
            CornerRadius = new CornerRadius(3)
        };

        var badgeText = new TextBlock
        {
            Text = label,
            FontSize = 9,
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = foregroundColor
        };

        if (!isPreviewBadge)
        {
            badgeText.Opacity = 0.7;
        }

        badge.Child = badgeText;
        return badge;
    }

    /// <summary>
    /// Gets the badge label of a feature-toggle page from settings.
    /// </summary>
    private string GetBadgeLabel(PageMetadata registration) =>
        FeatureToggleRegistry.GetBadgeLabel(_settings.FeatureToggles, registration.FeatureToggleKey) ?? string.Empty;

    /// <summary>
    /// Gets the Feature Toggle value from settings; pages without a registered toggle stay visible.
    /// </summary>
    private bool GetFeatureToggleValue(string key) =>
        FeatureToggleRegistry.GetPageAvailabilityOrDefault(_settings.FeatureToggles, key);
}
