// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.Controls;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

/// <summary>
/// A panel that collapses into a narrow vertical tab and expands to show its content.
/// Behaves like Visual Studio's auto-hide panels or a vertical expander.
/// </summary>
public sealed partial class CollapsibleColumn : UserControl
{
    /// <summary>
    /// Header text displayed in both collapsed tab and expanded header.
    /// </summary>
    public static readonly DependencyProperty HeaderProperty =
        DependencyProperty.Register(nameof(Header), typeof(string), typeof(CollapsibleColumn),
            new PropertyMetadata("Panel", OnHeaderChanged));

    /// <summary>
    /// Segoe Fluent Icons glyph for the panel icon.
    /// </summary>
    public static readonly DependencyProperty GlyphProperty =
        DependencyProperty.Register(nameof(Glyph), typeof(string), typeof(CollapsibleColumn),
            new PropertyMetadata("\uE8F1", OnGlyphChanged));

    /// <summary>
    /// Controls whether the panel is expanded (true) or collapsed (false).
    /// </summary>
    public static readonly DependencyProperty IsExpandedProperty =
        DependencyProperty.Register(nameof(IsExpanded), typeof(bool), typeof(CollapsibleColumn),
            new PropertyMetadata(true, OnIsExpandedChanged));

    /// <summary>
    /// The content displayed inside the expanded panel.
    /// </summary>
    public static readonly DependencyProperty PanelContentProperty =
        DependencyProperty.Register(nameof(PanelContent), typeof(object), typeof(CollapsibleColumn),
            new PropertyMetadata(null, OnPanelContentChanged));

    /// <summary>
    /// Optional header actions shown next to the header text.
    /// </summary>
    public static readonly DependencyProperty HeaderActionsProperty =
        DependencyProperty.Register(nameof(HeaderActions), typeof(UIElement), typeof(CollapsibleColumn),
            new PropertyMetadata(null, OnHeaderActionsChanged));

    public CollapsibleColumn()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

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

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        ApplyGlyph();
        ApplyExpansionState();
        ApplyHeaderActions();
    }

    private static void OnHeaderChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is CollapsibleColumn control)
            control.HeaderTextBlock.Text = (string)e.NewValue;
    }

    private static void OnGlyphChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is CollapsibleColumn control)
            control.ApplyGlyph();
    }

    private static void OnIsExpandedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is CollapsibleColumn control)
            control.ApplyExpansionState();
    }

    private static void OnPanelContentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is CollapsibleColumn control)
            control.ContentArea.Content = e.NewValue;
    }

    private static void OnHeaderActionsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is CollapsibleColumn control)
            control.ApplyHeaderActions();
    }

    private void ApplyGlyph()
    {
        if (CollapsedIcon is not null)
            CollapsedIcon.Glyph = Glyph;
        if (ExpandedIcon is not null)
            ExpandedIcon.Glyph = Glyph;
    }

    private void ApplyExpansionState()
    {
        VisualStateManager.GoToState(this, IsExpanded ? "Expanded" : "Collapsed", true);
    }

    private void ApplyHeaderActions()
    {
        if (HeaderActionsPresenter is null)
        {
            return;
        }

        HeaderActionsPresenter.Content = HeaderActions;
        HeaderActionsPresenter.Visibility = HeaderActions is null ? Visibility.Collapsed : Visibility.Visible;
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
