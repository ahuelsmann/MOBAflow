// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.WinUI.Controls;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

/// <summary>
/// DockingManager Control mit Visual Studio-ähnlichem Layout nach Fluent Design System.
/// Unterstützt 5 Dock-Bereiche: Left, Right, Top, Bottom, Document (Center).
/// </summary>
public sealed partial class DockingManager : UserControl
{
    #region Dependency Properties

    public static readonly DependencyProperty LeftPanelContentProperty =
        DependencyProperty.Register(
            nameof(LeftPanelContent),
            typeof(UIElement),
            typeof(DockingManager),
            new PropertyMetadata(null));

    public static readonly DependencyProperty RightPanelContentProperty =
        DependencyProperty.Register(
            nameof(RightPanelContent),
            typeof(UIElement),
            typeof(DockingManager),
            new PropertyMetadata(null));

    public static readonly DependencyProperty TopPanelContentProperty =
        DependencyProperty.Register(
            nameof(TopPanelContent),
            typeof(UIElement),
            typeof(DockingManager),
            new PropertyMetadata(null));

    public static readonly DependencyProperty BottomPanelContentProperty =
        DependencyProperty.Register(
            nameof(BottomPanelContent),
            typeof(UIElement),
            typeof(DockingManager),
            new PropertyMetadata(null));

    public static readonly DependencyProperty DocumentAreaContentProperty =
        DependencyProperty.Register(
            nameof(DocumentAreaContent),
            typeof(UIElement),
            typeof(DockingManager),
            new PropertyMetadata(null));

    public static readonly DependencyProperty StatusBarContentProperty =
        DependencyProperty.Register(
            nameof(StatusBarContent),
            typeof(UIElement),
            typeof(DockingManager),
            new PropertyMetadata(null));

    public static readonly DependencyProperty LeftPanelWidthProperty =
        DependencyProperty.Register(
            nameof(LeftPanelWidth),
            typeof(double),
            typeof(DockingManager),
            new PropertyMetadata(240.0));

    public static readonly DependencyProperty RightPanelWidthProperty =
        DependencyProperty.Register(
            nameof(RightPanelWidth),
            typeof(double),
            typeof(DockingManager),
            new PropertyMetadata(240.0));

    public static readonly DependencyProperty TopPanelHeightProperty =
        DependencyProperty.Register(
            nameof(TopPanelHeight),
            typeof(double),
            typeof(DockingManager),
            new PropertyMetadata(100.0));

    public static readonly DependencyProperty BottomPanelHeightProperty =
        DependencyProperty.Register(
            nameof(BottomPanelHeight),
            typeof(double),
            typeof(DockingManager),
            new PropertyMetadata(100.0));

    public static readonly DependencyProperty IsLeftPanelVisibleProperty =
        DependencyProperty.Register(
            nameof(IsLeftPanelVisible),
            typeof(bool),
            typeof(DockingManager),
            new PropertyMetadata(true));

    public static readonly DependencyProperty IsRightPanelVisibleProperty =
        DependencyProperty.Register(
            nameof(IsRightPanelVisible),
            typeof(bool),
            typeof(DockingManager),
            new PropertyMetadata(true));

    public static readonly DependencyProperty IsTopPanelVisibleProperty =
        DependencyProperty.Register(
            nameof(IsTopPanelVisible),
            typeof(bool),
            typeof(DockingManager),
            new PropertyMetadata(false));

    public static readonly DependencyProperty IsBottomPanelVisibleProperty =
        DependencyProperty.Register(
            nameof(IsBottomPanelVisible),
            typeof(bool),
            typeof(DockingManager),
            new PropertyMetadata(true));

    #endregion

    public DockingManager()
    {
    }

    #region Properties

    public UIElement? LeftPanelContent
    {
        get => (UIElement?)GetValue(LeftPanelContentProperty);
        set => SetValue(LeftPanelContentProperty, value);
    }

    public UIElement? RightPanelContent
    {
        get => (UIElement?)GetValue(RightPanelContentProperty);
        set => SetValue(RightPanelContentProperty, value);
    }

    public UIElement? TopPanelContent
    {
        get => (UIElement?)GetValue(TopPanelContentProperty);
        set => SetValue(TopPanelContentProperty, value);
    }

    public UIElement? BottomPanelContent
    {
        get => (UIElement?)GetValue(BottomPanelContentProperty);
        set => SetValue(BottomPanelContentProperty, value);
    }

    public UIElement? DocumentAreaContent
    {
        get => (UIElement?)GetValue(DocumentAreaContentProperty);
        set => SetValue(DocumentAreaContentProperty, value);
    }

    public UIElement? StatusBarContent
    {
        get => (UIElement?)GetValue(StatusBarContentProperty);
        set => SetValue(StatusBarContentProperty, value);
    }

    public double LeftPanelWidth
    {
        get => (double)GetValue(LeftPanelWidthProperty);
        set => SetValue(LeftPanelWidthProperty, value);
    }

    public double RightPanelWidth
    {
        get => (double)GetValue(RightPanelWidthProperty);
        set => SetValue(RightPanelWidthProperty, value);
    }

    public double TopPanelHeight
    {
        get => (double)GetValue(TopPanelHeightProperty);
        set => SetValue(TopPanelHeightProperty, value);
    }

    public double BottomPanelHeight
    {
        get => (double)GetValue(BottomPanelHeightProperty);
        set => SetValue(BottomPanelHeightProperty, value);
    }

    public bool IsLeftPanelVisible
    {
        get => (bool)GetValue(IsLeftPanelVisibleProperty);
        set => SetValue(IsLeftPanelVisibleProperty, value);
    }

    public bool IsRightPanelVisible
    {
        get => (bool)GetValue(IsRightPanelVisibleProperty);
        set => SetValue(IsRightPanelVisibleProperty, value);
    }

    public bool IsTopPanelVisible
    {
        get => (bool)GetValue(IsTopPanelVisibleProperty);
        set => SetValue(IsTopPanelVisibleProperty, value);
    }

    public bool IsBottomPanelVisible
    {
        get => (bool)GetValue(IsBottomPanelVisibleProperty);
        set => SetValue(IsBottomPanelVisibleProperty, value);
    }

    #endregion
}
