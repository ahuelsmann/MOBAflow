// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.WinUI.Controls.Docking.Model;

using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Controls;

/// <summary>
/// A node that splits its area either horizontally or vertically into two child nodes.
/// </summary>
internal sealed partial class DockSplitNode : DockNode
{
    private DockNode? _trackedFirstNode;
    private DockNode? _trackedSecondNode;

    [ObservableProperty]
    private DockNode _firstNode = null!;

    [ObservableProperty]
    private DockNode _secondNode = null!;

    [ObservableProperty]
    private Orientation _orientation = Orientation.Horizontal;

    /// <summary>
    /// The size ratio between the first and second node.
    /// E.g., 0.5 means both nodes take 50% of the space.
    /// </summary>
    [ObservableProperty]
    private double _splitRatio = 0.5;

    public DockSplitNode(DockNode first, DockNode second, Orientation orientation)
    {
        FirstNode = first;
        SecondNode = second;
        Orientation = orientation;
    }

    protected override void OnPropertyChanged(System.ComponentModel.PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        if (e.PropertyName == nameof(FirstNode))
        {
            SyncChildMetadata(ref _trackedFirstNode, FirstNode);
        }

        if (e.PropertyName == nameof(SecondNode))
        {
            SyncChildMetadata(ref _trackedSecondNode, SecondNode);
        }

        if (e.PropertyName == nameof(DockPosition))
        {
            SyncChildMetadata(ref _trackedFirstNode, FirstNode);
            SyncChildMetadata(ref _trackedSecondNode, SecondNode);
        }
    }

    private void SyncChildMetadata(ref DockNode? trackedNode, DockNode? currentNode)
    {
        if (trackedNode is not null
            && !ReferenceEquals(trackedNode, currentNode)
            && ReferenceEquals(trackedNode.Parent, this))
        {
            trackedNode.Parent = null;
        }

        trackedNode = currentNode;
        if (currentNode is null)
        {
            return;
        }

        currentNode.Parent = this;
        currentNode.DockPosition = DockPosition;
    }
}
