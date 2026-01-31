// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.WinUI.Service;

using Common.Configuration;
using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

/// <summary>
/// Factory for creating NavigationViewItems from PageRegistration.
/// Handles icon creation, badge rendering, and Feature Toggle visibility binding.
/// </summary>
public sealed class NavigationItemFactory
{
    private readonly AppSettings _settings;

    public NavigationItemFactory(AppSettings settings)
    {
        _settings = settings;
    }

    /// <summary>
    /// Creates a NavigationViewItem from a PageRegistration.
    /// </summary>
    public NavigationViewItem CreateItem(PageRegistration registration)
    {
        var item = new NavigationViewItem
        {
            Tag = registration.Tag,
            Icon = CreateIcon(registration),
            Content = CreateContent(registration)
        };

        // Apply visibility based on Feature Toggle
        if (!string.IsNullOrEmpty(registration.FeatureToggleKey))
        {
            var isVisible = GetFeatureToggleValue(registration.FeatureToggleKey);
            item.Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
        }

        // Add tooltip for PathIcon pages
        if (!string.IsNullOrEmpty(registration.PathIconData))
        {
            ToolTipService.SetToolTip(item, registration.Title);
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
    private IconElement CreateIcon(PageRegistration registration)
    {
        // Use PathIcon if PathIconData is specified
        if (!string.IsNullOrEmpty(registration.PathIconData))
        {
            return new PathIcon
            {
                Data = (Geometry)XamlBindingHelper.ConvertValue(
                    typeof(Geometry), registration.PathIconData)
            };
        }

        // Use FontIcon with Glyph
        if (!string.IsNullOrEmpty(registration.IconGlyph))
        {
            return new FontIcon { Glyph = registration.IconGlyph };
        }

        // Fallback icon
        return new FontIcon { Glyph = "\uE7C3" }; // Page icon
    }

    /// <summary>
    /// Creates the content (title with optional badge).
    /// </summary>
    private object CreateContent(PageRegistration registration)
    {
        var badgeLabel = GetBadgeLabel(registration);
        var hasBadge = !string.IsNullOrEmpty(badgeLabel);

        // Simple title without badge
        if (!hasBadge && !registration.IsBold)
        {
            return registration.Title;
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
            Text = registration.Title,
            VerticalAlignment = VerticalAlignment.Center
        };

        if (registration.IsBold)
        {
            titleBlock.FontWeight = FontWeights.SemiBold;
        }

        panel.Children.Add(titleBlock);

        // Badge (Preview, SKIN, etc.)
        if (hasBadge)
        {
            var badge = CreateBadge(badgeLabel, registration);
            panel.Children.Add(badge);
        }

        return panel;
    }

    /// <summary>
    /// Creates a badge Border element.
    /// </summary>
    private Border CreateBadge(string label, PageRegistration registration)
    {
        // SKIN badge = purple (#5C2D91)
        var isSkinBadge = label.Equals("SKIN", StringComparison.OrdinalIgnoreCase);
        var isPreviewBadge = label.Equals("Preview", StringComparison.OrdinalIgnoreCase);

        Brush backgroundColor;
        Brush foregroundColor;

        if (isSkinBadge)
        {
            backgroundColor = new SolidColorBrush(Color.FromArgb(255, 92, 45, 145)); // #5C2D91
            foregroundColor = new SolidColorBrush(Colors.White);
        }
        else if (isPreviewBadge)
        {
            backgroundColor = new SolidColorBrush(Color.FromArgb(255, 92, 45, 145)); // #5C2D91
            //backgroundColor = (Brush)Application.Current.Resources["SystemAccentColorBrush"];
            foregroundColor = new SolidColorBrush(Colors.White);
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
    private string GetBadgeLabel(PageRegistration registration)
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
