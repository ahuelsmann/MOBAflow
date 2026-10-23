// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.Controls.Workflow;

using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;

using Moba.SharedUI.ViewModel;
using Moba.SharedUI.ViewModel.Action;

using System.Collections.Specialized;
using System.ComponentModel;

using Windows.Foundation;
using Windows.System;
using Windows.UI.Core;

/// <summary>
/// Interactive 2D node-graph canvas for editing sequential workflow actions.
/// Synchronizes selection, ordering, and parameters 1:1 with the tabular action list.
/// </summary>
internal sealed partial class WorkflowGraphCanvasControl : UserControl
{
    private const double GridSnapSize = 20.0;
    private const double DotGridSpacing = 24.0;
    private const double CardWidth = WorkflowViewModel.ActionNodeWidth;
    private const double TriggerCardHeight = 84.0;
    private const double ActionCardHeight = 132.0;
    private const double PortSize = 10.0;

    private readonly Dictionary<WorkflowActionViewModel, ActionNodeVisuals> _actionVisuals = [];
    private readonly Dictionary<WorkflowActionViewModel, Point> _dragRawPositions = [];

    private WorkflowLibraryViewModel? _subscribedLibrary;
    private WorkflowViewModel? _subscribedWorkflow;
    private Border? _triggerCard;
    private TextBlock? _triggerSubtitleText;
    private Ellipse? _triggerStatusDot;

    public static readonly DependencyProperty LibraryViewModelProperty = DependencyProperty.Register(
        nameof(LibraryViewModel),
        typeof(WorkflowLibraryViewModel),
        typeof(WorkflowGraphCanvasControl),
        new PropertyMetadata(null, OnLibraryViewModelChanged));

    /// <summary>
    /// Gets or sets the shared workflow library coordinator.
    /// </summary>
    public WorkflowLibraryViewModel? LibraryViewModel
    {
        get => (WorkflowLibraryViewModel?)GetValue(LibraryViewModelProperty);
        set => SetValue(LibraryViewModelProperty, value);
    }

    public WorkflowGraphCanvasControl()
    {
        InitializeComponent();
        Loaded += OnControlLoaded;
        Unloaded += OnControlUnloaded;
        ActualThemeChanged += OnActualThemeChanged;
    }

    private static void OnLibraryViewModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is WorkflowGraphCanvasControl control)
        {
            control.AttachLibrary(e.NewValue as WorkflowLibraryViewModel);
        }
    }

    private void OnControlLoaded(object sender, RoutedEventArgs e)
    {
        RenderDotGrid();
        AttachLibrary(LibraryViewModel);
    }

    private void OnControlUnloaded(object sender, RoutedEventArgs e)
    {
        DetachLibrary();
    }

    private void OnActualThemeChanged(FrameworkElement sender, object args)
    {
        RenderDotGrid();
        RebuildGraph();
    }

    private void AttachLibrary(WorkflowLibraryViewModel? library)
    {
        if (ReferenceEquals(_subscribedLibrary, library))
        {
            UpdateEmptyState();
            return;
        }

        DetachLibrary();
        _subscribedLibrary = library;

        if (_subscribedLibrary != null)
        {
            _subscribedLibrary.PropertyChanged += OnLibraryPropertyChanged;
            _subscribedLibrary.ValidationIssues.CollectionChanged += OnValidationIssuesChanged;
            AttachWorkflow(_subscribedLibrary.SelectedWorkflow);
        }
        else
        {
            AttachWorkflow(null);
        }
    }

    private void DetachLibrary()
    {
        AttachWorkflow(null);
        if (_subscribedLibrary != null)
        {
            _subscribedLibrary.PropertyChanged -= OnLibraryPropertyChanged;
            _subscribedLibrary.ValidationIssues.CollectionChanged -= OnValidationIssuesChanged;
            _subscribedLibrary = null;
        }
    }

    private void AttachWorkflow(WorkflowViewModel? workflow)
    {
        if (_subscribedWorkflow != null)
        {
            _subscribedWorkflow.Actions.CollectionChanged -= OnActionsCollectionChanged;
            foreach (var action in _actionVisuals.Keys.ToList())
            {
                action.PropertyChanged -= OnActionPropertyChanged;
            }
        }

        _subscribedWorkflow = workflow;

        if (_subscribedWorkflow != null)
        {
            _subscribedWorkflow.AutoLayoutActions(forceResetCustomPositions: false);
            _subscribedWorkflow.Actions.CollectionChanged += OnActionsCollectionChanged;
        }

        RebuildGraph();
    }

    private void OnLibraryPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(WorkflowLibraryViewModel.SelectedWorkflow))
        {
            AttachWorkflow(_subscribedLibrary?.SelectedWorkflow);
        }
        else if (e.PropertyName == nameof(WorkflowLibraryViewModel.SelectedAction))
        {
            UpdateSelectionVisuals();
        }
        else if (e.PropertyName is nameof(WorkflowLibraryViewModel.TriggerSummaryText)
                 or nameof(WorkflowLibraryViewModel.HasAssignedTriggers))
        {
            UpdateTriggerNodeVisuals();
        }
    }

    private void OnValidationIssuesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        foreach (var (action, visuals) in _actionVisuals)
        {
            UpdateActionStatusVisuals(action, visuals);
        }
    }

    private void OnActionsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        RebuildGraph();
    }

    private void OnActionPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is not WorkflowActionViewModel action ||
            !_actionVisuals.TryGetValue(action, out var visuals))
        {
            return;
        }

        if (e.PropertyName is nameof(WorkflowActionViewModel.CanvasX)
            or nameof(WorkflowActionViewModel.CanvasY))
        {
            Canvas.SetLeft(visuals.RootCard, action.CanvasX);
            Canvas.SetTop(visuals.RootCard, action.CanvasY);
            RenderConnections();
            return;
        }

        if (e.PropertyName == nameof(WorkflowActionViewModel.Name))
        {
            visuals.TitleText.Text = string.IsNullOrWhiteSpace(action.Name) ? action.TypeDisplayName : action.Name;
        }
        else if (e.PropertyName == nameof(WorkflowActionViewModel.ParameterSummary))
        {
            visuals.ParameterPillText.Text = action.ParameterSummary;
        }
        else if (e.PropertyName is nameof(WorkflowActionViewModel.Number)
                 or nameof(WorkflowActionViewModel.StepLabel))
        {
            visuals.StepText.Text = action.StepLabel;
            RenderConnections();
        }
        else if (e.PropertyName is nameof(WorkflowActionViewModel.DelayAfterMs)
                 or nameof(WorkflowActionViewModel.DelayBadgeText)
                 or nameof(WorkflowActionViewModel.HasDelayAfter))
        {
            RenderConnections();
        }
        else if (e.PropertyName is nameof(WorkflowActionViewModel.IsValidAction)
                 or nameof(WorkflowActionViewModel.HasValidationIssue)
                 or nameof(WorkflowActionViewModel.ValidationMessage))
        {
            UpdateActionStatusVisuals(action, visuals);
        }
    }

    private void RenderDotGrid()
    {
        DotGridCanvas.Children.Clear();
        var dotBrush = GetThemeBrush("ControlStrokeColorDefaultBrush");
        var width = DotGridCanvas.Width;
        var height = DotGridCanvas.Height;

        for (var y = DotGridSpacing; y < height; y += DotGridSpacing)
        {
            for (var x = DotGridSpacing; x < width; x += DotGridSpacing)
            {
                var dot = new Ellipse
                {
                    Width = 2,
                    Height = 2,
                    Fill = dotBrush,
                    Opacity = 0.55
                };
                Canvas.SetLeft(dot, x - 1);
                Canvas.SetTop(dot, y - 1);
                DotGridCanvas.Children.Add(dot);
            }
        }
    }

    private void RebuildGraph()
    {
        foreach (var action in _actionVisuals.Keys.ToList())
        {
            action.PropertyChanged -= OnActionPropertyChanged;
        }

        _actionVisuals.Clear();
        _dragRawPositions.Clear();
        NodesCanvas.Children.Clear();
        ConnectionsCanvas.Children.Clear();
        _triggerCard = null;
        _triggerSubtitleText = null;
        _triggerStatusDot = null;

        UpdateEmptyState();

        if (_subscribedWorkflow == null)
        {
            return;
        }

        // 1. Create Read-Only Workflow Trigger Start Node
        _triggerCard = CreateTriggerNodeCard();
        Canvas.SetLeft(_triggerCard, WorkflowViewModel.TriggerNodeCanvasX);
        Canvas.SetTop(_triggerCard, WorkflowViewModel.TriggerNodeCanvasY);
        NodesCanvas.Children.Add(_triggerCard);

        // 2. Create Action Node Cards
        foreach (var action in _subscribedWorkflow.Actions)
        {
            action.PropertyChanged += OnActionPropertyChanged;
            var visuals = CreateActionNodeCard(action);
            _actionVisuals[action] = visuals;
            Canvas.SetLeft(visuals.RootCard, action.CanvasX);
            Canvas.SetTop(visuals.RootCard, action.CanvasY);
            NodesCanvas.Children.Add(visuals.RootCard);
        }

        UpdateSelectionVisuals();
        RenderConnections();
    }

    private Border CreateTriggerNodeCard()
    {
        var cardGrid = new Grid
        {
            Padding = new Thickness(12, 10, 12, 10),
            RowSpacing = 4
        };
        cardGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        cardGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var headerGrid = new Grid { ColumnSpacing = 8 };
        headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var icon = new FontIcon
        {
            Glyph = "\uE945",
            FontSize = 14,
            Foreground = GetThemeBrush("AccentTextFillColorPrimaryBrush")
        };

        var title = new TextBlock
        {
            Text = "Workflow Trigger",
            Style = GetThemeStyle("BodyStrongTextBlockStyle"),
            TextTrimming = TextTrimming.CharacterEllipsis
        };
        Grid.SetColumn(title, 1);

        _triggerStatusDot = new Ellipse
        {
            Width = 8,
            Height = 8,
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(_triggerStatusDot, 2);

        headerGrid.Children.Add(icon);
        headerGrid.Children.Add(title);
        headerGrid.Children.Add(_triggerStatusDot);

        _triggerSubtitleText = new TextBlock
        {
            Style = GetThemeStyle("CaptionTextBlockStyle"),
            Foreground = GetThemeBrush("TextFillColorSecondaryBrush"),
            TextTrimming = TextTrimming.CharacterEllipsis
        };
        Grid.SetRow(_triggerSubtitleText, 1);

        cardGrid.Children.Add(headerGrid);
        cardGrid.Children.Add(_triggerSubtitleText);

        var outerGrid = new Grid
        {
            Width = CardWidth,
            Height = TriggerCardHeight
        };

        var cardBorder = new Border
        {
            Margin = new Thickness(0, 0, 0, PortSize / 2),
            CornerRadius = new CornerRadius(8),
            Background = GetThemeBrush("CardBackgroundFillColorSecondaryBrush"),
            BorderBrush = GetThemeBrush("CardStrokeColorDefaultBrush"),
            BorderThickness = new Thickness(1),
            Child = cardGrid
        };

        var bottomPort = CreatePortEllipse();
        bottomPort.HorizontalAlignment = HorizontalAlignment.Center;
        bottomPort.VerticalAlignment = VerticalAlignment.Bottom;

        outerGrid.Children.Add(cardBorder);
        outerGrid.Children.Add(bottomPort);

        var rootWrapper = new Border
        {
            Width = CardWidth,
            Height = TriggerCardHeight,
            Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent),
            Child = outerGrid
        };
        AutomationProperties.SetName(rootWrapper, "Workflow trigger node");
        ToolTipService.SetToolTip(rootWrapper, "Click to view workflow settings and trigger references");
        rootWrapper.Tapped += OnTriggerCardTapped;

        UpdateTriggerNodeVisuals();
        return rootWrapper;
    }

    private void UpdateTriggerNodeVisuals()
    {
        if (_triggerSubtitleText == null || _triggerStatusDot == null)
        {
            return;
        }

        var hasTriggers = _subscribedLibrary?.HasAssignedTriggers == true;
        _triggerSubtitleText.Text = _subscribedLibrary?.TriggerSummaryText ?? "Manual / Unassigned";
        _triggerStatusDot.Fill = GetThemeBrush(hasTriggers
            ? "SystemFillColorSuccessBrush"
            : "TextFillColorSecondaryBrush");
    }

    private ActionNodeVisuals CreateActionNodeCard(WorkflowActionViewModel action)
    {
        var contentStack = new StackPanel
        {
            Padding = new Thickness(12, 10, 12, 8),
            Spacing = 6
        };

        // Row 1: Icon + Title + Status Dot
        var headerGrid = new Grid { ColumnSpacing = 8 };
        headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var typeIcon = new FontIcon
        {
            Glyph = action.IconGlyph,
            FontSize = 14,
            Foreground = GetThemeBrush("TextFillColorPrimaryBrush")
        };

        var titleText = new TextBlock
        {
            Text = string.IsNullOrWhiteSpace(action.Name) ? action.TypeDisplayName : action.Name,
            Style = GetThemeStyle("BodyStrongTextBlockStyle"),
            TextTrimming = TextTrimming.CharacterEllipsis
        };
        Grid.SetColumn(titleText, 1);

        var statusDot = new Ellipse
        {
            Width = 8,
            Height = 8,
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(statusDot, 2);

        headerGrid.Children.Add(typeIcon);
        headerGrid.Children.Add(titleText);
        headerGrid.Children.Add(statusDot);

        // Row 2: Action Type Subtitle
        var subtitleText = new TextBlock
        {
            Text = action.TypeDisplayName,
            Style = GetThemeStyle("CaptionTextBlockStyle"),
            Foreground = GetThemeBrush("TextFillColorSecondaryBrush"),
            TextTrimming = TextTrimming.CharacterEllipsis
        };

        // Row 3: Parameter Summary Pill (AgentFlow badge style)
        var parameterPillText = new TextBlock
        {
            Text = action.ParameterSummary,
            Style = GetThemeStyle("CaptionTextBlockStyle"),
            Foreground = GetThemeBrush("TextFillColorPrimaryBrush"),
            TextTrimming = TextTrimming.CharacterEllipsis
        };

        var parameterPillBorder = new Border
        {
            Padding = new Thickness(8, 3, 8, 3),
            CornerRadius = new CornerRadius(4),
            Background = GetThemeBrush("SubtleFillColorSecondaryBrush"),
            BorderBrush = GetThemeBrush("CardStrokeColorDefaultBrush"),
            BorderThickness = new Thickness(1),
            HorizontalAlignment = HorizontalAlignment.Left,
            MaxWidth = CardWidth - 28,
            Child = parameterPillText
        };

        // Row 4: Footer (Step #N + Reorder buttons)
        var footerGrid = new Grid();
        footerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        footerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var stepText = new TextBlock
        {
            Text = action.StepLabel,
            Style = GetThemeStyle("CaptionTextBlockStyle"),
            Foreground = GetThemeBrush("TextFillColorSecondaryBrush"),
            VerticalAlignment = VerticalAlignment.Center
        };

        var reorderPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 2
        };
        Grid.SetColumn(reorderPanel, 1);

        var moveUpBtn = new Button
        {
            Padding = new Thickness(4, 2, 4, 2),
            MinWidth = 24,
            MinHeight = 22,
            Tag = action,
            Content = new FontIcon { Glyph = "\uE74A", FontSize = 10 }
        };
        AutomationProperties.SetName(moveUpBtn, "Move step up");
        ToolTipService.SetToolTip(moveUpBtn, "Move step earlier in sequence");
        moveUpBtn.Click += OnNodeMoveUpClicked;

        var moveDownBtn = new Button
        {
            Padding = new Thickness(4, 2, 4, 2),
            MinWidth = 24,
            MinHeight = 22,
            Tag = action,
            Content = new FontIcon { Glyph = "\uE74B", FontSize = 10 }
        };
        AutomationProperties.SetName(moveDownBtn, "Move step down");
        ToolTipService.SetToolTip(moveDownBtn, "Move step later in sequence");
        moveDownBtn.Click += OnNodeMoveDownClicked;

        reorderPanel.Children.Add(moveUpBtn);
        reorderPanel.Children.Add(moveDownBtn);

        footerGrid.Children.Add(stepText);
        footerGrid.Children.Add(reorderPanel);

        contentStack.Children.Add(headerGrid);
        contentStack.Children.Add(subtitleText);
        contentStack.Children.Add(parameterPillBorder);
        contentStack.Children.Add(footerGrid);

        var cardBorder = new Border
        {
            Margin = new Thickness(0, PortSize / 2, 0, PortSize / 2),
            CornerRadius = new CornerRadius(8),
            Background = GetThemeBrush("CardBackgroundFillColorDefaultBrush"),
            BorderBrush = GetThemeBrush("CardStrokeColorDefaultBrush"),
            BorderThickness = new Thickness(1),
            Child = contentStack
        };

        var topPort = CreatePortEllipse();
        topPort.HorizontalAlignment = HorizontalAlignment.Center;
        topPort.VerticalAlignment = VerticalAlignment.Top;

        var bottomPort = CreatePortEllipse();
        bottomPort.HorizontalAlignment = HorizontalAlignment.Center;
        bottomPort.VerticalAlignment = VerticalAlignment.Bottom;

        var outerGrid = new Grid
        {
            Width = CardWidth,
            Height = ActionCardHeight
        };
        outerGrid.Children.Add(cardBorder);
        outerGrid.Children.Add(topPort);
        outerGrid.Children.Add(bottomPort);

        var rootWrapper = new Border
        {
            Width = CardWidth,
            Height = ActionCardHeight,
            Tag = action,
            Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent),
            ManipulationMode = ManipulationModes.TranslateX | ManipulationModes.TranslateY,
            Child = outerGrid
        };
        AutomationProperties.SetName(rootWrapper, $"{action.StepLabel}: {action.Name}");
        rootWrapper.PointerPressed += OnActionCardPointerPressed;
        rootWrapper.ManipulationStarted += OnActionCardManipulationStarted;
        rootWrapper.ManipulationDelta += OnActionCardManipulationDelta;
        rootWrapper.ManipulationCompleted += OnActionCardManipulationCompleted;

        var visuals = new ActionNodeVisuals(
            rootWrapper,
            cardBorder,
            titleText,
            parameterPillText,
            stepText,
            statusDot);

        UpdateActionStatusVisuals(action, visuals);
        return visuals;
    }

    private static Ellipse CreatePortEllipse() => new()
    {
        Width = PortSize,
        Height = PortSize,
        Fill = GetThemeBrush("CardBackgroundFillColorDefaultBrush"),
        Stroke = GetThemeBrush("TextFillColorSecondaryBrush"),
        StrokeThickness = 1.5
    };

    private static void UpdateActionStatusVisuals(WorkflowActionViewModel action, ActionNodeVisuals visuals)
    {
        visuals.StatusDot.Fill = GetThemeBrush(action.IsValidAction
            ? "SystemFillColorSuccessBrush"
            : "SystemFillColorCriticalBrush");

        var tooltip = action.IsValidAction
            ? "Action payload is valid"
            : string.IsNullOrWhiteSpace(action.ValidationMessage)
                ? "Action has an invalid or missing payload"
                : action.ValidationMessage;
        ToolTipService.SetToolTip(visuals.StatusDot, tooltip);
    }

    private void UpdateSelectionVisuals()
    {
        var selectedAction = _subscribedLibrary?.SelectedAction;
        var accentBrush = GetThemeBrush("AccentFillColorDefaultBrush");
        var defaultStrokeBrush = GetThemeBrush("CardStrokeColorDefaultBrush");

        foreach (var (action, visuals) in _actionVisuals)
        {
            var isSelected = ReferenceEquals(action, selectedAction);
            visuals.CardBorder.BorderBrush = isSelected ? accentBrush : defaultStrokeBrush;
            visuals.CardBorder.BorderThickness = isSelected ? new Thickness(2) : new Thickness(1);
        }

        if (_triggerCard?.Child is Grid triggerGrid &&
            triggerGrid.Children.FirstOrDefault() is Border triggerBorder)
        {
            var isWorkflowSettingsSelected = _subscribedLibrary?.SelectedWorkflow != null && selectedAction == null;
            triggerBorder.BorderBrush = isWorkflowSettingsSelected ? accentBrush : defaultStrokeBrush;
            triggerBorder.BorderThickness = isWorkflowSettingsSelected ? new Thickness(2) : new Thickness(1);
        }
    }

    private void RenderConnections()
    {
        ConnectionsCanvas.Children.Clear();
        if (_subscribedWorkflow == null || _subscribedWorkflow.Actions.Count == 0)
        {
            return;
        }

        var actions = _subscribedWorkflow.Actions;
        var strokeBrush = GetThemeBrush("TextFillColorSecondaryBrush");

        // 1. Trigger Node (bottom port) -> Step #1 (top port)
        var triggerOutput = new Point(
            WorkflowViewModel.TriggerNodeCanvasX + (CardWidth / 2.0),
            WorkflowViewModel.TriggerNodeCanvasY + TriggerCardHeight);
        var firstInput = new Point(
            actions[0].CanvasX + (CardWidth / 2.0),
            actions[0].CanvasY);
        AddBezierConnection(triggerOutput, firstInput, strokeBrush, delayBadgeText: null);

        // 2. Sequential connections Step #i -> Step #i+1
        for (var i = 0; i < actions.Count - 1; i++)
        {
            var source = actions[i];
            var target = actions[i + 1];
            var startPoint = new Point(
                source.CanvasX + (CardWidth / 2.0),
                source.CanvasY + ActionCardHeight);
            var endPoint = new Point(
                target.CanvasX + (CardWidth / 2.0),
                target.CanvasY);

            var badgeText = source.HasDelayAfter ? source.DelayBadgeText : null;
            AddBezierConnection(startPoint, endPoint, strokeBrush, badgeText);
        }
    }

    private void AddBezierConnection(Point start, Point end, Brush strokeBrush, string? delayBadgeText)
    {
        var verticalSpan = Math.Abs(end.Y - start.Y);
        var horizontalSpan = Math.Abs(end.X - start.X);
        var tangentOffset = Math.Max(44.0, (verticalSpan * 0.45) + (horizontalSpan * 0.15));

        var control1 = new Point(start.X, start.Y + tangentOffset);
        var control2 = new Point(end.X, end.Y - tangentOffset);

        var figure = new PathFigure
        {
            StartPoint = start,
            IsClosed = false,
            IsFilled = false
        };
        figure.Segments.Add(new BezierSegment
        {
            Point1 = control1,
            Point2 = control2,
            Point3 = end
        });

        var geometry = new PathGeometry();
        geometry.Figures.Add(figure);

        var dashArray = new DoubleCollection { 4, 3 };
        var path = new Path
        {
            Data = geometry,
            Stroke = strokeBrush,
            StrokeThickness = 1.75,
            StrokeDashArray = dashArray
        };
        ConnectionsCanvas.Children.Add(path);

        // Arrowhead pointing into the top port of the target node
        var arrowPolygon = new Polygon
        {
            Fill = strokeBrush,
            Points =
            [
                new Point(end.X, end.Y),
                new Point(end.X - 4.5, end.Y - 8),
                new Point(end.X + 4.5, end.Y - 8)
            ]
        };
        ConnectionsCanvas.Children.Add(arrowPolygon);

        if (!string.IsNullOrWhiteSpace(delayBadgeText))
        {
            var midPoint = EvaluateCubicBezierMidpoint(start, control1, control2, end);
            var badge = new Border
            {
                Padding = new Thickness(7, 2, 7, 2),
                CornerRadius = new CornerRadius(10),
                Background = GetThemeBrush("CardBackgroundFillColorDefaultBrush"),
                BorderBrush = GetThemeBrush("CardStrokeColorDefaultBrush"),
                BorderThickness = new Thickness(1),
                Child = new TextBlock
                {
                    Text = $"⏱ {delayBadgeText}",
                    Style = GetThemeStyle("CaptionTextBlockStyle"),
                    Foreground = GetThemeBrush("TextFillColorSecondaryBrush")
                }
            };
            badge.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            Canvas.SetLeft(badge, midPoint.X - (badge.DesiredSize.Width / 2.0));
            Canvas.SetTop(badge, midPoint.Y - (badge.DesiredSize.Height / 2.0));
            ConnectionsCanvas.Children.Add(badge);
        }
    }

    private static Point EvaluateCubicBezierMidpoint(Point p0, Point p1, Point p2, Point p3) =>
        new(
            (0.125 * p0.X) + (0.375 * p1.X) + (0.375 * p2.X) + (0.125 * p3.X),
            (0.125 * p0.Y) + (0.375 * p1.Y) + (0.375 * p2.Y) + (0.125 * p3.Y));

    private void UpdateEmptyState()
    {
        if (_subscribedWorkflow == null)
        {
            EmptyStateTitleText.Text = "No workflow selected";
            EmptyStateSubtitleText.Text = "Select or create a workflow in the left column to view its action graph.";
            EmptyStatePanel.Visibility = Visibility.Visible;
        }
        else if (_subscribedWorkflow.Actions.Count == 0)
        {
            EmptyStateTitleText.Text = "No actions in this workflow";
            EmptyStateSubtitleText.Text = "Use 'Add action' in the toolbar above to add steps to the graph.";
            EmptyStatePanel.Visibility = Visibility.Visible;
        }
        else
        {
            EmptyStatePanel.Visibility = Visibility.Collapsed;
        }
    }

    private void OnTriggerCardTapped(object sender, TappedRoutedEventArgs e)
    {
        if (_subscribedLibrary?.ShowWorkflowSettingsCommand.CanExecute(null) == true)
        {
            _subscribedLibrary.ShowWorkflowSettingsCommand.Execute(null);
            UpdateSelectionVisuals();
        }
        e.Handled = true;
    }

    private void OnActionCardPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: WorkflowActionViewModel action } && _subscribedLibrary != null)
        {
            _subscribedLibrary.SelectedAction = action;
            Focus(FocusState.Programmatic);
        }
    }

    private void OnActionCardManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: WorkflowActionViewModel action })
        {
            _dragRawPositions[action] = new Point(action.CanvasX, action.CanvasY);
            if (_subscribedLibrary != null)
            {
                _subscribedLibrary.SelectedAction = action;
            }
        }
    }

    private void OnActionCardManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: WorkflowActionViewModel action })
        {
            return;
        }

        var zoom = CanvasScrollViewer.ZoomFactor <= 0 ? 1.0f : CanvasScrollViewer.ZoomFactor;
        if (!_dragRawPositions.TryGetValue(action, out var raw))
        {
            raw = new Point(action.CanvasX, action.CanvasY);
        }

        var nextRawX = Math.Clamp(raw.X + (e.Delta.Translation.X / zoom), 20.0, CanvasHost.Width - CardWidth - 20.0);
        var nextRawY = Math.Clamp(raw.Y + (e.Delta.Translation.Y / zoom), 20.0, CanvasHost.Height - ActionCardHeight - 20.0);
        _dragRawPositions[action] = new Point(nextRawX, nextRawY);

        var snappedX = Math.Round(nextRawX / GridSnapSize) * GridSnapSize;
        var snappedY = Math.Round(nextRawY / GridSnapSize) * GridSnapSize;
        action.SetCanvasPosition(snappedX, snappedY, isCustomPosition: true);
        e.Handled = true;
    }

    private void OnActionCardManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: WorkflowActionViewModel action })
        {
            _dragRawPositions.Remove(action);
        }
        e.Handled = true;
    }

    private void OnNodeMoveUpClicked(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: WorkflowActionViewModel action } && _subscribedLibrary != null)
        {
            _subscribedLibrary.SelectedAction = action;
            if (_subscribedLibrary.MoveSelectedActionUpCommand.CanExecute(null))
            {
                _subscribedLibrary.MoveSelectedActionUpCommand.Execute(null);
            }
        }
    }

    private void OnNodeMoveDownClicked(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: WorkflowActionViewModel action } && _subscribedLibrary != null)
        {
            _subscribedLibrary.SelectedAction = action;
            if (_subscribedLibrary.MoveSelectedActionDownCommand.CanExecute(null))
            {
                _subscribedLibrary.MoveSelectedActionDownCommand.Execute(null);
            }
        }
    }

    private void OnCanvasBackgroundPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (ReferenceEquals(e.OriginalSource, CanvasHost) &&
            _subscribedLibrary?.ShowWorkflowSettingsCommand.CanExecute(null) == true)
        {
            _subscribedLibrary.ShowWorkflowSettingsCommand.Execute(null);
            UpdateSelectionVisuals();
        }
    }

    private void OnAutoLayoutClicked(object sender, RoutedEventArgs e)
    {
        if (_subscribedLibrary?.AutoLayoutSelectedWorkflowCommand.CanExecute(null) == true)
        {
            _subscribedLibrary.AutoLayoutSelectedWorkflowCommand.Execute(null);
            RenderConnections();
        }
    }

    private void OnResetZoomClicked(object sender, RoutedEventArgs e)
    {
        _ = CanvasScrollViewer.ChangeView(0, 0, 1.0f);
    }

    private void OnRootKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (_subscribedLibrary?.SelectedWorkflow == null || _subscribedLibrary.SelectedAction == null)
        {
            return;
        }

        var altDown = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Menu)
            .HasFlag(CoreVirtualKeyStates.Down);
        if (altDown && e.Key == VirtualKey.Up && _subscribedLibrary.MoveSelectedActionUpCommand.CanExecute(null))
        {
            _subscribedLibrary.MoveSelectedActionUpCommand.Execute(null);
            e.Handled = true;
        }
        else if (altDown && e.Key == VirtualKey.Down && _subscribedLibrary.MoveSelectedActionDownCommand.CanExecute(null))
        {
            _subscribedLibrary.MoveSelectedActionDownCommand.Execute(null);
            e.Handled = true;
        }
        else if (e.Key == VirtualKey.Delete && _subscribedLibrary.DeleteSelectedActionCommand.CanExecute(null))
        {
            _subscribedLibrary.DeleteSelectedActionCommand.Execute(null);
            e.Handled = true;
        }
    }

    private static Brush GetThemeBrush(string resourceKey)
    {
        if (Application.Current?.Resources != null &&
            Application.Current.Resources.TryGetValue(resourceKey, out var value) &&
            value is Brush brush)
        {
            return brush;
        }

        return new SolidColorBrush(Microsoft.UI.Colors.Gray);
    }

    private static Style? GetThemeStyle(string resourceKey)
    {
        if (Application.Current?.Resources != null &&
            Application.Current.Resources.TryGetValue(resourceKey, out var value) &&
            value is Style style)
        {
            return style;
        }

        return null;
    }

    private sealed record ActionNodeVisuals(
        Border RootCard,
        Border CardBorder,
        TextBlock TitleText,
        TextBlock ParameterPillText,
        TextBlock StepText,
        Ellipse StatusDot);
}
