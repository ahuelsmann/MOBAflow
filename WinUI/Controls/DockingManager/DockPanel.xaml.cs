// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.WinUI.Controls;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

using System;

using Windows.ApplicationModel.DataTransfer;

/// <summary>
/// Ein dockbares Panel mit Header, Fluent-Design-Icon und Aktionsbuttons.
/// Wird in DockingManager-Bereichen verwendet.
/// Cherry-Picked: Close/Undock (Qt-ADS), Pin to AutoHide (Qt-ADS 4.x).
/// </summary>
public sealed partial class DockPanel : UserControl
{
    private const string DockPanelDataKey = "DockPanel";

    /// <summary>
    /// Wird ausgelöst, wenn der Close-Button geklickt wird.
    /// </summary>
    public event EventHandler? CloseRequested;

    /// <summary>
    /// Wird ausgelöst, wenn der Undock-Button geklickt wird (zurück als Tab in Document Area).
    /// </summary>
    public event EventHandler? UndockRequested;

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

    #endregion

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        ApplyGlyph();
        ApplyExpansionState();
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

    private void OnCollapsedTabPressed(object sender, PointerRoutedEventArgs e)
    {
        IsExpanded = true;
    }

    private void OnPinButtonClick(object sender, RoutedEventArgs e)
    {
        IsExpanded = !IsExpanded;
    }
}
