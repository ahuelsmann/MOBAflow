namespace Moba.WinUI.Controls.Docking.Workspace;

using Docking;

using Microsoft.UI.Xaml;

using Model;

using System;
using System.Collections.Generic;
using System.Linq;

public sealed class DockingWorkspaceService
{
    public DockingWorkspaceProjection BuildProjection(DockingWorkspaceState state, Func<string, UIElement> contentFactory)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(contentFactory);

        var documents = state.Documents
            .Select(documentState => CreateDocumentTab(documentState, contentFactory))
            .ToList();
        var toolWindowsById = state.ToolWindows.ToDictionary(item => item.Id, StringComparer.Ordinal);

        var projection = new DockingWorkspaceProjection
        {
            ActiveDocument = documents.FirstOrDefault(document => document.DocumentId == state.ActiveDocumentId)
                ?? documents.FirstOrDefault(),
            LeftNode = CreateNode(state.Left.Root, toolWindowsById, contentFactory),
            RightNode = CreateNode(state.Right.Root, toolWindowsById, contentFactory),
            TopNode = CreateNode(state.Top.Root, toolWindowsById, contentFactory),
            BottomNode = CreateNode(state.Bottom.Root, toolWindowsById, contentFactory)
        };

        foreach (var document in documents)
        {
            projection.Documents.Add(document);
        }

        foreach (var panel in CreateAutoHidePanels(state.Left.Root, toolWindowsById, contentFactory))
        {
            projection.LeftAutoHidePanels.Add(panel);
        }

        foreach (var panel in CreateAutoHidePanels(state.Right.Root, toolWindowsById, contentFactory))
        {
            projection.RightAutoHidePanels.Add(panel);
        }

        foreach (var panel in CreateAutoHidePanels(state.Top.Root, toolWindowsById, contentFactory))
        {
            projection.TopAutoHidePanels.Add(panel);
        }

        foreach (var panel in CreateAutoHidePanels(state.Bottom.Root, toolWindowsById, contentFactory))
        {
            projection.BottomAutoHidePanels.Add(panel);
        }

        return projection;
    }

    public bool DockDocumentToSide(DockingWorkspaceState state, string documentId, DockPosition position)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentException.ThrowIfNullOrWhiteSpace(documentId);

        if (position == DockPosition.Center)
        {
            return false;
        }

        var document = state.Documents.FirstOrDefault(item => item.Id == documentId);
        if (document is null)
        {
            return false;
        }

        state.Documents.Remove(document);

        var toolWindowId = GetDockedDocumentToolWindowId(documentId);
        var toolWindow = state.ToolWindows.FirstOrDefault(item => item.Id == toolWindowId);
        if (toolWindow is null)
        {
            state.ToolWindows.Add(new DockingToolWindowState
            {
                Id = toolWindowId,
                Title = document.Title,
                IconGlyph = document.IconGlyph,
                ContentId = document.ContentId,
                ContentBody = document.ContentBody,
                IsExpanded = true,
                IsAutoHidden = false
            });
        }
        else
        {
            toolWindow.Title = document.Title;
            toolWindow.IconGlyph = document.IconGlyph;
            toolWindow.ContentId = document.ContentId;
            toolWindow.ContentBody = document.ContentBody;
            toolWindow.IsExpanded = true;
            toolWindow.IsAutoHidden = false;
        }

        state.ActiveDocumentId = state.Documents.FirstOrDefault()?.Id;
        return DockToolWindowToSide(state, toolWindowId, position);
    }

    public bool DockToolWindowToSide(DockingWorkspaceState state, string toolWindowId, DockPosition position)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentException.ThrowIfNullOrWhiteSpace(toolWindowId);

        if (position == DockPosition.Center)
        {
            return false;
        }

        if (!state.ToolWindows.Any(item => item.Id == toolWindowId))
        {
            return false;
        }

        var toolWindow = state.ToolWindows.First(item => item.Id == toolWindowId);
        toolWindow.IsAutoHidden = false;
        toolWindow.IsExpanded = true;

        RemoveToolWindowFromAllSides(state, toolWindowId);

        var side = GetSide(state, position);
        if (side.Root is null)
        {
            side.Root = new DockingGroupState
            {
                DockPosition = position,
                ToolWindowIds = [toolWindowId]
            };
        }
        else if (TryGetPrimaryGroup(side.Root, out var group))
        {
            if (!group!.ToolWindowIds.Contains(toolWindowId, StringComparer.Ordinal))
            {
                group.ToolWindowIds.Add(toolWindowId);
            }
        }
        else
        {
            side.Root = new DockingGroupState
            {
                DockPosition = position,
                ToolWindowIds = [toolWindowId]
            };
        }

        NormalizeDockPosition(side.Root, position);
        SyncSideVisibility(state);
        return true;
    }

    public bool UpdateToolWindowExpansion(DockingWorkspaceState state, string toolWindowId, bool isExpanded)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentException.ThrowIfNullOrWhiteSpace(toolWindowId);

        var toolWindow = state.ToolWindows.FirstOrDefault(item => item.Id == toolWindowId);
        if (toolWindow is null)
        {
            return false;
        }

        toolWindow.IsExpanded = isExpanded;
        return true;
    }

    public bool UpdateToolWindowAutoHide(
        DockingWorkspaceState state,
        string toolWindowId,
        DockPosition position,
        bool isAutoHidden)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentException.ThrowIfNullOrWhiteSpace(toolWindowId);

        if (position == DockPosition.Center)
        {
            return false;
        }

        var toolWindow = state.ToolWindows.FirstOrDefault(item => item.Id == toolWindowId);
        if (toolWindow is null)
        {
            return false;
        }

        var side = GetSide(state, position);
        if (!ContainsToolWindow(side.Root, toolWindowId) && !DockToolWindowToSide(state, toolWindowId, position))
        {
            return false;
        }

        toolWindow.IsAutoHidden = isAutoHidden;
        if (!isAutoHidden)
        {
            toolWindow.IsExpanded = true;
        }

        SyncSideVisibility(state);
        return true;
    }

    public static string GetDockedDocumentToolWindowId(string documentId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(documentId);
        return $"document:{documentId}";
    }

    private static DocumentTab CreateDocumentTab(DockingDocumentState state, Func<string, UIElement> contentFactory)
    {
        return new DocumentTab
        {
            DocumentId = state.Id,
            Title = state.Title,
            IconGlyph = state.IconGlyph,
            Content = contentFactory(state.ContentId),
            IsModified = state.IsModified,
            IsPinned = state.IsPinned,
            Tag = state.Id
        };
    }

    private static DockNode? CreateNode(
        DockingLayoutNodeState? state,
        IReadOnlyDictionary<string, DockingToolWindowState> toolWindows,
        Func<string, UIElement> contentFactory)
    {
        switch (state)
        {
            case DockingGroupState groupState:
                var groupNode = new DockPanelGroupNode
                {
                    DockPosition = groupState.DockPosition,
                    LayoutMode = groupState.LayoutMode
                };

                foreach (var toolWindowId in groupState.ToolWindowIds)
                {
                    if (!toolWindows.TryGetValue(toolWindowId, out var toolWindow) || toolWindow.IsAutoHidden)
                    {
                        continue;
                    }

                    groupNode.Panels.Add(CreateDockPanel(toolWindow, groupState.DockPosition, contentFactory));
                }

                return groupNode.Panels.Count > 0 ? groupNode : null;

            case DockingSplitState splitState:
                var firstNode = CreateNode(splitState.FirstNode, toolWindows, contentFactory);
                var secondNode = CreateNode(splitState.SecondNode, toolWindows, contentFactory);

                if (firstNode is null)
                {
                    return secondNode;
                }

                if (secondNode is null)
                {
                    return firstNode;
                }

                return new DockSplitNode(firstNode, secondNode, splitState.Orientation)
                {
                    DockPosition = splitState.DockPosition,
                    SplitRatio = Math.Clamp(splitState.SplitRatio, 0.1, 0.9)
                };

            default:
                return null;
        }
    }

    private static IEnumerable<DockPanel> CreateAutoHidePanels(
        DockingLayoutNodeState? state,
        IReadOnlyDictionary<string, DockingToolWindowState> toolWindows,
        Func<string, UIElement> contentFactory)
    {
        switch (state)
        {
            case DockingGroupState groupState:
                foreach (var toolWindowId in groupState.ToolWindowIds)
                {
                    if (!toolWindows.TryGetValue(toolWindowId, out var toolWindow) || !toolWindow.IsAutoHidden)
                    {
                        continue;
                    }

                    yield return CreateDockPanel(toolWindow, groupState.DockPosition, contentFactory);
                }

                yield break;

            case DockingSplitState splitState:
                foreach (var panel in CreateAutoHidePanels(splitState.FirstNode, toolWindows, contentFactory))
                {
                    yield return panel;
                }

                foreach (var panel in CreateAutoHidePanels(splitState.SecondNode, toolWindows, contentFactory))
                {
                    yield return panel;
                }

                yield break;
        }
    }

    private static DockPanel CreateDockPanel(
        DockingToolWindowState toolWindow,
        DockPosition dockPosition,
        Func<string, UIElement> contentFactory)
    {
        return new DockPanel
        {
            PanelId = toolWindow.Id,
            PanelTitle = toolWindow.Title,
            PanelIconGlyph = toolWindow.IconGlyph,
            PanelContent = contentFactory(toolWindow.ContentId),
            IsExpanded = toolWindow.IsExpanded,
            DockPosition = dockPosition,
            Tag = toolWindow.Id
        };
    }

    private static DockingSideState GetSide(DockingWorkspaceState state, DockPosition position)
    {
        return position switch
        {
            DockPosition.Left => state.Left,
            DockPosition.Right => state.Right,
            DockPosition.Top => state.Top,
            DockPosition.Bottom => state.Bottom,
            _ => throw new ArgumentOutOfRangeException(nameof(position), position, null)
        };
    }

    private static void RemoveToolWindowFromAllSides(DockingWorkspaceState state, string toolWindowId)
    {
        state.Left.Root = RemoveToolWindowFromNode(state.Left.Root, toolWindowId, out _);
        state.Right.Root = RemoveToolWindowFromNode(state.Right.Root, toolWindowId, out _);
        state.Top.Root = RemoveToolWindowFromNode(state.Top.Root, toolWindowId, out _);
        state.Bottom.Root = RemoveToolWindowFromNode(state.Bottom.Root, toolWindowId, out _);
    }

    private static DockingLayoutNodeState? RemoveToolWindowFromNode(
        DockingLayoutNodeState? node,
        string toolWindowId,
        out bool removed)
    {
        removed = false;
        if (node is null)
        {
            return null;
        }

        if (node is DockingGroupState groupState)
        {
            removed = groupState.ToolWindowIds.RemoveAll(item => StringComparer.Ordinal.Equals(item, toolWindowId)) > 0;
            return groupState.ToolWindowIds.Count > 0 ? groupState : null;
        }

        if (node is not DockingSplitState splitState)
        {
            return node;
        }

        var firstNode = RemoveToolWindowFromNode(splitState.FirstNode, toolWindowId, out var removedFromFirst);
        var secondNode = RemoveToolWindowFromNode(splitState.SecondNode, toolWindowId, out var removedFromSecond);
        removed = removedFromFirst || removedFromSecond;

        if (firstNode is null && secondNode is null)
        {
            return null;
        }

        if (firstNode is null)
        {
            return secondNode;
        }

        if (secondNode is null)
        {
            return firstNode;
        }

        splitState.FirstNode = firstNode;
        splitState.SecondNode = secondNode;
        return splitState;
    }

    private static bool TryGetPrimaryGroup(DockingLayoutNodeState node, out DockingGroupState? group)
    {
        switch (node)
        {
            case DockingGroupState directGroup:
                group = directGroup;
                return true;
            case DockingSplitState splitNode when TryGetPrimaryGroup(splitNode.FirstNode, out group):
                return true;
            case DockingSplitState splitNode when TryGetPrimaryGroup(splitNode.SecondNode, out group):
                return true;
            default:
                group = null;
                return false;
        }
    }

    private static bool ContainsToolWindow(DockingLayoutNodeState? node, string toolWindowId)
    {
        return node switch
        {
            DockingGroupState groupState => groupState.ToolWindowIds.Contains(toolWindowId, StringComparer.Ordinal),
            DockingSplitState splitState => ContainsToolWindow(splitState.FirstNode, toolWindowId)
                || ContainsToolWindow(splitState.SecondNode, toolWindowId),
            _ => false
        };
    }

    private static void NormalizeDockPosition(DockingLayoutNodeState? node, DockPosition position)
    {
        if (node is null)
        {
            return;
        }

        node.DockPosition = position;

        if (node is DockingSplitState splitNode)
        {
            NormalizeDockPosition(splitNode.FirstNode, position);
            NormalizeDockPosition(splitNode.SecondNode, position);
        }
    }

    private static bool HasAnyVisibleToolWindows(
        DockingLayoutNodeState? node,
        IReadOnlyDictionary<string, DockingToolWindowState> toolWindows)
    {
        return node switch
        {
            DockingGroupState groupState => groupState.ToolWindowIds.Any(
                toolWindowId => toolWindows.TryGetValue(toolWindowId, out var toolWindow) && !toolWindow.IsAutoHidden),
            DockingSplitState splitNode => HasAnyVisibleToolWindows(splitNode.FirstNode, toolWindows)
                || HasAnyVisibleToolWindows(splitNode.SecondNode, toolWindows),
            _ => false
        };
    }

    private static void SyncSideVisibility(DockingWorkspaceState state)
    {
        var toolWindows = state.ToolWindows.ToDictionary(item => item.Id, StringComparer.Ordinal);
        state.Left.IsVisible = HasAnyVisibleToolWindows(state.Left.Root, toolWindows);
        state.Right.IsVisible = HasAnyVisibleToolWindows(state.Right.Root, toolWindows);
        state.Top.IsVisible = HasAnyVisibleToolWindows(state.Top.Root, toolWindows);
        state.Bottom.IsVisible = HasAnyVisibleToolWindows(state.Bottom.Root, toolWindows);
    }
}
