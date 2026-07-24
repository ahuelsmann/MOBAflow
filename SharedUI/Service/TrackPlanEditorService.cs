// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.SharedUI.Service;

using Backend.Service.TrackPlan;

using Domain;

using System.Text.Json;

using TrackLibrary.PikoA;

/// <summary>
/// Coordinates platform-neutral track-plan selection, mutations, validation, history, and dirty state.
/// </summary>
public sealed class TrackPlanEditorService : IDisposable
{
    private readonly EditableTrackPlan _plan;
    private readonly TrackPlanInteractionService _interactionService;
    private readonly SelectionService _selectionService;
    private readonly UndoRedoService<TrackPlanEditorDocument> _undoRedoService;
    private TrackPlanEditorDocument? _pendingGestureSnapshot;
    private bool _isApplyingDocument;
    private bool _disposed;

    public TrackPlanEditorService(
        EditableTrackPlan plan,
        TrackPlanInteractionService interactionService,
        SelectionService selectionService,
        UndoRedoService<TrackPlanEditorDocument> undoRedoService)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(interactionService);
        ArgumentNullException.ThrowIfNull(selectionService);
        ArgumentNullException.ThrowIfNull(undoRedoService);

        _plan = plan;
        _interactionService = interactionService;
        _selectionService = selectionService;
        _undoRedoService = undoRedoService;
        _plan.PlanChanged += OnPlanChanged;
        _selectionService.SelectionChanged += OnSelectionChanged;
    }

    /// <summary>Raised when selection, history, dirty state, or editor content changes.</summary>
    public event EventHandler? StateChanged;

    public IReadOnlyList<PlacedSegment> Segments => _plan.Segments;

    public IReadOnlyList<PortConnection> Connections => _plan.Connections;

    public Guid? SelectedTrackId => _selectionService.SelectedTrackId;

    public PlacedSegment? SelectedTrack => SelectedTrackId is Guid trackId
        ? _plan.Segments.FirstOrDefault(segment => segment.Segment.No == trackId)
        : null;

    public bool IsDirty { get; private set; }

    public bool CanUndo => _undoRedoService.CanUndo;

    public bool CanRedo => _undoRedoService.CanRedo;

    public bool CanDeleteSelectedTrack => SelectedTrack != null;

    public bool CanDisconnectSelectedTrack => SelectedTrackId is Guid trackId
        && _plan.Connections.Any(connection =>
            connection.SourceSegment == trackId || connection.TargetSegment == trackId);

    public bool CanRotateSelectedTrack => SelectedTrack != null && !CanDisconnectSelectedTrack;

    public void Select(Guid? trackId)
    {
        if (trackId.HasValue && _plan.Segments.All(segment => segment.Segment.No != trackId.Value))
        {
            trackId = null;
        }

        _selectionService.Select(trackId);
    }

    /// <summary>Resolves and selects the nearest segment for a pointer-initiated drag.</summary>
    public TrackPlanInteractionService.DragSelection? SelectForDrag(
        double worldX,
        double worldY,
        double toleranceMm = 12)
    {
        var selection = _interactionService.SelectForDrag(worldX, worldY, toleranceMm);
        Select(selection?.SelectedSegmentId);
        return selection;
    }

    public TrackPlanSnapHelper.SnapResult? FindBestSnap(
        PlacedSegment movingSegment,
        Guid? excludeSegmentId = null,
        IReadOnlySet<Guid>? movingGroup = null)
    {
        return _interactionService.FindBestSnap(movingSegment, excludeSegmentId, movingGroup);
    }

    public TrackPlanInteractionService.SnapPreview GetSnapPreview(
        PlacedSegment movingSegment,
        IReadOnlySet<Guid>? movingGroup = null,
        double thresholdMm = 25)
    {
        return _interactionService.GetSnapPreview(movingSegment, movingGroup, thresholdMm);
    }

    /// <summary>Captures one history boundary before a multi-event pointer gesture.</summary>
    public void BeginGesture()
    {
        _pendingGestureSnapshot ??= CaptureDocument();
    }

    /// <summary>Moves a connected group during a pointer gesture.</summary>
    public void MoveGroup(IReadOnlySet<Guid> movingGroup, double deltaX, double deltaY)
    {
        ArgumentNullException.ThrowIfNull(movingGroup);
        _interactionService.MoveGroup(movingGroup, deltaX, deltaY);
    }

    /// <summary>Completes a drag, optionally snapping it, healing topology, and recording one history entry.</summary>
    public void CompleteMove(Guid movedSegmentId, IReadOnlySet<Guid> movingGroup, bool snapEnabled)
    {
        ArgumentNullException.ThrowIfNull(movingGroup);
        var placed = _plan.Segments.FirstOrDefault(segment => segment.Segment.No == movedSegmentId);
        if (placed != null && snapEnabled)
        {
            var snap = _interactionService.FindBestSnap(placed, movedSegmentId, movingGroup);
            if (snap != null)
            {
                _interactionService.MoveWithSnap(movedSegmentId, placed, snap, movingGroup);
            }
        }

        _plan.HealImplicitConnections();
        CompleteGesture();
    }

    /// <summary>Records the final state of the active pointer gesture when it changed the document.</summary>
    public void CompleteGesture()
    {
        if (_pendingGestureSnapshot == null)
        {
            return;
        }

        RecordSnapshotIfChanged(_pendingGestureSnapshot);
        _pendingGestureSnapshot = null;
    }

    public void PlaceSegment(PlacedSegment placed, bool snapEnabled)
    {
        ArgumentNullException.ThrowIfNull(placed);
        var before = CaptureDocument();
        var snap = snapEnabled ? _interactionService.FindBestSnap(placed) : null;
        if (snap == null)
        {
            _plan.AddSegment(placed);
        }
        else
        {
            _interactionService.AddWithSnap(snap);
        }

        _plan.HealImplicitConnections();
        RecordSnapshotIfChanged(before);
    }

    public void DeleteSelectedTrack()
    {
        if (SelectedTrackId is not Guid trackId)
        {
            return;
        }

        var before = CaptureDocument();
        _plan.RemoveSegment(trackId);
        _selectionService.Select(null);
        RecordSnapshotIfChanged(before);
    }

    public void DisconnectSelectedTrack()
    {
        if (SelectedTrackId is not Guid trackId || !CanDisconnectSelectedTrack)
        {
            return;
        }

        var before = CaptureDocument();
        _plan.DisconnectSegmentFromGroup(trackId);
        RecordSnapshotIfChanged(before);
    }

    public void RotateSelectedTrack(double deltaDegrees)
    {
        var selected = SelectedTrack;
        if (selected == null || !CanRotateSelectedTrack)
        {
            return;
        }

        var before = CaptureDocument();
        _plan.UpdateSegmentPosition(
            selected.Segment.No,
            selected.X,
            selected.Y,
            NormalizeAngle(selected.RotationDegrees + deltaDegrees));
        RecordSnapshotIfChanged(before);
    }

    /// <summary>Updates rotation during a pointer gesture; call <see cref="BeginGesture"/> first.</summary>
    public void SetSelectedTrackRotation(double rotationDegrees)
    {
        var selected = SelectedTrack;
        if (selected == null || !CanRotateSelectedTrack)
        {
            return;
        }

        _plan.UpdateSegmentPosition(
            selected.Segment.No,
            selected.X,
            selected.Y,
            NormalizeAngle(rotationDegrees));
    }

    public void AssignSelectedTrackFeedback(int? inPort)
    {
        var selected = SelectedTrack;
        if (selected == null || selected.InPort == inPort)
        {
            return;
        }

        var before = CaptureDocument();
        _plan.UpdateSegmentInPort(selected.Segment.No, inPort);
        RecordSnapshotIfChanged(before);
    }

    public bool Undo()
    {
        var current = CaptureDocument();
        if (!_undoRedoService.TryUndo(current, out var previous))
        {
            return false;
        }

        ApplyDocument(previous, clearHistory: false, markClean: false);
        return true;
    }

    public bool Redo()
    {
        var current = CaptureDocument();
        if (!_undoRedoService.TryRedo(current, out var next))
        {
            return false;
        }

        ApplyDocument(next, clearHistory: false, markClean: false);
        return true;
    }

    public TrackPlanEditorDocument CaptureDocument()
    {
        return TrackPlanEditorDocument.FromEditableTrackPlan(_plan);
    }

    public void ApplyDocument(TrackPlanEditorDocument document, bool clearHistory, bool markClean)
    {
        ArgumentNullException.ThrowIfNull(document);
        var (placements, connections) = document.ToEditableTrackPlanData();

        _isApplyingDocument = true;
        try
        {
            _pendingGestureSnapshot = null;
            _selectionService.Select(null);
            _plan.LoadFromPlacements(placements, connections);
            _plan.HealImplicitConnections();
            if (clearHistory)
            {
                _undoRedoService.Clear();
            }

            IsDirty = !markClean;
        }
        finally
        {
            _isApplyingDocument = false;
        }

        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    public TrackPlanEditorValidationResult Validate()
    {
        var messages = new List<string>();
        if (_plan.Segments.Count == 0)
        {
            messages.Add("The track plan contains no tracks.");
            return new TrackPlanEditorValidationResult(messages);
        }

        var knownSegments = _plan.Segments.Select(segment => segment.Segment.No).ToHashSet();
        var portUsage = new Dictionary<(Guid SegmentId, string PortName), int>();

        foreach (var connection in _plan.Connections)
        {
            if (!knownSegments.Contains(connection.SourceSegment))
            {
                messages.Add($"Connection references unknown source segment {connection.SourceSegment}.");
            }

            if (!knownSegments.Contains(connection.TargetSegment))
            {
                messages.Add($"Connection references unknown target segment {connection.TargetSegment}.");
            }

            if (connection.SourceSegment == connection.TargetSegment)
            {
                messages.Add($"Segment {connection.SourceSegment} is connected to itself.");
            }

            IncrementPortUsage(portUsage, (connection.SourceSegment, connection.SourcePort));
            IncrementPortUsage(portUsage, (connection.TargetSegment, connection.TargetPort));
        }

        foreach (var usage in portUsage.Where(entry => entry.Value > 1))
        {
            messages.Add($"Port {usage.Key.PortName} of segment {usage.Key.SegmentId} is used multiple times.");
        }

        var analysis = TrackPlanValidationHelper.Analyze(_plan.Segments, _plan.Connections);
        if (analysis.ConnectedGroups.Count > 1)
        {
            messages.Add($"The track plan consists of {analysis.ConnectedGroups.Count} disconnected groups.");
        }

        foreach (var overlappingPort in analysis.OverlappingPorts)
        {
            messages.Add(
                $"Disconnected ports {overlappingPort.LeftPortName}/{overlappingPort.RightPortName} overlap geometrically.");
        }

        if (analysis.OpenPorts.Count > 0)
        {
            messages.Add($"There are {analysis.OpenPorts.Count} open track ends.");
        }

        return new TrackPlanEditorValidationResult(messages);
    }

    public void MarkClean()
    {
        if (!IsDirty)
        {
            return;
        }

        IsDirty = false;
        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _plan.PlanChanged -= OnPlanChanged;
        _selectionService.SelectionChanged -= OnSelectionChanged;
    }

    private void RecordSnapshotIfChanged(TrackPlanEditorDocument before)
    {
        var beforeJson = JsonSerializer.Serialize(before, JsonOptions.Default);
        var afterJson = JsonSerializer.Serialize(CaptureDocument(), JsonOptions.Default);
        if (string.Equals(beforeJson, afterJson, StringComparison.Ordinal))
        {
            return;
        }

        _undoRedoService.Record(before);
        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    private void OnPlanChanged(object? sender, EventArgs e)
    {
        _ = sender;
        _ = e;
        if (!_isApplyingDocument)
        {
            IsDirty = true;
        }

        if (SelectedTrackId.HasValue && SelectedTrack == null)
        {
            _selectionService.Select(null);
        }

        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    private void OnSelectionChanged(object? sender, EventArgs e)
    {
        _ = sender;
        _ = e;
        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    private static void IncrementPortUsage(
        IDictionary<(Guid SegmentId, string PortName), int> usage,
        (Guid SegmentId, string PortName) key)
    {
        usage[key] = usage.TryGetValue(key, out var count) ? count + 1 : 1;
    }

    private static double NormalizeAngle(double degrees)
    {
        while (degrees >= 360)
        {
            degrees -= 360;
        }

        while (degrees < 0)
        {
            degrees += 360;
        }

        return degrees;
    }
}

/// <summary>Platform-neutral validation result for the current editor document.</summary>
public sealed record TrackPlanEditorValidationResult(IReadOnlyList<string> Messages)
{
    public bool IsValid => Messages.Count == 0;
}
