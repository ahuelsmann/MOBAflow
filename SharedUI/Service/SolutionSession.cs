// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.SharedUI.Service;

using Backend.Interface;

using Common.Extension;

using CommunityToolkit.Mvvm.ComponentModel;

using Domain;

using Interface;

using Microsoft.Extensions.Logging;

using Sound;

using System.ComponentModel;

using ViewModel;

/// <summary>
/// Owns the loaded solution: its file path, dirty and save state, auto-save coordination, and the selected project
/// and journey. MOBAflow registers it as <see cref="ISolutionSession"/>, <see cref="IProjectContext"/> and
/// <see cref="IJourneySelectionContext"/>.
/// </summary>
public sealed partial class SolutionSession : ObservableObject, ISolutionSession
{
    private static readonly Action<ILogger, Exception?> LogAutoSaveFailed =
        LoggerMessage.Define(
            LogLevel.Warning,
            new EventId(2, nameof(LogAutoSaveFailed)),
            "Auto-save solution failed");

    /// <summary>View-model properties that are UI-only or runtime-backed and must not persist the solution.</summary>
    private static readonly HashSet<string> NonPersistentProperties = new(StringComparer.Ordinal)
    {
        "IsSelected", "IsExpanded", "IsHighlighted", "IsCurrentStation", "CurrentStation", "CurrentPos",
    };

    private static readonly Action<ILogger, Exception?> LogRuntimeUpdateFailed =
        LoggerMessage.Define(
            LogLevel.Warning,
            new EventId(3, nameof(LogRuntimeUpdateFailed)),
            "Update project runtime failed");

    private static readonly Action<ILogger, Exception?> LogProjectActivationFailed =
        LoggerMessage.Define(
            LogLevel.Warning,
            new EventId(1, nameof(LogProjectActivationFailed)),
            "Activate project runtime failed");

    private readonly IIoService _ioService;
    private readonly IUiDispatcher _uiDispatcher;
    private readonly IConnectionRuntime _runtimeConnection;
    private readonly ISoundPlayer? _soundPlayer;
    private readonly ILoggerFactory? _loggerFactory;
    private readonly ILogger<SolutionSession> _logger;

    /// <summary>Ensures at most one solution file write runs at a time (avoids races on temp/rename writes).</summary>
    private readonly SemaphoreSlim _saveSemaphore = new(1, 1);

    private int _autoSaveSuppressionCount;
    private int _drainStarted;
    private long _saveRequestedVersion;
    private bool _isShuttingDown;

    /// <summary>
    /// Initializes a new instance of the <see cref="SolutionSession"/> class and selects the first project.
    /// </summary>
    /// <param name="solution">The application's solution instance; it is replaced in place on load.</param>
    /// <param name="ioService">File access for solutions; <see cref="NullIoService"/> on hosts without files.</param>
    /// <param name="uiDispatcher">Dispatcher for state bound to the UI.</param>
    /// <param name="runtimeConnection">Runtime lifecycle used to activate a loaded or new project.</param>
    /// <param name="logger">Logger for background failures.</param>
    /// <param name="soundPlayer">Optional sound player passed to project view models.</param>
    /// <param name="loggerFactory">Optional logger factory passed to project view models.</param>
    public SolutionSession(
        Solution solution,
        IIoService ioService,
        IUiDispatcher uiDispatcher,
        IConnectionRuntime runtimeConnection,
        ILogger<SolutionSession> logger,
        ISoundPlayer? soundPlayer = null,
        ILoggerFactory? loggerFactory = null)
    {
        ArgumentNullException.ThrowIfNull(solution);
        ArgumentNullException.ThrowIfNull(ioService);
        ArgumentNullException.ThrowIfNull(uiDispatcher);
        ArgumentNullException.ThrowIfNull(runtimeConnection);
        ArgumentNullException.ThrowIfNull(logger);

        _ioService = ioService;
        _uiDispatcher = uiDispatcher;
        _runtimeConnection = runtimeConnection;
        _logger = logger;
        _soundPlayer = soundPlayer;
        _loggerFactory = loggerFactory;

        // Ensure the solution always has at least one project.
        if (solution.Projects.Count == 0)
        {
            solution.Projects.Add(new Project { Name = "(Untitled Project)" });
        }

        Solution = solution;
        SolutionViewModel = new SolutionViewModel(solution, _uiDispatcher, _ioService, _soundPlayer, _loggerFactory);
        HasSolution = solution.Projects.Count > 0;
        SelectedProject = SolutionViewModel.Projects.FirstOrDefault();
    }

    /// <inheritdoc />
    public event EventHandler? SolutionSaving;

    /// <inheritdoc />
    public event EventHandler? SolutionLoaded;

    /// <inheritdoc />
    public event EventHandler? SolutionReplacing;

    /// <inheritdoc />
    public event PropertyChangedEventHandler? ModelChanged;

    /// <inheritdoc />
    public Solution Solution { get; }

    /// <inheritdoc />
    [ObservableProperty]
    public partial SolutionViewModel? SolutionViewModel { get; private set; }

    /// <inheritdoc />
    [ObservableProperty]
    public partial ProjectViewModel? SelectedProject { get; set; }

    /// <inheritdoc />
    [ObservableProperty]
    public partial JourneyViewModel? SelectedJourney { get; set; }

    /// <inheritdoc />
    [ObservableProperty]
    public partial string? CurrentSolutionPath { get; set; }

    /// <inheritdoc />
    [ObservableProperty]
    public partial bool HasUnsavedChanges { get; private set; }

    /// <inheritdoc />
    [ObservableProperty]
    public partial bool HasSolution { get; private set; }

    /// <inheritdoc />
    [ObservableProperty]
    public partial SolutionSaveState SolutionSaveState { get; private set; } = SolutionSaveState.NotSaved;

    /// <inheritdoc />
    [ObservableProperty]
    public partial string SolutionSaveStatusText { get; private set; } = "Not saved";

    /// <inheritdoc />
    public bool CanSave => _ioService is not NullIoService;

    /// <inheritdoc />
    public bool IsAutoSaveSuppressed => Volatile.Read(ref _autoSaveSuppressionCount) > 0;

    /// <inheritdoc />
    public async Task SaveSolutionInternalAsync()
    {
        MarkDirty();
        var requestVersion = BeginAutoSaveRequest();

        if (_ioService is NullIoService)
        {
            SetSaveStatus(SolutionSaveState.NotSaved, "Not saved");
            return;
        }

        var currentPath = CurrentSolutionPath;
        if (_isShuttingDown || string.IsNullOrWhiteSpace(currentPath))
        {
            SetSaveStatus(
                SolutionSaveState.NotSaved,
                string.IsNullOrWhiteSpace(currentPath)
                    ? "Not saved - choose Save As"
                    : "Not saved - application is shutting down");
            return;
        }

        try
        {
            await SaveCoreAsync(currentPath, allowPathSelection: false, requestVersion).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException
                                   or InvalidOperationException or NotSupportedException)
        {
            if (IsLatestAutoSaveRequest(requestVersion))
            {
                SetSaveStatus(SolutionSaveState.NotSaved, $"Not saved - {ex.Message}");
            }

            throw;
        }
    }

    /// <inheritdoc />
    public async Task<SolutionSaveResult> SaveSolutionWithStatusAsync()
    {
        await SaveSolutionInternalAsync().ConfigureAwait(false);
        return await _uiDispatcher.InvokeOnUiAsync(() => Task.FromResult(
            new SolutionSaveResult(SolutionSaveState, SolutionSaveStatusText))).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task<bool> SaveAsync(bool allowPathSelection) =>
        SaveCoreAsync(CurrentSolutionPath, allowPathSelection);

    /// <inheritdoc />
    public async Task NewSolutionAsync()
    {
        // Stay on the UI thread: the following steps change collections bound to the UI.
        if (HasUnsavedChanges && !await SaveCoreAsync(CurrentSolutionPath, allowPathSelection: true).ConfigureAwait(true))
        {
            return;
        }

        using (SuppressAutoSave())
        {
            Solution.Projects.Clear();
            Solution.Name = "New Solution";

            var newProject = new Project
            {
                Name = "New Project",
                Journeys = [],
                Workflows = [],
                Trains = []
            };
            Solution.Projects.Add(newProject);

            SolutionViewModel?.Refresh();

            CurrentSolutionPath = null;
            MarkDirty();
            SetSaveStatus(SolutionSaveState.NotSaved, "Not saved - choose Save As");

            ClearSelection();

            await _runtimeConnection.ActivateProjectAsync(newProject).ConfigureAwait(false);

            SolutionLoaded?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <inheritdoc />
    public async Task LoadSolutionAsync()
    {
        if (_ioService is NullIoService)
            return;

        var (loadedSolution, path, error) = await _ioService.LoadAsync().ConfigureAwait(false);
        ApplyLoadResult(loadedSolution, path, error);
    }

    /// <inheritdoc />
    public async Task LoadSolutionFromPathAsync(string filePath)
    {
        if (_ioService is NullIoService)
            return;

        var (loadedSolution, path, error) = await _ioService.LoadFromPathAsync(filePath).ConfigureAwait(false);
        ApplyLoadResult(loadedSolution, path, error);
    }

    /// <inheritdoc />
    public ProjectViewModel AddProject(Project project)
    {
        ArgumentNullException.ThrowIfNull(project);
        Solution.Projects.Add(project);

        var projectViewModel = new ProjectViewModel(project, _uiDispatcher, _ioService, _soundPlayer, _loggerFactory);
        SolutionViewModel!.Projects.Add(projectViewModel);

        SelectedProject = projectViewModel;
        HasSolution = true;
        return projectViewModel;
    }

    /// <inheritdoc />
    public void RemoveProject(ProjectViewModel project)
    {
        ArgumentNullException.ThrowIfNull(project);
        Solution.Projects.Remove(project.Model);
        SolutionViewModel!.Projects.Remove(project);

        SelectedProject = SolutionViewModel.Projects.FirstOrDefault();
        if (SelectedProject == null)
        {
            HasSolution = false;
        }
    }

    /// <inheritdoc />
    public void TrackChanges(INotifyPropertyChanged source)
    {
        ArgumentNullException.ThrowIfNull(source);
        source.PropertyChanged -= OnTrackedModelPropertyChanged;
        source.PropertyChanged += OnTrackedModelPropertyChanged;
    }

    /// <inheritdoc />
    public void UntrackChanges(INotifyPropertyChanged source)
    {
        ArgumentNullException.ThrowIfNull(source);
        source.PropertyChanged -= OnTrackedModelPropertyChanged;
    }

    /// <inheritdoc />
    public IDisposable SuppressAutoSave()
    {
        Interlocked.Increment(ref _autoSaveSuppressionCount);
        return new AutoSaveSuppression(this);
    }

    /// <inheritdoc />
    public void BeginShutdown() => _isShuttingDown = true;

    /// <inheritdoc />
    public async Task DrainPendingSaveAsync()
    {
        if (Interlocked.CompareExchange(ref _drainStarted, 1, 0) != 0)
        {
            return;
        }

        try
        {
            await _saveSemaphore.WaitAsync().ConfigureAwait(false);
            _saveSemaphore.Release();
        }
        catch (ObjectDisposedException)
        {
            return;
        }

        _saveSemaphore.Dispose();
    }

    partial void OnSelectedProjectChanged(ProjectViewModel? oldValue, ProjectViewModel? newValue)
    {
        if (oldValue != null)
        {
            UntrackChanges(oldValue);
        }

        if (newValue == null)
        {
            return;
        }

        TrackChanges(newValue);
        foreach (var workflow in newValue.Workflows)
        {
            TrackChanges(workflow);
        }

        foreach (var train in newValue.Trains)
        {
            TrackChanges(train);
        }
    }

    partial void OnSelectedJourneyChanged(JourneyViewModel? oldValue, JourneyViewModel? newValue)
    {
        if (oldValue != null)
        {
            UntrackChanges(oldValue);
        }

        if (newValue != null)
        {
            TrackChanges(newValue);
        }
    }

    private void OnTrackedModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (IsAutoSaveSuppressed || (e.PropertyName is { } name && NonPersistentProperties.Contains(name)))
        {
            return;
        }

        // The runtime reads a snapshot of the project; refresh it so the change takes effect immediately.
        if (SelectedProject is { } project)
        {
            _runtimeConnection.UpdateProjectAsync(project.Model).Observe(ex => LogRuntimeUpdateFailed(_logger, ex));
        }

        ModelChanged?.Invoke(sender, e);
        SaveSolutionInternalAsync().Observe(ex => LogAutoSaveFailed(_logger, ex));
    }

    private void ApplyLoadResult(Solution? loadedSolution, string? path, string? error)
    {
        if (!string.IsNullOrEmpty(error))
        {
            throw new InvalidOperationException($"Failed to load solution: {error}");
        }

        if (loadedSolution != null && path != null)
        {
            _uiDispatcher.InvokeOnUi(() => ApplyLoadedSolution(loadedSolution, path));
        }
    }

    /// <summary>Single source of truth for applying a loaded solution.</summary>
    private void ApplyLoadedSolution(Solution loadedSolution, string path)
    {
        using (SuppressAutoSave())
        {
            ClearSelection();

            Solution.Projects.Clear();
            foreach (var project in loadedSolution.Projects)
            {
                Solution.Projects.Add(project);
            }

            Solution.Name = loadedSolution.Name;
            SolutionViewModel?.Refresh();

            CurrentSolutionPath = path;
            HasUnsavedChanges = false;
            SolutionSaveState = SolutionSaveState.Saved;
            SolutionSaveStatusText = "Saved";
            HasSolution = Solution.Projects.Count > 0;

            if (Solution.Projects.Count > 0)
            {
                SelectedProject = SolutionViewModel?.Projects.FirstOrDefault();
                _runtimeConnection.ActivateProjectAsync(Solution.Projects[0])
                    .Observe(ex => LogProjectActivationFailed(_logger, ex));
            }

            OnPropertyChanged(nameof(Solution));
            SolutionLoaded?.Invoke(this, EventArgs.Empty);
        }
    }

    private void ClearSelection()
    {
        SolutionReplacing?.Invoke(this, EventArgs.Empty);
        SelectedProject = null;
        SelectedJourney = null;
    }

    private async Task<bool> SaveCoreAsync(string? currentPath, bool allowPathSelection, long? autoSaveRequestVersion = null)
    {
        if (_ioService is NullIoService || _isShuttingDown)
            return false;

        if (!await TryEnterSaveAsync().ConfigureAwait(false))
            return false;

        try
        {
            SolutionSaving?.Invoke(this, EventArgs.Empty);

            var result = await SaveAtPathAsync(currentPath, allowPathSelection).ConfigureAwait(false);
            return CompleteSave(result, autoSaveRequestVersion);
        }
        finally
        {
            ReleaseSaveSemaphore();
        }
    }

    private async Task<bool> TryEnterSaveAsync()
    {
        try
        {
            await _saveSemaphore.WaitAsync().ConfigureAwait(false);
            return true;
        }
        catch (ObjectDisposedException)
        {
            return false;
        }
    }

    private async Task<(bool success, string? path, string? error)> SaveAtPathAsync(string? currentPath, bool allowPathSelection)
    {
        if (!string.IsNullOrWhiteSpace(currentPath))
            return await _ioService.SaveAsync(Solution, currentPath).ConfigureAwait(false);

        if (allowPathSelection)
            return await _ioService.SaveAsAsync(Solution).ConfigureAwait(false);

        return (false, null, null);
    }

    private bool CompleteSave((bool success, string? path, string? error) result, long? autoSaveRequestVersion)
    {
        if (result.success && result.path != null)
        {
            ApplySuccessfulSave(result.path, autoSaveRequestVersion);
            return true;
        }

        if (!string.IsNullOrEmpty(result.error))
            throw new InvalidOperationException($"Failed to save solution: {result.error}");

        return false;
    }

    private void ApplySuccessfulSave(string path, long? autoSaveRequestVersion)
    {
        var isLatestAutoSave = !autoSaveRequestVersion.HasValue || IsLatestAutoSaveRequest(autoSaveRequestVersion.Value);
        _uiDispatcher.InvokeOnUi(() =>
        {
            CurrentSolutionPath = path;
            HasUnsavedChanges = !isLatestAutoSave;
            SolutionSaveState = isLatestAutoSave ? SolutionSaveState.Saved : SolutionSaveState.Saving;
            SolutionSaveStatusText = isLatestAutoSave ? "Saved" : "Saving";
        });
    }

    private void ReleaseSaveSemaphore()
    {
        try
        {
            _saveSemaphore.Release();
        }
        catch (ObjectDisposedException)
        {
            // The semaphore was disposed during shutdown while this save was finishing.
        }
    }

    private void MarkDirty() => _uiDispatcher.InvokeOnUi(() => HasUnsavedChanges = true);

    private long BeginAutoSaveRequest()
    {
        var version = Interlocked.Increment(ref _saveRequestedVersion);
        SetSaveStatus(SolutionSaveState.Saving, "Saving");
        return version;
    }

    private bool IsLatestAutoSaveRequest(long version) => version == Volatile.Read(ref _saveRequestedVersion);

    private void SetSaveStatus(SolutionSaveState state, string text)
    {
        _uiDispatcher.InvokeOnUi(() =>
        {
            SolutionSaveState = state;
            SolutionSaveStatusText = text;
        });
    }

    private sealed class AutoSaveSuppression(SolutionSession session) : IDisposable
    {
        private int _disposed;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 0)
            {
                Interlocked.Decrement(ref session._autoSaveSuppressionCount);
            }
        }
    }
}
