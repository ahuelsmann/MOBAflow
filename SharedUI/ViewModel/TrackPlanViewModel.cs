// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using Common.Configuration;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Interface;

using Microsoft.Extensions.Logging;

using TrackPlan.Renderer;
using TrackLibrary.PikoA;
using Backend.Service.TrackPlan;

/// <summary>
/// ViewModel wrapper for <see cref="TrackPlan"/> used by the track plan editor UI.
/// </summary>
public sealed partial class TrackPlanViewModel : ObservableObject, IViewModelWrapper<TrackPlan>
{
    private readonly TrackPlan _model;
    private readonly AppSettings _settings;
    private readonly ISettingsService _settingsService;
    private readonly ILogger<TrackPlanViewModel> _logger;
    private readonly EditableTrackPlan _editablePlan;
    private readonly SelectionService _selectionService;
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
        EditableTrackPlan editablePlan,
        SelectionService selectionService,
        AppSettings settings,
        ISettingsService settingsService,
        ILogger<TrackPlanViewModel> logger)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(editablePlan);
        ArgumentNullException.ThrowIfNull(selectionService);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(settingsService);
        ArgumentNullException.ThrowIfNull(logger);
        _model = model;
        _editablePlan = editablePlan;
        _selectionService = selectionService;
        _settings = settings;
        _settingsService = settingsService;
        _logger = logger;
        _isToolboxExpanded = _settings.Layout.TrackPlanPage.IsToolboxExpanded;
        _isPropertiesExpanded = _settings.Layout.TrackPlanPage.IsPropertiesExpanded;
        _selectionService.SelectionChanged += OnSelectionChanged;
    }

    /// <summary>
    /// Gets the underlying track plan domain model.
    /// </summary>
    public TrackPlan Model => _model;

    /// <summary>Selected track identity, independent of a concrete UI control.</summary>
    public Guid? SelectedTrackId => _selectionService.SelectedTrackId;

    public bool CanDeleteSelectedTrack => SelectedTrackId.HasValue;

    public bool CanDisconnectSelectedTrack => SelectedTrackId.HasValue
        && _editablePlan.Connections.Any(connection =>
            connection.SourceSegment == SelectedTrackId.Value || connection.TargetSegment == SelectedTrackId.Value);

    public bool CanRotateSelectedTrack => SelectedTrackId.HasValue
        && _editablePlan.Segments.Any(segment => segment.Segment.No == SelectedTrackId.Value)
        && !CanDisconnectSelectedTrack;

    public IReadOnlySet<(double X, double Y)> SnapPreviewPorts { get; private set; } = new HashSet<(double X, double Y)>();

    /// <summary>Raised immediately before a command mutates the editor document.</summary>
    public event EventHandler? EditorMutationStarting;

    [RelayCommand]
    public void SelectTrack(Guid? trackId) => _selectionService.Select(trackId);

    [RelayCommand(CanExecute = nameof(CanDeleteSelectedTrack))]
    public void DeleteSelectedTrack()
    {
        if (SelectedTrackId is not Guid trackId)
            return;

        BeginMutation();
        _editablePlan.RemoveSegment(trackId);
        _selectionService.Select(null);
    }

    [RelayCommand(CanExecute = nameof(CanDisconnectSelectedTrack))]
    public void DisconnectSelectedTrack()
    {
        if (SelectedTrackId is not Guid trackId)
            return;

        BeginMutation();
        _editablePlan.DisconnectSegmentFromGroup(trackId);
    }

    [RelayCommand(CanExecute = nameof(CanRotateSelectedTrack))]
    public void RotateSelectedTrack(double deltaDegrees)
    {
        if (SelectedTrackId is not Guid trackId)
            return;

        var placed = _editablePlan.Segments.FirstOrDefault(segment => segment.Segment.No == trackId);
        if (placed == null || !CanRotateSelectedTrack)
            return;

        BeginMutation();
        _editablePlan.UpdateSegmentPosition(trackId, placed.X, placed.Y, NormalizeAngle(placed.RotationDegrees + deltaDegrees));
    }

    [RelayCommand]
    public void AssignSelectedTrackFeedback(int? inPort)
    {
        if (SelectedTrackId is not Guid trackId)
            return;

        var placed = _editablePlan.Segments.FirstOrDefault(segment => segment.Segment.No == trackId);
        if (placed == null || placed.InPort == inPort)
            return;

        BeginMutation();
        _editablePlan.UpdateSegmentInPort(trackId, inPort);
    }

    /// <summary>Updates the renderer-neutral connector preview projected by the interaction layer.</summary>
    public void SetSnapPreview(IReadOnlySet<(double X, double Y)> ports)
    {
        ArgumentNullException.ThrowIfNull(ports);
        SnapPreviewPorts = ports;
        OnPropertyChanged(nameof(SnapPreviewPorts));
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

    private void BeginMutation() => EditorMutationStarting?.Invoke(this, EventArgs.Empty);

    private void OnSelectionChanged(object? sender, EventArgs e)
    {
        RefreshEditorState();
    }

    /// <summary>Refreshes command availability after a layout mutation.</summary>
    public void RefreshEditorState()
    {
        OnPropertyChanged(nameof(SelectedTrackId));
        OnPropertyChanged(nameof(CanDeleteSelectedTrack));
        OnPropertyChanged(nameof(CanDisconnectSelectedTrack));
        OnPropertyChanged(nameof(CanRotateSelectedTrack));
        DeleteSelectedTrackCommand.NotifyCanExecuteChanged();
        DisconnectSelectedTrackCommand.NotifyCanExecuteChanged();
        RotateSelectedTrackCommand.NotifyCanExecuteChanged();
    }

    private static double NormalizeAngle(double degrees)
    {
        while (degrees >= 360)
            degrees -= 360;
        while (degrees < 0)
            degrees += 360;
        return degrees;
    }
}
