// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.WinUI.Service;

using Common.Configuration;

using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;

/// <summary>
/// Factory for creating NavigationViewItems from PageMetadata.
/// Handles icon creation, badge rendering, and Feature Toggle visibility binding.
/// </summary>
internal sealed class NavigationItemFactory
{
    private readonly AppSettings _settings;

    public NavigationItemFactory(AppSettings settings)
    {
        _settings = settings;
    }

    /// <summary>
    /// Creates a NavigationViewItem from a PageMetadata.
    /// </summary>
    public NavigationViewItem CreateItem(PageMetadata page)
    {
        var item = new NavigationViewItem
        {
            Tag = page.Tag,
            Icon = CreateIcon(page),
            Content = CreateContent(page)
        };

        // Apply visibility based on Feature Toggle
        if (!string.IsNullOrEmpty(page.FeatureToggleKey))
        {
            var isVisible = GetFeatureToggleValue(page.FeatureToggleKey);
            item.Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
        }

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
    private object CreateContent(PageMetadata page)
    {
        var badgeLabel = GetBadgeLabel(page);
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

        // Badge (Preview, SKIN, etc.)
        if (hasBadge)
        {
            var badge = CreateBadge(badgeLabel, page);
            panel.Children.Add(badge);
        }

        return panel;
    }

    /// <summary>
    /// Creates a badge Border element.
    /// </summary>
    private Border CreateBadge(string label, PageMetadata registration)
    {
        // SKIN badge = purple (#5C2D91)
        var isSkinBadge = label.Equals("SKIN", StringComparison.OrdinalIgnoreCase);
        var isPreviewBadge = label.Equals("Preview", StringComparison.OrdinalIgnoreCase);

        Brush backgroundColor;
        Brush foregroundColor;

        if (isSkinBadge || isPreviewBadge)
        {
            backgroundColor = (Brush)Application.Current.Resources["AccentFillColorDefaultBrush"];
            foregroundColor = (Brush)Application.Current.Resources["TextFillColorInverseBrush"];
        }
        else
        {
            backgroundColor = (Brush)Application.Current.Resources["SystemFillColorCautionBackgroundBrush"];
            foregroundColor = (Brush)Application.Current.Resources["SystemFillColorCautionBrush"];
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

        if (!isSkinBadge && !isPreviewBadge)
        {
            badgeText.Opacity = 0.7;
        }

        badge.Child = badgeText;
        return badge;
    }

    /// <summary>
    /// Gets the badge label from settings or returns hardcoded "SKIN" for theme-enabled pages.
    /// </summary>
    private string GetBadgeLabel(PageMetadata registration)
    {
        // Hardcoded SKIN badge for theme-enabled pages
        if (registration.Tag is "traincontrol2" or "signalbox2")
        {
            return "SKIN";
        }

        // Get from FeatureToggleSettings
        if (string.IsNullOrEmpty(registration.BadgeLabelKey))
        {
            return string.Empty;
        }

        var property = typeof(FeatureToggleSettings).GetProperty(registration.BadgeLabelKey);
        if (property == null)
        {
            return string.Empty;
        }

        return property.GetValue(_settings.FeatureToggles) as string ?? string.Empty;
    }

    /// <summary>
    /// Gets the Feature Toggle value from settings.
    /// </summary>
    private bool GetFeatureToggleValue(string key)
    {
        var property = typeof(FeatureToggleSettings).GetProperty(key);
        if (property == null)
        {
            return true; // Default: visible
        }

        return property.GetValue(_settings.FeatureToggles) as bool? ?? true;
    }
}
