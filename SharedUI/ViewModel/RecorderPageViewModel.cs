// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.SharedUI.ViewModel;

using Backend.Interface;

using Common.Recording;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Interface;

using Microsoft.Extensions.Logging;

using System.Collections.ObjectModel;

/// <summary>
/// Coordinates recording lifecycle, annotations, filtering, and artifact import/export for RecorderPage.
/// </summary>
public sealed partial class RecorderPageViewModel : ObservableObject, IDisposable
{
    private const int TimelineBatchSize = 512;
    private readonly IRecordingSessionService _recordingSessionService;
    private readonly IRecordingReplayService _recordingReplayService;
    private readonly IRecordingFileService _recordingFileService;
    private readonly IRecordingContextProvider _recordingContextProvider;
    private readonly IUiDispatcher _uiDispatcher;
    private readonly ILogger<RecorderPageViewModel> _logger;
    private readonly List<RecordingEntry> _journalEntries = [];
    private RecordingSessionSnapshot _pendingSnapshot;
    private RecordingReplaySnapshot _pendingReplaySnapshot;
    private Guid? _loadedSessionId;
    private long _lastLoadedSequence;
    private int _statusRefreshScheduled;
    private int _timelineRefreshScheduled;
    private int _replayRefreshScheduled;
    private Guid? _replayLoadedSessionId;
    private bool _isDisposed;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartCommand))]
    private string _sessionName = "Recording";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddMarkerCommand))]
    [NotifyCanExecuteChangedFor(nameof(AddNoteCommand))]
    private string _annotationText = string.Empty;

    [ObservableProperty]
    private string _categoryFilter = string.Empty;

    [ObservableProperty]
    private string _sourceFilter = string.Empty;

    [ObservableProperty]
    private string _severityFilter = string.Empty;

    [ObservableProperty]
    private string _entityFilter = string.Empty;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private RecordingEntry? _selectedEntry;

    [ObservableProperty]
    private ObservableCollection<RecordingEntry> _timelineEntries = [];

    [ObservableProperty]
    private RecordingSessionState _state;

    [ObservableProperty]
    private string _statusText = "Ready to record";

    [ObservableProperty]
    private string _entryCountText = "0 entries";

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartCommand))]
    [NotifyCanExecuteChangedFor(nameof(PauseCommand))]
    [NotifyCanExecuteChangedFor(nameof(ResumeCommand))]
    [NotifyCanExecuteChangedFor(nameof(StopCommand))]
    [NotifyCanExecuteChangedFor(nameof(AddMarkerCommand))]
    [NotifyCanExecuteChangedFor(nameof(AddNoteCommand))]
    [NotifyCanExecuteChangedFor(nameof(ExportCommand))]
    [NotifyCanExecuteChangedFor(nameof(ImportCommand))]
    private bool _isBusy;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(PlayReplayCommand))]
    [NotifyCanExecuteChangedFor(nameof(PauseReplayCommand))]
    [NotifyCanExecuteChangedFor(nameof(StepReplayCommand))]
    [NotifyCanExecuteChangedFor(nameof(SeekReplayCommand))]
    [NotifyCanExecuteChangedFor(nameof(CancelReplayCommand))]
    private bool _isReplayBusy;

    [ObservableProperty]
    private RecordingReplayState _replayState;

    [ObservableProperty]
    private int _replayPosition;

    [ObservableProperty]
    private int _replayMaximum;

    [ObservableProperty]
    private double _replaySeekPosition;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(PlayReplayCommand))]
    private double _selectedReplaySpeed = 1;

    [ObservableProperty]
    private string _replayStatusText = "Load or complete a recording to start isolated replay";

    [ObservableProperty]
    private string _replayElapsedText = "00:00:00";

    [ObservableProperty]
    private RecordingEntry? _currentReplayEntry;

    /// <summary>Initializes a RecorderPage ViewModel over the shared recording session.</summary>
    public RecorderPageViewModel(
        IRecordingSessionService recordingSessionService,
        IRecordingReplayService recordingReplayService,
        IRecordingFileService recordingFileService,
        IRecordingContextProvider recordingContextProvider,
        IUiDispatcher uiDispatcher,
        ILogger<RecorderPageViewModel> logger)
    {
        _recordingSessionService = recordingSessionService ?? throw new ArgumentNullException(nameof(recordingSessionService));
        _recordingReplayService = recordingReplayService ?? throw new ArgumentNullException(nameof(recordingReplayService));
        _recordingFileService = recordingFileService ?? throw new ArgumentNullException(nameof(recordingFileService));
        _recordingContextProvider = recordingContextProvider ?? throw new ArgumentNullException(nameof(recordingContextProvider));
        _uiDispatcher = uiDispatcher ?? throw new ArgumentNullException(nameof(uiDispatcher));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _pendingSnapshot = recordingSessionService.CurrentStatus;
        _pendingReplaySnapshot = recordingReplayService.CurrentStatus;
        _recordingSessionService.StatusChanged += OnStatusChanged;
        _recordingReplayService.StatusChanged += OnReplayStatusChanged;
        ApplyStatus(_pendingSnapshot);
        ApplyReplayStatus(_pendingReplaySnapshot);
    }

    /// <summary>Gets whether at least one filtered timeline entry is visible.</summary>
    public bool HasEntries => this.TimelineEntries.Count > 0;

    /// <summary>Gets whether an actionable error message is available.</summary>
    public bool HasError => !string.IsNullOrWhiteSpace(this.ErrorMessage);

    /// <summary>Gets whether a session is actively recording.</summary>
    public bool IsRecording => this.State == RecordingSessionState.Recording;

    /// <summary>Gets whether a session is paused.</summary>
    public bool IsPaused => this.State == RecordingSessionState.Paused;

    /// <summary>Gets whether a completed or imported artifact can be exported.</summary>
    public bool CanExportArtifact => _recordingSessionService.CurrentArtifact is not null;

    /// <summary>Gets the supported deterministic replay speed multipliers.</summary>
    public IReadOnlyList<double> ReplaySpeeds { get; } = [0.25, 0.5, 1, 2, 4, 8];

    /// <summary>Gets whether an artifact is loaded into the isolated replay boundary.</summary>
    public bool IsReplayLoaded => _pendingReplaySnapshot.IsArtifactLoaded;

    partial void OnCategoryFilterChanged(string value) => RebuildFilteredTimeline();

    partial void OnSourceFilterChanged(string value) => RebuildFilteredTimeline();

    partial void OnSeverityFilterChanged(string value) => RebuildFilteredTimeline();

    partial void OnEntityFilterChanged(string value) => RebuildFilteredTimeline();

    partial void OnSearchTextChanged(string value) => RebuildFilteredTimeline();

    partial void OnSelectedReplaySpeedChanged(double value)
    {
        if (ReplayState == RecordingReplayState.Playing)
        {
            ApplyReplayOperationResult(_recordingReplayService.Play(value));
        }
    }

    partial void OnTimelineEntriesChanged(ObservableCollection<RecordingEntry> value)
    {
        OnPropertyChanged(nameof(HasEntries));
    }

    partial void OnErrorMessageChanged(string? value)
    {
        OnPropertyChanged(nameof(HasError));
    }

    partial void OnStateChanged(RecordingSessionState value)
    {
        OnPropertyChanged(nameof(IsRecording));
        OnPropertyChanged(nameof(IsPaused));
        RefreshCommandStates();
    }

    [RelayCommand(CanExecute = nameof(CanStart))]
    private void Start()
    {
        ClearError();
        var result = _recordingSessionService.Start(new RecordingSessionStartRequest(
            SessionName,
            _recordingContextProvider.SourceApplicationVersion,
            _recordingContextProvider.GetProjectIdentity()));
        ApplyOperationResult(result);
    }

    private bool CanStart() =>
        !this.IsBusy
        && !string.IsNullOrWhiteSpace(this.SessionName)
        && this.State is not RecordingSessionState.Recording
        and not RecordingSessionState.Paused
        and not RecordingSessionState.Stopping;

    [RelayCommand(CanExecute = nameof(CanPause))]
    private void Pause() => ApplyOperationResult(_recordingSessionService.Pause());

    private bool CanPause() => !this.IsBusy && this.State == RecordingSessionState.Recording;

    [RelayCommand(CanExecute = nameof(CanResume))]
    private void Resume() => ApplyOperationResult(_recordingSessionService.Resume());

    private bool CanResume() => !this.IsBusy && this.State == RecordingSessionState.Paused;

    [RelayCommand(CanExecute = nameof(CanStop))]
    private async Task StopAsync()
    {
        ClearError();
        IsBusy = true;
        try
        {
            var result = await _recordingSessionService.StopAsync().ConfigureAwait(false);
            await _uiDispatcher.InvokeOnUiAsync(() =>
            {
                ApplyOperationResult(result.Operation);
                return Task.CompletedTask;
            });
        }
        catch (Exception exception)
        {
            await ReportUnexpectedErrorAsync("Stopping the recording failed.", exception);
        }
        finally
        {
            await _uiDispatcher.InvokeOnUiAsync(() =>
            {
                IsBusy = false;
                return Task.CompletedTask;
            });
        }
    }

    private bool CanStop() =>
        !this.IsBusy && this.State is RecordingSessionState.Recording or RecordingSessionState.Paused;

    [RelayCommand(CanExecute = nameof(CanAddAnnotation))]
    private void AddMarker()
    {
        ApplyOperationResult(_recordingSessionService.AddMarker(AnnotationText));
        if (!HasError) AnnotationText = string.Empty;
    }

    [RelayCommand(CanExecute = nameof(CanAddAnnotation))]
    private void AddNote()
    {
        ApplyOperationResult(_recordingSessionService.AddNote(AnnotationText));
        if (!HasError) AnnotationText = string.Empty;
    }

    private bool CanAddAnnotation() =>
        !this.IsBusy
        && !string.IsNullOrWhiteSpace(this.AnnotationText)
        && this.State is RecordingSessionState.Recording or RecordingSessionState.Paused;

    [RelayCommand(CanExecute = nameof(CanExport))]
    private async Task ExportAsync(CancellationToken cancellationToken)
    {
        var artifact = _recordingSessionService.CurrentArtifact;
        if (artifact is null) return;

        ClearError();
        IsBusy = true;
        try
        {
            var result = await _recordingFileService.ExportAsync(artifact, cancellationToken).ConfigureAwait(false);
            await _uiDispatcher.InvokeOnUiAsync(() =>
            {
                if (!result.Succeeded && !result.WasCancelled)
                {
                    ErrorMessage = result.ErrorMessage ?? "The recording could not be exported.";
                }
                else if (result.Succeeded)
                {
                    StatusText = $"Exported to {result.Path}";
                }

                return Task.CompletedTask;
            });
        }
        catch (Exception exception)
        {
            await ReportUnexpectedErrorAsync("Exporting the recording failed.", exception);
        }
        finally
        {
            await _uiDispatcher.InvokeOnUiAsync(() =>
            {
                IsBusy = false;
                return Task.CompletedTask;
            });
        }
    }

    private bool CanExport() => !IsBusy && CanExportArtifact;

    [RelayCommand(CanExecute = nameof(CanImport))]
    private async Task ImportAsync(CancellationToken cancellationToken)
    {
        ClearError();
        IsBusy = true;
        try
        {
            var fileResult = await _recordingFileService.ImportAsync(cancellationToken).ConfigureAwait(false);
            await _uiDispatcher.InvokeOnUiAsync(() =>
            {
                if (!fileResult.Succeeded && !fileResult.WasCancelled)
                {
                    ErrorMessage = fileResult.ErrorMessage ?? "The recording could not be imported.";
                }
                else if (fileResult.Artifact is not null)
                {
                    ApplyOperationResult(_recordingSessionService.Import(fileResult.Artifact));
                }

                return Task.CompletedTask;
            });
        }
        catch (Exception exception)
        {
            await ReportUnexpectedErrorAsync("Importing the recording failed.", exception);
        }
        finally
        {
            await _uiDispatcher.InvokeOnUiAsync(() =>
            {
                IsBusy = false;
                return Task.CompletedTask;
            });
        }
    }

    private bool CanImport() =>
        !this.IsBusy && this.State is not RecordingSessionState.Recording
        and not RecordingSessionState.Paused
        and not RecordingSessionState.Stopping;

    [RelayCommand(CanExecute = nameof(CanPlayReplay))]
    private void PlayReplay()
    {
        ApplyReplayOperationResult(_recordingReplayService.Play(SelectedReplaySpeed));
    }

    private bool CanPlayReplay() =>
        !IsReplayBusy
        && IsReplayLoaded
        && ReplayState is not RecordingReplayState.Playing
        and not RecordingReplayState.Faulted;

    [RelayCommand(CanExecute = nameof(CanPauseReplay))]
    private void PauseReplay()
    {
        ApplyReplayOperationResult(_recordingReplayService.Pause());
    }

    private bool CanPauseReplay() =>
        !this.IsReplayBusy && this.ReplayState == RecordingReplayState.Playing;

    [RelayCommand(CanExecute = nameof(CanStepReplay))]
    private async Task StepReplayAsync(CancellationToken cancellationToken)
    {
        await ExecuteReplayOperationAsync(
            () => _recordingReplayService.StepAsync(cancellationToken),
            "Stepping isolated replay failed.");
    }

    private bool CanStepReplay() =>
        !IsReplayBusy
        && IsReplayLoaded
        && ReplayPosition < ReplayMaximum
        && ReplayState is not RecordingReplayState.Playing
        and not RecordingReplayState.Faulted;

    [RelayCommand(CanExecute = nameof(CanSeekReplay))]
    private async Task SeekReplayAsync(CancellationToken cancellationToken)
    {
        var targetPosition = (int)Math.Clamp(Math.Round(ReplaySeekPosition), 0, ReplayMaximum);
        await ExecuteReplayOperationAsync(
            () => _recordingReplayService.SeekAsync(targetPosition, cancellationToken),
            "Seeking isolated replay failed.");
    }

    private bool CanSeekReplay() =>
        !IsReplayBusy
        && IsReplayLoaded
        && ReplayState is not RecordingReplayState.Playing
        and not RecordingReplayState.Faulted;

    [RelayCommand(CanExecute = nameof(CanCancelReplay))]
    private async Task CancelReplayAsync(CancellationToken cancellationToken)
    {
        await ExecuteReplayOperationAsync(
            () => _recordingReplayService.CancelAsync(cancellationToken),
            "Cancelling isolated replay failed.");
    }

    private bool CanCancelReplay() =>
        !IsReplayBusy
        && IsReplayLoaded
        && (ReplayPosition > 0 || ReplayState == RecordingReplayState.Playing);

    [RelayCommand]
    private void ClearFilters()
    {
        CategoryFilter = string.Empty;
        SourceFilter = string.Empty;
        SeverityFilter = string.Empty;
        EntityFilter = string.Empty;
        SearchText = string.Empty;
    }

    private void OnStatusChanged(RecordingSessionSnapshot snapshot)
    {
        _pendingSnapshot = snapshot;
        if (Interlocked.Exchange(ref _statusRefreshScheduled, 1) != 0) return;

        _uiDispatcher.InvokeOnUiLowPriority(() =>
        {
            Interlocked.Exchange(ref _statusRefreshScheduled, 0);
            ApplyStatus(_pendingSnapshot);
        });
    }

    private void OnReplayStatusChanged(RecordingReplaySnapshot snapshot)
    {
        _pendingReplaySnapshot = snapshot;
        if (Interlocked.Exchange(ref _replayRefreshScheduled, 1) != 0) return;

        _uiDispatcher.InvokeOnUiLowPriority(() =>
        {
            Interlocked.Exchange(ref _replayRefreshScheduled, 0);
            ApplyReplayStatus(_pendingReplaySnapshot);
        });
    }

    private void ApplyStatus(RecordingSessionSnapshot snapshot)
    {
        if (_loadedSessionId != snapshot.SessionId)
        {
            _loadedSessionId = snapshot.SessionId;
            _lastLoadedSequence = 0;
            _journalEntries.Clear();
            TimelineEntries = [];
            SelectedEntry = null;
        }

        State = snapshot.State;
        EntryCountText = snapshot.EntryCount == 1 ? "1 entry" : $"{snapshot.EntryCount:N0} entries";
        StatusText = CreateStatusText(snapshot);
        if (snapshot.LastFailureCode != RecordingFailureCode.None && !string.IsNullOrWhiteSpace(snapshot.LastFailureMessage))
        {
            ErrorMessage = snapshot.LastFailureMessage;
        }

        OnPropertyChanged(nameof(CanExportArtifact));
        RefreshCommandStates();
        ScheduleTimelineRefresh();
        LoadReplayArtifactIfAvailable();
    }

    private void ApplyReplayStatus(RecordingReplaySnapshot snapshot)
    {
        _pendingReplaySnapshot = snapshot;
        ReplayState = snapshot.State;
        ReplayPosition = snapshot.Position;
        ReplayMaximum = snapshot.TotalEntryCount;
        ReplaySeekPosition = snapshot.Position;
        SelectedReplaySpeed = snapshot.Speed;
        CurrentReplayEntry = snapshot.CurrentEntry;
        ReplayElapsedText = snapshot.Elapsed.ToString("hh\\:mm\\:ss\\.fff");
        ReplayStatusText = CreateReplayStatusText(snapshot);
        if (snapshot.LastFailureCode is RecordingReplayFailureCode.LiveHardwareConnected
            or RecordingReplayFailureCode.ApplyFailed
            or RecordingReplayFailureCode.InternalError)
        {
            ErrorMessage = snapshot.LastFailureMessage;
        }

        OnPropertyChanged(nameof(IsReplayLoaded));
        RefreshReplayCommandStates();
    }

    private void LoadReplayArtifactIfAvailable()
    {
        var artifact = _recordingSessionService.CurrentArtifact;
        if (artifact is null || _replayLoadedSessionId == artifact.Metadata.SessionId) return;

        var result = _recordingReplayService.Load(artifact);
        if (result.Succeeded)
        {
            _replayLoadedSessionId = artifact.Metadata.SessionId;
        }
        else
        {
            ApplyReplayOperationResult(result);
        }
    }

    private void ScheduleTimelineRefresh()
    {
        if (Interlocked.Exchange(ref _timelineRefreshScheduled, 1) != 0) return;
        _uiDispatcher.InvokeOnUiLowPriority(RefreshTimelineBatch);
    }

    private void RefreshTimelineBatch()
    {
        Interlocked.Exchange(ref _timelineRefreshScheduled, 0);
        var entries = _recordingSessionService.ReadEntries(_lastLoadedSequence, TimelineBatchSize);
        if (entries.Count == 0) return;

        var filter = CreateFilter();
        foreach (var entry in entries)
        {
            _journalEntries.Add(entry);
            _lastLoadedSequence = entry.Sequence;
            if (filter.Matches(entry)) TimelineEntries.Add(entry);
        }

        OnPropertyChanged(nameof(HasEntries));
        if (entries.Count == TimelineBatchSize) ScheduleTimelineRefresh();
    }

    private void RebuildFilteredTimeline()
    {
        var selectedSequence = SelectedEntry?.Sequence;
        TimelineEntries = new ObservableCollection<RecordingEntry>(CreateFilter().Apply(_journalEntries));
        SelectedEntry = selectedSequence is null
            ? null
            : TimelineEntries.FirstOrDefault(entry => entry.Sequence == selectedSequence.Value);
    }

    private RecordingFilter CreateFilter()
    {
        var entityValue = string.IsNullOrWhiteSpace(this.EntityFilter) ? null : this.EntityFilter.Trim();
        var hasEntityId = Guid.TryParse(entityValue, out var entityId);
        return new RecordingFilter(
            categories: ToFilterValues(this.CategoryFilter),
            sources: ToFilterValues(this.SourceFilter),
            severities: ToFilterValues(this.SeverityFilter),
            entityKind: hasEntityId ? null : entityValue,
            entityId: hasEntityId ? entityId : null,
            text: this.SearchText);
    }

    private static IEnumerable<string> ToFilterValues(string value) =>
        string.IsNullOrWhiteSpace(value) ? [] : [value.Trim()];

    private static string CreateStatusText(RecordingSessionSnapshot snapshot) => snapshot.State switch
    {
        RecordingSessionState.Idle => "Ready to record",
        RecordingSessionState.Recording => snapshot.IsLimitReached ? "Recording limit reached" : "Recording",
        RecordingSessionState.Paused => "Recording paused",
        RecordingSessionState.Stopping => "Stopping and draining accepted entries",
        RecordingSessionState.Completed => "Recording completed",
        RecordingSessionState.Faulted => "Recording completed with a fault",
        _ => snapshot.State.ToString()
    };

    private static string CreateReplayStatusText(RecordingReplaySnapshot snapshot) => snapshot.State switch
    {
        RecordingReplayState.Idle => "Load or complete a recording to start isolated replay",
        RecordingReplayState.Ready => "Isolated replay ready",
        RecordingReplayState.Playing => $"Replaying at {snapshot.Speed:0.##}x",
        RecordingReplayState.Paused => "Isolated replay paused",
        RecordingReplayState.Completed => "Isolated replay completed",
        RecordingReplayState.Blocked => snapshot.LastFailureMessage ?? "Isolated replay blocked",
        RecordingReplayState.Faulted => snapshot.LastFailureMessage ?? "Isolated replay failed",
        _ => snapshot.State.ToString()
    };

    private void ApplyOperationResult(RecordingOperationResult result)
    {
        if (result.Succeeded)
        {
            ClearError();
            return;
        }

        ErrorMessage = result.Message;
    }

    private void ApplyReplayOperationResult(RecordingReplayOperationResult result)
    {
        if (result.Succeeded)
        {
            ClearError();
            return;
        }

        ErrorMessage = result.Message;
    }

    private async Task ExecuteReplayOperationAsync(
        Func<Task<RecordingReplayOperationResult>> operation,
        string unexpectedErrorMessage)
    {
        ClearError();
        IsReplayBusy = true;
        try
        {
            var result = await operation().ConfigureAwait(false);
            await _uiDispatcher.InvokeOnUiAsync(() =>
            {
                ApplyReplayOperationResult(result);
                return Task.CompletedTask;
            });
        }
        catch (Exception exception)
        {
            await ReportUnexpectedErrorAsync(unexpectedErrorMessage, exception);
        }
        finally
        {
            await _uiDispatcher.InvokeOnUiAsync(() =>
            {
                IsReplayBusy = false;
                return Task.CompletedTask;
            });
        }
    }

    private void ClearError() => this.ErrorMessage = null;

    private async Task ReportUnexpectedErrorAsync(string message, Exception exception)
    {
        _logger.LogError(exception, "{RecorderOperationError}", message);
        await _uiDispatcher.InvokeOnUiAsync(() =>
        {
            ErrorMessage = message;
            return Task.CompletedTask;
        });
    }

    private void RefreshCommandStates()
    {
        this.StartCommand.NotifyCanExecuteChanged();
        this.PauseCommand.NotifyCanExecuteChanged();
        this.ResumeCommand.NotifyCanExecuteChanged();
        this.StopCommand.NotifyCanExecuteChanged();
        this.AddMarkerCommand.NotifyCanExecuteChanged();
        this.AddNoteCommand.NotifyCanExecuteChanged();
        this.ExportCommand.NotifyCanExecuteChanged();
        this.ImportCommand.NotifyCanExecuteChanged();
    }

    private void RefreshReplayCommandStates()
    {
        this.PlayReplayCommand.NotifyCanExecuteChanged();
        this.PauseReplayCommand.NotifyCanExecuteChanged();
        this.StepReplayCommand.NotifyCanExecuteChanged();
        this.SeekReplayCommand.NotifyCanExecuteChanged();
        this.CancelReplayCommand.NotifyCanExecuteChanged();
    }

    /// <summary>Releases the session status subscription owned by this ViewModel.</summary>
    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;
        _recordingSessionService.StatusChanged -= OnStatusChanged;
        _recordingReplayService.StatusChanged -= OnReplayStatusChanged;
    }
}
