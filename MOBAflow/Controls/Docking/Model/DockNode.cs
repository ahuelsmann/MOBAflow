// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.WinUI.Controls.Docking.Model;

using CommunityToolkit.Mvvm.ComponentModel;

/// <summary>
/// Base class for all nodes in the docking hierarchy.
/// </summary>
public abstract partial class DockNode : ObservableObject
{
    /// <summary>
    /// The parent node in the hierarchy, or null if this is a root node.
    /// </summary>
    public DockNode? Parent { get; set; }

    /// <summary>
    /// The docking position (Left, Right, Top, Bottom) this node belongs to.
    /// </summary>
    [ObservableProperty]
    private DockPosition _dockPosition = DockPosition.Left;
}
