// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using CommunityToolkit.Mvvm.Input;

using Domain;

using Interface;

using System.ComponentModel;

/// <summary>
/// MainWindowViewModel - Solution and Project Management.
/// The <see cref="ISolutionSession"/> owns the solution, its persistence and the project/journey selection;
/// this partial exposes them for XAML binding and keeps the page-level reactions.
/// </summary>
public partial class MainWindowViewModel
{
    private ProjectViewModel? _observedSelectedProject;
    private JourneyViewModel? _observedSelectedJourney;

    #region Solution Events
    /// <summary>
    /// Raised before saving the Solution. Subscribers should sync their data to Domain models.
    /// </summary>
    public event EventHandler? SolutionSaving
    {
        add => _session.SolutionSaving += value;
        remove => _session.SolutionSaving -= value;
    }

    /// <summary>
    /// Raised after loading a Solution. Subscribers should load their data from Domain models.
    /// </summary>
    public event EventHandler? SolutionLoaded
    {
        add => _session.SolutionLoaded += value;
        remove => _session.SolutionLoaded -= value;
    }
    #endregion

    #region Session-backed state
    /// <summary>Gets the solution session that owns the loaded solution.</summary>
    public ISolutionSession SolutionSession => _session;

    /// <summary>Gets the loaded solution.</summary>
    public Solution Solution => _session.Solution;

    /// <summary>Gets or sets the file the solution was loaded from or saved to.</summary>
    public string? CurrentSolutionPath
    {
        get => _session.CurrentSolutionPath;
        set => _session.CurrentSolutionPath = value;
    }

    /// <summary>Gets whether the solution has changes that were not written yet.</summary>
    public bool HasUnsavedChanges => _session.HasUnsavedChanges;

    /// <summary>Gets whether the solution contains at least one project.</summary>
    public bool HasSolution => _session.HasSolution;

    /// <summary>Gets the view model of the loaded solution.</summary>
    public SolutionViewModel? SolutionViewModel => _session.SolutionViewModel;

    /// <summary>Gets or sets the selected project.</summary>
    public ProjectViewModel? SelectedProject
    {
        get => _session.SelectedProject;
        set => _session.SelectedProject = value;
    }

    /// <summary>Gets or sets the selected journey.</summary>
    public JourneyViewModel? SelectedJourney
    {
        get => _session.SelectedJourney;
        set => _session.SelectedJourney = value;
    }

    /// <summary>Gets the current non-interactive solution persistence state.</summary>
    public SolutionSaveState SolutionSaveState => _session.SolutionSaveState;

    /// <summary>Gets an actionable, non-modal description of the current persistence state.</summary>
    public string SolutionSaveStatusText => _session.SolutionSaveStatusText;

    private void AttachSolutionSession()
    {
        _session.PropertyChanged += OnSolutionSessionPropertyChanged;
        _session.SolutionReplacing += OnSolutionReplacing;
        _session.SolutionLoaded += OnSessionSolutionLoaded;
    }

    private void OnSolutionSessionPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(ISolutionSession.SelectedProject):
                var oldProject = _observedSelectedProject;
                _observedSelectedProject = _session.SelectedProject;
                OnPropertyChanged(nameof(SelectedProject));
                HandleSelectedProjectChanged(oldProject, _observedSelectedProject);
                break;
            case nameof(ISolutionSession.SelectedJourney):
                var oldJourney = _observedSelectedJourney;
                _observedSelectedJourney = _session.SelectedJourney;
                OnPropertyChanged(nameof(SelectedJourney));
                AddStationCommand.NotifyCanExecuteChanged();
                AddStationFromCityCommand.NotifyCanExecuteChanged();
                HandleSelectedJourneyChanged(oldJourney, _observedSelectedJourney);
                break;
            case nameof(ISolutionSession.CurrentSolutionPath):
                OnPropertyChanged(nameof(CurrentSolutionPath));
                UpdateSolutionLoadedStatus();
                break;
            case nameof(ISolutionSession.HasUnsavedChanges):
            case nameof(ISolutionSession.HasSolution):
            case nameof(ISolutionSession.SolutionViewModel):
            case nameof(ISolutionSession.SolutionSaveState):
            case nameof(ISolutionSession.SolutionSaveStatusText):
            case nameof(ISolutionSession.Solution):
                OnPropertyChanged(e.PropertyName);
                break;
        }
    }

    private void OnSolutionReplacing(object? sender, EventArgs e) => ClearAllSelections();

    private void OnSessionSolutionLoaded(object? sender, EventArgs e)
    {
        SaveSolutionCommand.NotifyCanExecuteChanged();
        ConnectCommand.NotifyCanExecuteChanged();
        LoadCities();
    }
    #endregion

    #region Solution Management
    [RelayCommand(CanExecute = nameof(CanSaveSolution))]
    private async Task SaveSolutionAsync()
    {
        await _session.SaveAsync(allowPathSelection: true);
    }

    /// <summary>
    /// Marks the solution as changed and persists it without opening a file picker.
    /// </summary>
    public Task SaveSolutionInternalAsync() => _session.SaveSolutionInternalAsync();

    /// <summary>
    /// Persists solution changes and returns the resulting host persistence status.
    /// </summary>
    public Task<SolutionSaveResult> SaveSolutionWithStatusAsync() => _session.SaveSolutionWithStatusAsync();

    [RelayCommand]
    private Task NewSolutionAsync() => _session.NewSolutionAsync();

    [RelayCommand]
    private Task LoadSolutionAsync() => _session.LoadSolutionAsync();

    /// <summary>
    /// Loads a solution from a specific file path.
    /// Used by auto-load functionality to ensure the same code path as manual loading.
    /// </summary>
    public Task LoadSolutionFromPathAsync(string filePath) => _session.LoadSolutionFromPathAsync(filePath);

    private bool CanSaveSolution() => _session.CanSave;

    /// <summary>
    /// Clears all selections across all pages to reset property panels.
    /// Called when the session replaces the solution and when the selected project is deleted.
    /// </summary>
    private void ClearAllSelections()
    {
        SelectedProject = null;
        SelectedJourney = null;

        // Journeys Page
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
