// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.WinUI.Controls;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;

/// <summary>
/// Groups multiple DockPanels into a TabView for a single dock position.
/// Single panel: renders DockPanel directly (no tab overhead).
/// Multiple panels: renders TabView with tabs per panel.
/// </summary>
public sealed class DockPanelGroup : UserControl
{
    private readonly List<DockPanel> _panels = [];
    private readonly Grid _singlePanelView;
    private readonly ContentPresenter _singlePanelPresenter;
    private readonly TabView _panelTabView;
    private readonly TextBlock _emptyState;

    /// <summary>
    /// Raised when a panel is closed via tab close button.
    /// </summary>
    public event EventHandler<DockPanel>? PanelClosed;

    /// <summary>
    /// Raised when a panel requests to be pinned to auto-hide.
    /// </summary>
    public event EventHandler<DockPanel>? PanelPinRequested;

    /// <summary>
    /// Raised when a panel requests undocking back to document area.
    /// </summary>
    public event EventHandler<DockPanel>? PanelUndockRequested;

    public DockPanelGroup()
    {
        _singlePanelPresenter = new ContentPresenter();
        _singlePanelView = new Grid { Visibility = Visibility.Collapsed };
        _singlePanelView.Children.Add(_singlePanelPresenter);

        _panelTabView = new TabView
        {
            Visibility = Visibility.Collapsed,
            IsAddTabButtonVisible = false,
            CanReorderTabs = true,
            CanDragTabs = true,
            TabWidthMode = TabViewWidthMode.SizeToContent,
            CloseButtonOverlayMode = TabViewCloseButtonOverlayMode.OnPointerOver
        };
        _panelTabView.TabCloseRequested += OnTabCloseRequested;
        _panelTabView.SelectionChanged += OnTabSelectionChanged;

        _emptyState = new TextBlock
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            FontSize = 12,
            Text = "Drop panel here",
            Visibility = Visibility.Collapsed
        };

        var rootGrid = new Grid();
        rootGrid.Children.Add(_singlePanelView);
        rootGrid.Children.Add(_panelTabView);
        rootGrid.Children.Add(_emptyState);

        Content = rootGrid;
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

    private void UpdateView()
    {
        _emptyState.Visibility = Visibility.Collapsed;
        _singlePanelView.Visibility = Visibility.Collapsed;
        _panelTabView.Visibility = Visibility.Collapsed;

        switch (_panels.Count)
        {
            case 0:
                _emptyState.Visibility = Visibility.Visible;
                _singlePanelPresenter.Content = null;
                _panelTabView.TabItems.Clear();
                break;

            case 1:
                _singlePanelView.Visibility = Visibility.Visible;
                _singlePanelPresenter.Content = _panels[0];
                _panelTabView.TabItems.Clear();
                break;

            default:
                _panelTabView.Visibility = Visibility.Visible;
                _singlePanelPresenter.Content = null;
                RebuildTabs();
                break;
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

    private void OnTabCloseRequested(TabView sender, TabViewTabCloseRequestedEventArgs args)
    {
        if (args.Tab?.Tag is DockPanel panel)
        {
            RemovePanel(panel);
            PanelClosed?.Invoke(this, panel);
        }
    }

    private void OnTabSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
    }

    private void WirePanelEvents(DockPanel panel)
    {
        panel.CloseRequested += OnPanelCloseRequested;
        panel.PinToggleRequested += OnPanelPinToggleRequested;
        panel.UndockRequested += OnPanelUndockRequested;
    }

    private void UnwirePanelEvents(DockPanel panel)
    {
        panel.CloseRequested -= OnPanelCloseRequested;
        panel.PinToggleRequested -= OnPanelPinToggleRequested;
        panel.UndockRequested -= OnPanelUndockRequested;
    }

    private void OnPanelCloseRequested(object? sender, EventArgs e)
    {
        if (sender is DockPanel panel)
        {
            RemovePanel(panel);
            PanelClosed?.Invoke(this, panel);
        }
    }

    private void OnPanelPinToggleRequested(object? sender, EventArgs e)
    {
        if (sender is DockPanel panel)
        {
            PanelPinRequested?.Invoke(this, panel);
        }
    }

    private void OnPanelUndockRequested(object? sender, EventArgs e)
    {
        if (sender is DockPanel panel)
        {
            PanelUndockRequested?.Invoke(this, panel);
        }
    }
}
