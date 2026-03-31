namespace Moba.WinUI.Controls.Docking.Workspace;

using System;
using Moba.WinUI.Controls.Docking;
using Microsoft.UI.Xaml.Controls;
using Moba.WinUI.Controls.Docking.Model;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.Json.Serialization;

internal sealed class DockingWorkspaceState
{
    public int Version { get; set; } = 2;

    public string? ActiveDocumentId { get; set; }

    public List<DockingDocumentState> Documents { get; set; } = [];

    public List<DockingToolWindowState> ToolWindows { get; set; } = [];

    public DockingSideState Left { get; set; } = new()
    {
        Position = DockPosition.Left,
        Extent = 240,
        IsVisible = true
    };

    public DockingSideState Right { get; set; } = new()
    {
        Position = DockPosition.Right,
        Extent = 260,
        IsVisible = true
    };

    public DockingSideState Top { get; set; } = new()
    {
        Position = DockPosition.Top,
        Extent = 100,
        IsVisible = false
    };

    public DockingSideState Bottom { get; set; } = new()
    {
        Position = DockPosition.Bottom,
        Extent = 180,
        IsVisible = true
    };
}

internal sealed class DockingSideState
{
    public DockPosition Position { get; set; }

    public bool IsVisible { get; set; }

    public double Extent { get; set; }

    public DockingLayoutNodeState? Root { get; set; }
}

[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(DockingGroupState), "group")]
[JsonDerivedType(typeof(DockingSplitState), "split")]
internal abstract class DockingLayoutNodeState
{
    public DockPosition DockPosition { get; set; }
}

internal sealed class DockingGroupState : DockingLayoutNodeState
{
    public DockGroupLayoutMode LayoutMode { get; set; } = DockGroupLayoutMode.Tabbed;

    public List<string> ToolWindowIds { get; set; } = [];
}

internal sealed class DockingSplitState : DockingLayoutNodeState
{
    public Orientation Orientation { get; set; } = Orientation.Horizontal;

    public double SplitRatio { get; set; } = 0.5;

    public DockingLayoutNodeState FirstNode { get; set; } = null!;

    public DockingLayoutNodeState SecondNode { get; set; } = null!;
}

internal sealed class DockingDocumentState
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    public string Title { get; set; } = "Untitled";

    public string IconGlyph { get; set; } = "\uE71E";

    public string ContentId { get; set; } = string.Empty;

    public string ContentTitle { get; set; } = string.Empty;

    public string ContentBody { get; set; } = string.Empty;

    public bool IsModified { get; set; }

    public bool IsPinned { get; set; }
}

internal sealed class DockingToolWindowState
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    public string Title { get; set; } = "Tool Window";

    public string IconGlyph { get; set; } = "\uE71E";

    public string ContentId { get; set; } = string.Empty;

    public string ContentBody { get; set; } = string.Empty;

    public bool IsExpanded { get; set; } = true;
}

internal sealed class DockingWorkspaceProjection
{
    public ObservableCollection<DocumentTab> Documents { get; } = [];

    public DocumentTab? ActiveDocument { get; init; }

    public DockNode? LeftNode { get; init; }

    public DockNode? RightNode { get; init; }

    public DockNode? TopNode { get; init; }

    public DockNode? BottomNode { get; init; }
}
