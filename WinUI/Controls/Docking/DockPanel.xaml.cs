// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.WinUI.Controls.Docking;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

using System;

using Windows.ApplicationModel.DataTransfer;

/// <summary>
/// Specifies the docking position of a panel in the DockingManager.
/// </summary>
internal enum DockPosition
{
    Left,
    Right,
    Top,
    Bottom,
    Center
}

/// <summary>
/// Ein dockbares Panel mit Header, Fluent-Design-Icon und Aktionsbuttons.
/// Wird in DockingManager-Bereichen verwendet.
/// Cherry-Picked: Close/Undock (Qt-ADS), Pin to AutoHide (Qt-ADS 4.x).
/// </summary>
internal sealed partial class DockPanel
{
    private const string DockPanelDataKey = "DockPanel";

    /// <summary>
    /// Wird ausgelöst, wenn der Expand/Collapse-Status geändert wird.
    /// </summary>
    public event EventHandler? IsExpandedChanged;

    #region Dependency Properties

    public static readonly DependencyProperty PanelTitleProperty =
        DependencyProperty.Register(
            nameof(PanelTitle),
            typeof(string),
            typeof(DockPanel),
            new PropertyMetadata("Panel"));

    public static readonly DependencyProperty PanelIconGlyphProperty =
        DependencyProperty.Register(
            nameof(PanelIconGlyph),
            typeof(string),
            typeof(DockPanel),
            new PropertyMetadata("\uE71E", OnPanelIconGlyphChanged));

    public static readonly DependencyProperty PanelContentProperty =
        DependencyProperty.Register(
            nameof(PanelContent),
            typeof(UIElement),
            typeof(DockPanel),
            new PropertyMetadata(null));

    public static readonly DependencyProperty IsExpandedProperty =
        DependencyProperty.Register(
            nameof(IsExpanded),
            typeof(bool),
            typeof(DockPanel),
            new PropertyMetadata(true, OnIsExpandedChanged));

    public static readonly DependencyProperty DockPositionProperty =
        DependencyProperty.Register(
            nameof(DockPosition),
            typeof(DockPosition),
            typeof(DockPanel),
            new PropertyMetadata(DockPosition.Left, OnDockPositionChanged));

    #endregion

    public DockPanel()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    #region Properties

    public string PanelTitle
    {
        get => (string)GetValue(PanelTitleProperty);
        set => SetValue(PanelTitleProperty, value);
    }

    public string PanelIconGlyph
    {
        get => (string)GetValue(PanelIconGlyphProperty);
        set => SetValue(PanelIconGlyphProperty, value);
    }

    public UIElement? PanelContent
    {
        get => (UIElement?)GetValue(PanelContentProperty);
        set => SetValue(PanelContentProperty, value);
    }

    public bool IsExpanded
    {
        get => (bool)GetValue(IsExpandedProperty);
        set => SetValue(IsExpandedProperty, value);
    }

    public DockPosition DockPosition
    {
        get => (DockPosition)GetValue(DockPositionProperty);
        set => SetValue(DockPositionProperty, value);
    }

    #endregion

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        ApplyGlyph();
        ApplyExpansionState();
        ApplyDockPosition();
    }

    private void OnDragStarting(UIElement sender, DragStartingEventArgs args)
    {
        args.Data.Properties[DockPanelDataKey] = this;
        args.Data.RequestedOperation = DataPackageOperation.Move;
    }

    private static void OnPanelIconGlyphChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is DockPanel panel)
        {
            panel.ApplyGlyph();
        }
    }

    private static void OnIsExpandedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is DockPanel panel)
        {
            panel.ApplyExpansionState();
            panel.IsExpandedChanged?.Invoke(panel, EventArgs.Empty);
        }
    }

    private static void OnDockPositionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is DockPanel panel)
        {
            panel.ApplyDockPosition();
        }
    }

    private void ApplyGlyph()
    {
        if (CollapsedIcon is not null)
        {
            CollapsedIcon.Glyph = PanelIconGlyph;
        }

        if (PanelIcon is not null)
        {
            PanelIcon.Glyph = PanelIconGlyph;
        }
    }

    private void ApplyExpansionState()
    {
        VisualStateManager.GoToState(this, IsExpanded ? "Expanded" : "Collapsed", true);
    }

    private void ApplyDockPosition()
    {
        if (CollapsedTab is null || ExpandedPanel is null)
        {
            return;
        }

        var isHorizontal = DockPosition is DockPosition.Top or DockPosition.Bottom;

        if (isHorizontal)
        {
            // Top/Bottom: Full width, fixed height when collapsed
            CollapsedTab.Width = double.NaN;
            CollapsedTab.Height = 32;
            CollapsedTab.HorizontalAlignment = HorizontalAlignment.Stretch;
            CollapsedTab.VerticalAlignment = VerticalAlignment.Top;

            // ExpandedPanel should also stretch horizontally
            ExpandedPanel.Width = double.NaN;
            ExpandedPanel.Height = double.NaN;
            ExpandedPanel.HorizontalAlignment = HorizontalAlignment.Stretch;
            ExpandedPanel.VerticalAlignment = VerticalAlignment.Stretch;

            if (CollapsedStack is not null)
            {
                CollapsedStack.Orientation = Orientation.Horizontal;
            }
        }
        else
        {
            // Left/Right: Fixed width when collapsed, full height
            CollapsedTab.Width = 32;
            CollapsedTab.Height = double.NaN;
            CollapsedTab.HorizontalAlignment = HorizontalAlignment.Left;
            CollapsedTab.VerticalAlignment = VerticalAlignment.Stretch;

            // ExpandedPanel should also stretch vertically
            ExpandedPanel.Width = double.NaN;
            ExpandedPanel.Height = double.NaN;
            ExpandedPanel.HorizontalAlignment = HorizontalAlignment.Stretch;
            ExpandedPanel.VerticalAlignment = VerticalAlignment.Stretch;

            if (CollapsedStack is not null)
            {
                CollapsedStack.Orientation = Orientation.Vertical;
            }
        }
    }

    private void OnCollapsedTabPressed(object sender, PointerRoutedEventArgs e)
    {
        IsExpanded = true;
    }

    private void OnPinButtonClick(object sender, RoutedEventArgs e)
    {
        IsExpanded = !IsExpanded;
    }
}