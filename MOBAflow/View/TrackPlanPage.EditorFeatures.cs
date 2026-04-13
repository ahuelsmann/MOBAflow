namespace Moba.WinUI.View;

using Domain;

using Microsoft.Graphics.Canvas;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using Moba.SharedUI.Service;

using System.Diagnostics;
using System.IO;
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
    private string? _currentTrackPlanPath;
    private string? _lastSavedDocumentJson;
    private bool _hasUnsavedChanges;
    private bool _showGrid;
    private bool _showPortHover = true;
    private bool _showValidationOverlay = true;
    private double _trackOpacity = 0.8;
    private bool _isApplyingDocumentState;

    private void InitializeEditorFeatures()
    {
        LoadTrackPlanButton.Click += async (_, _) => await LoadTrackPlanAsync();
        SaveTrackPlanButton.Click += async (_, _) => await SaveTrackPlanAsync(false);
        SaveTrackPlanAsButton.Click += async (_, _) => await SaveTrackPlanAsync(true);
        UndoButton.Click += (_, _) => Undo();
        RedoButton.Click += (_, _) => Redo();
        ValidateButton.Click += async (_, _) => await ValidateCurrentPlanAsync();
        FitButton.Click += (_, _) => FitToContent();
        ResetZoomButton.Click += (_, _) => ResetZoom();
        RotateLeftButton.Click += (_, _) => RotateSelectedSegment(-15);
        RotateRightButton.Click += (_, _) => RotateSelectedSegment(15);
        GridToggle.Checked += (_, _) => ToggleGrid(true);
        GridToggle.Unchecked += (_, _) => ToggleGrid(false);
        PortHoverToggle.Checked += (_, _) => TogglePortHover(true);
        PortHoverToggle.Unchecked += (_, _) => TogglePortHover(false);
        ValidationOverlayToggle.Checked += (_, _) => ToggleValidationOverlay(true);
        ValidationOverlayToggle.Unchecked += (_, _) => ToggleValidationOverlay(false);
        TrackOpacitySlider.ValueChanged += (_, _) =>
        {
            _trackOpacity = TrackOpacitySlider.Value;
            RefreshCanvas();
        };

        _showGrid = GridToggle.IsChecked == true;
        _showPortHover = PortHoverToggle.IsChecked != false;
        _showValidationOverlay = ValidationOverlayToggle.IsChecked != false;
        _trackOpacity = TrackOpacitySlider.Value;
        _lastSavedDocumentJson = SerializeDocument(CaptureDocumentState());
        _hasUnsavedChanges = false;
        UpdateValidationOverlayVisibility();
        UpdateHistoryButtons();
        UpdateCommandStates();
    }

    private void ToggleGrid(bool isEnabled)
    {
        _showGrid = isEnabled;
        RefreshCanvas();
    }

    private void TogglePortHover(bool isEnabled)
    {
        _showPortHover = isEnabled;
        if (!isEnabled)
        {
            ClearPortHighlights();
            return;
        }

        if (_draggedPlaced != null)
            UpdatePortHighlights();
    }

    private void ToggleValidationOverlay(bool isEnabled)
    {
        _showValidationOverlay = isEnabled;
        UpdateValidationOverlayVisibility();
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
        RefreshDirtyState();
        UpdateHistoryButtons();
        UpdateCommandStates();
    }

    private void RefreshDirtyState()
    {
        var currentJson = SerializeDocument(CaptureDocumentState());
        _hasUnsavedChanges = _lastSavedDocumentJson == null
            ? _plan.Segments.Count > 0 || _plan.Connections.Count > 0
            : !string.Equals(currentJson, _lastSavedDocumentJson, StringComparison.Ordinal);
    }

    private void UpdateHistoryButtons()
    {
        UndoButton.IsEnabled = _undoStack.Count > 0;
        RedoButton.IsEnabled = _redoStack.Count > 0;
    }

    private void UpdateCommandStates()
    {
        var ioServiceAvailable = _ioService is not NullIoService;
        LoadTrackPlanButton.IsEnabled = ioServiceAvailable;
        SaveTrackPlanButton.IsEnabled = ioServiceAvailable;
        SaveTrackPlanAsButton.IsEnabled = ioServiceAvailable;
        ValidateButton.IsEnabled = _plan.Segments.Count > 0;
        FitButton.IsEnabled = _plan.Segments.Count > 0;
        OpenSvgInBrowserButton.IsEnabled = _plan.Segments.Count > 0;
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
            StatusText.Text = "Rotation ist nur für nicht verbundene Gleise verfügbar.";
            UpdateCommandStates();
            return;
        }

        var before = CaptureDocumentState();
        var newRotation = NormalizeAngle(placed.RotationDegrees + deltaDegrees);
        _plan.UpdateSegmentPosition(placed.Segment.No, placed.X, placed.Y, newRotation);
        CommitHistorySnapshot(before);
        StatusText.Text = $"Rotation auf {newRotation:F0}° gesetzt.";
        UpdateSelectionInfo();
    }

    private async Task<bool> SaveTrackPlanAsync(bool saveAs)
    {
        var ioService = _ioService;
        if (ioService is NullIoService)
        {
            StatusText.Text = "Speichern ist auf dieser Plattform nicht verfügbar.";
            return false;
        }

        var path = saveAs ? null : _currentTrackPlanPath;
        if (string.IsNullOrWhiteSpace(path))
            path = await ioService.SaveJsonFileAsync("track-plan");

        if (string.IsNullOrWhiteSpace(path))
            return false;

        try
        {
            var document = CaptureDocumentState();
            var json = SerializeDocument(document);
            var tempPath = path + ".tmp";
            await File.WriteAllTextAsync(tempPath, json);
            File.Move(tempPath, path, overwrite: true);

            _currentTrackPlanPath = path;
            _lastSavedDocumentJson = json;
            _hasUnsavedChanges = false;
            UpdateHistoryButtons();
            UpdateCommandStates();
            StatusText.Text = $"Track plan gespeichert: {path}";
            return true;
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Speichern fehlgeschlagen: {ex.Message}";
            return false;
        }
    }

    private async Task LoadTrackPlanAsync()
    {
        var ioService = _ioService;
        if (ioService is NullIoService)
        {
            StatusText.Text = "Laden ist auf dieser Plattform nicht verfügbar.";
            return;
        }

        if (!await ConfirmDiscardOrSaveAsync()) return;

        var path = await ioService.BrowseForJsonFileAsync();
        if (string.IsNullOrWhiteSpace(path)) return;

        try
        {
            var json = await File.ReadAllTextAsync(path);
            var document = JsonSerializer.Deserialize<TrackPlanEditorDocument>(json, JsonOptions.Default);
            if (document == null)
            {
                StatusText.Text = "TrackPlan-Datei konnte nicht geladen werden.";
                return;
            }

            ApplyEditorDocument(document, clearHistory: true);
            _currentTrackPlanPath = path;
            _lastSavedDocumentJson = SerializeDocument(CaptureDocumentState());
            _hasUnsavedChanges = false;
            StatusText.Text = $"Track plan geladen: {path}";
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Laden fehlgeschlagen: {ex.Message}";
        }
    }

    private async Task<bool> ConfirmDiscardOrSaveAsync()
    {
        if (!_hasUnsavedChanges)
            return true;

        var dialog = new ContentDialog
        {
            XamlRoot = XamlRoot,
            Title = "Ungespeicherte Änderungen",
            Content = "Der aktuelle Gleisplan enthält ungespeicherte Änderungen. Soll vor dem Laden gespeichert werden?",
            PrimaryButtonText = "Speichern",
            SecondaryButtonText = "Verwerfen",
            CloseButtonText = "Abbrechen",
            DefaultButton = ContentDialogButton.Primary
        };

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.None)
            return false;

        if (result == ContentDialogResult.Primary)
            return await SaveTrackPlanAsync(false);

        return true;
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

    private void LoadCurrentPlacementsAsDocument(IReadOnlyList<PlacedSegment> placements, IReadOnlyList<PortConnection> connections, bool clearHistory)
    {
        var document = TrackPlanEditorDocument.FromData(placements, connections);
        ApplyEditorDocument(document, clearHistory);
        _currentTrackPlanPath = null;
        _lastSavedDocumentJson = null;
        RefreshDirtyState();
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
        RefreshDirtyState();
        UpdateHistoryButtons();
        StatusText.Text = "Undo ausgeführt.";
    }

    private void Redo()
    {
        if (_redoStack.Count == 0)
            return;

        var current = CaptureDocumentState();
        var next = _redoStack.Pop();
        _undoStack.Push(current);
        ApplyEditorDocument(next, clearHistory: false);
        RefreshDirtyState();
        UpdateHistoryButtons();
        StatusText.Text = "Redo ausgeführt.";
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
            StatusText.Text = "Kein Gleisplan zum Einpassen vorhanden.";
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
        RefreshDirtyState();
        StatusText.Text = "Gleisplan eingepasst.";
    }

    private void ResetZoom()
    {
        ZoomSlider.Value = 1.0;
        CanvasScrollViewer.ChangeView(null, null, 1.0f);
        RefreshDirtyState();
        StatusText.Text = "Zoom auf 100% gesetzt.";
    }

    private async Task ValidateCurrentPlanAsync()
    {
        var messages = CollectValidationMessages();
        var title = messages.Count == 0 ? "Gleisplan gültig" : "Validierung abgeschlossen";
        var content = messages.Count == 0
            ? "Keine Probleme gefunden."
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
        StatusText.Text = messages.Count == 0 ? "Validierung erfolgreich." : $"Validierung abgeschlossen: {messages.Count} Hinweis(e).";
    }

    private List<string> CollectValidationMessages()
    {
        var messages = new List<string>();
        if (_plan.Segments.Count == 0)
        {
            messages.Add("Der Gleisplan enthält keine Gleise.");
            return messages;
        }

        var knownSegments = _plan.Segments.Select(s => s.Segment.No).ToHashSet();
        var portUsage = new Dictionary<(Guid SegmentId, string PortName), int>();

        foreach (var connection in _plan.Connections)
        {
            if (!knownSegments.Contains(connection.SourceSegment))
                messages.Add($"Verbindung referenziert unbekanntes Quellsegment {connection.SourceSegment}.");
            if (!knownSegments.Contains(connection.TargetSegment))
                messages.Add($"Verbindung referenziert unbekanntes Zielsegment {connection.TargetSegment}.");
            if (connection.SourceSegment == connection.TargetSegment)
                messages.Add($"Segment {connection.SourceSegment} ist mit sich selbst verbunden.");

            var sourceKey = (connection.SourceSegment, connection.SourcePort);
            var targetKey = (connection.TargetSegment, connection.TargetPort);
            portUsage[sourceKey] = portUsage.TryGetValue(sourceKey, out var sourceCount) ? sourceCount + 1 : 1;
            portUsage[targetKey] = portUsage.TryGetValue(targetKey, out var targetCount) ? targetCount + 1 : 1;
        }

        foreach (var usage in portUsage.Where(p => p.Value > 1))
            messages.Add($"Port {usage.Key.PortName} von Segment {usage.Key.SegmentId} wird mehrfach verwendet.");

        var analysis = TrackPlanValidationHelper.Analyze(_plan.Segments, _plan.Connections);

        if (analysis.ConnectedGroups.Count > 1)
            messages.Add($"Der Gleisplan besteht aus {analysis.ConnectedGroups.Count} unverbundenen Gruppen.");

        foreach (var overlappingPort in analysis.OverlappingPorts)
            messages.Add($"Unverbundene Ports {overlappingPort.LeftPortName}/{overlappingPort.RightPortName} liegen geometrisch übereinander.");

        if (analysis.OpenPorts.Count > 0)
            messages.Add($"Es gibt {analysis.OpenPorts.Count} offene Gleisenden.");

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
        var minorColor = Color.FromArgb(255, 236, 236, 236);
        var majorColor = Color.FromArgb(255, 208, 208, 208);

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

    private static Color ApplyOpacity(Color color, double opacity)
    {
        var alpha = (byte)Math.Clamp(Math.Round(opacity * 255), 0, 255);
        return Color.FromArgb(alpha, color.R, color.G, color.B);
    }

    private void ExportCurrentTrackPlanToBrowser()
    {
        if (_plan.Segments.Count == 0)
        {
            StatusText.Text = "Es gibt keinen aktuellen Gleisplan zum Exportieren.";
            return;
        }

        var svg = new PlacedTrackPlanSvgRenderer().Render(_plan.Segments, _trackOpacity, _showGrid);
        var path = Path.Combine(Path.GetTempPath(), "trackplan-current.html");
        new SvgExporter().Export(svg, path);

        if (OperatingSystem.IsWindows())
            Process.Start(new ProcessStartInfo { FileName = path, UseShellExecute = true });

        StatusText.Text = $"SVG geöffnet: {path}";
    }

    private bool IsCtrlPressed()
    {
        return InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down);
    }

    private bool IsShiftPressed()
    {
        return InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Shift).HasFlag(CoreVirtualKeyStates.Down);
    }
}
