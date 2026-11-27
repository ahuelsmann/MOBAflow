// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.MOBAflow.Controls;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Markup;

/// <summary>
/// Base class for a panel that collapses into a narrow vertical tab and expands to show its content.
/// Behaves like Visual Studio's auto-hide panels or a vertical expander.
/// </summary>
[ContentProperty(Name = nameof(PanelContent))]
[TemplatePart(Name = CollapsedTabPartName, Type = typeof(ButtonBase))]
[TemplatePart(Name = CollapseButtonPartName, Type = typeof(ButtonBase))]
[TemplatePart(Name = ExpandedPanelPartName, Type = typeof(FrameworkElement))]
public abstract class CollapsibleColumnBase : Control
{
    private const string CollapsedTabPartName = "CollapsedTab";
    private const string CollapseButtonPartName = "CollapseButton";
    private const string ExpandedPanelPartName = "ExpandedPanel";

    private UIElement? _collapsedTab;
    private ButtonBase? _collapseButton;
    private FrameworkElement? _expandedPanel;

    /// <summary>
    /// Header text displayed in both collapsed tab and expanded header.
    /// </summary>
    public static readonly DependencyProperty HeaderProperty =
        DependencyProperty.Register(nameof(Header), typeof(string), typeof(CollapsibleColumnBase),
            new PropertyMetadata("Panel", OnHeaderChanged));

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

    protected override void OnApplyTemplate()
    {
        if (_collapsedTab is ButtonBase previousTabButton)
        {
            previousTabButton.Click -= OnCollapsedTabClick;
        }
        else if (_collapsedTab is not null)
        {
            _collapsedTab.PointerPressed -= OnCollapsedTabPressed;
        }

        if (_collapseButton is not null)
        {
            _collapseButton.Click -= OnCollapseButtonClick;
        }

        base.OnApplyTemplate();

        _expandedPanel = GetTemplateChild(ExpandedPanelPartName) as FrameworkElement;
        _collapsedTab = GetTemplateChild(CollapsedTabPartName) as UIElement;
        _collapseButton = GetTemplateChild(CollapseButtonPartName) as ButtonBase;

        // Attach event handlers to parts from the template if they exist
        if (_collapsedTab is ButtonBase collapsedTabButton)
        {
            collapsedTabButton.Click += OnCollapsedTabClick;
        }
        else if (_collapsedTab is not null)
        {
            _collapsedTab.PointerPressed += OnCollapsedTabPressed;
        }

        if (_collapseButton is not null)
        {
            _collapseButton.Click += OnCollapseButtonClick;
        }

        UpdateCollapsedTabAccessibleName();
        ApplyExpansionState();
    }

    internal static string FormatExpandTabAccessibleName(string? header)
    {
        if (string.IsNullOrWhiteSpace(header))
        {
            return "Expand panel";
        }

        var trimmed = header.Trim();
        return trimmed.EndsWith("panel", StringComparison.OrdinalIgnoreCase)
            ? $"Expand {trimmed}"
            : $"Expand {trimmed} panel";
    }

    private static void OnHeaderChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is CollapsibleColumnBase control)
        {
            control.UpdateCollapsedTabAccessibleName();
        }
    }

    private static void OnIsExpandedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is CollapsibleColumnBase control)
        {
            control.ApplyExpansionState();
        }
    }

    private void UpdateCollapsedTabAccessibleName()
    {
        if (_collapsedTab is not null)
        {
            AutomationProperties.SetName(_collapsedTab, FormatExpandTabAccessibleName(Header));
        }
    }

    private void ApplyExpansionState()
    {
        VisualStateManager.GoToState(this, IsExpanded ? "Expanded" : "Collapsed", true);
    }

    private void OnCollapsedTabClick(object sender, RoutedEventArgs e)
    {
        var focusState = (_collapsedTab as Control)?.FocusState ?? FocusState.Unfocused;
        IsExpanded = true;
        if (focusState != FocusState.Unfocused)
        {
            var targetFocusState = focusState == FocusState.Keyboard
                ? FocusState.Keyboard
                : FocusState.Programmatic;
            _ = DispatcherQueue?.TryEnqueue(() => FocusExpandedTarget(targetFocusState));
        }
    }

    private void OnCollapsedTabPressed(object sender, PointerRoutedEventArgs e)
    {
        IsExpanded = true;
    }

    private void OnCollapseButtonClick(object sender, RoutedEventArgs e)
    {
        var focusState = _collapseButton?.FocusState ?? FocusState.Unfocused;
        IsExpanded = false;
        if (focusState == FocusState.Keyboard)
        {
            _ = DispatcherQueue?.TryEnqueue(() => (_collapsedTab as Control)?.Focus(FocusState.Keyboard));
        }
    }

    private void FocusExpandedTarget(FocusState targetFocusState)
    {
        if (!IsExpanded || XamlRoot is null)
        {
            return;
        }

        if (_expandedPanel is not null &&
            FocusManager.FindFirstFocusableElement(_expandedPanel) is Control firstFocusable)
        {
            firstFocusable.Focus(targetFocusState);
            return;
        }

        _collapseButton?.Focus(targetFocusState);
    }
}
