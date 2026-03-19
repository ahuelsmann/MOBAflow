// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.Controls;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Markup;

/// <summary>
/// Base class for a panel that collapses into a narrow vertical tab and expands to show its content.
/// Behaves like Visual Studio's auto-hide panels or a vertical expander.
/// </summary>
[ContentProperty(Name = nameof(PanelContent))]
public abstract class CollapsibleColumnBase : Control
{
    /// <summary>
    /// Header text displayed in both collapsed tab and expanded header.
    /// </summary>
    public static readonly DependencyProperty HeaderProperty =
        DependencyProperty.Register(nameof(Header), typeof(string), typeof(CollapsibleColumnBase),
            new PropertyMetadata("Panel"));

    /// <summary>
    /// Segoe Fluent Icons glyph for the panel icon.
    /// </summary>
    public static readonly DependencyProperty GlyphProperty =
        DependencyProperty.Register(nameof(Glyph), typeof(string), typeof(CollapsibleColumnBase),
            new PropertyMetadata("\uE8F1"));

    /// <summary>
    /// Controls whether the panel is expanded (true) or collapsed (false).
    /// </summary>
    public static readonly DependencyProperty IsExpandedProperty =
        DependencyProperty.Register(nameof(IsExpanded), typeof(bool), typeof(CollapsibleColumnBase),
            new PropertyMetadata(true, OnIsExpandedChanged));

    /// <summary>
    /// The content displayed inside the expanded panel.
    /// </summary>
    public static readonly DependencyProperty PanelContentProperty =
        DependencyProperty.Register(nameof(PanelContent), typeof(object), typeof(CollapsibleColumnBase),
            new PropertyMetadata(null));

    /// <summary>
    /// Optional header actions shown next to the header text.
    /// </summary>
    public static readonly DependencyProperty HeaderActionsProperty =
        DependencyProperty.Register(nameof(HeaderActions), typeof(UIElement), typeof(CollapsibleColumnBase),
            new PropertyMetadata(null));

    public string Header
    {
        get => (string)GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }

    public string Glyph
    {
        get => (string)GetValue(GlyphProperty);
        set => SetValue(GlyphProperty, value);
    }

    public bool IsExpanded
    {
        get => (bool)GetValue(IsExpandedProperty);
        set => SetValue(IsExpandedProperty, value);
    }

    /// <summary>
    /// The content displayed inside the expanded panel.
    /// Set via XAML content syntax thanks to ContentProperty attribute.
    /// </summary>
    public object? PanelContent
    {
        get => GetValue(PanelContentProperty);
        set => SetValue(PanelContentProperty, value);
    }

    /// <summary>
    /// Optional header actions shown next to the header text.
    /// </summary>
    public UIElement? HeaderActions
    {
        get => (UIElement?)GetValue(HeaderActionsProperty);
        set => SetValue(HeaderActionsProperty, value);
    }

    protected CollapsibleColumnBase()
    {
        // By default, the style key will be set in the derived classes.
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        // Attach event handlers to parts from the template if they exist
        if (GetTemplateChild("CollapsedTab") is UIElement collapsedTab)
        {
            collapsedTab.PointerPressed += OnCollapsedTabPressed;
        }

        if (GetTemplateChild("CollapseButton") is Microsoft.UI.Xaml.Controls.Primitives.ButtonBase collapseButton)
        {
            collapseButton.Click += OnCollapseButtonClick;
        }

        ApplyExpansionState();
    }

    private static void OnIsExpandedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is CollapsibleColumnBase control)
        {
            control.ApplyExpansionState();
        }
    }

    private void ApplyExpansionState()
    {
        VisualStateManager.GoToState(this, IsExpanded ? "Expanded" : "Collapsed", true);
    }

    private void OnCollapsedTabPressed(object sender, PointerRoutedEventArgs e)
    {
        IsExpanded = true;
    }

    private void OnCollapseButtonClick(object sender, RoutedEventArgs e)
    {
        IsExpanded = false;
    }
}
