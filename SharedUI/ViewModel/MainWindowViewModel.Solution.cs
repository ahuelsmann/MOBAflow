// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using CommunityToolkit.Mvvm.Input;

using Domain;

using Service;

/// <summary>
/// MainWindowViewModel - Solution and Project Management
/// Handles solution lifecycle (New, Load, Save) and project management.
/// </summary>
public partial class MainWindowViewModel
{
    #region Solution Events
    /// <summary>
    /// Raised before saving the Solution. Subscribers should sync their data to Domain models.
    /// </summary>
    public event EventHandler? SolutionSaving;

    /// <summary>
    /// Raised after loading a Solution. Subscribers should load their data from Domain models.
    /// </summary>
    public event EventHandler? SolutionLoaded;
    #endregion

    #region Solution Management
    partial void OnSolutionChanged(Solution value)
    {
        // Ensure Solution always has at least one project
        if (value.Projects.Count == 0)
        {
            value.Projects.Add(new Project { Name = "(Untitled Project)" });
        }

        SolutionViewModel = new SolutionViewModel(value, _uiDispatcher, _ioService, _executionContext.SoundPlayer, _loggerFactory);
        HasSolution = value.Projects.Count > 0;

        // Auto-select first project if no project is selected
        if (SelectedProject == null)
        {
            SelectedProject = SolutionViewModel.Projects.FirstOrDefault();
        }

        // NOTE: JourneyManager initialization moved to ApplyLoadedSolution()
        // This ensures JourneyManager is always initialized with the REAL loaded project,
        // not the empty default project created here.

        SaveSolutionCommand.NotifyCanExecuteChanged();
        ConnectCommand.NotifyCanExecuteChanged();
        OnPropertyChanged(nameof(SelectedProject));

        LoadCities();
    }

    [RelayCommand(CanExecute = nameof(CanSaveSolution))]
    private async Task SaveSolutionAsync()
    {
        await SaveSolutionCoreAsync(CurrentSolutionPath, allowPathSelection: true);
    }

    /// <summary>
    /// Marks the solution as changed and persists it without opening a file picker.
    /// </summary>
    public async Task SaveSolutionInternalAsync()
    {
        MarkSolutionDirty();
        var requestVersion = BeginSolutionAutoSaveRequest();

        // Skip if IoService not available (WebApp/MAUI)
        if (_ioService is NullIoService)
        {
            SetSolutionSaveStatus(SolutionSaveState.NotSaved, "Not saved");
            return;
        }

        var currentPath = CurrentSolutionPath;
        if (_isShuttingDown || string.IsNullOrWhiteSpace(currentPath))
        {
            SetSolutionSaveStatus(
                SolutionSaveState.NotSaved,
                string.IsNullOrWhiteSpace(currentPath)
                    ? "Not saved - choose Save As"
                    : "Not saved - application is shutting down");
            return;
        }

        try
        {
            await SaveSolutionCoreAsync(
                currentPath,
                allowPathSelection: false,
                requestVersion).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException
                                   or InvalidOperationException or NotSupportedException)
        {
            if (IsLatestSolutionAutoSaveRequest(requestVersion))
            {
                SetSolutionSaveStatus(SolutionSaveState.NotSaved, $"Not saved - {ex.Message}");
            }

            throw;
        }
    }

    private async Task<bool> SaveSolutionCoreAsync(
        string? currentPath,
        bool allowPathSelection,
        long? autoSaveRequestVersion = null)
    {
        if (_ioService is NullIoService || _isShuttingDown)
            return false;

        if (!await TryEnterSolutionSaveAsync().ConfigureAwait(false))
            return false;

        try
        {
            // Notify subscribers to sync their data before saving
            SolutionSaving?.Invoke(this, EventArgs.Empty);

            var result = await SaveSolutionAtPathAsync(
                currentPath,
                allowPathSelection).ConfigureAwait(false);
            return CompleteSolutionSave(result, autoSaveRequestVersion);
        }
        finally
        {
            ReleaseSolutionSaveSemaphore();
        }
    }

    private async Task<bool> TryEnterSolutionSaveAsync()
    {
        try
        {
            await _solutionSaveSemaphore.WaitAsync().ConfigureAwait(false);
            return true;
        }
        catch (ObjectDisposedException)
        {
            return false;
        }
    }

    private async Task<(bool success, string? path, string? error)> SaveSolutionAtPathAsync(
        string? currentPath,
        bool allowPathSelection)
    {
        if (!string.IsNullOrWhiteSpace(currentPath))
            return await _ioService.SaveAsync(Solution, currentPath).ConfigureAwait(false);

        if (allowPathSelection)
            return await _ioService.SaveAsAsync(Solution).ConfigureAwait(false);

        return (false, null, null);
    }

    private bool CompleteSolutionSave(
        (bool success, string? path, string? error) result,
        long? autoSaveRequestVersion)
    {
        if (result.success && result.path != null)
        {
            ApplySuccessfulSolutionSave(result.path, autoSaveRequestVersion);
            return true;
        }

        if (!string.IsNullOrEmpty(result.error))
            throw new InvalidOperationException($"Failed to save solution: {result.error}");

        return false;
    }

    private void ApplySuccessfulSolutionSave(string path, long? autoSaveRequestVersion)
    {
        var isLatestAutoSave = !autoSaveRequestVersion.HasValue ||
            IsLatestSolutionAutoSaveRequest(autoSaveRequestVersion.Value);
        // Marshal to UI thread to update observable properties bound to UI
        _uiDispatcher.InvokeOnUi(() =>
        {
            CurrentSolutionPath = path;
            HasUnsavedChanges = !isLatestAutoSave;
            SolutionSaveState = isLatestAutoSave
                ? SolutionSaveState.Saved
                : SolutionSaveState.Saving;
            SolutionSaveStatusText = isLatestAutoSave ? "Saved" : "Saving";
        });
    }

    private void ReleaseSolutionSaveSemaphore()
    {
        try
        {
            _solutionSaveSemaphore.Release();
        }
        catch (ObjectDisposedException)
        {
            // Semaphore was disposed during shutdown while this save was finishing.
        }
    }

    private void MarkSolutionDirty()
    {
        _uiDispatcher.InvokeOnUi(() => HasUnsavedChanges = true);
    }

    [RelayCommand]
    private async Task NewSolutionAsync()
    {
        if (HasUnsavedChanges &&
            !await SaveSolutionCoreAsync(CurrentSolutionPath, allowPathSelection: true))
        {
            return;
        }

        BeginSuppressSolutionAutoSave();
        try
        {
            // Clear existing Solution (DI singleton)
            Solution.Projects.Clear();
            Solution.Name = "New Solution";

            // Add default project
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
            MarkSolutionDirty();
            SetSolutionSaveStatus(SolutionSaveState.NotSaved, "Not saved - choose Save As");

            // ✅ Clear all selections to reset property panels across all pages
            ClearAllSelections();

            await _mobaRuntime.ActivateProjectAsync(newProject).ConfigureAwait(false);

            SaveSolutionCommand.NotifyCanExecuteChanged();
            ConnectCommand.NotifyCanExecuteChanged();

            SolutionLoaded?.Invoke(this, EventArgs.Empty);
        }
        finally
        {
            EndSuppressSolutionAutoSave();
        }
    }

    [RelayCommand]
    private async Task LoadSolutionAsync()
    {
        // Skip if IoService not available (WebApp/MAUI)
        if (_ioService is NullIoService)
            return;

        var (loadedSolution, path, error) = await _ioService.LoadAsync().ConfigureAwait(false);

        if (!string.IsNullOrEmpty(error))
        {
            throw new InvalidOperationException($"Failed to load solution: {error}");
        }

        if (loadedSolution != null && path != null)
        {
            // Marshal to UI thread to update observable properties bound to UI
            _uiDispatcher.InvokeOnUi(() => ApplyLoadedSolution(loadedSolution, path));
        }
    }

    /// <summary>
    /// Loads a solution from a specific file path.
    /// Used by auto-load functionality to ensure the same code path as manual loading.
    /// </summary>
    public async Task LoadSolutionFromPathAsync(string filePath)
    {
        // Skip if IoService not available (WebApp/MAUI)
        if (_ioService is NullIoService)
            return;

        var (loadedSolution, path, error) = await _ioService.LoadFromPathAsync(filePath).ConfigureAwait(false);

        if (!string.IsNullOrEmpty(error))
        {
            throw new InvalidOperationException($"Failed to load solution: {error}");
        }

        if (loadedSolution != null && path != null)
        {
            // Marshal to UI thread to update observable properties bound to UI
            _uiDispatcher.InvokeOnUi(() => ApplyLoadedSolution(loadedSolution, path));
        }
    }

    /// <summary>
    /// Applies a loaded solution to the ViewModel.
    /// Single source of truth for all solution loading operations.
    /// </summary>
    private void ApplyLoadedSolution(Solution loadedSolution, string path)
    {
        BeginSuppressSolutionAutoSave();
        try
        {
            // ✅ Clear all selections first to prevent stale data from previous solution
            ClearAllSelections();

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
                // Auto-select first project after loading
                SelectedProject = SolutionViewModel?.Projects.FirstOrDefault();

                ObserveBackgroundTask(_mobaRuntime.ActivateProjectAsync(Solution.Projects[0]), "Activate project runtime");
            }

            SaveSolutionCommand.NotifyCanExecuteChanged();
            ConnectCommand.NotifyCanExecuteChanged();
            LoadCities();

            OnPropertyChanged(nameof(Solution));

            // Notify subscribers to load their data after loading
            SolutionLoaded?.Invoke(this, EventArgs.Empty);
        }
        finally
        {
            EndSuppressSolutionAutoSave();
        }
    }

    private bool CanSaveSolution() => _ioService is not NullIoService;

    /// <summary>
    /// Clears all selections across all pages to reset property panels.
    /// This ensures property panels show "No item selected" instead of stale data.
    /// 
    /// Called in the following scenarios:
    /// - Creating a new solution (NewSolutionAsync)
    /// - Loading a solution from file (ApplyLoadedSolution)
    /// - Deleting the currently selected project (DeleteProject)
    /// </summary>
    private void ClearAllSelections()
    {
        // Solution Page
        SelectedProject = null;

        // Journeys Page
        SelectedJourney = null;
        SelectedStation = null;
        JourneysPageSelectedObject = null;

        // Workflows Page
        SelectedWorkflow = null;
        SelectedAction = null;
        WorkflowsPageSelectedObject = null;

        // Wagons & Locomotives
        SelectedTrain = null;
        SelectedLocomotive = null;
        SelectedPassengerWagon = null;
        SelectedGoodsWagon = null;
        SelectedVehicle = null;

        // General
        CurrentSelectedObject = null;
    }

    /// <summary>
    /// Loads cities from City Library into AvailableCities for UI binding.
    /// Cities are master data loaded from CityService, not stored in Project.
    /// </summary>
    private void LoadCities()
    {
        // Cities are loaded from CityLibrary on startup, not from the solution file.
    }
    #endregion
}
