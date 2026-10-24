// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.SharedUI.Service;

using Backend.Interface;

using Common.Extension;

using CommunityToolkit.Mvvm.ComponentModel;

using Domain;

using Interface;

using Microsoft.Extensions.Logging;

using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json;

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

    private readonly ISolutionIoService _solutionIo;
    private readonly IUiDispatcher _uiDispatcher;
    private readonly IProjectRuntimeHost _projectRuntimes;
    private readonly SolutionViewModelServices _viewModelServices;
    private readonly ILogger<SolutionSession> _logger;

    /// <summary>Ensures at most one solution file write runs at a time (avoids races on temp/rename writes).</summary>
    private readonly SemaphoreSlim _saveSemaphore = new(1, 1);

    private int _autoSaveSuppressionCount;
    private int _drainStarted;

    /// <summary>Counts edits that requested a save; a save snapshot records the count it contains.</summary>
    private long _saveRequestedVersion;

    /// <summary>Changes on the UI thread whenever the solution is replaced; a save only applies to its own generation.</summary>
    private long _solutionGeneration;

    /// <summary>The project each tracked source belonged to when it was tracked, for runtime refreshes after a project switch.</summary>
    private readonly ConditionalWeakTable<object, ProjectViewModel> _trackedOwners = [];
    private bool _isShuttingDown;

    /// <summary>
    /// Initializes a new instance of the <see cref="SolutionSession"/> class and selects the first project.
    /// </summary>
    /// <param name="solution">The application's solution instance; it is replaced in place on load.</param>
    /// <param name="solutionIo">Solution file access; <see cref="NullSolutionIoService"/> on hosts without files.</param>
    /// <param name="uiDispatcher">Dispatcher for state bound to the UI.</param>
    /// <param name="projectRuntimes">Creates and discards one runtime per project of the solution.</param>
    /// <param name="logger">Logger for background failures.</param>
    /// <param name="viewModelServices">Services passed to the solution and project view models; file picking is unavailable when omitted.</param>
    public SolutionSession(
        Solution solution,
        ISolutionIoService solutionIo,
        IUiDispatcher uiDispatcher,
        IProjectRuntimeHost projectRuntimes,
        ILogger<SolutionSession> logger,
        SolutionViewModelServices? viewModelServices = null)
    {
        ArgumentNullException.ThrowIfNull(solution);
        ArgumentNullException.ThrowIfNull(solutionIo);
        ArgumentNullException.ThrowIfNull(uiDispatcher);
        ArgumentNullException.ThrowIfNull(projectRuntimes);
        ArgumentNullException.ThrowIfNull(logger);

        _solutionIo = solutionIo;
        _uiDispatcher = uiDispatcher;
        _projectRuntimes = projectRuntimes;
        _logger = logger;
        _viewModelServices = viewModelServices ?? new SolutionViewModelServices(new NullFilePickerService());

        // Ensure the solution always has at least one project.
        if (solution.Projects.Count == 0)
        {
            solution.Projects.Add(new Project { Name = "(Untitled Project)" });
        }

        Solution = solution;
        SolutionViewModel = new SolutionViewModel(solution, _uiDispatcher, _viewModelServices.FilePicker, _viewModelServices.SoundPlayer, _viewModelServices.LoggerFactory);
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
    public bool CanSave => _solutionIo.CanPersist;

    /// <inheritdoc />
    public bool IsAutoSaveSuppressed => Volatile.Read(ref _autoSaveSuppressionCount) > 0;

    /// <inheritdoc />
    public async Task SaveSolutionInternalAsync()
    {
        MarkDirty();
        var requestVersion = BeginAutoSaveRequest();

        if (!_solutionIo.CanPersist)
        {
            SetSaveStatus(SolutionSaveState.NotSaved, "Not saved");
            return;
        }

        var currentPath = CurrentSolutionPath;
        var generation = Volatile.Read(ref _solutionGeneration);
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
            await SaveCoreAsync(currentPath, allowPathSelection: false, generation, requestVersion).ConfigureAwait(false);
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
        SaveCoreAsync(CurrentSolutionPath, allowPathSelection, Volatile.Read(ref _solutionGeneration));

    /// <inheritdoc />
    public async Task NewSolutionAsync()
    {
        // Stay on the UI thread: the following steps change collections bound to the UI.
        if (HasUnsavedChanges && !await SaveCoreAsync(CurrentSolutionPath, allowPathSelection: true, Volatile.Read(ref _solutionGeneration)).ConfigureAwait(true))
        {
            return;
        }

        using (SuppressAutoSave())
        {
            Interlocked.Increment(ref _solutionGeneration);
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

            // Subscribers of SolutionLoaded change UI-bound collections; stay on the UI thread.
            await _projectRuntimes.LoadAsync([.. Solution.Projects]).ConfigureAwait(true);

            SolutionLoaded?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <inheritdoc />
    public async Task LoadSolutionAsync()
    {
        if (!_solutionIo.CanPersist)
            return;

        var (loadedSolution, path, error) = await _solutionIo.LoadAsync().ConfigureAwait(false);
        ApplyLoadResult(loadedSolution, path, error);
    }

    /// <inheritdoc />
    public async Task LoadSolutionFromPathAsync(string filePath)
    {
        if (!_solutionIo.CanPersist)
            return;

        var (loadedSolution, path, error) = await _solutionIo.LoadFromPathAsync(filePath).ConfigureAwait(false);
        ApplyLoadResult(loadedSolution, path, error);
    }

    /// <inheritdoc />
    public Task StartRuntimesAsync() => _projectRuntimes.LoadAsync([.. Solution.Projects]);

    /// <inheritdoc />
    public IReadOnlyList<string> GetConnectedProjectNames(IReadOnlyCollection<Guid>? projectIds = null)
    {
        var connected = _projectRuntimes.ConnectedProjectIds;
        return
        [
            .. Solution.Projects
                .Where(project => connected.Contains(project.Id) && (projectIds is null || projectIds.Contains(project.Id)))
                .Select(project => project.Name)
        ];
    }

    /// <inheritdoc />
    public Task StopRuntimesAsync(CancellationToken cancellationToken = default) =>
        _projectRuntimes.LoadAsync([], cancellationToken);

    /// <inheritdoc />
    public ProjectViewModel AddProject(Project project)
    {
        ArgumentNullException.ThrowIfNull(project);
        Solution.Projects.Add(project);
        _projectRuntimes.AddAsync(project).Observe(ex => LogProjectActivationFailed(_logger, ex));

        var projectViewModel = new ProjectViewModel(project, _uiDispatcher, _viewModelServices.FilePicker, _viewModelServices.SoundPlayer, _viewModelServices.LoggerFactory);
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
        _projectRuntimes.RemoveAsync(project.Model.Id).Observe(ex => LogProjectActivationFailed(_logger, ex));

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
        if (SelectedProject is { } owner)
        {
            _trackedOwners.AddOrUpdate(source, owner);
        }
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
        // Selecting a project only decides which runtime the UI shows; every project keeps running.
        _projectRuntimes.SelectProject(newValue?.Model.Id);
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

        // The runtime reads a snapshot of the project; refresh it so the change takes effect immediately. A change can
        // arrive after another project was selected, so the runtime of the project that owns the source is refreshed.
        // A new Z21 assignment needs a new runtime on the new connection.
        if (OwnerOf(sender) is { } project)
        {
            var update = sender is ProjectViewModel
                && e.PropertyName is nameof(ProjectViewModel.Z21IpAddress) or nameof(ProjectViewModel.Z21Port)
                ? _projectRuntimes.ReplaceAsync(project.Model)
                : _projectRuntimes.UpdateAsync(project.Model);
            update.Observe(ex => LogRuntimeUpdateFailed(_logger, ex));
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
            Interlocked.Increment(ref _solutionGeneration);
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

            // One runtime per project; the runtimes of the previous solution are discarded.
            _projectRuntimes.LoadAsync([.. Solution.Projects])
                .Observe(ex => LogProjectActivationFailed(_logger, ex));
            if (Solution.Projects.Count > 0)
            {
                SelectedProject = SolutionViewModel?.Projects.FirstOrDefault();
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

    private ProjectViewModel? OwnerOf(object? sender) => sender switch
    {
        ProjectViewModel project => project,
        not null when _trackedOwners.TryGetValue(sender, out var owner) => owner,
        _ => SelectedProject
    };

    // Saves enter the write lock before they take their snapshot, so writes happen in snapshot order and a later write
    // never holds an older state. The snapshot is taken on the UI thread: SolutionSaving subscribers and the
    // serialization see one consistent state, and a snapshot is only written to the path of its own solution.
    private async Task<bool> SaveCoreAsync(
        string? currentPath, bool allowPathSelection, long generation, long? autoSaveRequestVersion = null)
    {
        if (!_solutionIo.CanPersist || _isShuttingDown)
            return false;

        if (!await TryEnterSaveAsync().ConfigureAwait(false))
            return false;

        try
        {
            var snapshot = await OnUiAsync(() => Task.FromResult(TakeSnapshot(generation))).ConfigureAwait(false);
            if (snapshot is null)
                return false;

            var result = await SaveAtPathAsync(snapshot.Solution, currentPath, allowPathSelection).ConfigureAwait(false);
            return CompleteSave(result, generation, snapshot.EditVersion, isManualSave: !autoSaveRequestVersion.HasValue);
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

    /// <summary>Runs <paramref name="work"/> on the UI thread (inline when already there) and returns its result.</summary>
    private Task<T> OnUiAsync<T>(Func<Task<T>> work)
    {
        var completion = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        _uiDispatcher.InvokeOnUi(() =>
        {
            try
            {
                _ = CompleteFromAsync(work(), completion);
            }
            catch (Exception ex) when (completion.TrySetException(ex))
            {
                // The awaiting save receives the failure, as it did when SolutionSaving ran inside the save.
            }
        });
        return completion.Task;
    }

    private static async Task CompleteFromAsync<T>(Task<T> work, TaskCompletionSource<T> completion)
    {
        try
        {
            completion.TrySetResult(await work.ConfigureAwait(false));
        }
        catch (Exception ex) when (completion.TrySetException(ex))
        {
            // Forwarded to the awaiting save.
        }
    }

    private SaveSnapshot? TakeSnapshot(long generation)
    {
        // A solution replaced since the request is not saved under its old path.
        if (generation != Volatile.Read(ref _solutionGeneration))
            return null;

        SolutionSaving?.Invoke(this, EventArgs.Empty);
        return new SaveSnapshot(CopyForSave(Solution), Volatile.Read(ref _saveRequestedVersion));
    }

    private static Solution CopyForSave(Solution solution) =>
        JsonSerializer.Deserialize<Solution>(JsonSerializer.Serialize(solution, JsonOptions.Default), JsonOptions.Default)
        ?? throw new InvalidOperationException("The solution could not be copied for saving.");

    private async Task<(bool success, string? path, string? error)> SaveAtPathAsync(
        Solution snapshot, string? currentPath, bool allowPathSelection)
    {
        if (!string.IsNullOrWhiteSpace(currentPath))
            return await _solutionIo.SaveAsync(snapshot, currentPath).ConfigureAwait(false);

        // The picker belongs to the UI thread; the save continues on the thread pool after the snapshot.
        if (allowPathSelection)
            return await OnUiAsync(() => _solutionIo.SaveAsAsync(snapshot)).ConfigureAwait(false);

        return (false, null, null);
    }

    private bool CompleteSave((bool success, string? path, string? error) result, long generation, long editVersion, bool isManualSave)
    {
        if (result.success && result.path != null)
        {
            ApplySuccessfulSave(result.path, generation, editVersion, isManualSave);
            return true;
        }

        if (!string.IsNullOrEmpty(result.error))
            throw new InvalidOperationException($"Failed to save solution: {result.error}");

        return false;
    }

    private void ApplySuccessfulSave(string path, long generation, long editVersion, bool isManualSave)
    {
        // The written snapshot holds every edit up to its edit version; a later edit keeps the solution dirty.
        var isLatest = editVersion == Volatile.Read(ref _saveRequestedVersion);
        _uiDispatcher.InvokeOnUi(() =>
        {
            // The file of an earlier solution was written; the loaded solution keeps its own path and state.
            if (generation != Volatile.Read(ref _solutionGeneration))
            {
                return;
            }

            CurrentSolutionPath = path;
            HasUnsavedChanges = !isLatest;
            SolutionSaveState = isLatest ? SolutionSaveState.Saved : SolutionSaveState.Saving;
            SolutionSaveStatusText = isLatest ? "Saved" : "Saving";

            // An auto-save that is not the latest is followed by the save of the newer edit. A manual save is not:
            // edits made while its picker was open could not be saved without a path, so save them to the new path now.
            if (!isLatest && isManualSave)
            {
                SaveSolutionInternalAsync().Observe(ex => LogAutoSaveFailed(_logger, ex));
            }
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

    /// <summary>A copy of the solution and the edit version it contains.</summary>
    private sealed record SaveSnapshot(Solution Solution, long EditVersion);

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
