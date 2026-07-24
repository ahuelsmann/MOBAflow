// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using Common.Configuration;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Interface;

using Microsoft.Extensions.Logging;

using Service;

using TrackPlan.Renderer;
using TrackLibrary.PikoA;

/// <summary>
/// ViewModel wrapper for <see cref="TrackPlan"/> used by the track plan editor UI.
/// </summary>
public sealed partial class TrackPlanViewModel : ObservableObject, IViewModelWrapper<TrackPlan>
{
    private readonly TrackPlan _model;
    private readonly AppSettings _settings;
    private readonly ISettingsService _settingsService;
    private readonly IDialogService _dialogService;
    private readonly ILogger<TrackPlanViewModel> _logger;
    private readonly TrackPlanEditorService _editorService;
    private bool _isToolboxExpanded;
    private bool _isPropertiesExpanded;

    /// <summary>
    /// Initializes a new instance of the <see cref="TrackPlanViewModel"/> class.
    /// </summary>
    /// <param name="model">The track plan domain model.</param>
    /// <param name="settings">Application settings (layout persistence).</param>
    /// <param name="settingsService">Settings service for persisting layout changes.</param>
    /// <param name="logger">Logger for persistence failures.</param>
    public TrackPlanViewModel(
        TrackPlan model,
        TrackPlanEditorService editorService,
        IDialogService dialogService,
        AppSettings settings,
        ISettingsService settingsService,
        ILogger<TrackPlanViewModel> logger)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(editorService);
        ArgumentNullException.ThrowIfNull(dialogService);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(settingsService);
        ArgumentNullException.ThrowIfNull(logger);
        _model = model;
        _editorService = editorService;
        _dialogService = dialogService;
        _settings = settings;
        _settingsService = settingsService;
        _logger = logger;
        _isToolboxExpanded = _settings.Layout.TrackPlanPage.IsToolboxExpanded;
        _isPropertiesExpanded = _settings.Layout.TrackPlanPage.IsPropertiesExpanded;
        _editorService.StateChanged += OnEditorStateChanged;
    }

    /// <summary>
    /// Gets the underlying track plan domain model.
    /// </summary>
    public TrackPlan Model => _model;

    /// <summary>Selected track identity, independent of a concrete UI control.</summary>
    public Guid? SelectedTrackId => _editorService.SelectedTrackId;

    public PlacedSegment? SelectedTrack => _editorService.SelectedTrack;

    public bool HasSelectedTrack => SelectedTrack != null;

    public bool HasTracks => _editorService.Segments.Count > 0;

    public bool CanDeleteSelectedTrack => _editorService.CanDeleteSelectedTrack;

    public bool CanDisconnectSelectedTrack => _editorService.CanDisconnectSelectedTrack;

    public bool CanRotateSelectedTrack => _editorService.CanRotateSelectedTrack;

    public bool CanUndo => _editorService.CanUndo;

    public bool CanRedo => _editorService.CanRedo;

    public bool IsDirty => _editorService.IsDirty;

    public string SelectionSummary => CreateSelectionSummary();

    public IReadOnlySet<(double X, double Y)> SnapPreviewPorts { get; private set; } = new HashSet<(double X, double Y)>();

    public IReadOnlyList<string> ValidationMessages { get; private set; } = [];

    [ObservableProperty]
    private string _statusText = "Ready.";

    [RelayCommand]
    public void SelectTrack(Guid? trackId) => _editorService.Select(trackId);

    [RelayCommand(CanExecute = nameof(CanDeleteSelectedTrack))]
    public void DeleteSelectedTrack()
    {
        if (!CanDeleteSelectedTrack)
            return;

        _editorService.DeleteSelectedTrack();
        StatusText = "Track deleted.";
    }

    [RelayCommand(CanExecute = nameof(CanDisconnectSelectedTrack))]
    public void DisconnectSelectedTrack()
    {
        if (!CanDisconnectSelectedTrack)
            return;

        _editorService.DisconnectSelectedTrack();
        StatusText = "Track disconnected.";
    }

    [RelayCommand(CanExecute = nameof(CanRotateSelectedTrack))]
    public void RotateSelectedTrack(double deltaDegrees)
    {
        if (!CanRotateSelectedTrack)
            return;

        _editorService.RotateSelectedTrack(deltaDegrees);
        StatusText = $"Rotation set to {SelectedTrack?.RotationDegrees:F0}°.";
    }

    [RelayCommand]
    public void AssignSelectedTrackFeedback(int? inPort)
    {
        if (SelectedTrack == null || SelectedTrack.InPort == inPort)
            return;

        _editorService.AssignSelectedTrackFeedback(inPort);
        StatusText = inPort.HasValue
            ? $"Feedback address set to {inPort.Value}."
            : "Feedback address cleared.";
    }

    /// <summary>Updates the renderer-neutral connector preview projected by the interaction layer.</summary>
    public void SetSnapPreview(IReadOnlySet<(double X, double Y)> ports)
    {
        ArgumentNullException.ThrowIfNull(ports);
        SnapPreviewPorts = ports;
        OnPropertyChanged(nameof(SnapPreviewPorts));
    }

    /// <summary>Selects the nearest track and returns the drag state for the WinUI pointer adapter.</summary>
    public TrackPlanInteractionService.DragSelection? SelectForDrag(double worldX, double worldY)
    {
        return _editorService.SelectForDrag(worldX, worldY);
    }

    public TrackPlanSnapHelper.SnapResult? FindBestSnap(
        PlacedSegment movingSegment,
        Guid? excludeSegmentId = null,
        IReadOnlySet<Guid>? movingGroup = null)
    {
        return _editorService.FindBestSnap(movingSegment, excludeSegmentId, movingGroup);
    }

    public TrackPlanInteractionService.SnapPreview GetSnapPreview(
        PlacedSegment movingSegment,
        IReadOnlySet<Guid>? movingGroup = null,
        double thresholdMm = 25)
    {
        return _editorService.GetSnapPreview(movingSegment, movingGroup, thresholdMm);
    }

    public void BeginGesture() => _editorService.BeginGesture();

    public void MoveGroup(IReadOnlySet<Guid> movingGroup, double deltaX, double deltaY)
    {
        _editorService.MoveGroup(movingGroup, deltaX, deltaY);
    }

    public void CompleteMove(Guid movedSegmentId, IReadOnlySet<Guid> movingGroup, bool snapEnabled)
    {
        _editorService.CompleteMove(movedSegmentId, movingGroup, snapEnabled);
    }

    public void CompleteGesture() => _editorService.CompleteGesture();

    public void PlaceSegment(PlacedSegment placed, bool snapEnabled)
    {
        _editorService.PlaceSegment(placed, snapEnabled);
        StatusText = "Track placed.";
    }

    public void SetSelectedTrackRotation(double rotationDegrees)
    {
        _editorService.SetSelectedTrackRotation(rotationDegrees);
    }

    [RelayCommand(CanExecute = nameof(CanUndo))]
    public void Undo()
    {
        if (!_editorService.Undo())
            return;

        StatusText = "Undo executed.";
    }

    [RelayCommand(CanExecute = nameof(CanRedo))]
    public void Redo()
    {
        if (!_editorService.Redo())
            return;

        StatusText = "Redo executed.";
    }

    [RelayCommand(CanExecute = nameof(HasTracks))]
    public async Task ValidateAsync()
    {
        var result = _editorService.Validate();
        ValidationMessages = result.Messages;
        OnPropertyChanged(nameof(ValidationMessages));

        var title = result.IsValid ? "Track Plan Valid" : "Validation Completed";
        var message = result.IsValid
            ? "No issues found."
            : string.Join(
                Environment.NewLine,
                result.Messages.Select((validationMessage, index) => $"{index + 1}. {validationMessage}"));
        await _dialogService.ShowConfirmationAsync(
            title,
            message,
            "OK",
            "Cancel",
            isCancelDefault: false);

        StatusText = result.IsValid
            ? "Validation successful."
            : $"Validation completed: {result.Messages.Count} hint(s).";
    }

    public bool IsToolboxExpanded
    {
        get => _isToolboxExpanded;
        set
        {
            if (!SetProperty(ref _isToolboxExpanded, value))
                return;

            _settings.Layout.TrackPlanPage.IsToolboxExpanded = value;
            PersistSettingsSafely();
        }
    }

    public bool IsPropertiesExpanded
    {
        get => _isPropertiesExpanded;
        set
        {
            if (!SetProperty(ref _isPropertiesExpanded, value))
                return;

            _settings.Layout.TrackPlanPage.IsPropertiesExpanded = value;
            PersistSettingsSafely();
        }
    }

    private void PersistSettingsSafely()
    {
        _settingsService.SaveSettingsAsync(_settings).ContinueWith(
            t =>
            {
                if (t.Exception != null)
                {
                    _logger.LogWarning(t.Exception.GetBaseException(), "Track plan layout settings save failed");
                }
            },
            CancellationToken.None,
            TaskContinuationOptions.OnlyOnFaulted,
            TaskScheduler.Default);
    }

    private void OnEditorStateChanged(object? sender, EventArgs e)
    {
        _ = sender;
        _ = e;
        RefreshEditorState();
    }

    /// <summary>Refreshes command availability after a layout mutation.</summary>
    public void RefreshEditorState()
    {
        OnPropertyChanged(nameof(SelectedTrackId));
        OnPropertyChanged(nameof(SelectedTrack));
        OnPropertyChanged(nameof(HasSelectedTrack));
        OnPropertyChanged(nameof(HasTracks));
        OnPropertyChanged(nameof(CanDeleteSelectedTrack));
        OnPropertyChanged(nameof(CanDisconnectSelectedTrack));
        OnPropertyChanged(nameof(CanRotateSelectedTrack));
        OnPropertyChanged(nameof(CanUndo));
        OnPropertyChanged(nameof(CanRedo));
        OnPropertyChanged(nameof(IsDirty));
        OnPropertyChanged(nameof(SelectionSummary));
        DeleteSelectedTrackCommand.NotifyCanExecuteChanged();
        DisconnectSelectedTrackCommand.NotifyCanExecuteChanged();
        RotateSelectedTrackCommand.NotifyCanExecuteChanged();
        UndoCommand.NotifyCanExecuteChanged();
        RedoCommand.NotifyCanExecuteChanged();
        ValidateCommand.NotifyCanExecuteChanged();
    }

    private string CreateSelectionSummary()
    {
        var selected = SelectedTrack;
        if (selected == null)
        {
            return "No selection";
        }

        var entry = PikoACatalog.All.FirstOrDefault(candidate => candidate.SegmentType == selected.Segment.GetType());
        var code = entry?.Code ?? selected.Segment.GetType().Name;
        var displayName = entry?.DisplayName ?? code;
        var connectionCount = _editorService.Connections.Count(connection =>
            connection.SourceSegment == selected.Segment.No || connection.TargetSegment == selected.Segment.No);
        return $"{code}{Environment.NewLine}{displayName}{Environment.NewLine}{Environment.NewLine}"
            + $"Position: X={selected.X:F0} mm, Y={selected.Y:F0} mm{Environment.NewLine}"
            + $"Rotation: {selected.RotationDegrees:F0}°{Environment.NewLine}"
            + $"Connections: {connectionCount}";
    }
}
