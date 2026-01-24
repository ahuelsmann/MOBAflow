// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;

using Moba.TrackLibrary.PikoA.Catalog;
using Moba.TrackPlan.Constraint;
using Moba.TrackPlan.Editor.ViewModel;
using Moba.TrackPlan.Graph;
using Moba.TrackPlan.TrackSystem;
using Moba.WinUI.Rendering;

using System.Collections.ObjectModel;
using System.Globalization;

using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.UI;

namespace Moba.WinUI.View;

public sealed partial class TrackPlanPage : Page
{
    private const double DisplayScale = 0.5;
    private const double SnapDistance = 30.0;
    private const double GridSize = 50.0;
    private const double PortRadius = 8.0;
    private const double SingleRotationHandleLength = 80.0;

    private readonly TrackPlanEditorViewModel _viewModel;
    private readonly ITrackCatalog _catalog = new PikoATrackCatalog();
    private readonly CanvasRenderer _renderer = new();

    private bool _isPanning;
    private Point _panStart;
    private double _panScrollHorizontalStart;
    private double _panScrollVerticalStart;

    private DateTime _lastClickTime;
    private Point _lastClickPosition;
    private int _clickCount;
    private const double ClickTimeThreshold = 400;
    private const double ClickDistanceThreshold = 10;

    private bool _isRotatingGroup;
    private Point2D _rotationCenter;
    private double _rotationStartAngle;
    private Dictionary<Guid, double> _rotationStartRotations = new();
    private Dictionary<Guid, Point2D> _rotationStartPositions = new();

    private SolidColorBrush _trackBrush = null!;
    private SolidColorBrush _trackSelectedBrush = null!;
    private SolidColorBrush _trackHoverBrush = null!;
    private SolidColorBrush _portOpenBrush = null!;
    private SolidColorBrush _portConnectedBrush = null!;
    private SolidColorBrush _gridBrush = null!;
    private SolidColorBrush _feedbackBrush = null!;
    private SolidColorBrush _snapPreviewBrush = null!;

    public ObservableCollection<ConstraintViolation> Violations { get; } = [];

    public IReadOnlyList<TrackTemplate> StraightTemplates =>
        _catalog.GetByCategory(TrackGeometryKind.Straight).ToList();

    public IReadOnlyList<TrackTemplate> CurveTemplates =>
        _catalog.GetByCategory(TrackGeometryKind.Curve).ToList();

    public IReadOnlyList<TrackTemplate> SwitchTemplates =>
        _catalog.GetByCategory(TrackGeometryKind.Switch).ToList();

    public TrackPlanPage(TrackPlanEditorViewModel viewModel)
    {
        _viewModel = viewModel;
        InitializeComponent();

        Loaded += OnLoaded;
        ActualThemeChanged += OnThemeChanged;
        KeyDown += OnKeyDown;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        UpdateTheme();
        UpdateStatistics();
        RenderGraph();
        Focus(FocusState.Programmatic);
    }

    private void OnThemeChanged(FrameworkElement sender, object args)
    {
        UpdateTheme();
        RenderGraph();
    }

    private void UpdateTheme()
    {
        // Use Fluent Design System ThemeResources with safe fallbacks
        var resources = Application.Current.Resources;

        // Helper to safely get color from resources
        Color GetColorResource(string key, Color fallback)
        {
            try
            {
                if (resources.TryGetValue(key, out var value) && value is Color color)
                    return color;
            }
            catch { }
            return fallback;
        }

        // Get theme colors with safe fallbacks
        var accentColor = GetColorResource("SystemAccentColor", Color.FromArgb(255, 0, 120, 215)); // Windows blue
        var textPrimaryColor = GetColorResource("TextFillColorPrimary", Colors.Black);
        var textSecondaryColor = GetColorResource("TextFillColorSecondary", Colors.Gray);

        // Track normal state - primary text color
        _trackBrush = new SolidColorBrush(textPrimaryColor);

        // Selected track - system accent
        _trackSelectedBrush = new SolidColorBrush(accentColor);

        // Hovered track - accent with reduced emphasis (slightly darker)
        var hoverColor = accentColor;
        if (hoverColor.A > 0)
            hoverColor = Color.FromArgb(hoverColor.A,
                (byte)(hoverColor.R * 0.8),
                (byte)(hoverColor.G * 0.8),
                (byte)(hoverColor.B * 0.8));
        _trackHoverBrush = new SolidColorBrush(hoverColor);

        // Open port - warning/attention (orange)
        var attentionColor = Color.FromArgb(255, 255, 140, 0); // Orange
        _portOpenBrush = new SolidColorBrush(attentionColor);

        // Connected port - success (green)
        var successColor = Color.FromArgb(255, 34, 177, 76); // Green
        _portConnectedBrush = new SolidColorBrush(successColor);

        // Grid lines - subtle secondary text with opacity
        var gridColor = textSecondaryColor;
        _gridBrush = new SolidColorBrush(Color.FromArgb((byte)(255 * 0.15), gridColor.R, gridColor.G, gridColor.B));

        // Feedback/error - system error (red)
        var errorColor = Color.FromArgb(255, 196, 43, 28); // Red
        _feedbackBrush = new SolidColorBrush(errorColor);

        // Snap preview - warning/attention highlight
        _snapPreviewBrush = new SolidColorBrush(attentionColor);
    }

    private void OnKeyDown(object sender, KeyRoutedEventArgs e)
    {
        switch (e.Key)
        {
            case Windows.System.VirtualKey.Escape:
                _viewModel.ClearSelection();
                RenderGraph();
                UpdatePropertiesPanel();
                StatusText.Text = "Selection cleared";
                e.Handled = true;
                break;

            case Windows.System.VirtualKey.Delete:
                DeleteSelected_Click(this, new RoutedEventArgs());
                e.Handled = true;
                break;

            case Windows.System.VirtualKey.A when IsCtrlPressed():
                _viewModel.SelectedTrackIds.Clear();
                foreach (var edge in _viewModel.Graph.Edges)
                    _viewModel.SelectedTrackIds.Add(edge.Id);
                RenderGraph();
                StatusText.Text = $"Selected all {_viewModel.SelectedTrackIds.Count} tracks";
                e.Handled = true;
                break;
        }
    }

    private static bool IsCtrlPressed() =>
        Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Control)
            .HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);

    private void UpdateStatistics()
    {
        NodeCountText.Text = _viewModel.Graph.Nodes.Count.ToString();
        EdgeCountText.Text = _viewModel.Graph.Edges.Count.ToString();
        EndcapCountText.Text = _viewModel.Graph.Endcaps.Count.ToString();
        ZoomLevelText.Text = $"{CanvasScrollViewer.ZoomFactor:P0}";
    }

    private void Element_DragStarting(UIElement sender, DragStartingEventArgs args)
    {
        if (sender is FrameworkElement { DataContext: TrackTemplate t })
        {
            args.Data.SetText(t.Id);
            args.Data.RequestedOperation = DataPackageOperation.Copy;
            StatusText.Text = $"Dragging {t.Id}…";
        }
        else args.Cancel = true;
    }

    private async void GraphCanvas_DragEnter(object sender, DragEventArgs e)
    {
        if (!e.DataView.Contains(StandardDataFormats.Text))
        {
            _viewModel.CancelGhostPlacement();
            return;
        }

        var deferral = e.GetDeferral();
        try
        {
            var templateId = await e.DataView.GetTextAsync();
            _viewModel.BeginGhostPlacement(templateId);
        }
        finally
        {
            deferral.Complete();
        }
    }

    private void GraphCanvas_DragLeave(object sender, DragEventArgs e)
    {
        _viewModel.CancelGhostPlacement();
        RenderGraph();
    }

    private async void GraphCanvas_DragOver(object sender, DragEventArgs e)
    {
        e.AcceptedOperation = DataPackageOperation.Copy;
        e.DragUIOverride.Caption = "Drop to place track";

        if (!e.DataView.Contains(StandardDataFormats.Text))
        {
            _viewModel.CancelGhostPlacement();
            RenderGraph();
            return;
        }

        var deferral = e.GetDeferral();
        try
        {
            var templateId = await e.DataView.GetTextAsync();
            if (_viewModel.GhostPlacement is null || _viewModel.GhostPlacement.TemplateId != templateId)
                _viewModel.BeginGhostPlacement(templateId);

            var pos = e.GetPosition(GraphCanvas);
            var world = new Point2D(pos.X / DisplayScale, pos.Y / DisplayScale);

            _viewModel.UpdateGhostPlacement(
                world,
                gridSnap: GridToggle.IsChecked == true,
                gridSize: GridSize,
                snapDistance: SnapDistance,
                snapEnabled: SnapToggle.IsChecked == true);

            RenderGraph();
        }
        finally
        {
            deferral.Complete();
        }
    }

    private async void GraphCanvas_Drop(object sender, DragEventArgs e)
    {
        if (!e.DataView.Contains(StandardDataFormats.Text))
        {
            _viewModel.CancelGhostPlacement();
            return;
        }

        var templateId = await e.DataView.GetTextAsync();
        var pos = e.GetPosition(GraphCanvas);

        var world = new Point2D(pos.X / DisplayScale, pos.Y / DisplayScale);
        var placement = _viewModel.GhostPlacement is not null
            ? _viewModel.CommitGhostPlacement(SnapToggle.IsChecked == true, SnapDistance)
            : null;

        var finalPosition = placement?.Position ?? world;
        var rotation = placement?.RotationDeg ?? 0.0;
        var preview = placement?.Preview;

        var status = _viewModel.DropTrack(
            templateId,
            finalPosition,
            gridSnap: GridToggle.IsChecked == true && placement is null,
            gridSize: GridSize,
            snapEnabled: SnapToggle.IsChecked == true,
            snapDistance: SnapDistance,
            out _,
            rotationDegOverride: rotation,
            snapPreview: preview);

        StatusText.Text = status;

        UpdateStatistics();
        RenderGraph();
        UpdatePropertiesPanel();
    }

    private void ShowContextMenu(Point pos)
    {
        var world = new Point2D(pos.X / DisplayScale, pos.Y / DisplayScale);
        var hit = _viewModel.HitTest(world, hitRadius: 40.0);

        var menu = new MenuFlyout();

        if (hit.HasValue)
        {
            if (_viewModel.SelectedTrackId != hit.Value)
            {
                _viewModel.SelectedTrackIds.Clear();
                _viewModel.SelectedTrackIds.Add(hit.Value);
            }

            var edge = _viewModel.Graph.Edges.FirstOrDefault(e => e.Id == hit.Value);
            var template = edge != null ? _catalog.GetById(edge.TemplateId) : null;

            menu.Items.Add(CreateMenuItem("Delete", Symbol.Delete, () =>
            {
                DeleteSelected_Click(this, new RoutedEventArgs());
            }));

            menu.Items.Add(CreateMenuItem("Rotate 15°", Symbol.Rotate, () =>
            {
                if (_viewModel.Rotations.TryGetValue(hit.Value, out var rot))
                {
                    _viewModel.Rotations[hit.Value] = (rot + 15) % 360;
                    RenderGraph();
                }
            }));

            menu.Items.Add(CreateMenuItem("Disconnect", Symbol.Cancel, () =>
            {
                DisconnectSelected_Click(this, new RoutedEventArgs());
            }));

            menu.Items.Add(new MenuFlyoutSeparator());

            if (template != null)
            {
                foreach (var end in template.Ends)
                {
                    var portId = end.Id;
                    var hasIsolator = _viewModel.HasIsolator(hit.Value, portId);
                    var label = hasIsolator ? $"Remove Isolator ({portId})" : $"Add Isolator ({portId})";

                    menu.Items.Add(CreateMenuItem(label, Symbol.TwoPage, () =>
                    {
                        _viewModel.ToggleIsolator(hit.Value, portId);
                        RenderGraph();
                    }));
                }
            }

            menu.Items.Add(new MenuFlyoutSeparator());

            menu.Items.Add(CreateMenuItem("Create Section", Symbol.Highlight, () =>
            {
                CreateSection_Click(this, new RoutedEventArgs());
            }));

            menu.Items.Add(CreateMenuItem("Select Connected", Symbol.SelectAll, () =>
            {
                _viewModel.SelectConnectedGroup(hit.Value);
                StatusText.Text = $"Selected {_viewModel.SelectedTrackIds.Count} connected tracks";
                RenderGraph();
                UpdatePropertiesPanel();
            }));
        }
        else if (_viewModel.SelectedTrackIds.Count > 0)
        {
            // Multi-selection context menu (when no new track is hit)
            var count = _viewModel.SelectedTrackIds.Count;
            var countLabel = count == 1 ? "" : $" ({count})";

            menu.Items.Add(CreateMenuItem($"Delete{countLabel}", Symbol.Delete, () =>
            {
                DeleteSelected_Click(this, new RoutedEventArgs());
            }));

            menu.Items.Add(CreateMenuItem($"Disconnect{countLabel}", Symbol.Cancel, () =>
            {
                DisconnectSelected_Click(this, new RoutedEventArgs());
            }));

            menu.Items.Add(new MenuFlyoutSeparator());

            menu.Items.Add(CreateMenuItem("Clear Selection", Symbol.Clear, () =>
            {
                _viewModel.ClearSelection();
                RenderGraph();
                UpdatePropertiesPanel();
            }));
        }
        else
        {
            menu.Items.Add(CreateMenuItem("Clear Selection", Symbol.Clear, () =>
            {
                _viewModel.ClearSelection();
                RenderGraph();
                UpdatePropertiesPanel();
            }));
        }

        menu.ShowAt(GraphCanvas, pos);
    }

    private static MenuFlyoutItem CreateMenuItem(string text, Symbol icon, Action action)
    {
        var item = new MenuFlyoutItem
        {
            Text = text,
            Icon = new SymbolIcon(icon)
        };
        item.Click += (s, e) => action();
        return item;
    }

    private int DetectClickCount(Point pos)
    {
        var now = DateTime.Now;
        var timeDiff = (now - _lastClickTime).TotalMilliseconds;
        var distDiff = Math.Sqrt(Math.Pow(pos.X - _lastClickPosition.X, 2) + Math.Pow(pos.Y - _lastClickPosition.Y, 2));

        if (timeDiff < ClickTimeThreshold && distDiff < ClickDistanceThreshold)
            _clickCount++;
        else
            _clickCount = 1;

        _lastClickTime = now;
        _lastClickPosition = pos;
        return _clickCount;
    }

    private (Point2D Center, Point2D HandlePosition)? GetRotationHandleInfo()
    {
        if (_viewModel.SelectedTrackIds.Count < 2)
            return null;

        double minX = double.MaxValue, minY = double.MaxValue;
        double maxX = double.MinValue, maxY = double.MinValue;

        foreach (var trackId in _viewModel.SelectedTrackIds)
        {
            if (_viewModel.Positions.TryGetValue(trackId, out var pos))
            {
                minX = Math.Min(minX, pos.X - 60);
                minY = Math.Min(minY, pos.Y - 60);
                maxX = Math.Max(maxX, pos.X + 60);
                maxY = Math.Max(maxY, pos.Y + 60);
            }
        }

        if (minX == double.MaxValue)
            return null;

        var centerX = (minX + maxX) / 2;
        var centerY = (minY + maxY) / 2;
        var handleY = minY - 40;

        return (new Point2D(centerX, centerY), new Point2D(centerX, handleY));
    }

    private (Point2D Center, Point2D HandlePosition)? GetSingleRotationHandleInfo()
    {
        if (_viewModel.SelectedTrackIds.Count != 1)
            return null;

        var id = _viewModel.SelectedTrackId ?? _viewModel.SelectedTrackIds.First();
        if (!_viewModel.Positions.TryGetValue(id, out var pos))
            return null;

        var handle = new Point2D(pos.X, pos.Y - SingleRotationHandleLength);
        return (pos, handle);
    }

    private void GraphCanvas_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        var p = e.GetCurrentPoint(GraphCanvas);
        var pos = p.Position;

        if (p.Properties.IsRightButtonPressed)
        {
            _isPanning = true;
            _panStart = pos;
            _panScrollHorizontalStart = CanvasScrollViewer.HorizontalOffset;
            _panScrollVerticalStart = CanvasScrollViewer.VerticalOffset;
            GraphCanvas.CapturePointer(e.Pointer);
            ProtectedCursor = Microsoft.UI.Input.InputSystemCursor.Create(Microsoft.UI.Input.InputSystemCursorShape.SizeAll);
            e.Handled = true;
            return;
        }

        if (p.Properties.IsLeftButtonPressed)
        {
            var clickCount = DetectClickCount(pos);
            var world = new Point2D(pos.X / DisplayScale, pos.Y / DisplayScale);
            var isCtrlPressed = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Control)
                .HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);
            var isShiftPressed = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Shift)
                .HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);

            var handleInfo = GetRotationHandleInfo();
            if (handleInfo.HasValue)
            {
                var (center, handlePos) = handleInfo.Value;
                var distToHandle = Math.Sqrt(Math.Pow(world.X - handlePos.X, 2) + Math.Pow(world.Y - handlePos.Y, 2));

                if (distToHandle < 20)
                {
                    _isRotatingGroup = true;
                    _rotationCenter = center;
                    _rotationStartAngle = Math.Atan2(world.Y - center.Y, world.X - center.X);
                    _rotationStartRotations.Clear();
                    _rotationStartPositions.Clear();

                    foreach (var trackId in _viewModel.SelectedTrackIds)
                    {
                        if (_viewModel.Rotations.TryGetValue(trackId, out var rot))
                            _rotationStartRotations[trackId] = rot;
                        if (_viewModel.Positions.TryGetValue(trackId, out var pos2))
                            _rotationStartPositions[trackId] = pos2;
                    }

                    GraphCanvas.CapturePointer(e.Pointer);
                    StatusText.Text = "Rotating group...";
                    return;
                }
            }

            var singleHandleInfo = GetSingleRotationHandleInfo();
            if (singleHandleInfo.HasValue)
            {
                var (center, handlePos) = singleHandleInfo.Value;
                var distToHandle = Math.Sqrt(Math.Pow(world.X - handlePos.X, 2) + Math.Pow(world.Y - handlePos.Y, 2));

                if (distToHandle < 20)
                {
                    _isRotatingGroup = true;
                    _rotationCenter = center;
                    _rotationStartAngle = Math.Atan2(world.Y - center.Y, world.X - center.X);
                    _rotationStartRotations.Clear();
                    _rotationStartPositions.Clear();

                    foreach (var trackId in _viewModel.SelectedTrackIds)
                    {
                        if (_viewModel.Rotations.TryGetValue(trackId, out var rot))
                            _rotationStartRotations[trackId] = rot;
                        if (_viewModel.Positions.TryGetValue(trackId, out var pos2))
                            _rotationStartPositions[trackId] = pos2;
                    }

                    GraphCanvas.CapturePointer(e.Pointer);
                    StatusText.Text = "Rotating track...";
                    return;
                }
            }

            if (clickCount >= 3)
            {
                var hit = _viewModel.HitTest(world, hitRadius: 40.0);
                if (hit.HasValue)
                {
                    _viewModel.SelectConnectedGroup(hit.Value);
                    StatusText.Text = $"Selected {_viewModel.SelectedTrackIds.Count} connected tracks";
                    RenderGraph();
                    UpdatePropertiesPanel();
                    return;
                }
            }

            if (clickCount == 2)
            {
                var hit = _viewModel.HitTest(world, hitRadius: 40.0);
                if (hit.HasValue)
                {
                    var edge = _viewModel.Graph.Edges.FirstOrDefault(e => e.Id == hit.Value);
                    var template = edge != null ? _catalog.GetById(edge.TemplateId) : null;

                    if (template?.Geometry.GeometryKind == TrackGeometryKind.Switch)
                    {
                        _viewModel.ToggleSwitchBranch(hit.Value);
                        StatusText.Text = "Toggled switch branch";
                        RenderGraph();
                        return;
                    }
                }
            }

            if (isShiftPressed && _viewModel.SelectedTrackId.HasValue)
            {
                var hit = _viewModel.HitTest(world, hitRadius: 40.0);
                if (hit.HasValue && hit.Value != _viewModel.SelectedTrackId.Value)
                {
                    _viewModel.SelectPathBetween(_viewModel.SelectedTrackId.Value, hit.Value);
                    StatusText.Text = $"Selected path with {_viewModel.SelectedTrackIds.Count} tracks";
                    RenderGraph();
                    UpdatePropertiesPanel();
                    return;
                }
            }

            _viewModel.PointerDown(world, true, isCtrlPressed);
            GraphCanvas.CapturePointer(e.Pointer);
            RenderGraph();
            UpdatePropertiesPanel();
        }
    }

    private void GraphCanvas_PointerMoved(object sender, PointerRoutedEventArgs e)
    {
        var p = e.GetCurrentPoint(GraphCanvas);
        var pos = p.Position;

        CoordinatesText.Text = $"X: {pos.X:F0}  Y: {pos.Y:F0}";

        if (_isPanning)
        {
            var deltaX = pos.X - _panStart.X;
            var deltaY = pos.Y - _panStart.Y;

            CanvasScrollViewer.ChangeView(
                _panScrollHorizontalStart - deltaX,
                _panScrollVerticalStart - deltaY,
                null,
                disableAnimation: true);

            e.Handled = true;
            return;
        }

        if (_isRotatingGroup)
        {
            var world = new Point2D(pos.X / DisplayScale, pos.Y / DisplayScale);
            var currentAngle = Math.Atan2(world.Y - _rotationCenter.Y, world.X - _rotationCenter.X);
            var deltaAngle = (currentAngle - _rotationStartAngle) * 180.0 / Math.PI;

            foreach (var trackId in _viewModel.SelectedTrackIds)
            {
                if (_rotationStartRotations.TryGetValue(trackId, out var startRot))
                    _viewModel.Rotations[trackId] = NormalizeDeg(startRot + deltaAngle);

                if (_rotationStartPositions.TryGetValue(trackId, out var startPos))
                {
                    var dx = startPos.X - _rotationCenter.X;
                    var dy = startPos.Y - _rotationCenter.Y;
                    var deltaRad = deltaAngle * Math.PI / 180.0;
                    var newX = _rotationCenter.X + dx * Math.Cos(deltaRad) - dy * Math.Sin(deltaRad);
                    var newY = _rotationCenter.Y + dx * Math.Sin(deltaRad) + dy * Math.Cos(deltaRad);
                    _viewModel.Positions[trackId] = new Point2D(newX, newY);
                }
            }

            StatusText.Text = $"Rotating: {deltaAngle:F1}°";
            RenderGraph();
            return;
        }

        var worldPos = new Point2D(pos.X / DisplayScale, pos.Y / DisplayScale);
        _viewModel.PointerMove(worldPos, GridToggle.IsChecked == true, GridSize, SnapDistance);
        RenderGraph();
    }

    private static double NormalizeDeg(double deg)
    {
        while (deg < 0) deg += 360;
        while (deg >= 360) deg -= 360;
        return deg;
    }

    private void GraphCanvas_PointerReleased(object sender, PointerRoutedEventArgs e)
    {
        var point = e.GetCurrentPoint(GraphCanvas);

        if (_isPanning)
        {
            _isPanning = false;
            GraphCanvas.ReleasePointerCaptures();
            ProtectedCursor = null;

            var deltaX = Math.Abs(point.Position.X - _panStart.X);
            var deltaY = Math.Abs(point.Position.Y - _panStart.Y);
            if (deltaX < 5 && deltaY < 5)
                ShowContextMenu(point.Position);

            e.Handled = true;
            return;
        }

        if (_isRotatingGroup)
        {
            _isRotatingGroup = false;
            _rotationStartRotations.Clear();
            _rotationStartPositions.Clear();
            GraphCanvas.ReleasePointerCaptures();
            StatusText.Text = "Rotation complete";
            RenderGraph();
            UpdateStatistics();
            return;
        }

        var pos = point.Position;
        var world = new Point2D(pos.X / DisplayScale, pos.Y / DisplayScale);
        var isCtrlPressed = Microsoft.UI.Input.KeyboardHelper.GetKeyState(Windows.System.VirtualKey.Control)
            .HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);

        var status = _viewModel.PointerUp(
            world,
            snapEnabled: SnapToggle.IsChecked == true,
            snapDistance: SnapDistance,
            gridSnap: GridToggle.IsChecked == true,
            gridSize: GridSize,
            isCtrlPressed: isCtrlPressed);

        StatusText.Text = status;
        GraphCanvas.ReleasePointerCaptures();

        RenderGraph();
        UpdateStatistics();
        UpdatePropertiesPanel();
    }

    private void GraphCanvas_PointerWheelChanged(object sender, PointerRoutedEventArgs e)
    {
        UpdateStatistics();
    }

    private void UpdatePropertiesPanel()
    {
        CreateSectionButton.IsEnabled = _viewModel.SelectedTrackIds.Count > 0;
        ToggleIsolatorButton.IsEnabled = _viewModel.SelectedTrackId.HasValue;

        if (_viewModel.SelectedTrackId.HasValue)
        {
            var section = _viewModel.GetSectionForTrack(_viewModel.SelectedTrackId.Value);
            SectionInfoText.Text = section is not null
                ? $"Section: {section.Name}"
                : "No section assigned";
        }
        else
        {
            SectionInfoText.Text = _viewModel.SelectedTrackIds.Count > 0
                ? $"{_viewModel.SelectedTrackIds.Count} tracks selected"
                : "No section assigned";
        }

        if (_viewModel.SelectedTrackId is null)
        {
            PropertiesPanel.Visibility = Visibility.Collapsed;
            SelectionInfoText.Text = _viewModel.SelectedTrackIds.Count > 0
                ? $"{_viewModel.SelectedTrackIds.Count} tracks selected"
                : "No section assigned";
            return;
        }

        var id = _viewModel.SelectedTrackId.Value;
        var edge = _viewModel.Graph.Edges.First(e => e.Id == id);

        PropertiesPanel.Visibility = Visibility.Visible;
        SelectionInfoText.Text = $"Selected: {edge.TemplateId}";

        TrackIdTextBox.Text = id.ToString()[..8];
        TemplateIdTextBox.Text = edge.TemplateId;

        if (_viewModel.Positions.TryGetValue(id, out var pos))
        {
            PositionXBox.Value = pos.X;
            PositionYBox.Value = pos.Y;
        }

        if (_viewModel.Rotations.TryGetValue(id, out var rot))
            RotationBox.Value = rot;

        FeedbackPointBox.Value = edge.FeedbackPointNumber ?? double.NaN;
    }

    private void PositionBox_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
    {
        if (_viewModel.SelectedTrackId is null || double.IsNaN(args.NewValue))
            return;

        var id = _viewModel.SelectedTrackId.Value;
        var pos = _viewModel.Positions[id];

        if (sender == PositionXBox)
            _viewModel.Positions[id] = new Point2D(args.NewValue, pos.Y);
        else
            _viewModel.Positions[id] = new Point2D(pos.X, args.NewValue);

        RenderGraph();
    }

    private void RotationBox_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
    {
        if (_viewModel.SelectedTrackId is null || double.IsNaN(args.NewValue))
            return;

        _viewModel.Rotations[_viewModel.SelectedTrackId.Value] = args.NewValue;
        RenderGraph();
    }

    private void FeedbackPointBox_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
    {
        if (_viewModel.SelectedTrackId is null)
            return;

        var edge = _viewModel.Graph.Edges.First(e => e.Id == _viewModel.SelectedTrackId);
        edge.FeedbackPointNumber = double.IsNaN(args.NewValue) ? null : (int)args.NewValue;

        RenderGraph();
    }

    private void DeleteSelected_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel.SelectedTrackIds.Count == 0)
            return;

        // Get count before deletion
        int count = _viewModel.SelectedTrackIds.Count;

        // Remove all selected tracks
        foreach (var id in _viewModel.SelectedTrackIds.ToList())
        {
            _viewModel.RemoveTrack(id);
        }

        _viewModel.ClearSelection();

        StatusText.Text = count == 1
            ? "Track deleted"
            : $"Deleted {count} tracks";

        UpdateStatistics();
        RenderGraph();
        UpdatePropertiesPanel();
    }

    private void DisconnectSelected_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel.SelectedTrackIds.Count == 0)
            return;

        var service = _viewModel.ConnectionService;
        int totalDisconnected = 0;

        // Disconnect all ports for all selected tracks
        foreach (var id in _viewModel.SelectedTrackIds)
        {
            var edge = _viewModel.Graph.Edges.FirstOrDefault(e => e.Id == id);
            if (edge is null) continue;

            foreach (var port in edge.Connections.Keys.ToList())
            {
                if (service.IsPortConnected(id, port))
                {
                    service.Disconnect(id, port);
                    totalDisconnected++;
                }
            }
        }

        StatusText.Text = totalDisconnected == 0
            ? "No connections to disconnect"
            : totalDisconnected == 1
                ? "Disconnected 1 port"
                : $"Disconnected {totalDisconnected} ports";

        RenderGraph();
    }

    private void CreateSection_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel.SelectedTrackIds.Count == 0)
            return;

        var sectionCount = _viewModel.Graph.Sections.Count + 1;
        var section = _viewModel.CreateSectionFromSelection(
            $"Block {sectionCount}",
            GetSectionColor(sectionCount));

        if (section is not null)
        {
            StatusText.Text = $"Created section '{section.Name}' with {section.TrackIds.Count} track(s)";
            UpdateSectionsList();
            RenderGraph();
        }
    }

    private void ToggleIsolator_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel.SelectedTrackId is null)
            return;

        var edgeId = _viewModel.SelectedTrackId.Value;
        var edge = _viewModel.Graph.Edges.FirstOrDefault(e => e.Id == edgeId);
        if (edge is null) return;

        var template = _catalog.GetById(edge.TemplateId);
        if (template is null) return;

        var portId = template.Ends.FirstOrDefault()?.Id ?? "A";
        var hasIsolator = _viewModel.ToggleIsolator(edgeId, portId);

        StatusText.Text = hasIsolator
            ? $"Added isolator at {edge.TemplateId}.{portId}"
            : $"Removed isolator at {edge.TemplateId}.{portId}";

        RenderGraph();
    }

    private Section? _editingSection;

    private void SectionsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var selected = SectionsList.SelectedItem;
        if (selected is null)
        {
            SectionEditorPanel.Visibility = Visibility.Collapsed;
            return;
        }

        var index = SectionsList.SelectedIndex;
        if (index >= 0 && index < _viewModel.Graph.Sections.Count)
        {
            var section = _viewModel.Graph.Sections[index];
            _editingSection = section;

            SectionNameBox.Text = section.Name;

            foreach (ComboBoxItem item in SectionFunctionCombo.Items)
            {
                if (item.Tag?.ToString() == section.Function)
                {
                    SectionFunctionCombo.SelectedItem = item;
                    break;
                }
            }

            SectionEditorPanel.Visibility = Visibility.Visible;

            _viewModel.SelectedTrackIds.Clear();
            foreach (var trackId in section.TrackIds)
                _viewModel.SelectedTrackIds.Add(trackId);
            RenderGraph();
        }
    }

    private void SectionNameBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_editingSection is null) return;
        _editingSection.Name = SectionNameBox.Text;
        UpdateSectionsList();
        RenderGraph();
    }

    private void SectionFunctionCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_editingSection is null) return;
        if (SectionFunctionCombo.SelectedItem is ComboBoxItem item)
            _editingSection.Function = item.Tag?.ToString() ?? "Track";
    }

    private void SectionColorButton_Click(object sender, RoutedEventArgs e)
    {
        if (_editingSection is null) return;
        if (sender is Button button && button.Tag is string color)
        {
            _editingSection.Color = color;
            UpdateSectionsList();
            RenderGraph();
        }
    }

    private void DeleteSection_Click(object sender, RoutedEventArgs e)
    {
        if (_editingSection is null) return;

        _viewModel.Graph.Sections.Remove(_editingSection);
        _editingSection = null;
        SectionEditorPanel.Visibility = Visibility.Collapsed;
        UpdateSectionsList();
        RenderGraph();
        StatusText.Text = "Section deleted";
    }

    private void UpdateSectionsList()
    {
        var sectionItems = _viewModel.Graph.Sections.Select(s => new
        {
            s.Name,
            Color = new SolidColorBrush(ParseColor(s.Color)),
            TrackCount = $"{s.TrackIds.Count} tracks"
        }).ToList();

        SectionsList.ItemsSource = sectionItems;
    }

    private static string GetSectionColor(int index)
    {
        var colors = new[] { "#0078D4", "#107C10", "#FFB900", "#E81123", "#B4009E", "#00B294", "#FF8C00" };
        return colors[(index - 1) % colors.Length];
    }

    private static Color ParseColor(string hex)
    {
        hex = hex.TrimStart('#');
        if (hex.Length == 6)
        {
            return Color.FromArgb(255,
                byte.Parse(hex[..2], NumberStyles.HexNumber),
                byte.Parse(hex[2..4], NumberStyles.HexNumber),
                byte.Parse(hex[4..6], NumberStyles.HexNumber));
        }
        return Colors.Gray;
    }

    private void ValidateButton_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.Validate();
        Violations.Clear();

        foreach (var v in _viewModel.Violations)
            Violations.Add(v);

        StatusText.Text = Violations.Count == 0
            ? "Validation passed"
            : $"Found {Violations.Count} issue(s)";
    }

    private void ZoomFit_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel.Positions.Count == 0)
        {
            CanvasScrollViewer.ChangeView(0, 0, 1.0f);
            return;
        }

        var xs = _viewModel.Positions.Values.Select(p => p.X * DisplayScale).ToList();
        var ys = _viewModel.Positions.Values.Select(p => p.Y * DisplayScale).ToList();

        var minX = xs.Min() - 100;
        var maxX = xs.Max() + 100;
        var minY = ys.Min() - 100;
        var maxY = ys.Max() + 100;

        var contentWidth = maxX - minX;
        var contentHeight = maxY - minY;

        var zoomX = CanvasScrollViewer.ViewportWidth / contentWidth;
        var zoomY = CanvasScrollViewer.ViewportHeight / contentHeight;
        var zoom = (float)Math.Min(zoomX, zoomY);

        zoom = Math.Clamp(zoom, 0.1f, 2.0f);

        CanvasScrollViewer.ChangeView(minX, minY, zoom);
        UpdateStatistics();
    }

    private void ZoomReset_Click(object sender, RoutedEventArgs e)
    {
        CanvasScrollViewer.ChangeView(null, null, 1.0f);
        UpdateStatistics();
    }

    private void RenderGraph()
    {
        GraphCanvas.Children.Clear();

        if (GridToggle.IsChecked == true)
            RenderGrid();

        _renderer.Render(
            GraphCanvas,
            _viewModel,
            _catalog,
            _trackBrush,
            _trackSelectedBrush,
            _trackHoverBrush);

        if (_viewModel.GhostPlacement is { } ghostPlacement)
        {
            var ghostTemplate = _catalog.GetById(ghostPlacement.TemplateId);
            if (ghostTemplate is not null)
            {
                var ghostPosition = ghostPlacement.Position;
                var ghostRotation = ghostPlacement.RotationDeg;

                if (_viewModel.CurrentSnapPreview is { } preview)
                {
                    ghostPosition = preview.PreviewPosition;
                    ghostRotation = preview.PreviewRotation;
                }

                _renderer.RenderGhostTrack(
                    GraphCanvas,
                    ghostTemplate,
                    ghostPosition,
                    ghostRotation,
                    _snapPreviewBrush);
            }
        }

        var dragPreview = _viewModel.GetCurrentDragPreviewPose();
        if (dragPreview is { } dp)
        {
            _renderer.RenderGhostTrack(
                GraphCanvas,
                dp.Template,
                dp.Position,
                dp.RotationDeg,
                _snapPreviewBrush);
        }

        RenderSingleRotationHandle();
        RenderPorts();
        RenderFeedback();
        RenderIsolators();
        RenderSectionLabels();
        RenderSnapPreview();
        RenderSelectionRectangle();
        RenderSelectionBoundingBox();
    }

    private void RenderSingleRotationHandle()
    {
        if (_viewModel.SelectedTrackIds.Count != 1 || !_viewModel.SelectedTrackId.HasValue)
            return;

        var id = _viewModel.SelectedTrackId.Value;
        if (!_viewModel.Positions.TryGetValue(id, out var pos))
            return;

        var startX = pos.X * DisplayScale;
        var startY = pos.Y * DisplayScale;
        var endY = startY - SingleRotationHandleLength * DisplayScale;

        var line = new Line
        {
            X1 = startX,
            Y1 = startY,
            X2 = startX,
            Y2 = endY,
            Stroke = _trackSelectedBrush,
            StrokeThickness = 2
        };

        var circle = new Ellipse
        {
            Width = 18,
            Height = 18,
            Fill = _trackSelectedBrush,
            Stroke = new SolidColorBrush(Colors.White),
            StrokeThickness = 2
        };

        Canvas.SetLeft(circle, startX - 9);
        Canvas.SetTop(circle, endY - 9);

        GraphCanvas.Children.Add(line);
        GraphCanvas.Children.Add(circle);
    }

    private void RenderGrid()
    {
        double w = GraphCanvas.Width;
        double h = GraphCanvas.Height;

        for (double x = 0; x <= w; x += GridSize)
        {
            GraphCanvas.Children.Add(new Line
            {
                X1 = x,
                Y1 = 0,
                X2 = x,
                Y2 = h,
                Stroke = _gridBrush,
                StrokeThickness = 1
            });
        }

        for (double y = 0; y <= h; y += GridSize)
        {
            GraphCanvas.Children.Add(new Line
            {
                X1 = 0,
                Y1 = y,
                X2 = w,
                Y2 = y,
                Stroke = _gridBrush,
                StrokeThickness = 1
            });
        }
    }

    private void RenderPorts()
    {
        var service = _viewModel.ConnectionService;

        foreach (var edge in _viewModel.Graph.Edges)
        {
            if (!_viewModel.Positions.TryGetValue(edge.Id, out var pos))
                continue;

            var rot = _viewModel.Rotations.GetValueOrDefault(edge.Id, 0);
            var template = _catalog.GetById(edge.TemplateId);
            if (template is null)
                continue;

            foreach (var end in template.Ends)
            {
                var offset = GetPortOffset(template, end.Id, rot);
                var x = (pos.X + offset.X) * DisplayScale;
                var y = (pos.Y + offset.Y) * DisplayScale;

                bool connected = service.IsPortConnected(edge.Id, end.Id);

                var ellipse = new Ellipse
                {
                    Width = PortRadius * 2,
                    Height = PortRadius * 2,
                    Fill = connected ? _portConnectedBrush : _portOpenBrush,
                    Stroke = new SolidColorBrush(Colors.White),
                    StrokeThickness = 2
                };

                Canvas.SetLeft(ellipse, x - PortRadius);
                Canvas.SetTop(ellipse, y - PortRadius);
                GraphCanvas.Children.Add(ellipse);
            }
        }
    }

    private void RenderFeedback()
    {
        foreach (var edge in _viewModel.Graph.Edges)
        {
            if (!edge.FeedbackPointNumber.HasValue)
                continue;

            var pos = _viewModel.Positions[edge.Id];
            var px = pos.X * DisplayScale;
            var py = pos.Y * DisplayScale;

            var dot = new Ellipse { Width = 16, Height = 16, Fill = _feedbackBrush };

            Canvas.SetLeft(dot, px - 8);
            Canvas.SetTop(dot, py + 20);
            GraphCanvas.Children.Add(dot);

            var label = new TextBlock
            {
                Text = edge.FeedbackPointNumber.Value.ToString(),
                FontSize = 10,
                Foreground = new SolidColorBrush(Colors.White),
                FontWeight = Microsoft.UI.Text.FontWeights.Bold
            };

            Canvas.SetLeft(label, px - 4);
            Canvas.SetTop(label, py + 22);
            GraphCanvas.Children.Add(label);
        }
    }

    private void RenderIsolators()
    {
        foreach (var isolator in _viewModel.Graph.Isolators)
        {
            var edge = _viewModel.Graph.Edges.FirstOrDefault(e => e.Id == isolator.EdgeId);
            if (edge is null) continue;

            var template = _catalog.GetById(edge.TemplateId);
            if (template is null) continue;

            if (!_viewModel.Positions.TryGetValue(edge.Id, out var pos)) continue;
            if (!_viewModel.Rotations.TryGetValue(edge.Id, out var rot)) continue;

            var portOffset = GetPortOffset(template, isolator.PortId, rot);
            var portX = (pos.X + portOffset.X) * DisplayScale;
            var portY = (pos.Y + portOffset.Y) * DisplayScale;

            var rotRad = rot * Math.PI / 180.0;
            var dirX = Math.Cos(rotRad);
            var dirY = Math.Sin(rotRad);
            var perpX = Math.Cos(rotRad + Math.PI / 2);
            var perpY = Math.Sin(rotRad + Math.PI / 2);

            double size = 6;
            double gap = 3;

            var tri1 = new Polygon
            {
                Fill = _feedbackBrush,
                Points = new PointCollection
                {
                    new Point(portX - gap - size * dirX + size * perpX, portY - gap - size * dirY + size * perpY),
                    new Point(portX - gap, portY - gap),
                    new Point(portX - gap - size * dirX - size * perpX, portY - gap - size * dirY - size * perpY)
                }
            };
            GraphCanvas.Children.Add(tri1);

            var tri2 = new Polygon
            {
                Fill = _feedbackBrush,
                Points = new PointCollection
                {
                    new Point(portX + gap + size * dirX + size * perpX, portY + gap + size * dirY + size * perpY),
                    new Point(portX + gap, portY + gap),
                    new Point(portX + gap + size * dirX - size * perpX, portY + gap + size * dirY - size * perpY)
                }
            };
            GraphCanvas.Children.Add(tri2);
        }
    }

    private void RenderSectionLabels()
    {
        foreach (var section in _viewModel.Graph.Sections)
        {
            if (section.TrackIds.Count == 0 || string.IsNullOrWhiteSpace(section.Name))
                continue;

            double sumX = 0, sumY = 0;
            int count = 0;
            foreach (var trackId in section.TrackIds)
            {
                if (_viewModel.Positions.TryGetValue(trackId, out var pos))
                {
                    sumX += pos.X;
                    sumY += pos.Y;
                    count++;
                }
            }

            if (count == 0) continue;

            var centerX = (sumX / count) * DisplayScale;
            var centerY = (sumY / count) * DisplayScale;

            var label = new TextBlock
            {
                Text = section.Name,
                FontSize = 14,
                FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                Foreground = new SolidColorBrush(ParseColor(section.Color)),
            };

            Canvas.SetLeft(label, centerX - 20);
            Canvas.SetTop(label, centerY - 25);
            GraphCanvas.Children.Add(label);
        }
    }

    private void RenderSelectionBoundingBox()
    {
        if (_viewModel.SelectedTrackIds.Count < 2)
            return;

        double minX = double.MaxValue, minY = double.MaxValue;
        double maxX = double.MinValue, maxY = double.MinValue;

        foreach (var trackId in _viewModel.SelectedTrackIds)
        {
            if (_viewModel.Positions.TryGetValue(trackId, out var pos))
            {
                var px = pos.X * DisplayScale;
                var py = pos.Y * DisplayScale;
                minX = Math.Min(minX, px - 30);
                minY = Math.Min(minY, py - 30);
                maxX = Math.Max(maxX, px + 30);
                maxY = Math.Max(maxY, py + 30);
            }
        }

        if (minX == double.MaxValue) return;

        var rect = new Rectangle
        {
            Width = maxX - minX,
            Height = maxY - minY,
            Stroke = new SolidColorBrush(Colors.LimeGreen),
            StrokeThickness = 2,
            StrokeDashArray = new DoubleCollection { 5, 3 },
            Fill = null
        };

        Canvas.SetLeft(rect, minX);
        Canvas.SetTop(rect, minY);
        GraphCanvas.Children.Add(rect);

        var handleX = (minX + maxX) / 2;
        var handleY = minY - 20;

        var handle = new Ellipse
        {
            Width = 12,
            Height = 12,
            Fill = new SolidColorBrush(Colors.LimeGreen),
            Stroke = new SolidColorBrush(Colors.White),
            StrokeThickness = 2
        };

        Canvas.SetLeft(handle, handleX - 6);
        Canvas.SetTop(handle, handleY - 6);
        GraphCanvas.Children.Add(handle);

        var handleLine = new Line
        {
            X1 = handleX,
            Y1 = handleY,
            X2 = handleX,
            Y2 = minY,
            Stroke = new SolidColorBrush(Colors.LimeGreen),
            StrokeThickness = 1
        };
        GraphCanvas.Children.Add(handleLine);
    }

    private void RenderSnapPreview()
    {
        var preview = _viewModel.CurrentSnapPreview;
        if (preview is null)
            return;

        var fromX = preview.MovingPortPosition.X * DisplayScale;
        var fromY = preview.MovingPortPosition.Y * DisplayScale;
        var toX = preview.TargetPortPosition.X * DisplayScale;
        var toY = preview.TargetPortPosition.Y * DisplayScale;

        // Connection line between ports
        var line = new Line
        {
            X1 = fromX,
            Y1 = fromY,
            X2 = toX,
            Y2 = toY,
            Stroke = _snapPreviewBrush,
            StrokeThickness = 3,
            StrokeDashArray = null,
            Opacity = 0.85
        };
        GraphCanvas.Children.Add(line);

        // Highlight moving port (which port will snap)
        var movingRing = new Ellipse
        {
            Width = PortRadius * 3,
            Height = PortRadius * 3,
            Stroke = _snapPreviewBrush,
            StrokeThickness = 2.5,
            Fill = new SolidColorBrush(_snapPreviewBrush.Color) { Opacity = 0.2 }
        };
        Canvas.SetLeft(movingRing, fromX - PortRadius * 1.5);
        Canvas.SetTop(movingRing, fromY - PortRadius * 1.5);
        GraphCanvas.Children.Add(movingRing);

        // Label for moving port
        var movingLabel = new TextBlock
        {
            Text = preview.MovingPortId,
            FontSize = 12,
            FontWeight = Microsoft.UI.Text.FontWeights.Bold,
            Foreground = _snapPreviewBrush,
            Opacity = 0.9
        };
        Canvas.SetLeft(movingLabel, fromX + 10);
        Canvas.SetTop(movingLabel, fromY - 10);
        GraphCanvas.Children.Add(movingLabel);

        // Highlight target port
        var targetRing = new Ellipse
        {
            Width = PortRadius * 3,
            Height = PortRadius * 3,
            Stroke = _snapPreviewBrush,
            StrokeThickness = 2.5,
            Fill = new SolidColorBrush(_snapPreviewBrush.Color) { Opacity = 0.2 }
        };
        Canvas.SetLeft(targetRing, toX - PortRadius * 1.5);
        Canvas.SetTop(targetRing, toY - PortRadius * 1.5);
        GraphCanvas.Children.Add(targetRing);

        // Label for target port
        var targetLabel = new TextBlock
        {
            Text = preview.TargetPortId,
            FontSize = 12,
            FontWeight = Microsoft.UI.Text.FontWeights.Bold,
            Foreground = _snapPreviewBrush,
            Opacity = 0.9
        };
        Canvas.SetLeft(targetLabel, toX + 10);
        Canvas.SetTop(targetLabel, toY - 10);
        GraphCanvas.Children.Add(targetLabel);

        var previewX = preview.PreviewPosition.X * DisplayScale;
        var previewY = preview.PreviewPosition.Y * DisplayScale;

        var previewDot = new Ellipse
        {
            Width = 14,
            Height = 14,
            Fill = _snapPreviewBrush,
            Opacity = 0.7
        };

        Canvas.SetLeft(previewDot, previewX - 7);
        Canvas.SetTop(previewDot, previewY - 7);
        GraphCanvas.Children.Add(previewDot);
    }

    private void RenderSelectionRectangle()
    {
        var rect = _viewModel.SelectionRectangle;
        if (rect is null)
            return;

        var (start, end) = rect.Value;

        var x1 = start.X * DisplayScale;
        var y1 = start.Y * DisplayScale;
        var x2 = end.X * DisplayScale;
        var y2 = end.Y * DisplayScale;

        var left = Math.Min(x1, x2);
        var top = Math.Min(y1, y2);
        var width = Math.Abs(x2 - x1);
        var height = Math.Abs(y2 - y1);

        var selectionRect = new Rectangle
        {
            Width = width,
            Height = height,
            Stroke = _trackSelectedBrush,
            StrokeThickness = 1,
            StrokeDashArray = new DoubleCollection { 4, 2 },
            Fill = new SolidColorBrush(Color.FromArgb(30, 0, 120, 215))
        };

        Canvas.SetLeft(selectionRect, left);
        Canvas.SetTop(selectionRect, top);
        GraphCanvas.Children.Add(selectionRect);
    }

    private static Point2D GetPortOffset(TrackTemplate template, string portId, double rotationDeg)
    {
        var spec = template.Geometry;
        double rotRad = rotationDeg * Math.PI / 180.0;

        static Point2D Rot(Point2D p, double r) =>
            new(p.X * Math.Cos(r) - p.Y * Math.Sin(r),
                p.X * Math.Sin(r) + p.Y * Math.Cos(r));

        if (spec.GeometryKind == TrackGeometryKind.Straight)
        {
            double length = spec.LengthMm!.Value;
            var local = portId == "A" ? new Point2D(0, 0) : new Point2D(length, 0);
            return Rot(local, rotRad);
        }

        if (spec.GeometryKind == TrackGeometryKind.Curve)
        {
            double radius = spec.RadiusMm!.Value;
            double sweepRad = spec.AngleDeg!.Value * Math.PI / 180.0;

            if (portId == "A")
                return Rot(new Point2D(0, 0), rotRad);

            var endLocal = new Point2D(
                radius * Math.Sin(sweepRad),
                radius - radius * Math.Cos(sweepRad));

            return Rot(endLocal, rotRad);
        }

        if (spec.GeometryKind == TrackGeometryKind.Switch)
        {
            double length = spec.LengthMm!.Value;
            double radius = spec.RadiusMm!.Value;
            double sweepRad = spec.AngleDeg!.Value * Math.PI / 180.0;
            double junction = spec.JunctionOffsetMm ?? (length / 2.0);

            if (portId == "A")
                return Rot(new Point2D(0, 0), rotRad);

            if (portId == "B")
                return Rot(new Point2D(length, 0), rotRad);

            if (portId == "C")
            {
                var j = new Point2D(junction, 0);
                var center = new Point2D(j.X, j.Y + radius);
                double startAngle = Math.Atan2(j.Y - center.Y, j.X - center.X);

                var endLocal = new Point2D(
                    center.X + radius * Math.Cos(startAngle + sweepRad),
                    center.Y + radius * Math.Sin(startAngle + sweepRad));

                var rel = new Point2D(endLocal.X - 0.0, endLocal.Y - 0.0);
                return Rot(rel, rotRad);
            }
        }

        return new Point2D(0, 0);
    }
}
