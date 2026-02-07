// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.WinUI.Controls;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;

/// <summary>
/// Ein dockbares Panel mit Header, Fluent-Design-Icon und Aktionsbuttons.
/// Wird in DockingManager-Bereichen verwendet.
/// </summary>
public sealed partial class DockPanel : UserControl
{
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
            new PropertyMetadata(true));

    public static readonly DependencyProperty IsMaximizedProperty =
        DependencyProperty.Register(
            nameof(IsMaximized),
            typeof(bool),
            typeof(DockPanel),
            new PropertyMetadata(false));

    #endregion

    public DockPanel()
    {
        
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
}
