// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.WinUI.Controls.Docking;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Model;
using System.ComponentModel;

internal sealed class DockNodePresenter : UserControl
{
    private DockNode? _observedNode;
    private readonly ContentControl _nodeContainer;

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
            if (!ReferenceEquals(_nodeContainer.Content, group))
            {
                _nodeContainer.Content = group;
            }
        }
        else if (node is DockSplitNode splitNode)
        {
            var splitControl = _nodeContainer.Content as DockSplitControl ?? new DockSplitControl();
            splitControl.SplitNode = splitNode;
            if (!ReferenceEquals(_nodeContainer.Content, splitControl))
            {
                _nodeContainer.Content = splitControl;
            }
        }
        else
        {
            _nodeContainer.Content = null;
        }
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        ObserveNode(_observedNode, null);
    }
}
