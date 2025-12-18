// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.WinUI.View;

using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Service;
using SharedUI.ViewModel;
using System.ComponentModel;
using System.Diagnostics;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.System;
using Windows.UI.Core;

/// <summary>
/// Track Plan Editor Page - Interactive drag & drop track planning.
/// Code-behind handles drag & drop operations (WinUI limitation - no good XAML Behavior support).
/// </summary>
public sealed partial class TrackPlanEditorPage : INotifyPropertyChanged
{
    #region Fields
    private readonly SnapToConnectService _snapService;
    private string _zoomLevelText = "100%";
    #endregion

    public TrackPlanEditorPage(TrackPlanEditorViewModel viewModel, SnapToConnectService snapService)
    {
        InitializeComponent();
        DataContext = viewModel;
        _snapService = snapService;
    }

    public TrackPlanEditorViewModel ViewModel => (TrackPlanEditorViewModel)DataContext;

    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Current zoom level display text.
    /// </summary>
    public string ZoomLevelText
    {
        get => _zoomLevelText;
        set
        {
            _zoomLevelText = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ZoomLevelText)));
        }
    }

    private string _mousePositionText = "X: 0  Y: 0";
    
    /// <summary>
    /// Current mouse position display text.
    /// </summary>
    public string MousePositionText
    {
        get => _mousePositionText;
        set
        {
            _mousePositionText = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(MousePositionText)));
        }
    }

    #region Keyboard Shortcuts
    private void Page_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        // Check for Ctrl modifier
        var ctrlPressed = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control)
            .HasFlag(CoreVirtualKeyStates.Down);

        switch (e.Key)
        {
            case VirtualKey.Delete:
                // Delete selected segment(s)
                if (ViewModel.DeleteSelectedCommand.CanExecute(null))
                    ViewModel.DeleteSelectedCommand.Execute(null);
                e.Handled = true;
                break;
                
            case VirtualKey.A when ctrlPressed:
                // Ctrl+A: Select all
                if (ViewModel.SelectAllCommand.CanExecute(null))
                    ViewModel.SelectAllCommand.Execute(null);
                e.Handled = true;
                break;
                
            case VirtualKey.Escape:
                // Deselect all segments
                if (ViewModel.DeselectAllCommand.CanExecute(null))
                    ViewModel.DeselectAllCommand.Execute(null);
                e.Handled = true;
                break;
        }
    }
    #endregion

    #region Zoom Controls
    private void ZoomSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
    {
        if (CanvasScrollViewer == null) return;

        
        var zoomFactor = (float)(e.NewValue / 100.0);
        CanvasScrollViewer.ChangeView(null, null, zoomFactor);
        ZoomLevelText = $"{e.NewValue:F0}%";
    }

    private void ZoomIn_Click(object sender, RoutedEventArgs e)
    {
        ZoomSlider.Value = Math.Min(ZoomSlider.Maximum, ZoomSlider.Value + 25);
    }

    private void ZoomOut_Click(object sender, RoutedEventArgs e)
    {
        ZoomSlider.Value = Math.Max(ZoomSlider.Minimum, ZoomSlider.Value - 25);
    }
    #endregion

    #region Drag & Drop (Track Library → Canvas)
    /// <summary>
    /// Start dragging a track template from the library.
    /// </summary>
    private void TrackLibrary_DragItemsStarting(object sender, DragItemsStartingEventArgs e)
    {
        Debug.WriteLine($"🔵 DragItemsStarting: Items.Count={e.Items.Count}");
        
        if (e.Items.Count > 0)
        {
            var item = e.Items[0];
            Debug.WriteLine($"🔵 First item type: {item?.GetType().Name}");
            
            if (item is TrackTemplateViewModel template)
            {
                Debug.WriteLine($"✅ Template found: {template.ArticleCode}");
                
                // Store template in drag data
                e.Data.Properties.Add("TrackTemplate", template);
                e.Data.RequestedOperation = DataPackageOperation.Link;
                
                Debug.WriteLine("✅ Drag operation set to LINK");
            }
            else
            {
                Debug.WriteLine($"❌ Item is not TrackTemplateViewModel: {item?.GetType().Name}");
            }
        }
        else
        {
            Debug.WriteLine("❌ No items in drag");
        }
    }

    /// <summary>
    /// Handle drag-over event on canvas (allow drop + show snap preview).
    /// </summary>
    private void TrackCanvas_DragOver(object sender, DragEventArgs e)
    {
        Debug.WriteLine($"🟡 DragOver - AllowedOperations: {e.AllowedOperations}");
        
        // Always accept Link operation for app-internal drag & drop
        e.AcceptedOperation = DataPackageOperation.Link;
        
        Debug.WriteLine("✅ DragOver - AcceptedOperation set to LINK");
        
        // Update snap preview during drag
        var position = e.GetPosition(TrackCanvas);
        UpdateSnapPreview(position);
    }

    /// <summary>
    /// Handle drop event on canvas (add track segment at drop position with snap-to-connect).
    /// </summary>
    private void TrackCanvas_Drop(object sender, DragEventArgs e)
    {
        Debug.WriteLine("🟢 DROP EVENT FIRED!");
        
        // Get drop position relative to canvas
        var position = e.GetPosition(TrackCanvas);
        Debug.WriteLine($"📍 Drop position: {position.X}, {position.Y}");

        // Get dragged template
        bool hasTrackTemplate = e.DataView.Properties.TryGetValue("TrackTemplate", out var templateObj);
        Debug.WriteLine($"🔍 DataView.Properties contains 'TrackTemplate': {hasTrackTemplate}");
        
        if (hasTrackTemplate && templateObj is TrackTemplateViewModel template)
        {
            Debug.WriteLine($"✅ TEMPLATE FOUND: {template.ArticleCode}");
            
            // Determine position (snap or drop position)
            double x = position.X;
            double y = position.Y;
            TrackSegmentViewModel? targetSegment = null;
            bool targetIsStart = false;
            
            // Try to snap to nearby segment endpoint
            var snapResult = _snapService.FindSnapEndpoint(
                position, 
                0, 
                ViewModel.PlacedSegments);
            
            if (snapResult.HasValue)
            {
                x = snapResult.Value.SnapPosition.X;
                y = snapResult.Value.SnapPosition.Y;
                
                // Find the segment we're snapping to
                foreach (var segment in ViewModel.PlacedSegments)
                {
                    var endpoints = _snapService.GetEndpoints(segment);
                    foreach (var ep in endpoints)
                    {
                        if (Math.Abs(ep.Position.X - x) < 1 && Math.Abs(ep.Position.Y - y) < 1)
                        {
                            targetSegment = segment;
                            targetIsStart = ep.IsStart;
                            break;
                        }
                    }
                    if (targetSegment != null) break;
                }
                
                Debug.WriteLine($"✅ Snapping to: ({x:F0}, {y:F0})");
            }
            
            // Add segment at the correct position (with PathData starting at that position)
            ViewModel.AddTrackSegmentAtPosition(template, x, y);
            
            // Create connection if snapped
            if (targetSegment != null && ViewModel.PlacedSegments.Count > 0)
            {
                var newSegment = ViewModel.PlacedSegments[^1];
                // The new segment's start point is at the snap position
                ViewModel.CreateConnection(newSegment, true, targetSegment, targetIsStart, x, y);
            }
            
            Debug.WriteLine($"✅ Segment placed at: ({x:F0}, {y:F0})");
        }
        else
        {
            Debug.WriteLine("❌ Template not found in drop data");
            Debug.WriteLine($"   templateObj is TrackTemplateViewModel: {templateObj is TrackTemplateViewModel}");
            Debug.WriteLine($"   templateObj type: {templateObj?.GetType().Name ?? "null"}");
        }
        
        // Hide snap preview
        ViewModel.IsSnapPreviewVisible = false;
        SnapPreviewPath.Visibility = Visibility.Collapsed;
    }
    #endregion

    #region Snap Preview (Visual Feedback)
    /// <summary>
    /// Update snap preview during drag operation.
    /// </summary>
    private void UpdateSnapPreview(Point position)
    {
        try
        {
            var snapTarget = _snapService.FindSnapTarget(position, ViewModel.PlacedSegments);
            
            if (snapTarget.HasValue)
            {
                // Create line geometry directly instead of parsing
                var lineGeometry = new LineGeometry
                {
                    StartPoint = position,
                    EndPoint = snapTarget.Value
                };
                
                SnapPreviewPath.Data = lineGeometry;
                SnapPreviewPath.Visibility = Visibility.Visible;
                Debug.WriteLine($"✅ Snap preview shown from {position} to {snapTarget.Value}");
            }
            else
            {
                SnapPreviewPath.Visibility = Visibility.Collapsed;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ UpdateSnapPreview Exception: {ex.Message}");
            SnapPreviewPath.Visibility = Visibility.Collapsed;
        }
    }
    #endregion

    #region Right-Click Pan
    private bool _isPanning;
    private Point _panStartPosition;
    private double _panStartHorizontalOffset;
    private double _panStartVerticalOffset;

    private void TrackCanvas_PointerPressed_Pan(object sender, PointerRoutedEventArgs e)
    {
        var pointer = e.GetCurrentPoint(TrackCanvas);

        // Right-click starts panning
        if (pointer.Properties.IsRightButtonPressed)
        {
            _isPanning = true;
            // Use position relative to ScrollViewer to avoid feedback loop
            _panStartPosition = e.GetCurrentPoint(CanvasScrollViewer).Position;
            _panStartHorizontalOffset = CanvasScrollViewer.HorizontalOffset;
            _panStartVerticalOffset = CanvasScrollViewer.VerticalOffset;
            TrackCanvas.CapturePointer(e.Pointer);
            e.Handled = true;
            Debug.WriteLine($"🖱️ Pan started at ({_panStartPosition.X:F0}, {_panStartPosition.Y:F0})");
        }
        // Left-click on empty canvas area deselects segments and shows layout properties
        else if (pointer.Properties.IsLeftButtonPressed && _draggingSegment == null)
        {
            // Clear all segment selections
            foreach (var s in ViewModel.PlacedSegments)
            {
                if (s.IsSelected)
                    s.IsSelected = false;
            }
            ViewModel.SelectedSegment = null;

            Debug.WriteLine("🔵 Canvas clicked - showing layout properties");
        }
    }

    private void TrackCanvas_PointerMoved_Pan(PointerRoutedEventArgs e)
    {
        if (!_isPanning) return;

        // Use position relative to ScrollViewer (fixed reference frame)
        var currentPosition = e.GetCurrentPoint(CanvasScrollViewer).Position;
        var deltaX = currentPosition.X - _panStartPosition.X;
        var deltaY = currentPosition.Y - _panStartPosition.Y;

        // Skip if movement is too small
        if (Math.Abs(deltaX) < 1 && Math.Abs(deltaY) < 1) return;

        // Calculate new scroll position
        var newHorizontalOffset = _panStartHorizontalOffset - deltaX;
        var newVerticalOffset = _panStartVerticalOffset - deltaY;

        // Apply scroll
        CanvasScrollViewer.ChangeView(
            newHorizontalOffset,
            newVerticalOffset,
            null,
            disableAnimation: true);

        // Update start position for next delta calculation (incremental panning)
        // This makes panning work even when scroll limits are reached
        _panStartPosition = currentPosition;
        _panStartHorizontalOffset = CanvasScrollViewer.HorizontalOffset;
        _panStartVerticalOffset = CanvasScrollViewer.VerticalOffset;
    }

    private void TrackCanvas_PointerReleased_Pan(PointerRoutedEventArgs e)
    {
        if (_isPanning)
        {
            _isPanning = false;
            TrackCanvas.ReleasePointerCapture(e.Pointer);
        }
    }
    #endregion

    #region Selection & Dragging
    private TrackSegmentViewModel? _draggingSegment;
    private Point _dragStartPosition;
    
    /// <summary>
    /// Handle click on track segment (select it and start drag).
    /// </summary>
    private void Segment_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (sender is FrameworkElement element && element.DataContext is TrackSegmentViewModel segment)
        {
            // Clear previous selection
            foreach (var s in ViewModel.PlacedSegments)
            {
                if (s.IsSelected)
                    s.IsSelected = false;
            }

            // Select clicked segment and prepare for drag
            segment.IsSelected = true;
            ViewModel.SelectedSegment = segment;
            _draggingSegment = segment;
            _dragStartPosition = e.GetCurrentPoint(TrackCanvas).Position;
            
            // Capture pointer on the CANVAS (not the element) for reliable drag tracking
            TrackCanvas.CapturePointer(e.Pointer);
            
            Debug.WriteLine($"🔵 Segment selected: {segment.ArticleCode} at ({segment.CenterX}, {segment.CenterY})");
            e.Handled = true;
        }
    }
    
    /// <summary>
    /// Handle pointer move on canvas for dragging selected segment or panning.
    /// Connected segments move together as a group.
    /// </summary>
    private void TrackCanvas_PointerMoved_Drag(object sender, PointerRoutedEventArgs e)
    {
        var currentPosition = e.GetCurrentPoint(TrackCanvas).Position;
        
        // Update mouse position display (convert to mm - 1px = 2mm at scale 0.5)
        var mmX = currentPosition.X * 2;
        var mmY = currentPosition.Y * 2;
        MousePositionText = $"X: {mmX:F0}mm  Y: {mmY:F0}mm";
        
        // Handle right-click panning
        if (_isPanning)
        {
            TrackCanvas_PointerMoved_Pan(e);
            e.Handled = true;
            return;
        }
        
        if (_draggingSegment != null && e.Pointer.IsInContact)
        {
            // Calculate delta movement
            var deltaX = currentPosition.X - _dragStartPosition.X;
            var deltaY = currentPosition.Y - _dragStartPosition.Y;
            
            // Move connected group (all segments connected to the dragged one)
            ViewModel.MoveConnectedGroup(_draggingSegment, deltaX, deltaY);
            
            // Update drag start for next move
            _dragStartPosition = currentPosition;
            
            // Check for snap target (exclude segments in the same group)
            var group = ViewModel.GetConnectedGroup(_draggingSegment);
            var otherSegments = ViewModel.PlacedSegments.Where(s => !group.Contains(s));
            var snapTarget = _snapService.FindSnapTarget(currentPosition, otherSegments);
            
            if (snapTarget.HasValue)
            {
                // Show snap preview
                var lineGeometry = new LineGeometry
                {
                    StartPoint = new Point(_draggingSegment.CenterX, _draggingSegment.CenterY),
                    EndPoint = snapTarget.Value
                };
                SnapPreviewPath.Data = lineGeometry;
                SnapPreviewPath.Visibility = Visibility.Visible;
            }
            else
            {
                SnapPreviewPath.Visibility = Visibility.Collapsed;
            }
            
            Debug.WriteLine($"📍 Dragging to ({_draggingSegment.CenterX:F0}, {_draggingSegment.CenterY:F0})");
            e.Handled = true;
        }
    }
    
    
    /// <summary>
    /// Handle pointer release on canvas - end drag/pan and snap if applicable.
    /// </summary>
    private void TrackCanvas_PointerReleased(object sender, PointerRoutedEventArgs e)
    {
        // Handle panning release
        if (_isPanning)
        {
            TrackCanvas_PointerReleased_Pan(e);
            e.Handled = true;
            return;
        }
        
        if (_draggingSegment != null)
        {
            _ = e.GetCurrentPoint(TrackCanvas).Position;

            // Try to snap to nearby segment endpoint
            var snapResult = _snapService.FindSnapEndpoint(
                new Point(_draggingSegment.CenterX, _draggingSegment.CenterY), 
                _draggingSegment.Rotation,
                ViewModel.PlacedSegments.Where(s => s != _draggingSegment));

            if (snapResult.HasValue)
            {
                // Calculate delta to snap position
                var deltaX = snapResult.Value.SnapPosition.X - _draggingSegment.CenterX;
                var deltaY = snapResult.Value.SnapPosition.Y - _draggingSegment.CenterY;

                // Move segment to snap position (updates both position and PathData)
                _draggingSegment.MoveBy(deltaX, deltaY);

                Debug.WriteLine($"✅ Snapped segment to ({snapResult.Value.SnapPosition.X:F0}, {snapResult.Value.SnapPosition.Y:F0})");
            }

            // Try to auto-connect with nearby segments (works for both new and reconnecting segments)
            ViewModel.AutoConnectFromEndpoints(_draggingSegment);

            TrackCanvas.ReleasePointerCapture(e.Pointer);
            _draggingSegment = null;
            SnapPreviewPath.Visibility = Visibility.Collapsed;

            Debug.WriteLine("🔵 Drag ended");
            e.Handled = true;
        }
    }

    /// <summary>
    /// Handle right-click on canvas to start panning.
    /// </summary>
    private void TrackCanvas_RightTapped(object sender, RightTappedRoutedEventArgs e)
    {
        // Prevent context menu
        e.Handled = true;
    }
    #endregion
}