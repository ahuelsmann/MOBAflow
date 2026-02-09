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
    /// Wird ausgelöst, wenn der Pin-Button geklickt wird (Qt-ADS: "Auto Hide" toggle).
    /// </summary>
    public event EventHandler? PinToggleRequested;

    /// <summary>
    /// Wird ausgelöst, wenn der Undock-Button geklickt wird (zurück als Tab in Document Area).
    /// </summary>
    public event EventHandler? UndockRequested;

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
            new PropertyMetadata("\uE71E"));

    public static readonly DependencyProperty PanelContentProperty =
        DependencyProperty.Register(
            nameof(PanelContent),
            typeof(UIElement),
            typeof(DockPanel),
            new PropertyMetadata(null));

    public static readonly DependencyProperty IsPinnedProperty =
        DependencyProperty.Register(
            nameof(IsPinned),
            typeof(bool),
            typeof(DockPanel),
            new PropertyMetadata(true, OnIsPinnedChanged));

    public static readonly DependencyProperty IsMaximizedProperty =
        DependencyProperty.Register(
            nameof(IsMaximized),
            typeof(bool),
            typeof(DockPanel),
            new PropertyMetadata(false));

    #endregion

    public DockPanel()
    {
        InitializeComponent();
        CloseButton.Click += (_, _) => CloseRequested?.Invoke(this, EventArgs.Empty);
        PinButton.Click += (_, _) => PinToggleRequested?.Invoke(this, EventArgs.Empty);
        MaximizeButton.Click += (_, _) => UndockRequested?.Invoke(this, EventArgs.Empty);
        Loaded += (_, _) => UpdatePinButtonStyle(IsPinned);
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

    public bool IsPinned
    {
        get => (bool)GetValue(IsPinnedProperty);
        set => SetValue(IsPinnedProperty, value);
    }

    public bool IsMaximized
    {
        get => (bool)GetValue(IsMaximizedProperty);
        set => SetValue(IsMaximizedProperty, value);
    }

    #endregion

    private void OnDragStarting(UIElement sender, DragStartingEventArgs args)
    {
        args.Data.Properties[DockPanelDataKey] = this;
        args.Data.RequestedOperation = DataPackageOperation.Move;
    }

    private static void OnIsPinnedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is DockPanel panel && e.NewValue is bool isPinned)
        {
            panel.UpdatePinButtonStyle(isPinned);
        }
    }

    private void UpdatePinButtonStyle(bool isPinned)
    {
        if (PinIcon != null)
        {
            PinIcon.Foreground = isPinned
                ? Application.Current.Resources["TextFillColorSecondaryBrush"] as Microsoft.UI.Xaml.Media.Brush
                : Application.Current.Resources["SystemAccentColor"] as Microsoft.UI.Xaml.Media.Brush;
        }

        if (PinButton != null)
        {
            Microsoft.UI.Xaml.Controls.ToolTipService.SetToolTip(PinButton, isPinned ? "Auto Hide" : "Pin");
        }
    }
}
