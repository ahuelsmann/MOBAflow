// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.WinUI.Controls.Docking;

using Behavior;

using Microsoft.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Input;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;

using Model;

using System.Collections.ObjectModel;
using System.Collections.Specialized;

using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;

/// <summary>
/// DockingManager Control mit Visual Studio-style Layout nach Fluent Design System.
/// Features:
///   - DockPanelGroup per side (multiple panels with TabView grouping)
///   - Draggable proportional splitters (in dedicated Grid columns/rows)
///   - Auto-Hide sidebars
///   - Focus Highlighting
///   - Drag & Drop overlay with preview
/// </summary>
internal sealed partial class DockingManager
{
    private const string DockPanelDataKey = "DockPanel";
    private const string DocumentTabDataKey = "DocumentTab";
    private const double CollapsedTabWidth = 32;
    private bool _isOverlayVisible;

    // Splitter dragging state
    private bool _isSplitterDragging;
    private string? _activeSplitterTag;
    private Point _splitterDragStart;
    private double _splitterStartSize;

    private double _leftExpandedWidth;
    private double _rightExpandedWidth;
    private double _topExpandedHeight;
    private double _bottomExpandedHeight;
    private readonly Border _autoHidePreviewHost;
    private readonly FontIcon _autoHidePreviewIcon;
    private readonly TextBlock _autoHidePreviewTitle;
    private readonly ContentPresenter _autoHidePreviewContent;
    private readonly DispatcherQueueTimer _autoHidePreviewCloseTimer;
    private string? _activeAutoHidePanelId;
    private DockPosition? _activeAutoHideSide;
    private bool _isPointerOverAutoHidePreview;
    private bool _isPointerOverAutoHideSidebar;

    public event EventHandler<DocumentTabDockRequestedEventArgs>? DocumentTabDockRequested;

    public event EventHandler<DockPanelDockRequestedEventArgs>? DockPanelDockRequested;

    public event EventHandler<DockPanelAutoHideRequestedEventArgs>? DockPanelAutoHideRequested;

    public event EventHandler<DockPanelStateChangedEventArgs>? DockPanelStateChanged;

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

    public static readonly DependencyProperty LeftLayoutModeProperty =
        DependencyProperty.Register(
            nameof(LeftLayoutMode),
            typeof(DockGroupLayoutMode),
            typeof(DockingManager),
            new PropertyMetadata(DockGroupLayoutMode.Tabbed, OnLayoutModeChanged));

    public static readonly DependencyProperty RightLayoutModeProperty =
        DependencyProperty.Register(
            nameof(RightLayoutMode),
            typeof(DockGroupLayoutMode),
            typeof(DockingManager),
            new PropertyMetadata(DockGroupLayoutMode.Tabbed, OnLayoutModeChanged));

    public static readonly DependencyProperty TopLayoutModeProperty =
        DependencyProperty.Register(
            nameof(TopLayoutMode),
            typeof(DockGroupLayoutMode),
            typeof(DockingManager),
            new PropertyMetadata(DockGroupLayoutMode.Tabbed, OnLayoutModeChanged));

    public static readonly DependencyProperty BottomLayoutModeProperty =
        DependencyProperty.Register(
            nameof(BottomLayoutMode),
            typeof(DockGroupLayoutMode),
            typeof(DockingManager),
            new PropertyMetadata(DockGroupLayoutMode.Tabbed, OnLayoutModeChanged));

    public static readonly DependencyProperty LeftNodeProperty =
        DependencyProperty.Register(
            nameof(LeftNode),
            typeof(DockNode),
            typeof(DockingManager),
            new PropertyMetadata(null, OnLeftNodeChanged));

    public static readonly DependencyProperty RightNodeProperty =
        DependencyProperty.Register(
            nameof(RightNode),
            typeof(DockNode),
            typeof(DockingManager),
            new PropertyMetadata(null, OnRightNodeChanged));

    public static readonly DependencyProperty TopNodeProperty =
        DependencyProperty.Register(
            nameof(TopNode),
            typeof(DockNode),
            typeof(DockingManager),
            new PropertyMetadata(null, OnTopNodeChanged));

    public static readonly DependencyProperty BottomNodeProperty =
        DependencyProperty.Register(
            nameof(BottomNode),
            typeof(DockNode),
            typeof(DockingManager),
            new PropertyMetadata(null, OnBottomNodeChanged));

    public static readonly DependencyProperty LeftAutoHidePanelsProperty =
        DependencyProperty.Register(
            nameof(LeftAutoHidePanels),
            typeof(ObservableCollection<DockPanel>),
            typeof(DockingManager),
            new PropertyMetadata(null, OnLeftAutoHidePanelsChanged));

    public static readonly DependencyProperty RightAutoHidePanelsProperty =
        DependencyProperty.Register(
            nameof(RightAutoHidePanels),
            typeof(ObservableCollection<DockPanel>),
            typeof(DockingManager),
            new PropertyMetadata(null, OnRightAutoHidePanelsChanged));

    public static readonly DependencyProperty TopAutoHidePanelsProperty =
        DependencyProperty.Register(
            nameof(TopAutoHidePanels),
            typeof(ObservableCollection<DockPanel>),
            typeof(DockingManager),
            new PropertyMetadata(null, OnTopAutoHidePanelsChanged));

    public static readonly DependencyProperty BottomAutoHidePanelsProperty =
        DependencyProperty.Register(
            nameof(BottomAutoHidePanels),
            typeof(ObservableCollection<DockPanel>),
            typeof(DockingManager),
            new PropertyMetadata(null, OnBottomAutoHidePanelsChanged));

    private static void OnLayoutModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is DockingManager manager)
        {
            manager.SyncLayoutModesToGroups();
        }
    }

    private static void OnLeftNodeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is DockingManager manager)
        {
            manager.OnSideNodeChanged(DockPosition.Left, e.NewValue as DockNode);
        }
    }

    private static void OnRightNodeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is DockingManager manager)
        {
            manager.OnSideNodeChanged(DockPosition.Right, e.NewValue as DockNode);
        }
    }

    private static void OnTopNodeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is DockingManager manager)
        {
            manager.OnSideNodeChanged(DockPosition.Top, e.NewValue as DockNode);
        }
    }

    private static void OnBottomNodeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is DockingManager manager)
        {
            manager.OnSideNodeChanged(DockPosition.Bottom, e.NewValue as DockNode);
        }
    }

    private static void OnLeftAutoHidePanelsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is DockingManager manager)
        {
            manager.OnAutoHidePanelsChanged(
                DockPosition.Left,
                e.OldValue as ObservableCollection<DockPanel>,
                e.NewValue as ObservableCollection<DockPanel>);
        }
    }

    private static void OnRightAutoHidePanelsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is DockingManager manager)
        {
            manager.OnAutoHidePanelsChanged(
                DockPosition.Right,
                e.OldValue as ObservableCollection<DockPanel>,
                e.NewValue as ObservableCollection<DockPanel>);
        }
    }

    private static void OnTopAutoHidePanelsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is DockingManager manager)
        {
            manager.OnAutoHidePanelsChanged(
                DockPosition.Top,
                e.OldValue as ObservableCollection<DockPanel>,
                e.NewValue as ObservableCollection<DockPanel>);
        }
    }

    private static void OnBottomAutoHidePanelsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is DockingManager manager)
        {
            manager.OnAutoHidePanelsChanged(
                DockPosition.Bottom,
                e.OldValue as ObservableCollection<DockPanel>,
                e.NewValue as ObservableCollection<DockPanel>);
        }
    }

    private void OnSideNodeChanged(DockPosition side, DockNode? node)
    {
        if (node is not null)
        {
            NormalizeNodeForSide(node, side, null);
            ApplyLayoutMode(node, GetLayoutMode(side));
        }

        UpdateSideState(side, node);
    }

    private void SyncLayoutModesToGroups()
    {
        ApplyLayoutMode(LeftNode, LeftLayoutMode);
        ApplyLayoutMode(RightNode, RightLayoutMode);
        ApplyLayoutMode(TopNode, TopLayoutMode);
        ApplyLayoutMode(BottomNode, BottomLayoutMode);
    }

    #endregion

    public DockingManager()
    {
        InitializeComponent();
        (_autoHidePreviewHost, _autoHidePreviewIcon, _autoHidePreviewTitle, _autoHidePreviewContent) =
            CreateAutoHidePreviewHost();
        _autoHidePreviewCloseTimer = DispatcherQueue.CreateTimer();
        _autoHidePreviewCloseTimer.Interval = TimeSpan.FromMilliseconds(180);
        _autoHidePreviewCloseTimer.Tick += OnAutoHidePreviewCloseTimerTick;
        Grid.SetRow(_autoHidePreviewHost, 1);
        RootGrid.Children.Add(_autoHidePreviewHost);
        RootGrid.KeyDown += OnRootGridKeyDown;
        RootGrid.PointerPressed += OnRootGridPointerPressed;
        LeftAutoHideBar.PointerEntered += OnAutoHideSidebarPointerEntered;
        LeftAutoHideBar.PointerExited += OnAutoHideSidebarPointerExited;
        LeftAutoHideBar.LostFocus += OnAutoHideUiLostFocus;
        RightAutoHideBar.PointerEntered += OnAutoHideSidebarPointerEntered;
        RightAutoHideBar.PointerExited += OnAutoHideSidebarPointerExited;
        RightAutoHideBar.LostFocus += OnAutoHideUiLostFocus;
        TopAutoHideBar.PointerEntered += OnAutoHideSidebarPointerEntered;
        TopAutoHideBar.PointerExited += OnAutoHideSidebarPointerExited;
        TopAutoHideBar.LostFocus += OnAutoHideUiLostFocus;
        BottomAutoHideBar.PointerEntered += OnAutoHideSidebarPointerEntered;
        BottomAutoHideBar.PointerExited += OnAutoHideSidebarPointerExited;
        BottomAutoHideBar.LostFocus += OnAutoHideUiLostFocus;
        LeftNodePresenter.PanelExpansionChanged += OnRootPanelExpansionChanged;
        LeftNodePresenter.PanelAutoHideRequested += OnRootPanelAutoHideRequested;
        RightNodePresenter.PanelExpansionChanged += OnRootPanelExpansionChanged;
        RightNodePresenter.PanelAutoHideRequested += OnRootPanelAutoHideRequested;
        TopNodePresenter.PanelExpansionChanged += OnRootPanelExpansionChanged;
        TopNodePresenter.PanelAutoHideRequested += OnRootPanelAutoHideRequested;
        BottomNodePresenter.PanelExpansionChanged += OnRootPanelExpansionChanged;
        BottomNodePresenter.PanelAutoHideRequested += OnRootPanelAutoHideRequested;
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _leftExpandedWidth = LeftPanelWidth;
        _rightExpandedWidth = RightPanelWidth;
        _topExpandedHeight = TopPanelHeight;
        _bottomExpandedHeight = BottomPanelHeight;

        SyncLayoutModesToGroups();
        ResetObservedAutoHidePanels();
        RebuildAllAutoHideSidebars();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        UnobserveAutoHidePanels(LeftAutoHidePanels);
        UnobserveAutoHidePanels(RightAutoHidePanels);
        UnobserveAutoHidePanels(TopAutoHidePanels);
        UnobserveAutoHidePanels(BottomAutoHidePanels);
        _autoHidePreviewCloseTimer.Stop();
        HideAutoHidePreview();
    }

    private void OnRootGridKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key != Windows.System.VirtualKey.Escape || _autoHidePreviewHost.Visibility != Visibility.Visible)
        {
            return;
        }

        HideAutoHidePreview();
        e.Handled = true;
    }

    private void OnRootPanelExpansionChanged(object? sender, DockPanelExpansionChangedEventArgs e)
    {
        if (e.Panel is null)
        {
            return;
        }

        UpdateSideDimension(e.Panel.DockPosition);

        if (string.IsNullOrWhiteSpace(e.Panel.PanelId))
        {
            return;
        }

        DockPanelStateChanged?.Invoke(this, new DockPanelStateChangedEventArgs(e.Panel.PanelId, e.Panel.IsExpanded));
    }

    private void OnRootPanelAutoHideRequested(object? sender, DockPanelAutoHideIntentEventArgs e)
    {
        PinToAutoHide(e.Panel, e.Panel.DockPosition);
    }

    private void OnAutoHidePanelsChanged(
        DockPosition side,
        ObservableCollection<DockPanel>? oldValue,
        ObservableCollection<DockPanel>? newValue)
    {
        if (!ReferenceEquals(oldValue, newValue))
        {
            UnobserveAutoHidePanels(oldValue);
            ObserveAutoHidePanels(newValue);
        }

        RebuildAutoHideSidebar(side);
    }

    private void ResetObservedAutoHidePanels()
    {
        UnobserveAutoHidePanels(LeftAutoHidePanels);
        UnobserveAutoHidePanels(RightAutoHidePanels);
        UnobserveAutoHidePanels(TopAutoHidePanels);
        UnobserveAutoHidePanels(BottomAutoHidePanels);
        ObserveAutoHidePanels(LeftAutoHidePanels);
        ObserveAutoHidePanels(RightAutoHidePanels);
        ObserveAutoHidePanels(TopAutoHidePanels);
        ObserveAutoHidePanels(BottomAutoHidePanels);
    }

    private void ObserveAutoHidePanels(ObservableCollection<DockPanel>? panels)
    {
        if (panels is not null)
        {
            panels.CollectionChanged += OnObservedAutoHidePanelsCollectionChanged;
        }
    }

    private void UnobserveAutoHidePanels(ObservableCollection<DockPanel>? panels)
    {
        if (panels is not null)
        {
            panels.CollectionChanged -= OnObservedAutoHidePanelsCollectionChanged;
        }
    }

    private void OnObservedAutoHidePanelsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (ReferenceEquals(sender, LeftAutoHidePanels))
        {
            RebuildAutoHideSidebar(DockPosition.Left);
        }
        else if (ReferenceEquals(sender, RightAutoHidePanels))
        {
            RebuildAutoHideSidebar(DockPosition.Right);
        }
        else if (ReferenceEquals(sender, TopAutoHidePanels))
        {
            RebuildAutoHideSidebar(DockPosition.Top);
        }
        else if (ReferenceEquals(sender, BottomAutoHidePanels))
        {
            RebuildAutoHideSidebar(DockPosition.Bottom);
        }
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

    /// <summary>Layout mode of the left dock group (Split = even, Tabbed = tabs).</summary>
    public DockGroupLayoutMode LeftLayoutMode
    {
        get => (DockGroupLayoutMode)GetValue(LeftLayoutModeProperty);
        set => SetValue(LeftLayoutModeProperty, value);
    }

    /// <summary>Layout mode of the right dock group.</summary>
    public DockGroupLayoutMode RightLayoutMode
    {
        get => (DockGroupLayoutMode)GetValue(RightLayoutModeProperty);
        set => SetValue(RightLayoutModeProperty, value);
    }

    /// <summary>Layout mode of the top dock group.</summary>
    public DockGroupLayoutMode TopLayoutMode
    {
        get => (DockGroupLayoutMode)GetValue(TopLayoutModeProperty);
        set => SetValue(TopLayoutModeProperty, value);
    }

    /// <summary>Layout mode of the bottom dock group.</summary>
    public DockGroupLayoutMode BottomLayoutMode
    {
        get => (DockGroupLayoutMode)GetValue(BottomLayoutModeProperty);
        set => SetValue(BottomLayoutModeProperty, value);
    }

    /// <summary>Node tree for left dock area.</summary>
    public DockNode? LeftNode
    {
        get => (DockNode?)GetValue(LeftNodeProperty);
        set => SetValue(LeftNodeProperty, value);
    }

    /// <summary>Node tree for right dock area.</summary>
    public DockNode? RightNode
    {
        get => (DockNode?)GetValue(RightNodeProperty);
        set => SetValue(RightNodeProperty, value);
    }

    /// <summary>Node tree for top dock area.</summary>
    public DockNode? TopNode
    {
        get => (DockNode?)GetValue(TopNodeProperty);
        set => SetValue(TopNodeProperty, value);
    }

    /// <summary>Node tree for bottom dock area.</summary>
    public DockNode? BottomNode
    {
        get => (DockNode?)GetValue(BottomNodeProperty);
        set => SetValue(BottomNodeProperty, value);
    }

    public ObservableCollection<DockPanel>? LeftAutoHidePanels
    {
        get => (ObservableCollection<DockPanel>?)GetValue(LeftAutoHidePanelsProperty);
        set => SetValue(LeftAutoHidePanelsProperty, value);
    }

    public ObservableCollection<DockPanel>? RightAutoHidePanels
    {
        get => (ObservableCollection<DockPanel>?)GetValue(RightAutoHidePanelsProperty);
        set => SetValue(RightAutoHidePanelsProperty, value);
    }

    public ObservableCollection<DockPanel>? TopAutoHidePanels
    {
        get => (ObservableCollection<DockPanel>?)GetValue(TopAutoHidePanelsProperty);
        set => SetValue(TopAutoHidePanelsProperty, value);
    }

    public ObservableCollection<DockPanel>? BottomAutoHidePanels
    {
        get => (ObservableCollection<DockPanel>?)GetValue(BottomAutoHidePanelsProperty);
        set => SetValue(BottomAutoHidePanelsProperty, value);
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

        var rootNode = GetSideNode(position);
        if (!TryGetPrimaryGroupNode(rootNode, out var targetGroup))
        {
            SetSideNode(position, CreateGroupNode(position, panel));
            return;
        }

        targetGroup!.Panels.Add(panel);
        UpdateSideState(position, rootNode);
    }

    /// <summary>
    /// Removes a panel from whichever group it belongs to.
    /// Auto-hides the dock side if the group becomes empty.
    /// </summary>
    public void RemovePanelFromAllGroups(DockPanel panel)
    {
        ArgumentNullException.ThrowIfNull(panel);

        foreach (var side in new[] { DockPosition.Left, DockPosition.Right, DockPosition.Top, DockPosition.Bottom })
        {
            var currentNode = GetSideNode(side);
            var updatedNode = RemovePanelFromNode(currentNode, panel, out var removed);
            if (!removed)
            {
                continue;
            }

            if (!ReferenceEquals(currentNode, updatedNode))
            {
                SetSideNode(side, updatedNode);
            }
            else
            {
                UpdateSideState(side, updatedNode);
            }
        }
    }

    private void UpdateSideState(DockPosition side, DockNode? node)
    {
        var hasPanels = HasAnyPanels(node);
        SetPanelVisibility(side, hasPanels);
        if (hasPanels)
        {
            UpdateSideDimension(side);
        }
    }

    private void UpdateSideDimension(DockPosition side)
    {
        var rootNode = GetSideNode(side);
        if (!HasAnyPanels(rootNode))
        {
            return;
        }

        var anyExpanded = HasAnyExpandedPanels(rootNode);

        switch (side)
        {
            case DockPosition.Left:
                UpdateLeftWidth(anyExpanded);
                break;
            case DockPosition.Right:
                UpdateRightWidth(anyExpanded);
                break;
            case DockPosition.Top:
                UpdateTopHeight(anyExpanded);
                break;
            case DockPosition.Bottom:
                UpdateBottomHeight(anyExpanded);
                break;
        }
    }

    private DockNode? GetSideNode(DockPosition side) => side switch
    {
        DockPosition.Left => LeftNode,
        DockPosition.Right => RightNode,
        DockPosition.Top => TopNode,
        DockPosition.Bottom => BottomNode,
        _ => null
    };

    private void SetSideNode(DockPosition side, DockNode? node)
    {
        switch (side)
        {
            case DockPosition.Left:
                LeftNode = node;
                break;
            case DockPosition.Right:
                RightNode = node;
                break;
            case DockPosition.Top:
                TopNode = node;
                break;
            case DockPosition.Bottom:
                BottomNode = node;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(side), side, null);
        }
    }

    private DockGroupLayoutMode GetLayoutMode(DockPosition side) => side switch
    {
        DockPosition.Left => LeftLayoutMode,
        DockPosition.Right => RightLayoutMode,
        DockPosition.Top => TopLayoutMode,
        DockPosition.Bottom => BottomLayoutMode,
        _ => DockGroupLayoutMode.Tabbed
    };

    private static DockPanelGroupNode CreateGroupNode(DockPosition side, DockPanel panel)
    {
        var groupNode = new DockPanelGroupNode
        {
            DockPosition = side
        };
        groupNode.Panels.Add(panel);
        return groupNode;
    }

    private static void NormalizeNodeForSide(DockNode node, DockPosition side, DockNode? parent)
    {
        node.Parent = parent;
        node.DockPosition = side;

        if (node is DockSplitNode splitNode)
        {
            NormalizeNodeForSide(splitNode.FirstNode, side, splitNode);
            NormalizeNodeForSide(splitNode.SecondNode, side, splitNode);
        }
    }

    private static void ApplyLayoutMode(DockNode? node, DockGroupLayoutMode layoutMode)
    {
        switch (node)
        {
            case DockPanelGroupNode groupNode:
                groupNode.LayoutMode = layoutMode;
                break;
            case DockSplitNode splitNode:
                ApplyLayoutMode(splitNode.FirstNode, layoutMode);
                ApplyLayoutMode(splitNode.SecondNode, layoutMode);
                break;
        }
    }

    private static bool TryGetPrimaryGroupNode(DockNode? node, out DockPanelGroupNode? groupNode)
    {
        switch (node)
        {
            case DockPanelGroupNode directGroupNode:
                groupNode = directGroupNode;
                return true;
            case DockSplitNode splitNode when TryGetPrimaryGroupNode(splitNode.FirstNode, out groupNode):
                return true;
            case DockSplitNode splitNode when TryGetPrimaryGroupNode(splitNode.SecondNode, out groupNode):
                return true;
            default:
                groupNode = null;
                return false;
        }
    }

    private static DockNode? RemovePanelFromNode(DockNode? node, DockPanel panel, out bool removed)
    {
        removed = false;
        if (node is null)
        {
            return null;
        }

        if (node is DockPanelGroupNode groupNode)
        {
            if (groupNode.Panels.Remove(panel))
            {
                removed = true;
            }

            return groupNode.Panels.Count > 0 ? groupNode : null;
        }

        if (node is not DockSplitNode splitNode)
        {
            return node;
        }

        var firstNode = RemovePanelFromNode(splitNode.FirstNode, panel, out var removedFromFirst);
        var secondNode = RemovePanelFromNode(splitNode.SecondNode, panel, out var removedFromSecond);
        removed = removedFromFirst || removedFromSecond;

        if (firstNode is null && secondNode is null)
        {
            return null;
        }

        if (firstNode is null)
        {
            secondNode!.Parent = splitNode.Parent;
            return secondNode;
        }

        if (secondNode is null)
        {
            firstNode.Parent = splitNode.Parent;
            return firstNode;
        }

        if (!ReferenceEquals(splitNode.FirstNode, firstNode))
        {
            splitNode.FirstNode = firstNode;
        }

        if (!ReferenceEquals(splitNode.SecondNode, secondNode))
        {
            splitNode.SecondNode = secondNode;
        }

        return splitNode;
    }

    private static bool HasAnyPanels(DockNode? node) => node switch
    {
        DockPanelGroupNode groupNode => groupNode.Panels.Count > 0,
        DockSplitNode splitNode => HasAnyPanels(splitNode.FirstNode) || HasAnyPanels(splitNode.SecondNode),
        _ => false
    };

    private static bool HasAnyExpandedPanels(DockNode? node) => node switch
    {
        DockPanelGroupNode groupNode => groupNode.Panels.Any(panel => panel.IsExpanded),
        DockSplitNode splitNode => HasAnyExpandedPanels(splitNode.FirstNode) || HasAnyExpandedPanels(splitNode.SecondNode),
        _ => false
    };

    private void UpdateLeftWidth(bool anyExpanded)
    {
        if (anyExpanded)
        {
            if (LeftPanelWidth <= CollapsedTabWidth)
            {
                LeftPanelWidth = Math.Max(_leftExpandedWidth, CollapsedTabWidth);
            }
        }
        else
        {
            if (LeftPanelWidth > CollapsedTabWidth)
            {
                _leftExpandedWidth = LeftPanelWidth;
            }

            LeftPanelWidth = CollapsedTabWidth;
        }
    }

    private void UpdateRightWidth(bool anyExpanded)
    {
        if (anyExpanded)
        {
            if (RightPanelWidth <= CollapsedTabWidth)
            {
                RightPanelWidth = Math.Max(_rightExpandedWidth, CollapsedTabWidth);
            }
        }
        else
        {
            if (RightPanelWidth > CollapsedTabWidth)
            {
                _rightExpandedWidth = RightPanelWidth;
            }

            RightPanelWidth = CollapsedTabWidth;
        }
    }

    private void UpdateTopHeight(bool anyExpanded)
    {
        if (anyExpanded)
        {
            if (TopPanelHeight <= CollapsedTabWidth)
            {
                TopPanelHeight = Math.Max(_topExpandedHeight, 100.0);
            }
        }
        else
        {
            if (TopPanelHeight > CollapsedTabWidth)
            {
                _topExpandedHeight = TopPanelHeight;
            }

            TopPanelHeight = CollapsedTabWidth;
        }
    }

    private void UpdateBottomHeight(bool anyExpanded)
    {
        if (anyExpanded)
        {
            if (BottomPanelHeight <= CollapsedTabWidth)
            {
                BottomPanelHeight = Math.Max(_bottomExpandedHeight, 100.0);
            }
        }
        else
        {
            if (BottomPanelHeight > CollapsedTabWidth)
            {
                _bottomExpandedHeight = BottomPanelHeight;
            }

            BottomPanelHeight = CollapsedTabWidth;
        }
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

        var controlsPosition = (DockPosition)Enum.Parse(typeof(DockPosition), position.ToString());

        if (TryGetDraggedPanel(e, out var panel) && panel is not null)
        {
            if (!TryRaiseDockPanelRequest(panel, controlsPosition))
            {
                DockPanel(panel, controlsPosition);
            }
        }
        else if (TryGetDraggedDocumentTab(e, out var tab) && tab is not null)
        {
            DockDocumentTab(tab, controlsPosition);
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
        var origin = transform.TransformPoint(new Point(0, 0));

        PreviewBox.Width = Math.Max(0, target.ActualWidth);
        PreviewBox.Height = Math.Max(0, target.ActualHeight);
        Canvas.SetLeft(PreviewBox, origin.X);
        Canvas.SetTop(PreviewBox, origin.Y);
        PreviewBox.Visibility = Visibility.Visible;
    }

    private bool IsPointInsideRoot(Point position)
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

    private bool TryRaiseDockPanelRequest(DockPanel panel, DockPosition position)
    {
        ArgumentNullException.ThrowIfNull(panel);

        if (position == DockPosition.Center
            || string.IsNullOrWhiteSpace(panel.PanelId)
            || DockPanelDockRequested is null)
        {
            return false;
        }

        DockPanelDockRequested.Invoke(this, new DockPanelDockRequestedEventArgs(panel.PanelId, position));
        return true;
    }

    private void DockDocumentTab(DocumentTab tab, DockPosition position)
    {
        ArgumentNullException.ThrowIfNull(tab);

        if (position == DockPosition.Center)
        {
            return;
        }

        if (DocumentTabDockRequested is not null)
        {
            DocumentTabDockRequested.Invoke(this, new DocumentTabDockRequestedEventArgs(tab, position));
            return;
        }

        var wrapper = new DockPanel
        {
            PanelId = tab.DocumentId,
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
        UpdateExpandedWidthFromSplitter(_activeSplitterTag);
        _activeSplitterTag = null;
        ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.Arrow);
        e.Handled = true;
    }

    private void UpdateExpandedWidthFromSplitter(string? splitterTag)
    {
        switch (splitterTag)
        {
            case "Left":
                if (LeftPanelWidth > CollapsedTabWidth)
                {
                    _leftExpandedWidth = LeftPanelWidth;
                }
                break;
            case "Right":
                if (RightPanelWidth > CollapsedTabWidth)
                {
                    _rightExpandedWidth = RightPanelWidth;
                }
                break;
            case "Top":
                if (TopPanelHeight > CollapsedTabWidth)
                {
                    _topExpandedHeight = TopPanelHeight;
                }
                break;
            case "Bottom":
                if (BottomPanelHeight > CollapsedTabWidth)
                {
                    _bottomExpandedHeight = BottomPanelHeight;
                }
                break;
        }
    }

    #endregion

    #region Auto-Hide

    /// <summary>
    /// Moves a panel to the auto-hide sidebar at the specified side.
    /// </summary>
    public void PinToAutoHide(DockPanel panel, DockPosition side)
    {
        ArgumentNullException.ThrowIfNull(panel);

        if (side == DockPosition.Center || string.IsNullOrWhiteSpace(panel.PanelId))
        {
            return;
        }

        HideAutoHidePreview();
        DockPanelAutoHideRequested?.Invoke(this, new DockPanelAutoHideRequestedEventArgs(panel.PanelId, side, true));
    }

    private StackPanel GetAutoHideSidebar(DockPosition side) => side switch
    {
        DockPosition.Left => LeftAutoHideBar,
        DockPosition.Right => RightAutoHideBar,
        DockPosition.Top => TopAutoHideBar,
        DockPosition.Bottom => BottomAutoHideBar,
        _ => LeftAutoHideBar
    };

    private ObservableCollection<DockPanel>? GetAutoHidePanels(DockPosition side) => side switch
    {
        DockPosition.Left => LeftAutoHidePanels,
        DockPosition.Right => RightAutoHidePanels,
        DockPosition.Top => TopAutoHidePanels,
        DockPosition.Bottom => BottomAutoHidePanels,
        _ => null
    };

    private void RebuildAllAutoHideSidebars()
    {
        RebuildAutoHideSidebar(DockPosition.Left);
        RebuildAutoHideSidebar(DockPosition.Right);
        RebuildAutoHideSidebar(DockPosition.Top);
        RebuildAutoHideSidebar(DockPosition.Bottom);
    }

    private void RebuildAutoHideSidebar(DockPosition side)
    {
        var sidebar = GetAutoHideSidebar(side);
        var panels = GetAutoHidePanels(side);

        sidebar.Children.Clear();
        if (panels is null || panels.Count == 0)
        {
            sidebar.Visibility = Visibility.Collapsed;
            RefreshActiveAutoHidePreview();
            return;
        }

        foreach (var panel in panels)
        {
            sidebar.Children.Add(CreateAutoHideTabButton(panel, side));
        }

        sidebar.Visibility = Visibility.Visible;
        UpdateAutoHideTabVisualStates();
        RefreshActiveAutoHidePreview();
    }

    private Button CreateAutoHideTabButton(DockPanel panel, DockPosition side)
    {
        var button = new Button
        {
            Background = new SolidColorBrush(Colors.Transparent),
            BorderBrush = new SolidColorBrush(Colors.Transparent),
            Padding = new Thickness(8, 4, 8, 4),
            Margin = new Thickness(2),
            CornerRadius = new CornerRadius(4),
            Tag = panel.PanelId,
            Content = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 4,
                Children =
                {
                    new FontIcon
                    {
                        FontFamily = (FontFamily)
                            Application.Current.Resources["SymbolThemeFontFamily"],
                        FontSize = 12,
                        Glyph = panel.PanelIconGlyph
                    },
                    new TextBlock
                    {
                        Text = panel.PanelTitle,
                        FontSize = 11,
                        VerticalAlignment = VerticalAlignment.Center
                    }
                }
            }
        };

        ApplyAutoHideTabVisualState(
            button,
            _activeAutoHideSide == side && StringComparer.Ordinal.Equals(_activeAutoHidePanelId, panel.PanelId));

        button.PointerEntered += (_, _) =>
        {
            _isPointerOverAutoHideSidebar = true;
            CancelAutoHidePreviewClose();
            ShowAutoHidePreview(panel, side);
        };
        button.GotFocus += (_, _) =>
        {
            CancelAutoHidePreviewClose();
            ShowAutoHidePreview(panel, side);
        };
        button.LostFocus += OnAutoHideUiLostFocus;
        button.Click += (_, _) => ToggleAutoHidePanel(panel, side);
        return button;
    }

    private void ToggleAutoHidePanel(DockPanel panel, DockPosition side)
    {
        if (_activeAutoHideSide == side && StringComparer.Ordinal.Equals(_activeAutoHidePanelId, panel.PanelId))
        {
            HideAutoHidePreview();
        }
        else
        {
            ShowAutoHidePreview(panel, side);
        }
    }

    private void ShowAutoHidePreview(DockPanel panel, DockPosition side)
    {
        CancelAutoHidePreviewClose();
        _activeAutoHidePanelId = panel.PanelId;
        _activeAutoHideSide = side;
        _autoHidePreviewIcon.Glyph = panel.PanelIconGlyph;
        _autoHidePreviewTitle.Text = panel.PanelTitle;
        _autoHidePreviewContent.Content = null;
        _autoHidePreviewContent.Content = panel.PanelContent;
        UpdateAutoHidePreviewLayout(side);
        _autoHidePreviewHost.Visibility = Visibility.Visible;
        UpdateAutoHideTabVisualStates();
    }

    private void HideAutoHidePreview()
    {
        CancelAutoHidePreviewClose();
        _isPointerOverAutoHidePreview = false;
        _activeAutoHidePanelId = null;
        _activeAutoHideSide = null;
        _autoHidePreviewContent.Content = null;
        _autoHidePreviewHost.Visibility = Visibility.Collapsed;
        UpdateAutoHideTabVisualStates();
    }

    private void RefreshActiveAutoHidePreview()
    {
        if (_activeAutoHideSide is null || string.IsNullOrWhiteSpace(_activeAutoHidePanelId))
        {
            return;
        }

        var side = _activeAutoHideSide.Value;
        var panel = FindAutoHidePanel(side, _activeAutoHidePanelId);
        if (panel is null)
        {
            HideAutoHidePreview();
            return;
        }

        ShowAutoHidePreview(panel, side);
    }

    private DockPanel? FindAutoHidePanel(DockPosition side, string panelId)
    {
        var panels = GetAutoHidePanels(side);
        return panels?.FirstOrDefault(panel => StringComparer.Ordinal.Equals(panel.PanelId, panelId));
    }

    private (Border Host, FontIcon Icon, TextBlock Title, ContentPresenter Content) CreateAutoHidePreviewHost()
    {
        var icon = new FontIcon
        {
            FontFamily = (FontFamily)Application.Current.Resources["SymbolThemeFontFamily"],
            FontSize = 12,
            Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"],
            VerticalAlignment = VerticalAlignment.Center
        };

        var title = new TextBlock
        {
            FontSize = 12,
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = (Brush)Application.Current.Resources["TextFillColorPrimaryBrush"]
        };

        var restoreButton = new Button
        {
            Content = "Dock",
            Padding = new Thickness(8, 2, 8, 2),
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center
        };
        restoreButton.Click += OnAutoHidePreviewRestoreClicked;

        var header = new Grid
        {
            Padding = new Thickness(12, 8, 12, 8)
        };
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        Grid.SetColumn(icon, 0);
        Grid.SetColumn(title, 1);
        Grid.SetColumn(restoreButton, 2);
        title.Margin = new Thickness(8, 0, 8, 0);
        header.Children.Add(icon);
        header.Children.Add(title);
        header.Children.Add(restoreButton);

        var content = new ContentPresenter
        {
            Margin = new Thickness(12, 0, 12, 12),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };

        var layout = new Grid();
        layout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        layout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        Grid.SetRow(header, 0);
        Grid.SetRow(content, 1);
        layout.Children.Add(header);
        layout.Children.Add(content);

        var host = new Border
        {
            Visibility = Visibility.Collapsed,
            Background = (Brush)Application.Current.Resources["CardBackgroundFillColorDefaultBrush"],
            BorderBrush = (Brush)Application.Current.Resources["CardStrokeColorDefaultBrush"],
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Stretch,
            Margin = new Thickness(0),
            Child = layout
        };

        host.PointerEntered += OnAutoHidePreviewHostPointerEntered;
        host.PointerExited += OnAutoHidePreviewHostPointerExited;
        host.LostFocus += OnAutoHideUiLostFocus;

        return (host, icon, title, content);
    }

    private void OnAutoHidePreviewRestoreClicked(object? sender, RoutedEventArgs e)
    {
        if (_activeAutoHideSide is null || string.IsNullOrWhiteSpace(_activeAutoHidePanelId))
        {
            return;
        }

        var side = _activeAutoHideSide.Value;
        var panelId = _activeAutoHidePanelId;
        HideAutoHidePreview();
        DockPanelAutoHideRequested?.Invoke(this, new DockPanelAutoHideRequestedEventArgs(panelId, side, false));
    }

    private void UpdateAutoHidePreviewLayout(DockPosition side)
    {
        _autoHidePreviewHost.Width = double.NaN;
        _autoHidePreviewHost.Height = double.NaN;
        _autoHidePreviewHost.Margin = new Thickness(0);

        switch (side)
        {
            case DockPosition.Left:
                _autoHidePreviewHost.HorizontalAlignment = HorizontalAlignment.Left;
                _autoHidePreviewHost.VerticalAlignment = VerticalAlignment.Stretch;
                _autoHidePreviewHost.Width = Math.Max(_leftExpandedWidth, 240.0);
                _autoHidePreviewHost.Margin = new Thickness(28, 0, 0, 0);
                break;
            case DockPosition.Right:
                _autoHidePreviewHost.HorizontalAlignment = HorizontalAlignment.Right;
                _autoHidePreviewHost.VerticalAlignment = VerticalAlignment.Stretch;
                _autoHidePreviewHost.Width = Math.Max(_rightExpandedWidth, 240.0);
                _autoHidePreviewHost.Margin = new Thickness(0, 0, 28, 0);
                break;
            case DockPosition.Top:
                _autoHidePreviewHost.HorizontalAlignment = HorizontalAlignment.Stretch;
                _autoHidePreviewHost.VerticalAlignment = VerticalAlignment.Top;
                _autoHidePreviewHost.Height = Math.Max(_topExpandedHeight, 100.0);
                break;
            case DockPosition.Bottom:
                _autoHidePreviewHost.HorizontalAlignment = HorizontalAlignment.Stretch;
                _autoHidePreviewHost.VerticalAlignment = VerticalAlignment.Bottom;
                _autoHidePreviewHost.Height = Math.Max(_bottomExpandedHeight, 100.0);
                break;
        }
    }

    private void OnAutoHidePreviewCloseTimerTick(object? sender, object e)
    {
        if (_isPointerOverAutoHidePreview || _isPointerOverAutoHideSidebar || IsFocusWithinAutoHideUi())
        {
            return;
        }

        HideAutoHidePreview();
    }

    private void OnAutoHideSidebarPointerEntered(object sender, PointerRoutedEventArgs e)
    {
        _isPointerOverAutoHideSidebar = true;
        CancelAutoHidePreviewClose();
    }

    private void OnAutoHideSidebarPointerExited(object sender, PointerRoutedEventArgs e)
    {
        _isPointerOverAutoHideSidebar = false;
        ScheduleAutoHidePreviewClose();
    }

    private void OnAutoHidePreviewHostPointerEntered(object sender, PointerRoutedEventArgs e)
    {
        _isPointerOverAutoHidePreview = true;
        CancelAutoHidePreviewClose();
    }

    private void OnAutoHidePreviewHostPointerExited(object sender, PointerRoutedEventArgs e)
    {
        _isPointerOverAutoHidePreview = false;
        ScheduleAutoHidePreviewClose();
    }

    private void OnAutoHideUiLostFocus(object sender, RoutedEventArgs e)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            if (_autoHidePreviewHost.Visibility != Visibility.Visible)
            {
                return;
            }

            if (_isPointerOverAutoHidePreview || _isPointerOverAutoHideSidebar || IsFocusWithinAutoHideUi())
            {
                return;
            }

            HideAutoHidePreview();
        });
    }

    private void OnRootGridPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (_autoHidePreviewHost.Visibility != Visibility.Visible)
        {
            return;
        }

        var point = e.GetCurrentPoint(RootGrid).Position;
        if (IsPointInsideElement(_autoHidePreviewHost, point))
        {
            return;
        }

        if (_activeAutoHideSide is not null && IsPointInsideElement(GetAutoHideSidebar(_activeAutoHideSide.Value), point))
        {
            return;
        }

        HideAutoHidePreview();
    }

    private void ScheduleAutoHidePreviewClose()
    {
        if (_autoHidePreviewHost.Visibility != Visibility.Visible)
        {
            return;
        }

        _autoHidePreviewCloseTimer.Stop();
        _autoHidePreviewCloseTimer.Start();
    }

    private void CancelAutoHidePreviewClose()
    {
        _autoHidePreviewCloseTimer.Stop();
    }

    private void UpdateAutoHideTabVisualStates()
    {
        UpdateAutoHideTabVisualStates(LeftAutoHideBar, DockPosition.Left);
        UpdateAutoHideTabVisualStates(RightAutoHideBar, DockPosition.Right);
        UpdateAutoHideTabVisualStates(TopAutoHideBar, DockPosition.Top);
        UpdateAutoHideTabVisualStates(BottomAutoHideBar, DockPosition.Bottom);
    }

    private void UpdateAutoHideTabVisualStates(StackPanel sidebar, DockPosition side)
    {
        foreach (var child in sidebar.Children)
        {
            if (child is not Button button)
            {
                continue;
            }

            var isActive = _activeAutoHideSide == side
                && button.Tag is string panelId
                && StringComparer.Ordinal.Equals(_activeAutoHidePanelId, panelId);
            ApplyAutoHideTabVisualState(button, isActive);
        }
    }

    private static void ApplyAutoHideTabVisualState(Button button, bool isActive)
    {
        var activeBackground = (Brush)Application.Current.Resources["AccentFillColorDefaultBrush"];
        var inactiveBackground = new SolidColorBrush(Colors.Transparent);
        var activeBorderBrush = (Brush)Application.Current.Resources["AccentFillColorDefaultBrush"];
        var inactiveBorderBrush = new SolidColorBrush(Colors.Transparent);
        var activeForeground = (Brush)Application.Current.Resources["TextOnAccentFillColorPrimaryBrush"];
        var inactiveForeground = (Brush)Application.Current.Resources["TextFillColorPrimaryBrush"];

        button.Background = isActive ? activeBackground : inactiveBackground;
        button.BorderBrush = isActive ? activeBorderBrush : inactiveBorderBrush;
        button.BorderThickness = isActive ? new Thickness(1) : new Thickness(0);

        if (button.Content is not StackPanel stackPanel)
        {
            return;
        }

        foreach (var child in stackPanel.Children)
        {
            switch (child)
            {
                case FontIcon icon:
                    icon.Foreground = isActive ? activeForeground : inactiveForeground;
                    break;
                case TextBlock textBlock:
                    textBlock.Foreground = isActive ? activeForeground : inactiveForeground;
                    textBlock.FontWeight = isActive ? FontWeights.SemiBold : FontWeights.Normal;
                    break;
            }
        }
    }

    private bool IsFocusWithinAutoHideUi()
    {
        var focusedElement = FocusManager.GetFocusedElement(XamlRoot) as DependencyObject;
        if (focusedElement is null)
        {
            return false;
        }

        return IsDescendantOf(focusedElement, _autoHidePreviewHost)
            || IsDescendantOf(focusedElement, LeftAutoHideBar)
            || IsDescendantOf(focusedElement, RightAutoHideBar)
            || IsDescendantOf(focusedElement, TopAutoHideBar)
            || IsDescendantOf(focusedElement, BottomAutoHideBar);
    }

    private static bool IsDescendantOf(DependencyObject? element, DependencyObject ancestor)
    {
        var current = element;
        while (current is not null)
        {
            if (ReferenceEquals(current, ancestor))
            {
                return true;
            }

            current = VisualTreeHelper.GetParent(current);
        }

        return false;
    }

    private bool IsPointInsideElement(FrameworkElement element, Point point)
    {
        if (element.Visibility != Visibility.Visible || element.ActualWidth <= 0 || element.ActualHeight <= 0)
        {
            return false;
        }

        var transform = element.TransformToVisual(RootGrid);
        var origin = transform.TransformPoint(new Point(0, 0));
        return point.X >= origin.X
            && point.X <= origin.X + element.ActualWidth
            && point.Y >= origin.Y
            && point.Y <= origin.Y + element.ActualHeight;
    }

    private void SetPanelVisibility(DockPosition side, bool visible)
    {
        switch (side)
        {
            case DockPosition.Left:
                IsLeftPanelVisible = visible;
                // Left/Right: only change visibility, width stays the same
                break;
            case DockPosition.Right:
                IsRightPanelVisible = visible;
                // Left/Right: only change visibility, width stays the same
                break;
            case DockPosition.Top:
                IsTopPanelVisible = visible;
                // Top/Bottom: Height wird beim Collapse reduziert
                if (!visible)
                {
                    _topExpandedHeight = TopPanelHeight;
                    TopPanelHeight = CollapsedTabWidth;
                }
                else
                {
                    TopPanelHeight = _topExpandedHeight > 0 ? _topExpandedHeight : 100.0;
                }
                break;
            case DockPosition.Bottom:
                IsBottomPanelVisible = visible;
                // Top/Bottom: Height wird beim Collapse reduziert
                if (!visible)
                {
                    _bottomExpandedHeight = BottomPanelHeight;
                    BottomPanelHeight = CollapsedTabWidth;
                }
                else
                {
                    BottomPanelHeight = _bottomExpandedHeight > 0 ? _bottomExpandedHeight : 100.0;
                }
                break;
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
        var origin = transform.TransformPoint(new Point(0, 0));

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

internal sealed class DocumentTabDockRequestedEventArgs(DocumentTab document, DockPosition position) : EventArgs
{
    public DocumentTab Document { get; } = document;
    public DockPosition Position { get; } = position;
}

internal sealed class DockPanelDockRequestedEventArgs(string panelId, DockPosition position) : EventArgs
{
    public string PanelId { get; } = panelId;
    public DockPosition Position { get; } = position;
}

internal sealed class DockPanelAutoHideRequestedEventArgs(string panelId, DockPosition position, bool isAutoHidden) : EventArgs
{
    public string PanelId { get; } = panelId;
    public DockPosition Position { get; } = position;
    public bool IsAutoHidden { get; } = isAutoHidden;
}

internal sealed class DockPanelStateChangedEventArgs(string panelId, bool isExpanded) : EventArgs
{
    public string PanelId { get; } = panelId;
    public bool IsExpanded { get; } = isExpanded;
}

/// <summary>
/// EventArgs for a DockPanel undocked back to the document area.
/// </summary>
internal sealed class DockPanelUndockedEventArgs(DockPanel panel) : EventArgs
{
    public DockPanel Panel { get; } = panel;
}