// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.WinUI.ViewModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Controls.Docking;
using Controls.Docking.Model;
using Controls.Docking.Workspace;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using Service;

using System.Collections.ObjectModel;
using System.Linq;

/// <summary>
/// ViewModel for DockingPage demonstrating LayoutDocument capabilities.
/// (Host-side: no plugin dependencies)
/// </summary>
public sealed partial class DockingPageViewModel : ObservableObject
{
    private const double CollapsedPanelExtent = 32.0;

    [ObservableProperty]
    private ObservableCollection<DocumentTab> _openDocuments = new();

    [ObservableProperty]
    private DocumentTab? _activeDocument;

    [ObservableProperty]
    private DockNode? _leftNode;

    [ObservableProperty]
    private DockNode? _rightNode;

    [ObservableProperty]
    private DockNode? _topNode;

    [ObservableProperty]
    private DockNode? _bottomNode;

    [ObservableProperty]
    private ObservableCollection<DockPanel> _leftAutoHidePanels = new();

    [ObservableProperty]
    private ObservableCollection<DockPanel> _rightAutoHidePanels = new();

    [ObservableProperty]
    private ObservableCollection<DockPanel> _topAutoHidePanels = new();

    [ObservableProperty]
    private ObservableCollection<DockPanel> _bottomAutoHidePanels = new();

    [ObservableProperty]
    private double _leftPanelWidth = 240;

    [ObservableProperty]
    private double _rightPanelWidth = 260;

    [ObservableProperty]
    private double _topPanelHeight = 100;

    [ObservableProperty]
    private double _bottomPanelHeight = 180;

    [ObservableProperty]
    private bool _isLeftPanelVisible = true;

    [ObservableProperty]
    private bool _isRightPanelVisible = true;

    [ObservableProperty]
    private bool _isTopPanelVisible;

    [ObservableProperty]
    private bool _isBottomPanelVisible = true;

    private int _documentCounter = 1;
    private readonly DockingLayoutService _layoutService;
    private readonly DockingWorkspaceService _workspaceService;
    private DockingWorkspaceState _workspaceState;

    public DockingPageViewModel(DockingWorkspaceService workspaceService, DockingLayoutService layoutService)
    {
        _workspaceService = workspaceService;
        _layoutService = layoutService;
        _workspaceState = CreateDefaultWorkspaceState();
        ApplyWorkspaceProjection();
    }

    public async Task InitializeAsync()
    {
        var persistedState = await _layoutService.LoadLastLayoutAsync();
        if (persistedState is not null)
        {
            _workspaceState = persistedState;
        }

        ApplyWorkspaceProjection();
    }

    public Task PersistAsync()
    {
        return _layoutService.SaveLayoutAsync(_workspaceState);
    }

    public void HandleDocumentDockRequested(DocumentTab document, DockPosition position)
    {
        ArgumentNullException.ThrowIfNull(document);
        if (!_workspaceService.DockDocumentToSide(_workspaceState, document.DocumentId, position))
        {
            return;
        }

        ApplyWorkspaceProjection();
    }

    public void HandlePanelDockRequested(string panelId, DockPosition position)
    {
        if (string.IsNullOrWhiteSpace(panelId))
        {
            return;
        }

        if (!_workspaceService.DockToolWindowToSide(_workspaceState, panelId, position))
        {
            return;
        }

        ApplyWorkspaceProjection();
    }

    public void HandlePanelStateChanged(string panelId, bool isExpanded)
    {
        if (string.IsNullOrWhiteSpace(panelId))
        {
            return;
        }

        _workspaceService.UpdateToolWindowExpansion(_workspaceState, panelId, isExpanded);
    }

    public void HandlePanelAutoHideChanged(string panelId, DockPosition position, bool isAutoHidden)
    {
        if (string.IsNullOrWhiteSpace(panelId))
        {
            return;
        }

        if (!_workspaceService.UpdateToolWindowAutoHide(_workspaceState, panelId, position, isAutoHidden))
        {
            return;
        }

        ApplyWorkspaceProjection();
    }

    [RelayCommand]
    private void AddNewDocument()
    {
        var docNum = _documentCounter++;
        var id = $"doc:{docNum}";
        _workspaceState.Documents.Add(new DockingDocumentState
        {
            Id = id,
            Title = $"Document {docNum}.txt",
            IconGlyph = "\uE160",
            ContentId = $"content:doc:{docNum}",
            ContentTitle = $"Document {docNum}.txt",
            ContentBody = $"Generated sample content for document {docNum}.",
            IsModified = false,
            IsPinned = false
        });

        _workspaceState.ActiveDocumentId = id;
        ApplyWorkspaceProjection();
    }

    [RelayCommand]
    private void MarkAsModified()
    {
        var activeDocumentState = GetActiveDocumentState();
        if (activeDocumentState is null)
        {
            return;
        }

        activeDocumentState.IsModified = !activeDocumentState.IsModified;
        ApplyWorkspaceProjection();
    }

    [RelayCommand]
    private void CloseCurrentDocument()
    {
        var activeDocumentState = GetActiveDocumentState();
        if (activeDocumentState is null || _workspaceState.Documents.Count <= 1)
        {
            return;
        }

        _workspaceState.Documents.Remove(activeDocumentState);
        _workspaceState.ActiveDocumentId = _workspaceState.Documents.LastOrDefault()?.Id;
        ApplyWorkspaceProjection();
    }

    [RelayCommand]
    private void CloseAllModifiedDocuments()
    {
        var modifiedDocuments = _workspaceState.Documents.Where(document => document.IsModified).ToList();
        foreach (var document in modifiedDocuments)
        {
            _workspaceState.Documents.Remove(document);
        }

        _workspaceState.ActiveDocumentId = _workspaceState.Documents.FirstOrDefault()?.Id;
        ApplyWorkspaceProjection();
    }

    partial void OnActiveDocumentChanged(DocumentTab? value)
    {
        _workspaceState.ActiveDocumentId = value?.DocumentId;
    }

    partial void OnLeftPanelWidthChanged(double value)
    {
        if (value > CollapsedPanelExtent)
        {
            _workspaceState.Left.Extent = value;
        }
    }

    partial void OnRightPanelWidthChanged(double value)
    {
        if (value > CollapsedPanelExtent)
        {
            _workspaceState.Right.Extent = value;
        }
    }

    partial void OnTopPanelHeightChanged(double value)
    {
        if (value > CollapsedPanelExtent)
        {
            _workspaceState.Top.Extent = value;
        }
    }

    partial void OnBottomPanelHeightChanged(double value)
    {
        if (value > CollapsedPanelExtent)
        {
            _workspaceState.Bottom.Extent = value;
        }
    }

    partial void OnIsLeftPanelVisibleChanged(bool value)
    {
        _workspaceState.Left.IsVisible = value;
    }

    partial void OnIsRightPanelVisibleChanged(bool value)
    {
        _workspaceState.Right.IsVisible = value;
    }

    partial void OnIsTopPanelVisibleChanged(bool value)
    {
        _workspaceState.Top.IsVisible = value;
    }

    partial void OnIsBottomPanelVisibleChanged(bool value)
    {
        _workspaceState.Bottom.IsVisible = value;
    }

    private void ApplyWorkspaceProjection()
    {
        var projection = _workspaceService.BuildProjection(_workspaceState, CreateContent);

        OpenDocuments = projection.Documents;
        LeftNode = projection.LeftNode;
        RightNode = projection.RightNode;
        TopNode = projection.TopNode;
        BottomNode = projection.BottomNode;
        LeftAutoHidePanels = projection.LeftAutoHidePanels;
        RightAutoHidePanels = projection.RightAutoHidePanels;
        TopAutoHidePanels = projection.TopAutoHidePanels;
        BottomAutoHidePanels = projection.BottomAutoHidePanels;
        ActiveDocument = projection.ActiveDocument;

        LeftPanelWidth = _workspaceState.Left.Extent;
        RightPanelWidth = _workspaceState.Right.Extent;
        TopPanelHeight = _workspaceState.Top.Extent;
        BottomPanelHeight = _workspaceState.Bottom.Extent;

        IsLeftPanelVisible = _workspaceState.Left.IsVisible;
        IsRightPanelVisible = _workspaceState.Right.IsVisible;
        IsTopPanelVisible = _workspaceState.Top.IsVisible;
        IsBottomPanelVisible = _workspaceState.Bottom.IsVisible;

        _documentCounter = Math.Max(
            2,
            _workspaceState.Documents
                .Select(document => document.Id)
                .Select(TryExtractTrailingNumber)
                .DefaultIfEmpty(1)
                .Max() + 1);
    }

    private UIElement CreateContent(string contentId)
    {
        var document = _workspaceState.Documents.FirstOrDefault(item => item.ContentId == contentId);
        if (document is not null)
        {
            return CreateDocumentContent(document.ContentTitle, document.ContentBody);
        }

        var toolWindow = _workspaceState.ToolWindows.FirstOrDefault(item => item.ContentId == contentId);
        if (toolWindow is not null)
        {
            return CreatePanelContent(toolWindow.ContentBody);
        }

        return CreatePanelContent(contentId);
    }

    private DockingDocumentState? GetActiveDocumentState()
    {
        var documentId = ActiveDocument?.DocumentId ?? _workspaceState.ActiveDocumentId;
        return documentId is null
            ? null
            : _workspaceState.Documents.FirstOrDefault(document => document.Id == documentId);
    }

    private static DockingWorkspaceState CreateDefaultWorkspaceState()
    {
        const string solutionExplorerId = "tool:solution-explorer";
        const string classViewId = "tool:class-view";
        const string propertiesId = "tool:properties";
        const string outputId = "tool:output";
        const string problemsId = "tool:problems";
        const string documentId = "doc:1";

        return new DockingWorkspaceState
        {
            ActiveDocumentId = documentId,
            Documents =
            [
                new DockingDocumentState
                {
                    Id = documentId,
                    Title = "Document 1",
                    IconGlyph = "\uE8A5",
                    ContentId = "content:doc:1",
                    ContentTitle = "Document 1",
                    ContentBody = "Welcome to the docking demo.",
                    IsModified = false,
                    IsPinned = false
                }
            ],
            ToolWindows =
            [
                new DockingToolWindowState
                {
                    Id = solutionExplorerId,
                    Title = "Solution Explorer",
                    IconGlyph = "\uEC50",
                    ContentId = "content:tool:solution-explorer",
                    ContentBody = "Project structure and files.",
                    IsExpanded = true
                },
                new DockingToolWindowState
                {
                    Id = classViewId,
                    Title = "Class View",
                    IconGlyph = "\uE8B8",
                    ContentId = "content:tool:class-view",
                    ContentBody = "Types and symbols for the active solution.",
                    IsExpanded = true
                },
                new DockingToolWindowState
                {
                    Id = propertiesId,
                    Title = "Properties",
                    IconGlyph = "\uE946",
                    ContentId = "content:tool:properties",
                    ContentBody = "Details for the current selection.",
                    IsExpanded = true
                },
                new DockingToolWindowState
                {
                    Id = outputId,
                    Title = "Output",
                    IconGlyph = "\uE7BA",
                    ContentId = "content:tool:output",
                    ContentBody = "Build output, diagnostics and execution logs.",
                    IsExpanded = true
                },
                new DockingToolWindowState
                {
                    Id = problemsId,
                    Title = "Problems",
                    IconGlyph = "\uEA39",
                    ContentId = "content:tool:problems",
                    ContentBody = "Warnings and validation results.",
                    IsExpanded = true
                }
            ],
            Left = new DockingSideState
            {
                Position = DockPosition.Left,
                Extent = 240,
                IsVisible = true,
                Root = new DockingGroupState
                {
                    DockPosition = DockPosition.Left,
                    LayoutMode = DockGroupLayoutMode.Tabbed,
                    ToolWindowIds = [solutionExplorerId, classViewId]
                }
            },
            Right = new DockingSideState
            {
                Position = DockPosition.Right,
                Extent = 260,
                IsVisible = true,
                Root = new DockingGroupState
                {
                    DockPosition = DockPosition.Right,
                    LayoutMode = DockGroupLayoutMode.Tabbed,
                    ToolWindowIds = [propertiesId]
                }
            },
            Top = new DockingSideState
            {
                Position = DockPosition.Top,
                Extent = 100,
                IsVisible = false,
                Root = null
            },
            Bottom = new DockingSideState
            {
                Position = DockPosition.Bottom,
                Extent = 180,
                IsVisible = true,
                Root = new DockingSplitState
                {
                    DockPosition = DockPosition.Bottom,
                    Orientation = Orientation.Horizontal,
                    SplitRatio = 0.5,
                    FirstNode = new DockingGroupState
                    {
                        DockPosition = DockPosition.Bottom,
                        LayoutMode = DockGroupLayoutMode.Tabbed,
                        ToolWindowIds = [outputId]
                    },
                    SecondNode = new DockingGroupState
                    {
                        DockPosition = DockPosition.Bottom,
                        LayoutMode = DockGroupLayoutMode.Tabbed,
                        ToolWindowIds = [problemsId]
                    }
                }
            }
        };
    }

    private static int TryExtractTrailingNumber(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return 0;
        }

        var digits = new string(id.Reverse().TakeWhile(char.IsDigit).Reverse().ToArray());
        return int.TryParse(digits, out var value) ? value : 0;
    }

    private static UIElement CreatePanelContent(string description)
    {
        return new Border
        {
            Padding = new Thickness(12),
            Child = new TextBlock
            {
                Text = description,
                TextWrapping = TextWrapping.WrapWholeWords
            }
        };
    }

    private static UIElement CreateDocumentContent(string title, string body)
    {
        return new Border
        {
            Padding = new Thickness(16),
            Child = new StackPanel
            {
                Spacing = 12,
                Children =
                {
                    new TextBlock
                    {
                        FontSize = 20,
                        Text = title
                    },
                    new TextBlock
                    {
                        Text = body,
                        TextWrapping = TextWrapping.WrapWholeWords
                    }
                }
            }
        };
    }
}