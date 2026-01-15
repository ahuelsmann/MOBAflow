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
using Moba.TrackPlan.Renderer.World;
using Moba.TrackPlan.Service;
using Moba.TrackPlan.TrackSystem;
using Moba.WinUI.Rendering;

using System.Collections.ObjectModel;
using System.Globalization;

using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.UI;

namespace Moba.WinUI.View;

public sealed partial class TrackPlanEditorPage : Page
{
    private const double DisplayScale = 0.5;
    private const double SnapDistance = 30.0;
    private const double GridSize = 50.0;
    private const double PortRadius = 8.0;

    private readonly TrackPlanEditorViewModel _viewModel;
    private readonly ITrackCatalog _catalog = new PikoATrackCatalog();
    private readonly CanvasRenderer _renderer = new();

    // Pan state (right mouse button)
    private bool _isPanning;
    private Point _panStart;
    private double _panScrollHorizontalStart;
    private double _panScrollVerticalStart;

    // Click tracking for double/triple click detection
    private DateTime _lastClickTime;
    private Point _lastClickPosition;
    private int _clickCount;
    private const double ClickTimeThreshold = 400; // ms
    private const double ClickDistanceThreshold = 10; // pixels

    // Rotation handle tracking
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

    public TrackPlanEditorPage(TrackPlanEditorViewModel viewModel)
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
        bool dark = ActualTheme == ElementTheme.Dark ||
                    (ActualTheme == ElementTheme.Default &&
                     App.Current.RequestedTheme == ApplicationTheme.Dark);

        if (dark)
        {
            _trackBrush = new SolidColorBrush(Colors.Silver);
            _trackSelectedBrush = new SolidColorBrush(Colors.DeepSkyBlue);
            _trackHoverBrush = new SolidColorBrush(Colors.LightSkyBlue);
            _portOpenBrush = new SolidColorBrush(Colors.Orange);
            _portConnectedBrush = new SolidColorBrush(Colors.LimeGreen);
            _gridBrush = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255));
            _feedbackBrush = new SolidColorBrush(Colors.Red);
            _snapPreviewBrush = new SolidColorBrush(Colors.Yellow);
        }
        else
        {
            _trackBrush = new SolidColorBrush(Colors.DimGray);
            _trackSelectedBrush = new SolidColorBrush(Colors.Blue);
            _trackHoverBrush = new SolidColorBrush(Colors.CornflowerBlue);
            _portOpenBrush = new SolidColorBrush(Colors.DarkOrange);
            _portConnectedBrush = new SolidColorBrush(Colors.Green);
                    _gridBrush = new SolidColorBrush(Color.FromArgb(40, 0, 0, 0));
                    _feedbackBrush = new SolidColorBrush(Colors.DarkRed);
                    _snapPreviewBrush = new SolidColorBrush(Colors.Gold);
                }
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
                        // Ctrl+A: Select all tracks
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

    // --------------------------------------------------------------------
    // Toolbox Drag & Drop
    // --------------------------------------------------------------------

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

    private void GraphCanvas_DragOver(object sender, DragEventArgs e)
    {
        e.AcceptedOperation = DataPackageOperation.Copy;
        e.DragUIOverride.Caption = "Drop to place track";
    }

    private async void GraphCanvas_Drop(object sender, DragEventArgs e)
    {
        if (!e.DataView.Contains(StandardDataFormats.Text))
            return;

        var templateId = await e.DataView.GetTextAsync();
        var pos = e.GetPosition(GraphCanvas);

        var world = new Point2D(pos.X / DisplayScale, pos.Y / DisplayScale);

        var status = _viewModel.DropTrack(
            templateId,
            world,
            gridSnap: GridToggle.IsChecked == true,
            gridSize: GridSize,
                    snapEnabled: SnapToggle.IsChecked == true,
                    snapDistance: SnapDistance,
                    out _);

                StatusText.Text = status;

                UpdateStatistics();
                RenderGraph();
                UpdatePropertiesPanel();
            }

            // --------------------------------------------------------------------
            // Context Menu
            // --------------------------------------------------------------------

            private void ShowContextMenu(Point pos)
            {
                var world = new Point2D(pos.X / DisplayScale, pos.Y / DisplayScale);
                var hit = _viewModel.HitTest(world, hitRadius: 40.0);

                var menu = new MenuFlyout();

                if (hit.HasValue)
                {
                    // Select the track under cursor if not already selected
                    if (_viewModel.SelectedTrackId != hit.Value)
                    {
                        _viewModel.SelectedTrackIds.Clear();
                        _viewModel.SelectedTrackIds.Add(hit.Value);
                    }

                    var edge = _viewModel.Graph.Edges.FirstOrDefault(e => e.Id == hit.Value);
                    var template = edge != null ? _catalog.GetById(edge.TemplateId) : null;

                    menu.Items.Add(CreateMenuItem("Delete", Symbol.Delete, () => {
                        _viewModel.RemoveTrack(hit.Value);
                        UpdateStatistics();
                        RenderGraph();
                        UpdatePropertiesPanel();
                    }));

                    menu.Items.Add(CreateMenuItem("Rotate 15°", Symbol.Rotate, () => {
                        if (_viewModel.Rotations.TryGetValue(hit.Value, out var rot))
                        {
                            _viewModel.Rotations[hit.Value] = (rot + 15) % 360;
                            RenderGraph();
                        }
                    }));

                    menu.Items.Add(CreateMenuItem("Disconnect", Symbol.Cancel, () => {
                        DisconnectSelected_Click(this, new RoutedEventArgs());
                    }));

                    menu.Items.Add(new MenuFlyoutSeparator());

                    // Isolator submenu for each port
                    if (template != null)
                    {
                        foreach (var end in template.Ends)
                        {
                            var portId = end.Id;
                            var hasIsolator = _viewModel.HasIsolator(hit.Value, portId);
                            var label = hasIsolator ? $"Remove Isolator ({portId})" : $"Add Isolator ({portId})";

                            menu.Items.Add(CreateMenuItem(label, Symbol.TwoPage, () => {
                                _viewModel.ToggleIsolator(hit.Value, portId);
                                RenderGraph();
                            }));
                        }
                    }

                    menu.Items.Add(new MenuFlyoutSeparator());

                    menu.Items.Add(CreateMenuItem("Create Section", Symbol.Highlight, () => {
                        CreateSection_Click(this, new RoutedEventArgs());
                    }));

                    menu.Items.Add(CreateMenuItem("Select Connected", Symbol.SelectAll, () => {
                        _viewModel.SelectConnectedGroup(hit.Value);
                        StatusText.Text = $"Selected {_viewModel.SelectedTrackIds.Count} connected tracks";
                        RenderGraph();
                        UpdatePropertiesPanel();
                    }));
                }
                else
                {
                    // Empty space clicked
                    menu.Items.Add(CreateMenuItem("Clear Selection", Symbol.Clear, () => {
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

            // --------------------------------------------------------------------
            // Pointer Events
            // --------------------------------------------------------------------

    private int DetectClickCount(Point pos)
    {
        var now = DateTime.Now;
        var timeDiff = (now - _lastClickTime).TotalMilliseconds;
        var distDiff = Math.Sqrt(Math.Pow(pos.X - _lastClickPosition.X, 2) + Math.Pow(pos.Y - _lastClickPosition.Y, 2));

        if (timeDiff < ClickTimeThreshold && distDiff < ClickDistanceThreshold)
        {
            _clickCount++;
        }
        else
        {
            _clickCount = 1;
        }

        _lastClickTime = now;
        _lastClickPosition = pos;

            return _clickCount;
        }

        /// <summary>
        /// Gets the rotation handle position for the current selection bounding box.
        /// Returns null if there are fewer than 2 selected tracks.
        /// </summary>
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
            var handleY = minY - 40; // 20px above box in world coordinates

            return (new Point2D(centerX, centerY), new Point2D(centerX, handleY));
        }

        private void GraphCanvas_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            var p = e.GetCurrentPoint(GraphCanvas);
            var pos = p.Position;

            // Right mouse button: Start panning
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

                // Check for rotation handle hit first
                var handleInfo = GetRotationHandleInfo();
                if (handleInfo.HasValue)
                {
                    var (center, handlePos) = handleInfo.Value;
                    var distToHandle = Math.Sqrt(Math.Pow(world.X - handlePos.X, 2) + Math.Pow(world.Y - handlePos.Y, 2));

                        if (distToHandle < 20) // 20 world units hit radius for handle
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

                // Triple-click: select all connected tracks
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

            // Double-click: select switch branch or parallel track (for switches/crossings)
            if (clickCount == 2)
            {
                var hit = _viewModel.HitTest(world, hitRadius: 40.0);
                if (hit.HasValue)
                {
                    var edge = _viewModel.Graph.Edges.FirstOrDefault(e => e.Id == hit.Value);
                    var template = edge != null ? _catalog.GetById(edge.TemplateId) : null;

                    // For switches: toggle which branch is "selected" for routing
                    if (template?.Geometry.GeometryKind == TrackGeometryKind.Switch)
                    {
                        _viewModel.ToggleSwitchBranch(hit.Value);
                        StatusText.Text = "Toggled switch branch";
                        RenderGraph();
                        return;
                    }
                }
            }

            // Shift+Click: select path between last selected and clicked track
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

        // Handle panning (right mouse button)
        if (_isPanning)
        {
            var deltaX = pos.X - _panStart.X;
            var deltaY = pos.Y - _panStart.Y;

            // Pan in opposite direction of mouse movement (natural scrolling)
            CanvasScrollViewer.ChangeView(
                _panScrollHorizontalStart - deltaX,
                _panScrollVerticalStart - deltaY,
                null,
                disableAnimation: true);

            e.Handled = true;
            return;
        }

        // Handle rotation drag
        if (_isRotatingGroup)
        {
            var world = new Point2D(pos.X / DisplayScale, pos.Y / DisplayScale);
            var currentAngle = Math.Atan2(world.Y - _rotationCenter.Y, world.X - _rotationCenter.X);
            var deltaAngle = (currentAngle - _rotationStartAngle) * 180.0 / Math.PI;

            // Apply rotation to all selected tracks using original positions
            foreach (var trackId in _viewModel.SelectedTrackIds)
            {
                if (_rotationStartRotations.TryGetValue(trackId, out var startRot))
                {
                    _viewModel.Rotations[trackId] = NormalizeDeg(startRot + deltaAngle);
                }

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

        // Handle pan completion (right mouse button)
        if (_isPanning)
        {
            _isPanning = false;
            GraphCanvas.ReleasePointerCaptures();
            ProtectedCursor = null;

            // Show context menu only if we didn't move much (was a click, not a drag)
            var deltaX = Math.Abs(point.Position.X - _panStart.X);
            var deltaY = Math.Abs(point.Position.Y - _panStart.Y);
            if (deltaX < 5 && deltaY < 5)
            {
                ShowContextMenu(point.Position);
            }

            e.Handled = true;
            return;
        }

        // Handle rotation completion
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
        var isCtrlPressed = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Control)
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

    // --------------------------------------------------------------------
    // Selection & Properties
    // --------------------------------------------------------------------

    private void UpdatePropertiesPanel()
    {
        // Update section buttons based on selection
        CreateSectionButton.IsEnabled = _viewModel.SelectedTrackIds.Count > 0;
        ToggleIsolatorButton.IsEnabled = _viewModel.SelectedTrackId.HasValue;

        // Update section info
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
                : "No selection";
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
        if (_viewModel.SelectedTrackId is null)
            return;

        _viewModel.RemoveTrack(_viewModel.SelectedTrackId.Value);
        _viewModel.ClearSelection();

        UpdateStatistics();
        RenderGraph();
        UpdatePropertiesPanel();
    }

    private void DisconnectSelected_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel.SelectedTrackId is null)
            return;

        var id = _viewModel.SelectedTrackId.Value;
        var edge = _viewModel.Graph.Edges.First(e => e.Id == id);

        var service = _viewModel.ConnectionService;
        int count = 0;

        foreach (var port in edge.Connections.Keys.ToList())
        {
            if (service.IsPortConnected(id, port))
            {
                service.Disconnect(id, port);
                count++;
            }
        }

        StatusText.Text = count > 0
            ? $"Disconnected {count} port(s)"
            : "No connections";

        RenderGraph();
    }

    // --------------------------------------------------------------------
    // Section Management
    // --------------------------------------------------------------------

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

    private void SectionsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var selected = SectionsList.SelectedItem;
        if (selected is null)
        {
            SectionEditorPanel.Visibility = Visibility.Collapsed;
            return;
        }

        // Get the selected section
        var index = SectionsList.SelectedIndex;
        if (index >= 0 && index < _viewModel.Graph.Sections.Count)
        {
            var section = _viewModel.Graph.Sections[index];
            _editingSection = section;

            SectionNameBox.Text = section.Name;

            // Set function combo
            foreach (ComboBoxItem item in SectionFunctionCombo.Items)
            {
                if (item.Tag?.ToString() == section.Function)
                {
                    SectionFunctionCombo.SelectedItem = item;
                    break;
                }
            }

            SectionEditorPanel.Visibility = Visibility.Visible;

            // Highlight section tracks on canvas
            _viewModel.SelectedTrackIds.Clear();
            foreach (var trackId in section.TrackIds)
                _viewModel.SelectedTrackIds.Add(trackId);
            RenderGraph();
        }
    }

    private Section? _editingSection;

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
        {
            _editingSection.Function = item.Tag?.ToString() ?? "Track";
        }
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

    // --------------------------------------------------------------------
    // Toolbar
    // --------------------------------------------------------------------

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

    // --------------------------------------------------------------------
    // Rendering
    // --------------------------------------------------------------------

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

        RenderPorts();
        RenderFeedback();
        RenderIsolators();
        RenderSectionLabels();
        RenderSnapPreview();
        RenderSelectionRectangle();
        RenderSelectionBoundingBox();
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

                        var dot = new Ellipse
                        {
                            Width = 16,
                            Height = 16,
                            Fill = _feedbackBrush
                        };

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

                                // Draw isolator as two triangles pointing toward each other ▶◀
                                var rotRad = rot * Math.PI / 180.0;
                                var dirX = Math.Cos(rotRad);
                                var dirY = Math.Sin(rotRad);
                                var perpX = Math.Cos(rotRad + Math.PI / 2);
                                var perpY = Math.Sin(rotRad + Math.PI / 2);

                                double size = 6;
                                double gap = 3;

                                // Left triangle (pointing right) ▶
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

                                // Right triangle (pointing left) ◀
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

                                        // Calculate center position of section tracks
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

                                    // Draw green bounding box with dashed stroke
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

                                    // Draw rotation handle (circle outside the box)
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

                                    // Draw line connecting handle to box
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

                // Draw a dashed line from moving port to target port
                var fromX = preview.MovingPortPosition.X * DisplayScale;
                var fromY = preview.MovingPortPosition.Y * DisplayScale;
                var toX = preview.TargetPortPosition.X * DisplayScale;
                var toY = preview.TargetPortPosition.Y * DisplayScale;

                var line = new Line
                {
                    X1 = fromX,
                    Y1 = fromY,
                    X2 = toX,
                    Y2 = toY,
                    Stroke = _snapPreviewBrush,
                    StrokeThickness = 2,
                    StrokeDashArray = new DoubleCollection { 4, 2 }
                };
                GraphCanvas.Children.Add(line);

                // Highlight the target port with a larger ring
                var targetX = toX;
                var targetY = toY;

                var ring = new Ellipse
                {
                    Width = PortRadius * 3,
                    Height = PortRadius * 3,
                    Stroke = _snapPreviewBrush,
                    StrokeThickness = 3,
                    Fill = null
                };

                Canvas.SetLeft(ring, targetX - PortRadius * 1.5);
                Canvas.SetTop(ring, targetY - PortRadius * 1.5);
                GraphCanvas.Children.Add(ring);

                // Show preview position indicator (where the track will snap to)
                var previewX = preview.PreviewPosition.X * DisplayScale;
                var previewY = preview.PreviewPosition.Y * DisplayScale;

                var previewDot = new Ellipse
                {
                    Width = 12,
                    Height = 12,
                    Fill = _snapPreviewBrush,
                    Opacity = 0.6
                };

                    Canvas.SetLeft(previewDot, previewX - 6);
                    Canvas.SetTop(previewDot, previewY - 6);
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

            // --------------------------------------------------------------------
            // Port Offset Helper (UI only)
            // --------------------------------------------------------------------

            private static Point2D GetPortOffset(TrackTemplate template, string portId, double rotationDeg)
            {
                var spec = template.Geometry;
                double rotRad = rotationDeg * Math.PI / 180.0;

                static Point2D Rot(Point2D p, double r) =>
                    new(p.X * Math.Cos(r) - p.Y * Math.Sin(r),
               p.X * Math.Sin(r) + p.Y * Math.Cos(r));

        // Straight
        if (spec.GeometryKind == TrackGeometryKind.Straight)
        {
            double length = spec.LengthMm!.Value;
            var local = portId == "A"
                ? new Point2D(0, 0)
                : new Point2D(length, 0);

            return Rot(local, rotRad);
        }

        // Curve
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

        // Switch
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

                var rel = new Point2D(
                    endLocal.X - 0.0,
                    endLocal.Y - 0.0);

                return Rot(rel, rotRad);
            }
        }

                        return new Point2D(0, 0);
                    }
                }
