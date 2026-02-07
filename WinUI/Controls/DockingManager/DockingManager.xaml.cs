// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.WinUI.Controls;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Input;
using Moba.WinUI.Behavior;
using Windows.ApplicationModel.DataTransfer;
using System;
using System.Collections.Generic;

/// <summary>
/// DockingManager Control mit Visual Studio-style Layout nach Fluent Design System.
/// Features:
///   - DockPanelGroup per side (multiple panels with TabView grouping)
///   - Draggable proportional splitters (in dedicated Grid columns/rows)
///   - Auto-Hide sidebars
///   - Focus Highlighting
///   - Drag &amp; Drop overlay with preview
/// </summary>
public sealed partial class DockingManager : UserControl
{
    private const string DockPanelDataKey = "DockPanel";
    private const string DocumentTabDataKey = "DocumentTab";
    private bool _isOverlayVisible;

    // Splitter dragging state
    private bool _isSplitterDragging;
    private string? _activeSplitterTag;
    private Windows.Foundation.Point _splitterDragStart;
    private double _splitterStartSize;

    /// <summary>
    /// Raised when a DocumentTab is docked to a side via drag &amp; drop.
    /// </summary>
    public event EventHandler<DocumentTabDockedEventArgs>? TabDockedToSide;

    /// <summary>
    /// Raised when a DockPanel is closed.
    /// </summary>
    public event EventHandler<DockPanelClosedEventArgs>? PanelClosed;

    /// <summary>
    /// Raised when a DockPanel is undocked back to the document area.
    /// </summary>
    public event EventHandler<DockPanelUndockedEventArgs>? PanelUndocked;

    // Auto-Hide state
    private readonly Dictionary<DockPosition, List<AutoHideEntry>> _autoHideEntries = new()
    {
        [DockPosition.Left] = [],
        [DockPosition.Right] = [],
        [DockPosition.Top] = [],
        [DockPosition.Bottom] = []
    };
    private AutoHideEntry? _activeAutoHideEntry;

    #region Dependency Properties

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
        InitializeComponent();
        WireGroupEvents();
    }

    #region Properties

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

    #region Panel Group API

    /// <summary>
    /// Adds a panel to the group at the specified dock position.
    /// Multiple panels at the same position are automatically tab-grouped.
    /// </summary>
    public void DockPanel(DockPanel panel, DockPosition position)
    {
        ArgumentNullException.ThrowIfNull(panel);
        RemovePanelFromAllGroups(panel);

        if (position == DockPosition.Center)
        {
            return;
        }

        var group = GetPanelGroup(position);
        group.AddPanel(panel);
        SetPanelVisibility(position, true);
    }

    /// <summary>
    /// Removes a panel from whichever group it belongs to.
    /// Auto-hides the dock side if the group becomes empty.
    /// </summary>
    public void RemovePanelFromAllGroups(DockPanel panel)
    {
        RemoveFromGroupAndAutoHide(LeftPanelGroup, panel, DockPosition.Left);
        RemoveFromGroupAndAutoHide(RightPanelGroup, panel, DockPosition.Right);
        RemoveFromGroupAndAutoHide(TopPanelGroup, panel, DockPosition.Top);
        RemoveFromGroupAndAutoHide(BottomPanelGroup, panel, DockPosition.Bottom);
    }

    /// <summary>
    /// Finds which dock position a panel is currently docked at.
    /// </summary>
    public DockPosition? FindPanelPosition(DockPanel panel)
    {
        if (LeftPanelGroup.ContainsPanel(panel)) return DockPosition.Left;
        if (RightPanelGroup.ContainsPanel(panel)) return DockPosition.Right;
        if (TopPanelGroup.ContainsPanel(panel)) return DockPosition.Top;
        if (BottomPanelGroup.ContainsPanel(panel)) return DockPosition.Bottom;
        return null;
    }

    private DockPanelGroup GetPanelGroup(DockPosition position) => position switch
    {
        DockPosition.Left => LeftPanelGroup,
        DockPosition.Right => RightPanelGroup,
        DockPosition.Top => TopPanelGroup,
        DockPosition.Bottom => BottomPanelGroup,
        _ => BottomPanelGroup
    };

    private void RemoveFromGroupAndAutoHide(DockPanelGroup group, DockPanel panel, DockPosition side)
    {
        if (group.RemovePanel(panel) && group.IsEmpty)
        {
            SetPanelVisibility(side, false);
        }
    }

    private void WireGroupEvents()
    {
        WireSingleGroup(LeftPanelGroup, DockPosition.Left);
        WireSingleGroup(RightPanelGroup, DockPosition.Right);
        WireSingleGroup(TopPanelGroup, DockPosition.Top);
        WireSingleGroup(BottomPanelGroup, DockPosition.Bottom);
    }

    private void WireSingleGroup(DockPanelGroup group, DockPosition side)
    {
        group.PanelClosed += (_, panel) =>
        {
            if (group.IsEmpty)
            {
                SetPanelVisibility(side, false);
            }

            PanelClosed?.Invoke(this, new DockPanelClosedEventArgs(panel));
        };

        group.PanelPinRequested += (_, panel) =>
        {
            PinToAutoHide(panel, side);
        };

        group.PanelUndockRequested += (_, panel) =>
        {
            RemovePanelFromAllGroups(panel);
            PanelUndocked?.Invoke(this, new DockPanelUndockedEventArgs(panel));
        };
    }

    #endregion

    #region Drag & Drop

    private void OnDockAreaDragOver(object sender, DragEventArgs e)
    {
        e.AcceptedOperation = DataPackageOperation.Move;
        if (sender is FrameworkElement element)
        {
            element.Opacity = 0.85;
            ShowOverlay();
            UpdatePreviewBox(element);
        }
    }

    private void OnDockAreaDragLeave(object sender, DragEventArgs e)
    {
        if (sender is FrameworkElement element)
        {
            element.Opacity = 1.0;
        }

        var position = e.GetPosition(RootGrid);
        if (!IsPointInsideRoot(position))
        {
            HideOverlay();
        }
    }

    private void OnDockAreaDrop(object sender, DragEventArgs e)
    {
        if (sender is FrameworkElement element)
        {
            element.Opacity = 1.0;
        }

        if (sender is not DependencyObject dependencyObject)
        {
            HideOverlay();
            return;
        }

        var position = DockingDropBehavior.GetDockPosition(dependencyObject);

        if (TryGetDraggedPanel(e, out var panel))
        {
            DockPanel(panel, position);
        }
        else if (TryGetDraggedDocumentTab(e, out var tab))
        {
            DockDocumentTab(tab, position);
        }

        HideOverlay();
    }

    private void ShowOverlay()
    {
        if (_isOverlayVisible)
        {
            return;
        }

        _isOverlayVisible = true;
        OverlayLayer.Visibility = Visibility.Visible;
    }

    private void HideOverlay()
    {
        if (!_isOverlayVisible)
        {
            return;
        }

        _isOverlayVisible = false;
        OverlayLayer.Visibility = Visibility.Collapsed;
        PreviewBox.Visibility = Visibility.Collapsed;
    }

    private void UpdatePreviewBox(FrameworkElement target)
    {
        if (!_isOverlayVisible)
        {
            return;
        }

        var transform = target.TransformToVisual(RootGrid);
        var origin = transform.TransformPoint(new Windows.Foundation.Point(0, 0));

        PreviewBox.Width = Math.Max(0, target.ActualWidth);
        PreviewBox.Height = Math.Max(0, target.ActualHeight);
        Canvas.SetLeft(PreviewBox, origin.X);
        Canvas.SetTop(PreviewBox, origin.Y);
        PreviewBox.Visibility = Visibility.Visible;
    }

    private bool IsPointInsideRoot(Windows.Foundation.Point position)
    {
        return position.X >= 0
            && position.Y >= 0
            && position.X <= RootGrid.ActualWidth
            && position.Y <= RootGrid.ActualHeight;
    }

    private static bool TryGetDraggedPanel(DragEventArgs e, out DockPanel? panel)
    {
        panel = null;
        if (!e.DataView.Properties.TryGetValue(DockPanelDataKey, out var data))
        {
            return false;
        }

        panel = data as DockPanel;
        return panel is not null;
    }

    private static bool TryGetDraggedDocumentTab(DragEventArgs e, out DocumentTab? tab)
    {
        tab = null;
        if (!e.DataView.Properties.TryGetValue(DocumentTabDataKey, out var data))
        {
            return false;
        }

        tab = data as DocumentTab;
        return tab is not null;
    }

    /// <summary>
    /// Docks a DocumentTab as a DockPanel at the specified position.
    /// </summary>
    private void DockDocumentTab(DocumentTab tab, DockPosition position)
    {
        ArgumentNullException.ThrowIfNull(tab);

        TabDockedToSide?.Invoke(this, new DocumentTabDockedEventArgs(tab, position));

        if (position == DockPosition.Center)
        {
            return;
        }

        var wrapper = new DockPanel
        {
            PanelTitle = tab.Title,
            PanelIconGlyph = tab.IconGlyph,
            PanelContent = tab.Content
        };

        DockPanel(wrapper, position);
    }

    #endregion

    #region Splitter Dragging

    private void OnSplitterPointerEntered(object sender, PointerRoutedEventArgs e)
    {
        if (sender is not FrameworkElement splitter)
        {
            return;
        }

        var tag = splitter.Tag as string;
        var cursorShape = tag is "Left" or "Right"
            ? InputSystemCursorShape.SizeWestEast
            : InputSystemCursorShape.SizeNorthSouth;

        ProtectedCursor = InputSystemCursor.Create(cursorShape);
    }

    private void OnSplitterPointerExited(object sender, PointerRoutedEventArgs e)
    {
        if (!_isSplitterDragging)
        {
            ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.Arrow);
        }
    }

    private void OnSplitterPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (sender is not FrameworkElement splitter)
        {
            return;
        }

        _isSplitterDragging = true;
        _activeSplitterTag = splitter.Tag as string;
        _splitterDragStart = e.GetCurrentPoint(RootGrid).Position;
        _splitterStartSize = _activeSplitterTag switch
        {
            "Left" => LeftPanelWidth,
            "Right" => RightPanelWidth,
            "Top" => TopPanelHeight,
            "Bottom" => BottomPanelHeight,
            _ => 0
        };

        splitter.CapturePointer(e.Pointer);
        e.Handled = true;
    }

    private void OnSplitterPointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (!_isSplitterDragging || _activeSplitterTag is null)
        {
            return;
        }

        var current = e.GetCurrentPoint(RootGrid).Position;

        const double minSize = 80;
        const double maxSize = 600;

        switch (_activeSplitterTag)
        {
            case "Left":
                LeftPanelWidth = Math.Clamp(
                    _splitterStartSize + (current.X - _splitterDragStart.X), minSize, maxSize);
                break;
            case "Right":
                RightPanelWidth = Math.Clamp(
                    _splitterStartSize - (current.X - _splitterDragStart.X), minSize, maxSize);
                break;
            case "Top":
                TopPanelHeight = Math.Clamp(
                    _splitterStartSize + (current.Y - _splitterDragStart.Y), minSize, maxSize);
                break;
            case "Bottom":
                BottomPanelHeight = Math.Clamp(
                    _splitterStartSize - (current.Y - _splitterDragStart.Y), minSize, maxSize);
                break;
        }

        e.Handled = true;
    }

    private void OnSplitterPointerReleased(object sender, PointerRoutedEventArgs e)
    {
        if (sender is FrameworkElement splitter)
        {
            splitter.ReleasePointerCapture(e.Pointer);
        }

        _isSplitterDragging = false;
        _activeSplitterTag = null;
        ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.Arrow);
        e.Handled = true;
    }

    #endregion

    #region Auto-Hide

    /// <summary>
    /// Moves a panel to the auto-hide sidebar at the specified side.
    /// </summary>
    public void PinToAutoHide(DockPanel panel, DockPosition side)
    {
        ArgumentNullException.ThrowIfNull(panel);

        RemovePanelFromAllGroups(panel);

        var entry = new AutoHideEntry(panel, side);
        _autoHideEntries[side].Add(entry);

        var sidebar = GetAutoHideSidebar(side);
        sidebar.Visibility = Visibility.Visible;

        var tabButton = CreateAutoHideTabButton(entry);
        sidebar.Children.Add(tabButton);
    }

    /// <summary>
    /// Restores a panel from auto-hide back to its dock position.
    /// </summary>
    public void UnpinFromAutoHide(AutoHideEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        _autoHideEntries[entry.Side].Remove(entry);

        var sidebar = GetAutoHideSidebar(entry.Side);
        sidebar.Children.Clear();
        foreach (var remaining in _autoHideEntries[entry.Side])
        {
            sidebar.Children.Add(CreateAutoHideTabButton(remaining));
        }

        if (_autoHideEntries[entry.Side].Count == 0)
        {
            sidebar.Visibility = Visibility.Collapsed;
        }

        if (_activeAutoHideEntry == entry)
        {
            _activeAutoHideEntry = null;
        }

        DockPanel(entry.Panel, entry.Side);
    }

    private StackPanel GetAutoHideSidebar(DockPosition side) => side switch
    {
        DockPosition.Left => LeftAutoHideBar,
        DockPosition.Right => RightAutoHideBar,
        DockPosition.Top => TopAutoHideBar,
        DockPosition.Bottom => BottomAutoHideBar,
        _ => BottomAutoHideBar
    };

    private Button CreateAutoHideTabButton(AutoHideEntry entry)
    {
        var button = new Button
        {
            Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Transparent),
            BorderBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Transparent),
            Padding = new Thickness(8, 4, 8, 4),
            Margin = new Thickness(2),
            CornerRadius = new CornerRadius(4),
            Content = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 4,
                Children =
                {
                    new FontIcon
                    {
                        FontFamily = (Microsoft.UI.Xaml.Media.FontFamily)
                            Application.Current.Resources["SymbolThemeFontFamily"],
                        FontSize = 12,
                        Glyph = entry.Panel.PanelIconGlyph
                    },
                    new TextBlock
                    {
                        Text = entry.Panel.PanelTitle,
                        FontSize = 11,
                        VerticalAlignment = VerticalAlignment.Center
                    }
                }
            }
        };

        button.Click += (_, _) => ToggleAutoHidePanel(entry);
        return button;
    }

    private void ToggleAutoHidePanel(AutoHideEntry entry)
    {
        if (_activeAutoHideEntry == entry)
        {
            RemovePanelFromAllGroups(entry.Panel);
            SetPanelVisibility(entry.Side, false);
            _activeAutoHideEntry = null;
        }
        else
        {
            if (_activeAutoHideEntry is not null)
            {
                RemovePanelFromAllGroups(_activeAutoHideEntry.Panel);
                SetPanelVisibility(_activeAutoHideEntry.Side, false);
            }

            DockPanel(entry.Panel, entry.Side);
            _activeAutoHideEntry = entry;
        }
    }

    private void SetPanelVisibility(DockPosition side, bool visible)
    {
        switch (side)
        {
            case DockPosition.Left: IsLeftPanelVisible = visible; break;
            case DockPosition.Right: IsRightPanelVisible = visible; break;
            case DockPosition.Top: IsTopPanelVisible = visible; break;
            case DockPosition.Bottom: IsBottomPanelVisible = visible; break;
        }
    }

    #endregion

    #region Focus Highlighting

    /// <summary>
    /// Shows a highlight border around the specified dock area.
    /// </summary>
    public void HighlightDockArea(DockPosition position)
    {
        FocusHighlightBorder.Visibility = Visibility.Visible;

        var targetArea = position switch
        {
            DockPosition.Left => (FrameworkElement)LeftDockArea,
            DockPosition.Right => RightDockArea,
            DockPosition.Top => TopDockArea,
            DockPosition.Bottom => BottomDockArea,
            _ => DocumentArea
        };

        var transform = targetArea.TransformToVisual(RootGrid);
        var origin = transform.TransformPoint(new Windows.Foundation.Point(0, 0));

        FocusHighlightBorder.Width = targetArea.ActualWidth;
        FocusHighlightBorder.Height = targetArea.ActualHeight;
        FocusHighlightBorder.Margin = new Thickness(origin.X, origin.Y, 0, 0);
        FocusHighlightBorder.HorizontalAlignment = HorizontalAlignment.Left;
        FocusHighlightBorder.VerticalAlignment = VerticalAlignment.Top;
        FocusHighlightBorder.BorderThickness = new Thickness(2);
    }

    /// <summary>
    /// Clears focus highlighting.
    /// </summary>
    public void ClearFocusHighlight()
    {
        FocusHighlightBorder.Visibility = Visibility.Collapsed;
        FocusHighlightBorder.BorderThickness = new Thickness(0);
    }

    #endregion
}

/// <summary>
/// Represents an auto-hide panel entry in a sidebar.
/// </summary>
public sealed record AutoHideEntry(DockPanel Panel, DockPosition Side);

/// <summary>
/// EventArgs for a DocumentTab docked to a side.
/// </summary>
public sealed class DocumentTabDockedEventArgs(DocumentTab document, DockPosition position) : EventArgs
{
    public DocumentTab Document { get; } = document;
    public DockPosition Position { get; } = position;
}

/// <summary>
/// EventArgs for a closed DockPanel.
/// </summary>
public sealed class DockPanelClosedEventArgs(DockPanel panel) : EventArgs
{
    public DockPanel Panel { get; } = panel;
}

/// <summary>
/// EventArgs for a DockPanel undocked back to the document area.
/// </summary>
public sealed class DockPanelUndockedEventArgs(DockPanel panel) : EventArgs
{
    public DockPanel Panel { get; } = panel;
}
