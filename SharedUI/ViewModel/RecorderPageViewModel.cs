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
    private readonly IRecordingFileService _recordingFileService;
    private readonly IRecordingContextProvider _recordingContextProvider;
    private readonly IUiDispatcher _uiDispatcher;
    private readonly ILogger<RecorderPageViewModel> _logger;
    private readonly List<RecordingEntry> _journalEntries = [];
    private RecordingSessionSnapshot _pendingSnapshot;
    private Guid? _loadedSessionId;
    private long _lastLoadedSequence;
    private int _statusRefreshScheduled;
    private int _timelineRefreshScheduled;
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

    /// <summary>Initializes a RecorderPage ViewModel over the shared recording session.</summary>
    public RecorderPageViewModel(
        IRecordingSessionService recordingSessionService,
        IRecordingFileService recordingFileService,
        IRecordingContextProvider recordingContextProvider,
        IUiDispatcher uiDispatcher,
        ILogger<RecorderPageViewModel> logger)
    {
        _recordingSessionService = recordingSessionService ?? throw new ArgumentNullException(nameof(recordingSessionService));
        _recordingFileService = recordingFileService ?? throw new ArgumentNullException(nameof(recordingFileService));
        _recordingContextProvider = recordingContextProvider ?? throw new ArgumentNullException(nameof(recordingContextProvider));
        _uiDispatcher = uiDispatcher ?? throw new ArgumentNullException(nameof(uiDispatcher));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _pendingSnapshot = recordingSessionService.CurrentStatus;
        _recordingSessionService.StatusChanged += OnStatusChanged;
        ApplyStatus(_pendingSnapshot);
    }

    /// <summary>Gets whether at least one filtered timeline entry is visible.</summary>
    public bool HasEntries => TimelineEntries.Count > 0;

    /// <summary>Gets whether an actionable error message is available.</summary>
    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    /// <summary>Gets whether a session is actively recording.</summary>
    public bool IsRecording => State == RecordingSessionState.Recording;

    /// <summary>Gets whether a session is paused.</summary>
    public bool IsPaused => State == RecordingSessionState.Paused;

    /// <summary>Gets whether a completed or imported artifact can be exported.</summary>
    public bool CanExportArtifact => _recordingSessionService.CurrentArtifact is not null;

    partial void OnCategoryFilterChanged(string value) => RebuildFilteredTimeline();

    partial void OnSourceFilterChanged(string value) => RebuildFilteredTimeline();

    partial void OnSeverityFilterChanged(string value) => RebuildFilteredTimeline();

    partial void OnSearchTextChanged(string value) => RebuildFilteredTimeline();

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
        !IsBusy
        && !string.IsNullOrWhiteSpace(SessionName)
        && State is not RecordingSessionState.Recording
        and not RecordingSessionState.Paused
        and not RecordingSessionState.Stopping;

    [RelayCommand(CanExecute = nameof(CanPause))]
    private void Pause() => ApplyOperationResult(_recordingSessionService.Pause());

    private bool CanPause() => !IsBusy && State == RecordingSessionState.Recording;

    [RelayCommand(CanExecute = nameof(CanResume))]
    private void Resume() => ApplyOperationResult(_recordingSessionService.Resume());

    private bool CanResume() => !IsBusy && State == RecordingSessionState.Paused;

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
        !IsBusy && State is RecordingSessionState.Recording or RecordingSessionState.Paused;

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
        !IsBusy
        && !string.IsNullOrWhiteSpace(AnnotationText)
        && State is RecordingSessionState.Recording or RecordingSessionState.Paused;

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
        !IsBusy && State is not RecordingSessionState.Recording
        and not RecordingSessionState.Paused
        and not RecordingSessionState.Stopping;

    [RelayCommand]
    private void ClearFilters()
    {
        CategoryFilter = string.Empty;
        SourceFilter = string.Empty;
        SeverityFilter = string.Empty;
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

    private RecordingFilter CreateFilter() =>
        new(
            categories: ToFilterValues(CategoryFilter),
            sources: ToFilterValues(SourceFilter),
            severities: ToFilterValues(SeverityFilter),
            text: SearchText);

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

    private void ApplyOperationResult(RecordingOperationResult result)
    {
        if (result.Succeeded)
        {
            ClearError();
            return;
        }

        ErrorMessage = result.Message;
    }

    private void ClearError() => ErrorMessage = null;

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
        StartCommand.NotifyCanExecuteChanged();
        PauseCommand.NotifyCanExecuteChanged();
        ResumeCommand.NotifyCanExecuteChanged();
        StopCommand.NotifyCanExecuteChanged();
        AddMarkerCommand.NotifyCanExecuteChanged();
        AddNoteCommand.NotifyCanExecuteChanged();
        ExportCommand.NotifyCanExecuteChanged();
        ImportCommand.NotifyCanExecuteChanged();
    }

    /// <summary>Releases the session status subscription owned by this ViewModel.</summary>
    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;
        _recordingSessionService.StatusChanged -= OnStatusChanged;
    }
}