// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.View;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

using Moba.SharedUI.ViewModel;

using System.Diagnostics;

using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.System;

/// <summary>
/// Track Plan Editor Page - Topology-based track plan editor.
/// Code-behind handles: Drag & Drop, Selection, Pan (right-click).
/// </summary>
// ReSharper disable once PartialTypeWithSinglePart
public sealed partial class TrackPlanEditorPage
{
    private bool _isPanning;
    private Point _lastPanPosition;

    public TrackPlanEditorPage(TrackPlanEditorViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    public TrackPlanEditorViewModel ViewModel => (TrackPlanEditorViewModel)DataContext;

    #region Drag & Drop from Library

    private void TrackLibrary_DragItemsStarting(object sender, DragItemsStartingEventArgs e)
    {
        if (e.Items.FirstOrDefault() is TrackTemplateViewModel template)
        {
            e.Data.SetText(template.ArticleCode);
            e.Data.RequestedOperation = DataPackageOperation.Copy;
            Debug.WriteLine($"🎯 Dragging: {template.ArticleCode}");
        }
    }

    private void TrackCanvas_DragOver(object sender, DragEventArgs e)
    {
        e.AcceptedOperation = DataPackageOperation.Copy;
        e.DragUIOverride.Caption = "Add track";
    }

    private async void TrackCanvas_Drop(object sender, DragEventArgs e)
    {
        if (e.DataView.Contains(StandardDataFormats.Text))
        {
            var articleCode = await e.DataView.GetTextAsync();
            var template = ViewModel.TrackLibrary.FirstOrDefault(t => t.ArticleCode == articleCode);
            if (template != null)
            {
                ViewModel.AddSegmentCommand.Execute(template);
                Debug.WriteLine($"✅ Dropped: {articleCode}");
            }
        }
    }

    #endregion

    #region Selection & Pan

    private void TrackCanvas_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        var pointer = e.GetCurrentPoint(TrackCanvas);

        // Right-click or middle-click: Start panning
        if (pointer.Properties.IsRightButtonPressed || pointer.Properties.IsMiddleButtonPressed)
        {
            _isPanning = true;
            _lastPanPosition = pointer.Position;
            TrackCanvas.CapturePointer(e.Pointer);
            e.Handled = true;
            Debug.WriteLine("🖐️ Pan started");
            return;
        }

        // Left-click on empty area: Deselect
        if (pointer.Properties.IsLeftButtonPressed)
        {
            ViewModel.SelectedSegment = null;
        }
    }

    private void TrackCanvas_PointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (!_isPanning) return;

        var pointer = e.GetCurrentPoint(TrackCanvas);
        var currentPosition = pointer.Position;

        // Calculate delta (in screen space, so divide to zoom for canvas space)
        var deltaX = currentPosition.X - _lastPanPosition.X;
        var deltaY = currentPosition.Y - _lastPanPosition.Y;

        // Pan the viewport
        ViewModel.Pan(deltaX, deltaY);

            _lastPanPosition = currentPosition;
            e.Handled = true;
        }

        private void TrackCanvas_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            if (_isPanning)
            {
                _isPanning = false;
                TrackCanvas.ReleasePointerCapture(e.Pointer);
                e.Handled = true;
                Debug.WriteLine("🖐️ Pan ended");
            }
        }

        private void Segment_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            var pointer = e.GetCurrentPoint(sender as UIElement);

            // Only select on left-click (not right-click which starts pan)
            if (pointer.Properties.IsLeftButtonPressed)
            {
                if (sender is FrameworkElement element && element.DataContext is TrackSegmentViewModel segment)
                {
                    ViewModel.SelectedSegment = segment;

                    // Start drag on left-click
                    StartSegmentDrag(segment, e, element);

                    e.Handled = true;
                    Debug.WriteLine($"🔵 Selected: {segment.ArticleCode}");
                }
            }
        }

        #endregion

        #region Segment Drag & Snap

        private bool _isDraggingSegment;
        private TrackSegmentViewModel? _draggedSegment;
        private Point _lastDragPosition;
        private List<TrackSegmentViewModel> _dragGroup = [];
        private UIElement? _dragCaptureElement;

        private void StartSegmentDrag(TrackSegmentViewModel segment, PointerRoutedEventArgs e, UIElement captureElement)
        {
            var pointer = e.GetCurrentPoint(TrackCanvas);


            // Calculate drag offset (pointer position relative to segment position)
            // Account for current zoom and pan
            var zoomFactor = ViewModel.ZoomFactor;
            var canvasX = (pointer.Position.X - ViewModel.PanOffsetX) / zoomFactor;
            var canvasY = (pointer.Position.Y - ViewModel.PanOffsetY) / zoomFactor;

            segment.DragOffsetX = canvasX - segment.WorldTransform.TranslateX;
            segment.DragOffsetY = canvasY - segment.WorldTransform.TranslateY;
            segment.IsDragging = true;

            // Find connected group for group drag
            _dragGroup = ViewModel.FindConnectedGroup(segment.Id);
            foreach (var groupSegment in _dragGroup)
            {
                groupSegment.IsPartOfDragGroup = true;
                // Calculate offset for each group member relative to drag start
                groupSegment.DragOffsetX = canvasX - groupSegment.WorldTransform.TranslateX;
                groupSegment.DragOffsetY = canvasY - groupSegment.WorldTransform.TranslateY;
            }

            _isDraggingSegment = true;
            _draggedSegment = segment;
            _lastDragPosition = new Point(canvasX, canvasY);
            _dragCaptureElement = captureElement;


            // Capture pointer for drag tracking
            captureElement.CapturePointer(e.Pointer);

            Debug.WriteLine($"🎯 Drag started: {segment.ArticleCode} (group size: {_dragGroup.Count})");
        }

        private void Segment_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (!_isDraggingSegment || _draggedSegment == null) return;

            var pointer = e.GetCurrentPoint(TrackCanvas);

            // Convert to canvas coordinates
            var zoomFactor = ViewModel.ZoomFactor;
            var canvasX = (pointer.Position.X - ViewModel.PanOffsetX) / zoomFactor;
            var canvasY = (pointer.Position.Y - ViewModel.PanOffsetY) / zoomFactor;

            // Calculate delta from last position
            var deltaX = canvasX - _lastDragPosition.X;
            var deltaY = canvasY - _lastDragPosition.Y;

            // Move all segments in the drag group
            foreach (var segment in _dragGroup)
            {
                segment.MoveBy(deltaX, deltaY);
            }

            // Re-render to update PathData
            ViewModel.RefreshPathData();

            _lastDragPosition = new Point(canvasX, canvasY);
            e.Handled = true;
        }

        private void Segment_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            if (_isDraggingSegment && _draggedSegment != null)
            {

                Debug.WriteLine($"🎯 Drag ended: {_draggedSegment.ArticleCode}");

                // Clear drag state
                _draggedSegment.IsDragging = false;
                foreach (var segment in _dragGroup)
                {
                    segment.IsPartOfDragGroup = false;
                }

                _isDraggingSegment = false;
                _draggedSegment = null;
                _dragGroup.Clear();

                if (sender is UIElement elem)
                {
                    elem.ReleasePointerCapture(e.Pointer);
                }

                e.Handled = true;
            }
        }

        #endregion
            }
