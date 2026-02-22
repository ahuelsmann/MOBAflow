// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.WinUI.Controls;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;

/// <summary>
/// Legt fest, wie mehrere Panels in einer Dock-Gruppe dargestellt werden.
/// </summary>
public enum DockGroupLayoutMode
{
    /// <summary>Panels gleichmäßig aufteilen (Split).</summary>
    Split,

    /// <summary>Panels als Tabs gestapelt, per Tab auswählbar.</summary>
    Tabbed
}

/// <summary>
/// Gruppiert mehrere DockPanels für eine Dock-Position.
/// Ein Panel: direkte Anzeige. Mehrere: je nach LayoutMode gleichmäßig geteilt (Split) oder als Tabs (Tabbed).
/// </summary>
public sealed class DockPanelGroup : UserControl
{
    private readonly List<DockPanel> _panels = [];
    private readonly Grid _singlePanelView;
    private readonly ContentPresenter _singlePanelPresenter;
    private readonly TabView _panelTabView;
    private readonly Grid _splitView;
    private readonly TextBlock _emptyState;

    /// <summary>
    /// Raised when a panel requests undocking back to document area.
    /// </summary>
    public event EventHandler<DockPanel>? PanelUndockRequested;

    /// <summary>
    /// Raised when any panel expansion state changes.
    /// </summary>
    public event EventHandler? PanelExpansionChanged;

    public static readonly DependencyProperty ItemsSourceProperty =
        DependencyProperty.Register(
            nameof(ItemsSource),
            typeof(ObservableCollection<DockPanel>),
            typeof(DockPanelGroup),
            new PropertyMetadata(null, OnItemsSourceChanged));

    public static readonly DependencyProperty DockPositionProperty =
        DependencyProperty.Register(
            nameof(DockPosition),
            typeof(DockPosition),
            typeof(DockPanelGroup),
            new PropertyMetadata(DockPosition.Left, OnDockPositionChanged));

    public static readonly DependencyProperty LayoutModeProperty =
        DependencyProperty.Register(
            nameof(LayoutMode),
            typeof(DockGroupLayoutMode),
            typeof(DockPanelGroup),
            new PropertyMetadata(DockGroupLayoutMode.Tabbed, OnLayoutModeChanged));

    public DockPanelGroup()
    {
        _singlePanelPresenter = new ContentPresenter
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };

        _singlePanelView = new Grid
        {
            Visibility = Visibility.Collapsed,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };
        _singlePanelView.Children.Add(_singlePanelPresenter);

        _panelTabView = new TabView
        {
            Visibility = Visibility.Collapsed,
            IsAddTabButtonVisible = false,
            CanReorderTabs = true,
            CanDragTabs = true,
            TabWidthMode = TabViewWidthMode.Equal,
            CloseButtonOverlayMode = TabViewCloseButtonOverlayMode.OnPointerOver,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };

        _splitView = new Grid
        {
            Visibility = Visibility.Collapsed,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };

        _emptyState = new TextBlock
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            FontSize = 12,
            Text = "Drop panel here",
            Visibility = Visibility.Collapsed
        };

        var rootGrid = new Grid
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };
        rootGrid.Children.Add(_singlePanelView);
        rootGrid.Children.Add(_panelTabView);
        rootGrid.Children.Add(_splitView);
        rootGrid.Children.Add(_emptyState);

        Content = rootGrid;
    }

    /// <summary>
    /// Panels provided via binding for this group.
    /// </summary>
    public ObservableCollection<DockPanel>? ItemsSource
    {
        get => (ObservableCollection<DockPanel>?)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    /// <summary>
    /// The docking position of this panel group.
    /// </summary>
    public DockPosition DockPosition
    {
        get => (DockPosition)GetValue(DockPositionProperty);
        set => SetValue(DockPositionProperty, value);
    }

    /// <summary>
    /// Legt fest, ob mehrere Panels gleichmäßig geteilt (Split) oder als Tabs gestapelt (Tabbed) dargestellt werden.
    /// </summary>
    public DockGroupLayoutMode LayoutMode
    {
        get => (DockGroupLayoutMode)GetValue(LayoutModeProperty);
        set => SetValue(LayoutModeProperty, value);
    }

    /// <summary>
    /// All panels currently in this group.
    /// </summary>
    public IReadOnlyList<DockPanel> Panels => _panels;

    /// <summary>
    /// Whether this group has no panels.
    /// </summary>
    public bool IsEmpty => _panels.Count == 0;

    /// <summary>
    /// The currently active/visible panel.
    /// </summary>
    public DockPanel? ActivePanel => _panels.Count switch
    {
        0 => null,
        1 => _panels[0],
        _ => _panelTabView.SelectedItem as DockPanel
    };

    /// <summary>
    /// Adds a panel to this group. If already present, activates it.
    /// </summary>
    public void AddPanel(DockPanel panel)
    {
        ArgumentNullException.ThrowIfNull(panel);

        if (_panels.Contains(panel))
        {
            ActivatePanel(panel);
            return;
        }

        panel.DockPosition = DockPosition;
        WirePanelEvents(panel);
        _panels.Add(panel);
        UpdateView();
        ActivatePanel(panel);
    }

    /// <summary>
    /// Removes a panel from this group.
    /// </summary>
    public bool RemovePanel(DockPanel panel)
    {
        ArgumentNullException.ThrowIfNull(panel);

        if (!_panels.Remove(panel))
        {
            return false;
        }

        UnwirePanelEvents(panel);
        UpdateView();
        return true;
    }

    /// <summary>
    /// Checks if this group contains the specified panel.
    /// </summary>
    public bool ContainsPanel(DockPanel panel) => _panels.Contains(panel);

    /// <summary>
    /// Activates a specific panel (brings its tab to front).
    /// </summary>
    public void ActivatePanel(DockPanel panel)
    {
        if (_panels.Count > 1)
        {
            _panelTabView.SelectedItem = panel;
        }
    }

    private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is DockPanelGroup group)
        {
            group.UpdateItemsSource(
                e.OldValue as ObservableCollection<DockPanel>,
                e.NewValue as ObservableCollection<DockPanel>);
        }
    }

    private void UpdateItemsSource(ObservableCollection<DockPanel>? oldValue, ObservableCollection<DockPanel>? newValue)
    {
        if (oldValue is not null)
        {
            oldValue.CollectionChanged -= OnItemsSourceCollectionChanged;
        }

        if (newValue is not null)
        {
            newValue.CollectionChanged += OnItemsSourceCollectionChanged;
        }

        ResetPanels(newValue);
    }

    private void OnItemsSourceCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                if (e.NewItems is not null)
                {
                    foreach (var item in e.NewItems.OfType<DockPanel>())
                    {
                        AddPanel(item);
                    }
                }
                break;
            case NotifyCollectionChangedAction.Remove:
                if (e.OldItems is not null)
                {
                    foreach (var item in e.OldItems.OfType<DockPanel>())
                    {
                        RemovePanel(item);
                    }
                }
                break;
            case NotifyCollectionChangedAction.Replace:
                if (e.OldItems is not null)
                {
                    foreach (var item in e.OldItems.OfType<DockPanel>())
                    {
                        RemovePanel(item);
                    }
                }
                if (e.NewItems is not null)
                {
                    foreach (var item in e.NewItems.OfType<DockPanel>())
                    {
                        AddPanel(item);
                    }
                }
                break;
            case NotifyCollectionChangedAction.Reset:
                ResetPanels(ItemsSource);
                break;
        }
    }

    private void ResetPanels(ObservableCollection<DockPanel>? panels)
    {
        foreach (var panel in _panels.ToList())
        {
            UnwirePanelEvents(panel);
        }

        _panels.Clear();
        _panelTabView.TabItems.Clear();
        _singlePanelPresenter.Content = null;

        if (panels is not null)
        {
            foreach (var panel in panels)
            {
                panel.DockPosition = DockPosition;
                WirePanelEvents(panel);
                _panels.Add(panel);
            }
        }

        UpdateView();
    }

    private void UpdateView()
    {
        _emptyState.Visibility = Visibility.Collapsed;
        _singlePanelView.Visibility = Visibility.Collapsed;
        _panelTabView.Visibility = Visibility.Collapsed;
        _splitView.Visibility = Visibility.Collapsed;

        switch (_panels.Count)
        {
            case 0:
                _emptyState.Visibility = Visibility.Visible;
                _singlePanelPresenter.Content = null;
                _panelTabView.TabItems.Clear();
                _splitView.Children.Clear();
                _splitView.RowDefinitions.Clear();
                _splitView.ColumnDefinitions.Clear();
                break;

            case 1:
                _singlePanelView.Visibility = Visibility.Visible;
                _singlePanelPresenter.Content = _panels[0];
                _panelTabView.TabItems.Clear();
                _splitView.Children.Clear();
                _splitView.RowDefinitions.Clear();
                _splitView.ColumnDefinitions.Clear();
                break;

            default:
                _singlePanelPresenter.Content = null;

                if (LayoutMode == DockGroupLayoutMode.Split)
                {
                    _splitView.Visibility = Visibility.Visible;
                    RebuildSplitView();
                }
                else
                {
                    _panelTabView.Visibility = Visibility.Visible;
                    RebuildTabs();
                }
                break;
        }

        RaisePanelExpansionChanged();
    }

    private void RebuildSplitView()
    {
        _splitView.Children.Clear();
        _splitView.RowDefinitions.Clear();
        _splitView.ColumnDefinitions.Clear();

        var isVertical = DockPosition == DockPosition.Left || DockPosition == DockPosition.Right;
        var count = _panels.Count;

        if (isVertical)
        {
            for (var i = 0; i < count; i++)
            {
                _splitView.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            }

            for (var i = 0; i < count; i++)
            {
                var panel = _panels[i];
                Grid.SetRow(panel, i);
                _splitView.Children.Add(panel);
            }
        }
        else
        {
            for (var i = 0; i < count; i++)
            {
                _splitView.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            }

            for (var i = 0; i < count; i++)
            {
                var panel = _panels[i];
                Grid.SetColumn(panel, i);
                _splitView.Children.Add(panel);
            }
        }
    }

    private void RebuildTabs()
    {
        _panelTabView.TabItems.Clear();

        foreach (var panel in _panels)
        {
            var tabItem = new TabViewItem
            {
                Header = CreateTabHeader(panel),
                Content = panel,
                IsClosable = true,
                Tag = panel
            };

            _panelTabView.TabItems.Add(tabItem);
        }

        if (_panels.Count > 0)
        {
            _panelTabView.SelectedIndex = _panels.Count - 1;
        }
    }

    private static StackPanel CreateTabHeader(DockPanel panel)
    {
        return new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 6,
            Children =
            {
                new FontIcon
                {
                    FontFamily = (Microsoft.UI.Xaml.Media.FontFamily)
                        Application.Current.Resources["SymbolThemeFontFamily"],
                    FontSize = 12,
                    Glyph = panel.PanelIconGlyph
                },
                new TextBlock
                {
                    Text = panel.PanelTitle,
                    FontSize = 12,
                    VerticalAlignment = VerticalAlignment.Center
                }
            }
        };
    }

    private void WirePanelEvents(DockPanel panel)
    {
        panel.UndockRequested += OnPanelUndockRequested;
        panel.IsExpandedChanged += OnPanelIsExpandedChanged;
    }

    private void UnwirePanelEvents(DockPanel panel)
    {
        panel.UndockRequested -= OnPanelUndockRequested;
        panel.IsExpandedChanged -= OnPanelIsExpandedChanged;
    }

    private void OnPanelUndockRequested(object? sender, EventArgs e)
    {
        if (sender is DockPanel panel)
        {
            PanelUndockRequested?.Invoke(this, panel);
        }
    }

    private void OnPanelIsExpandedChanged(object? sender, EventArgs e)
    {
        RaisePanelExpansionChanged();
    }

    private void RaisePanelExpansionChanged()
    {
        PanelExpansionChanged?.Invoke(this, EventArgs.Empty);
    }

    private static void OnDockPositionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not DockPanelGroup group || e.NewValue is not DockPosition position)
        {
            return;
        }

        foreach (var panel in group._panels)
        {
            panel.DockPosition = position;
        }

        if (group._panels.Count >= 2 && group.LayoutMode == DockGroupLayoutMode.Split)
        {
            group.UpdateView();
        }
    }

    private static void OnLayoutModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is DockPanelGroup group && group._panels.Count >= 2)
        {
            group.UpdateView();
        }
    }
}
