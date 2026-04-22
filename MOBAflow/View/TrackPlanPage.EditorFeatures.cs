namespace Moba.WinUI.View;

using Domain;

using Microsoft.Graphics.Canvas;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using System.Diagnostics;
using System.Text.Json;

using TrackLibrary.PikoA;

using TrackPlan.Renderer;

using Windows.System;
using Windows.UI;
using Windows.UI.Core;

using Path = Path;

public sealed partial class TrackPlanPage
{
    private readonly Stack<TrackPlanEditorDocument> _undoStack = [];
    private readonly Stack<TrackPlanEditorDocument> _redoStack = [];
    private TrackPlanEditorDocument? _pendingDragSnapshot;
    private TrackPlanEditorDocument? _pendingRotationSnapshot;
    private bool _showGrid;
    private bool _showValidationOverlay = true;
    private bool _showTrackLabels = true;
    private bool _isApplyingDocumentState;

    private void InitializeEditorFeatures()
    {
        UndoButton.Click += (_, _) => Undo();
        RedoButton.Click += (_, _) => Redo();
        ValidateButton.Click += async (_, _) => await ValidateCurrentPlanAsync();
        FitButton.Click += (_, _) => FitToContent();
        ResetZoomButton.Click += (_, _) => ResetZoom();
        RotateLeftButton.Click += (_, _) => RotateSelectedSegment(-15);
        RotateRightButton.Click += (_, _) => RotateSelectedSegment(15);
        GridToggle.Checked += (_, _) => ToggleGrid(true);
        GridToggle.Unchecked += (_, _) => ToggleGrid(false);
        ValidationOverlayToggle.Checked += (_, _) => ToggleValidationOverlay(true);
        ValidationOverlayToggle.Unchecked += (_, _) => ToggleValidationOverlay(false);
        TrackLabelsToggle.Checked += (_, _) => ToggleTrackLabels(true);
        TrackLabelsToggle.Unchecked += (_, _) => ToggleTrackLabels(false);
        ActualThemeChanged += (_, _) =>
        {
            // Rebuild toolbox previews so their Stroke/Border/Background brushes resolve against
            // the new theme (these are plain Brush instances set in code and do not auto-update
            // like {ThemeResource} bindings). The Win2D canvas picks up the new colors via its
            // Draw handler.
            PopulateToolbox();
            RefreshCanvas();
        };

        _showGrid = GridToggle.IsChecked == true;
        _showValidationOverlay = ValidationOverlayToggle.IsChecked != false;
        _showTrackLabels = TrackLabelsToggle.IsChecked != false;
        UpdateValidationOverlayVisibility();
        UpdateHistoryButtons();
        UpdateCommandStates();
    }

    private void ToggleGrid(bool isEnabled)
    {
        _showGrid = isEnabled;
        RefreshCanvas();
    }

    private void ToggleValidationOverlay(bool isEnabled)
    {
        _showValidationOverlay = isEnabled;
        UpdateValidationOverlayVisibility();
        RefreshCanvas();
    }

    private void ToggleTrackLabels(bool isEnabled)
    {
        _showTrackLabels = isEnabled;
        RefreshCanvas();
    }

    private void UpdateValidationOverlayVisibility()
    {
        if (ValidationLegendPanel != null)
            ValidationLegendPanel.Visibility = _showValidationOverlay ? Visibility.Visible : Visibility.Collapsed;
    }

    private TrackPlanEditorDocument CaptureDocumentState()
    {
        return TrackPlanEditorDocument.FromEditableTrackPlan(
            _plan,
            _drawOffsetInitialized ? _cachedDrawOffsetX : null,
            _drawOffsetInitialized ? _cachedDrawOffsetY : null,
            ZoomSlider.Value);
    }

    private string SerializeDocument(TrackPlanEditorDocument document)
    {
        return JsonSerializer.Serialize(document, JsonOptions.Default);
    }

    private void CommitHistorySnapshot(TrackPlanEditorDocument? beforeSnapshot)
    {
        if (beforeSnapshot == null || _isApplyingDocumentState)
            return;

        var beforeJson = SerializeDocument(beforeSnapshot);
        var afterJson = SerializeDocument(CaptureDocumentState());
        if (beforeJson == afterJson)
            return;

        _undoStack.Push(beforeSnapshot);
        _redoStack.Clear();
        UpdateHistoryButtons();
        UpdateCommandStates();
    }

    private void UpdateHistoryButtons()
    {
        UndoButton.IsEnabled = _undoStack.Count > 0;
        RedoButton.IsEnabled = _redoStack.Count > 0;
    }

    private void UpdateCommandStates()
    {
        ValidateButton.IsEnabled = _plan.Segments.Count > 0;
        FitButton.IsEnabled = _plan.Segments.Count > 0;
        ExportDropDownButton.IsEnabled = _plan.Segments.Count > 0;
        ExportSvgInBrowserMenuItem.IsEnabled = _plan.Segments.Count > 0;
        RotateLeftButton.IsEnabled = CanRotateSelectedSegment(out _);
        RotateRightButton.IsEnabled = RotateLeftButton.IsEnabled;
    }

    private bool CanRotateSelectedSegment(out PlacedSegment? placed)
    {
        placed = null;
        if (_selectedSegmentId == null)
            return false;

        placed = _plan.Segments.FirstOrDefault(s => s.Segment.No == _selectedSegmentId.Value);
        if (placed == null)
            return false;

        var segmentNo = placed.Segment.No;
        return _plan.Connections.All(c => c.SourceSegment != segmentNo && c.TargetSegment != segmentNo);
    }

    private void RotateSelectedSegment(double deltaDegrees)
    {
        if (!CanRotateSelectedSegment(out var placed) || placed == null)
        {
            StatusText.Text = "Rotation is only available for disconnected tracks.";
            UpdateCommandStates();
            return;
        }

        var before = CaptureDocumentState();
        var newRotation = NormalizeAngle(placed.RotationDegrees + deltaDegrees);
        _plan.UpdateSegmentPosition(placed.Segment.No, placed.X, placed.Y, newRotation);
        CommitHistorySnapshot(before);
        StatusText.Text = $"Rotation set to {newRotation:F0}°.";
        UpdateSelectionInfo();
    }

    private void ApplyEditorDocument(TrackPlanEditorDocument document, bool clearHistory)
    {
        var (placements, connections) = document.ToEditableTrackPlanData();

        _isApplyingDocumentState = true;
        try
        {
            _selectedSegmentId = null;
            _draggedSegmentId = null;
            _draggedPlaced = null;
            _draggingGroup.Clear();
            _pendingDragSnapshot = null;
            _pendingRotationSnapshot = null;
            ClearGhost();
            ClearPortHighlights();
            _plan.LoadFromPlacements(placements, connections);
            _plan.HealImplicitConnections();

            if (document.OffsetX.HasValue && document.OffsetY.HasValue)
            {
                _cachedDrawOffsetX = document.OffsetX.Value;
                _cachedDrawOffsetY = document.OffsetY.Value;
                _drawOffsetInitialized = true;
            }
            else
            {
                RecalculateDrawOffset();
            }
        }
        finally
        {
            _isApplyingDocumentState = false;
        }

        if (clearHistory)
        {
            _undoStack.Clear();
            _redoStack.Clear();
        }

        if (document.ZoomFactor.HasValue)
        {
            var zoom = Math.Clamp(document.ZoomFactor.Value, 0.1, 3.0);
            ZoomSlider.Value = zoom;
            CanvasScrollViewer.ChangeView(0, 0, (float)zoom);
        }
        else
        {
            CanvasScrollViewer.ChangeView(0, 0, CanvasScrollViewer.ZoomFactor);
        }

        UpdateSelectionInfo();
        RefreshCanvas();
        UpdateHistoryButtons();
        UpdateCommandStates();
    }

    private void Undo()
    {
        if (_undoStack.Count == 0)
            return;

        var current = CaptureDocumentState();
        var previous = _undoStack.Pop();
        _redoStack.Push(current);
        ApplyEditorDocument(previous, clearHistory: false);
        UpdateHistoryButtons();
        StatusText.Text = "Undo executed.";
    }

    private void Redo()
    {
        if (_redoStack.Count == 0)
            return;

        var current = CaptureDocumentState();
        var next = _redoStack.Pop();
        _undoStack.Push(current);
        ApplyEditorDocument(next, clearHistory: false);
        UpdateHistoryButtons();
        StatusText.Text = "Redo executed.";
    }

    private void DeleteSelectedSegment()
    {
        if (_selectedSegmentId == null)
            return;

        var before = CaptureDocumentState();
        _plan.RemoveSegment(_selectedSegmentId.Value);
        _selectedSegmentId = null;
        UpdateSelectionInfo();
        CommitHistorySnapshot(before);
    }

    private void FitToContent()
    {
        var bounds = ComputeContentBoundsMm();
        if (bounds == null)
        {
            StatusText.Text = "No track plan available to fit.";
            return;
        }

        RecalculateDrawOffset();
        var widthMm = Math.Max(1, bounds.Value.MaxX - bounds.Value.MinX + (2 * ContentMarginMm));
        var heightMm = Math.Max(1, bounds.Value.MaxY - bounds.Value.MinY + (2 * ContentMarginMm));
        var viewportWidthPx = Math.Max(100, CanvasScrollViewer.ActualWidth - 32);
        var viewportHeightPx = Math.Max(100, CanvasScrollViewer.ActualHeight - 32);
        var zoom = Math.Clamp(Math.Min(viewportWidthPx / (widthMm * ScaleMmToPx), viewportHeightPx / (heightMm * ScaleMmToPx)), 0.1, 3.0);
        ZoomSlider.Value = zoom;
        CanvasScrollViewer.ChangeView(0, 0, (float)zoom);
        RefreshCanvas();
        StatusText.Text = "Track plan fitted.";
    }

    private void ResetZoom()
    {
        ZoomSlider.Value = 1.0;
        CanvasScrollViewer.ChangeView(null, null, 1.0f);
        StatusText.Text = "Zoom set to 100%.";
    }

    private async Task ValidateCurrentPlanAsync()
    {
        var messages = CollectValidationMessages();
        var title = messages.Count == 0 ? "Track Plan Valid" : "Validation Completed";
        var content = messages.Count == 0
            ? "No issues found."
            : string.Join(Environment.NewLine, messages.Select((m, i) => $"{i + 1}. {m}"));

        var dialog = new ContentDialog
        {
            XamlRoot = XamlRoot,
            Title = title,
            Content = new ScrollViewer
            {
                MaxHeight = 400,
                Content = new TextBlock
                {
                    Text = content,
                    TextWrapping = TextWrapping.Wrap
                }
            },
            CloseButtonText = "OK"
        };

        await dialog.ShowAsync();
        StatusText.Text = messages.Count == 0 ? "Validation successful." : $"Validation completed: {messages.Count} hint(s).";
    }

    private List<string> CollectValidationMessages()
    {
        var messages = new List<string>();
        if (_plan.Segments.Count == 0)
        {
            messages.Add("The track plan contains no tracks.");
            return messages;
        }

        var knownSegments = _plan.Segments.Select(s => s.Segment.No).ToHashSet();
        var portUsage = new Dictionary<(Guid SegmentId, string PortName), int>();

        foreach (var connection in _plan.Connections)
        {
            if (!knownSegments.Contains(connection.SourceSegment))
                messages.Add($"Connection references unknown source segment {connection.SourceSegment}.");
            if (!knownSegments.Contains(connection.TargetSegment))
                messages.Add($"Connection references unknown target segment {connection.TargetSegment}.");
            if (connection.SourceSegment == connection.TargetSegment)
                messages.Add($"Segment {connection.SourceSegment} is connected to itself.");

            var sourceKey = (connection.SourceSegment, connection.SourcePort);
            var targetKey = (connection.TargetSegment, connection.TargetPort);
            portUsage[sourceKey] = portUsage.TryGetValue(sourceKey, out var sourceCount) ? sourceCount + 1 : 1;
            portUsage[targetKey] = portUsage.TryGetValue(targetKey, out var targetCount) ? targetCount + 1 : 1;
        }

        foreach (var usage in portUsage.Where(p => p.Value > 1))
            messages.Add($"Port {usage.Key.PortName} of segment {usage.Key.SegmentId} is used multiple times.");

        var analysis = TrackPlanValidationHelper.Analyze(_plan.Segments, _plan.Connections);

        if (analysis.ConnectedGroups.Count > 1)
            messages.Add($"The track plan consists of {analysis.ConnectedGroups.Count} disconnected groups.");

        foreach (var overlappingPort in analysis.OverlappingPorts)
            messages.Add($"Disconnected ports {overlappingPort.LeftPortName}/{overlappingPort.RightPortName} overlap geometrically.");

        if (analysis.OpenPorts.Count > 0)
            messages.Add($"There are {analysis.OpenPorts.Count} open track ends.");

        return messages;
    }

    private void DrawGrid(CanvasDrawingSession drawingSession, double offsetX, double offsetY)
    {
        var widthPx = GraphCanvasControl?.ActualWidth > 0 ? GraphCanvasControl.ActualWidth : 3000;
        var heightPx = GraphCanvasControl?.ActualHeight > 0 ? GraphCanvasControl.ActualHeight : 2000;
        var worldMinX = -offsetX;
        var worldMaxX = (widthPx / ScaleMmToPx) - offsetX;
        var worldMinY = -offsetY;
        var worldMaxY = (heightPx / ScaleMmToPx) - offsetY;
        var startX = Math.Floor(worldMinX / 100.0) * 100.0;
        var startY = Math.Floor(worldMinY / 100.0) * 100.0;
        var minorColor = ThemeResourceResolver.ResolveColor(this, "TrackPlanGridMinorBrush", Color.FromArgb(255, 236, 236, 236));
        var majorColor = ThemeResourceResolver.ResolveColor(this, "TrackPlanGridMajorBrush", Color.FromArgb(255, 208, 208, 208));

        for (var worldX = startX; worldX <= worldMaxX; worldX += 100.0)
        {
            var displayX = (float)((worldX + offsetX) * ScaleMmToPx);
            var isMajor = Math.Abs(worldX % 500.0) < 0.001;
            drawingSession.DrawLine(displayX, 0, displayX, (float)heightPx, isMajor ? majorColor : minorColor, isMajor ? 1.5f : 1f);
        }

        for (var worldY = startY; worldY <= worldMaxY; worldY += 100.0)
        {
            var displayY = (float)((worldY + offsetY) * ScaleMmToPx);
            var isMajor = Math.Abs(worldY % 500.0) < 0.001;
            drawingSession.DrawLine(0, displayY, (float)widthPx, displayY, isMajor ? majorColor : minorColor, isMajor ? 1.5f : 1f);
        }
    }

    private void ExportCurrentTrackPlanToBrowser()
    {
        if (_plan.Segments.Count == 0)
        {
            StatusText.Text = "There is no current track plan to export.";
            return;
        }

        var svg = new PlacedTrackPlanSvgRenderer().Render(_plan.Segments, showGrid: _showGrid);
        var path = Path.Combine(Path.GetTempPath(), "trackplan-current.html");
        new SvgExporter().Export(svg, path);

        if (OperatingSystem.IsWindows())
            Process.Start(new ProcessStartInfo { FileName = path, UseShellExecute = true });

        StatusText.Text = $"SVG opened: {path}";
    }

    private bool IsCtrlPressed()
    {
        return InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down);
    }

}
