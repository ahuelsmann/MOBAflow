// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using CommunityToolkit.Mvvm.Input;
using Domain;
using Service;
using System.Diagnostics;

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

        SolutionViewModel = new SolutionViewModel(value, _uiDispatcher, _ioService);
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
        // Skip if IoService not available (WebApp/MAUI)
        if (_ioService is NullIoService)
        {
            Debug.WriteLine("⚠️ Save not supported on this platform");
            return;
        }

        // Notify subscribers to sync their data before saving
        SolutionSaving?.Invoke(this, EventArgs.Empty);

        // Debug: Log workflow actions before saving
        foreach (var project in Solution.Projects)
        {
            foreach (var workflow in project.Workflows)
            {
                Debug.WriteLine($"💾 Saving Workflow '{workflow.Name}' with {workflow.Actions.Count} actions:");
                foreach (var action in workflow.Actions)
                {
                    Debug.WriteLine($"   - Action: {action.Name} (Type: {action.Type}, Id: {action.Id})");
                }
            }
        }

        var (success, path, error) = await _ioService.SaveAsync(Solution, CurrentSolutionPath).ConfigureAwait(false);
        if (success && path != null)
        {
            // Marshal to UI thread to update observable properties bound to UI
            _uiDispatcher.InvokeOnUi(() =>
            {
                CurrentSolutionPath = path;
                HasUnsavedChanges = false;  // Clear the unsaved changes flag after successful save
            });
            Debug.WriteLine($"✅ Solution saved to {path}");
        }
        else if (!string.IsNullOrEmpty(error))
        {
            throw new InvalidOperationException($"Failed to save solution: {error}");
        }
    }

    [RelayCommand]
    private async Task NewSolutionAsync()
    {
        // Skip file operations if IoService not available (WebApp/MAUI)
        if (_ioService is not NullIoService && SaveSolutionCommand.CanExecute(null))
        {
            await SaveSolutionCommand.ExecuteAsync(null);
        }

        // Clear existing Solution (DI singleton)
        Solution.Projects.Clear();
        Solution.Name = "New Solution";

        // Add default project
        var newProject = new Project
        {
            Name = "New Project",
            Journeys = new List<Journey>(),
            Workflows = new List<Workflow>(),
            Trains = new List<Train>()
        };
        Solution.Projects.Add(newProject);

        SolutionViewModel?.Refresh();

        CurrentSolutionPath = null;
        HasUnsavedChanges = false;  // New solution starts as "clean"

        // ✅ Clear all selections to reset property panels across all pages
        ClearAllSelections();

        // Initialize JourneyManager with the new empty project
        InitializeJourneyManager(newProject);

        SaveSolutionCommand.NotifyCanExecuteChanged();
        ConnectCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand]
    private async Task LoadSolutionAsync()
    {
        // Skip if IoService not available (WebApp/MAUI)
        if (_ioService is NullIoService)
        {
            Debug.WriteLine("⚠️ Load not supported on this platform");
            return;
        }

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
        {
            Debug.WriteLine("⚠️ Load not supported on this platform");
            return;
        }

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
        HasSolution = Solution.Projects.Count > 0;
        HasUnsavedChanges = false;  // Just loaded, so no unsaved changes

        // Re-initialize JourneyManager with the loaded project
        // This is critical because the initial JourneyManager was created with an empty project
        if (Solution.Projects.Count > 0)
        {
            InitializeJourneyManager(Solution.Projects[0]);
            Debug.WriteLine($"✅ JourneyManager initialized after loading solution with {Solution.Projects[0].Journeys.Count} journeys");
            
            // Auto-select first project after loading
            SelectedProject = SolutionViewModel?.Projects.FirstOrDefault();
            if (SelectedProject != null)
            {
                Debug.WriteLine($"✅ Auto-selected first project: {SelectedProject.Name}");
            }
        }

        SaveSolutionCommand.NotifyCanExecuteChanged();
        ConnectCommand.NotifyCanExecuteChanged();
        LoadCities();

        OnPropertyChanged(nameof(Solution));

        // Notify subscribers to load their data after loading
        SolutionLoaded?.Invoke(this, EventArgs.Empty);

        Debug.WriteLine($"✅ Solution loaded: {path}");
        Debug.WriteLine($"   Projects: {Solution.Projects.Count}, Journeys: {Solution.Projects.FirstOrDefault()?.Journeys.Count ?? 0}");
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
        SolutionPageSelectedObject = null;
        
        // Journeys Page
        SelectedJourney = null;
        SelectedStation = null;
        JourneysPageSelectedObject = null;
        
        // Workflows Page
        SelectedWorkflow = null;
        SelectedAction = null;
        WorkflowsPageSelectedObject = null;
        
        // Trains Page
        SelectedLocomotive = null;
        SelectedPassengerWagon = null;
        SelectedGoodsWagon = null;
        TrainsPageSelectedObject = null;
        
        // General
        CurrentSelectedObject = null;
    }

    /// <summary>
    /// Loads cities from City Library into AvailableCities for UI binding.
    /// Cities are master data loaded from CityService, not stored in Project.
    /// </summary>
    private void LoadCities()
    {
        // Cities are loaded from CityLibrary, NOT from Project
        // This method can be removed or kept as no-op for backward compatibility
        Debug.WriteLine("ℹ️ LoadCities called - Cities are loaded from CityLibrary on startup");
    }
    #endregion
}

