// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.WinUI.Controls.Docking;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using Model;

using System.ComponentModel;

internal sealed class DockNodePresenter : UserControl
{
    private DockNode? _observedNode;
    private DockPanelGroup? _observedGroup;
    private DockSplitControl? _observedSplitControl;
    private readonly ContentControl _nodeContainer;

    public event EventHandler<DockPanelExpansionChangedEventArgs>? PanelExpansionChanged;

    public event EventHandler<DockPanelAutoHideIntentEventArgs>? PanelAutoHideRequested;

    public static readonly DependencyProperty NodeProperty =
        DependencyProperty.Register(
            nameof(Node),
            typeof(DockNode),
            typeof(DockNodePresenter),
            new PropertyMetadata(null, OnNodeChanged));

    public DockNodePresenter()
    {
        _nodeContainer = new ContentControl
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
            VerticalContentAlignment = VerticalAlignment.Stretch
        };
        Content = _nodeContainer;
        Unloaded += OnUnloaded;
    }

    public DockNode? Node
    {
        get => (DockNode?)GetValue(NodeProperty);
        set => SetValue(NodeProperty, value);
    }

    private static void OnNodeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is DockNodePresenter presenter)
        {
            presenter.ObserveNode(e.OldValue as DockNode, e.NewValue as DockNode);
            presenter.UpdateContent(e.NewValue as DockNode);
        }
    }

    private void ObserveNode(DockNode? previousNode, DockNode? nextNode)
    {
        if (previousNode is not null)
        {
            previousNode.PropertyChanged -= OnNodePropertyChanged;
        }

        _observedNode = nextNode;
        if (nextNode is not null)
        {
            nextNode.PropertyChanged += OnNodePropertyChanged;
        }
    }

    private void OnNodePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_observedNode is null)
        {
            return;
        }

        if (_observedNode is DockPanelGroupNode
            && e.PropertyName is not nameof(DockNode.DockPosition) and not nameof(DockPanelGroupNode.LayoutMode))
        {
            return;
        }

        UpdateContent(_observedNode);
    }

    private void UpdateContent(DockNode? node)
    {
        if (node is DockPanelGroupNode groupNode)
        {
            var group = _nodeContainer.Content as DockPanelGroup ?? new DockPanelGroup();
            group.DockPosition = groupNode.DockPosition;
            group.LayoutMode = groupNode.LayoutMode;
            group.ItemsSource = groupNode.Panels;
            ObserveContent(group);
            if (!ReferenceEquals(_nodeContainer.Content, group))
            {
                _nodeContainer.Content = group;
            }
        }
        else if (node is DockSplitNode splitNode)
        {
            var splitControl = _nodeContainer.Content as DockSplitControl ?? new DockSplitControl();
            splitControl.SplitNode = splitNode;
            ObserveContent(splitControl);
            if (!ReferenceEquals(_nodeContainer.Content, splitControl))
            {
                _nodeContainer.Content = splitControl;
            }
        }
        else
        {
            ObserveContent(null);
            _nodeContainer.Content = null;
        }
    }

    private void ObserveContent(object? content)
    {
        var nextGroup = content as DockPanelGroup;
        var nextSplitControl = content as DockSplitControl;

        if (ReferenceEquals(_observedGroup, nextGroup)
            && ReferenceEquals(_observedSplitControl, nextSplitControl))
        {
            return;
        }

        if (_observedGroup is not null)
        {
            _observedGroup.PanelExpansionChanged -= OnGroupPanelExpansionChanged;
            _observedGroup.PanelAutoHideRequested -= OnGroupPanelAutoHideRequested;
        }

        if (_observedSplitControl is not null)
        {
            _observedSplitControl.PanelExpansionChanged -= OnSplitControlPanelExpansionChanged;
            _observedSplitControl.PanelAutoHideRequested -= OnSplitControlPanelAutoHideRequested;
        }

        _observedGroup = nextGroup;
        _observedSplitControl = nextSplitControl;

        if (_observedGroup is not null)
        {
            _observedGroup.PanelExpansionChanged += OnGroupPanelExpansionChanged;
            _observedGroup.PanelAutoHideRequested += OnGroupPanelAutoHideRequested;
        }

        if (_observedSplitControl is not null)
        {
            _observedSplitControl.PanelExpansionChanged += OnSplitControlPanelExpansionChanged;
            _observedSplitControl.PanelAutoHideRequested += OnSplitControlPanelAutoHideRequested;
        }
    }

    private void OnGroupPanelExpansionChanged(object? sender, DockPanelExpansionChangedEventArgs e)
    {
        PanelExpansionChanged?.Invoke(this, e);
    }

    private void OnGroupPanelAutoHideRequested(object? sender, DockPanelAutoHideIntentEventArgs e)
    {
        PanelAutoHideRequested?.Invoke(this, e);
    }

    private void OnSplitControlPanelExpansionChanged(object? sender, DockPanelExpansionChangedEventArgs e)
    {
        PanelExpansionChanged?.Invoke(this, e);
    }

    private void OnSplitControlPanelAutoHideRequested(object? sender, DockPanelAutoHideIntentEventArgs e)
    {
        PanelAutoHideRequested?.Invoke(this, e);
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        ObserveNode(_observedNode, null);
        ObserveContent(null);
    }
}
