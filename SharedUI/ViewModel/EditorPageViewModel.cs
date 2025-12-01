// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Moba.Common.Configuration;
using Moba.Domain;
using Moba.Domain.Enum;
using Moba.SharedUI.Service;

using System.Collections.ObjectModel;

/// <summary>
/// ViewModel for the Editor page with tabs for Journeys, Workflows, Trains, Locomotives, Wagons, and Settings.
/// Provides full CRUD operations for the entire Solution hierarchy.
/// Exposes MainWindowViewModel selection properties for cross-view synchronization.
/// </summary>
public partial class EditorPageViewModel : ObservableObject
{
    private readonly Solution _solution;
    private readonly AppSettings _settings;
    private readonly ValidationService? _validationService;
    private readonly ICityLibraryService? _cityLibraryService;
    private readonly WinUI.MainWindowViewModel? _mainWindowViewModel;

    [ObservableProperty]
    private int _selectedTabIndex;

    [ObservableProperty]
    private ObservableCollection<Project> _projects;

    [ObservableProperty]
    private Project? _selectedProject;
    
    /// <summary>
    /// Available ColorScheme enum values for ComboBox binding.
    /// </summary>
    public Array ColorSchemeValues => Enum.GetValues(typeof(ColorScheme));
    
    /// <summary>
    /// Available PassengerClass enum values for ComboBox binding.
    /// </summary>
    public Array PassengerClassValues => Enum.GetValues(typeof(PassengerClass));
    
    /// <summary>
    /// Available CargoType enum values for ComboBox binding.
    /// </summary>
    public Array CargoTypeValues => Enum.GetValues(typeof(CargoType));
    
    // ✅ Expose MainWindowViewModel selection properties for binding with setters
    public JourneyViewModel? SelectedJourney
    {
        get
        {
            var value = _mainWindowViewModel?.SelectedJourney;
            System.Diagnostics.Debug.WriteLine($"🔍 EditorPageViewModel.SelectedJourney GET: {value?.Name ?? "null"}");
            return value;
        }
        set
        {
            System.Diagnostics.Debug.WriteLine($"🔍 EditorPageViewModel.SelectedJourney SET: {value?.Name ?? "null"}");
            if (_mainWindowViewModel != null)
            {
                _mainWindowViewModel.SelectedJourney = value;
                System.Diagnostics.Debug.WriteLine($"   MainWindowViewModel.SelectedJourney now: {_mainWindowViewModel.SelectedJourney?.Name ?? "null"}");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"   ⚠️ MainWindowViewModel is NULL!");
            }
        }
    }
    
    public StationViewModel? SelectedStation
    {
        get => _mainWindowViewModel?.SelectedStation;
        set { if (_mainWindowViewModel != null) _mainWindowViewModel.SelectedStation = value; }
    }
    
    public WorkflowViewModel? SelectedWorkflow
    {
        get => _mainWindowViewModel?.SelectedWorkflow;
        set { if (_mainWindowViewModel != null) _mainWindowViewModel.SelectedWorkflow = value; }
    }
    
    public TrainViewModel? SelectedTrain
    {
        get => _mainWindowViewModel?.SelectedTrain;
        set { if (_mainWindowViewModel != null) _mainWindowViewModel.SelectedTrain = value; }
    }

    public JourneyEditorViewModel JourneyEditor { get; private set; }
    public WorkflowEditorViewModel WorkflowEditor { get; private set; }
    public TrainEditorViewModel TrainEditor { get; private set; }
    public LocomotiveEditorViewModel LocomotiveEditor { get; private set; }
    public WagonEditorViewModel WagonEditor { get; private set; }
    public SettingsEditorViewModel SettingsEditor { get; private set; }

    public EditorPageViewModel(Solution solution, AppSettings settings, ValidationService? validationService = null, ICityLibraryService? cityLibraryService = null, WinUI.MainWindowViewModel? mainWindowViewModel = null)
    {
        _solution = solution;
        _settings = settings;
        _validationService = validationService;
        _cityLibraryService = cityLibraryService;
        _mainWindowViewModel = mainWindowViewModel;

        // Initialize Projects collection
        _projects = new ObservableCollection<Project>(solution.Projects);

        // Select first project by default
        _selectedProject = solution.Projects.FirstOrDefault();

        // Initialize all tab ViewModels
        InitializeEditors();
        
        // ✅ Subscribe to MainWindowViewModel selection changes
        if (_mainWindowViewModel != null)
        {
            System.Diagnostics.Debug.WriteLine($"✅ EditorPageViewModel: Subscribing to MainWindowViewModel PropertyChanged");
            _mainWindowViewModel.PropertyChanged += (s, e) =>
            {
                System.Diagnostics.Debug.WriteLine($"🔔 EditorPageViewModel: MainWindowViewModel PropertyChanged: {e.PropertyName}");
                if (e.PropertyName == nameof(_mainWindowViewModel.SelectedJourney))
                {
                    System.Diagnostics.Debug.WriteLine($"   → Notifying SelectedJourney changed to: {_mainWindowViewModel.SelectedJourney?.Name ?? "null"}");
                    OnPropertyChanged(nameof(SelectedJourney));
                }
                else if (e.PropertyName == nameof(_mainWindowViewModel.SelectedStation))
                    OnPropertyChanged(nameof(SelectedStation));
                else if (e.PropertyName == nameof(_mainWindowViewModel.SelectedWorkflow))
                    OnPropertyChanged(nameof(SelectedWorkflow));
                else if (e.PropertyName == nameof(_mainWindowViewModel.SelectedTrain))
                    OnPropertyChanged(nameof(SelectedTrain));
            };
        }
        else
        {
            System.Diagnostics.Debug.WriteLine($"⚠️ EditorPageViewModel: MainWindowViewModel is NULL - no subscription!");
        }
    }

    /// <summary>
    /// Refreshes the Editor with current Solution data.
    /// Call this after loading a new Solution to update all editors.
    /// </summary>
    public void Refresh()
    {
        System.Diagnostics.Debug.WriteLine($"🔄 EditorPageViewModel.Refresh() called - Solution has {_solution.Projects.Count} projects");

        if (_solution.Projects.Count > 0)
        {
            var firstProject = _solution.Projects[0];
            System.Diagnostics.Debug.WriteLine($"   First project: {firstProject.Name}, Journeys: {firstProject.Journeys.Count}");

            if (firstProject.Journeys.Count > 0)
            {
                System.Diagnostics.Debug.WriteLine($"   First journey: {firstProject.Journeys[0].Name}");
            }
        }

        // Update Projects collection from Solution
        Projects.Clear();
        foreach (var project in _solution.Projects)
        {
            Projects.Add(project);
        }

        // Force re-initialization by setting to null first, then to first project
        _selectedProject = null;
        SelectedProject = _solution.Projects.FirstOrDefault();

        System.Diagnostics.Debug.WriteLine($"✅ EditorPageViewModel.Refresh() complete - SelectedProject: {SelectedProject?.Name}");
    }

    private void InitializeEditors()
    {
        System.Diagnostics.Debug.WriteLine($"🔄 InitializeEditors called - Project: {_selectedProject?.Name}, Journeys: {_selectedProject?.Journeys.Count ?? 0}");

        // ✅ Remember current selection BEFORE reinitializing
        var previouslySelectedJourneyModel = _mainWindowViewModel?.SelectedJourney?.Model;
        var previouslySelectedWorkflowModel = _mainWindowViewModel?.SelectedWorkflow?.Model;
        var previouslySelectedTrainModel = _mainWindowViewModel?.SelectedTrain?.Model;

        // Initialize all editor sub-ViewModels with the selected project
        JourneyEditor = new JourneyEditorViewModel(_selectedProject, _validationService, _cityLibraryService);
        WorkflowEditor = new WorkflowEditorViewModel(_selectedProject, _validationService);
        TrainEditor = new TrainEditorViewModel(_selectedProject, _validationService);
        LocomotiveEditor = new LocomotiveEditorViewModel(_selectedProject);
        WagonEditor = new WagonEditorViewModel(_selectedProject);
        SettingsEditor = new SettingsEditorViewModel(_settings);

        // Notify UI that all editors have been recreated
        OnPropertyChanged(nameof(JourneyEditor));
        OnPropertyChanged(nameof(WorkflowEditor));
        OnPropertyChanged(nameof(TrainEditor));
        OnPropertyChanged(nameof(LocomotiveEditor));
        OnPropertyChanged(nameof(WagonEditor));
        OnPropertyChanged(nameof(SettingsEditor));

        System.Diagnostics.Debug.WriteLine($"✅ InitializeEditors complete - JourneyEditor.Journeys.Count: {JourneyEditor.Journeys.Count}");
        
        // ✅ Restore selection after reinitializing
        // Note: JourneyEditor.Journeys contains Journey models, not JourneyViewModels
        // We need to find the matching JourneyViewModel in SolutionViewModel
        if (previouslySelectedJourneyModel != null && _mainWindowViewModel?.SolutionViewModel != null)
        {
            // Find the JourneyViewModel that wraps this model
            foreach (var projectVM in _mainWindowViewModel.SolutionViewModel.Projects)
            {
                var matchingJourneyVM = projectVM.Journeys.FirstOrDefault(j => j.Model == previouslySelectedJourneyModel);
                if (matchingJourneyVM != null)
                {
                    System.Diagnostics.Debug.WriteLine($"🔄 Restoring SelectedJourney: {matchingJourneyVM.Name}");
                    _mainWindowViewModel.SelectedJourney = matchingJourneyVM;
                    break;
                }
            }
        }
        
        if (previouslySelectedWorkflowModel != null && _mainWindowViewModel?.SolutionViewModel != null)
        {
            foreach (var projectVM in _mainWindowViewModel.SolutionViewModel.Projects)
            {
                var matchingWorkflowVM = projectVM.Workflows.FirstOrDefault(w => w.Model == previouslySelectedWorkflowModel);
                if (matchingWorkflowVM != null)
                {
                    _mainWindowViewModel.SelectedWorkflow = matchingWorkflowVM;
                    break;
                }
            }
        }
        
        if (previouslySelectedTrainModel != null && _mainWindowViewModel?.SolutionViewModel != null)
        {
            foreach (var projectVM in _mainWindowViewModel.SolutionViewModel.Projects)
            {
                var matchingTrainVM = projectVM.Trains.FirstOrDefault(t => t.Model == previouslySelectedTrainModel);
                if (matchingTrainVM != null)
                {
                    _mainWindowViewModel.SelectedTrain = matchingTrainVM;
                    break;
                }
            }
        }
    }

    /// <summary>
    /// Adds a new Project to the Solution.
    /// </summary>
    [RelayCommand]
    private void AddProject()
    {
        var newProject = new Project
        {
            Name = "New Project"
        };

        _solution.Projects.Add(newProject);
        Projects.Add(newProject);
        SelectedProject = newProject;
    }

    /// <summary>
    /// Deletes the selected Project from the Solution.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanDeleteProject))]
    private void DeleteProject()
    {
        if (SelectedProject == null) return;

        _solution.Projects.Remove(SelectedProject);
        Projects.Remove(SelectedProject);
        SelectedProject = Projects.FirstOrDefault();
    }

    private bool CanDeleteProject() => SelectedProject != null && Projects.Count > 1;

    /// <summary>
    /// Called when the selected project changes - reinitialize all editors.
    /// </summary>
    partial void OnSelectedProjectChanged(Project? value)
    {
        System.Diagnostics.Debug.WriteLine($"🔄 OnSelectedProjectChanged - New project: {value?.Name ?? "null"}");
        InitializeEditors();
        DeleteProjectCommand.NotifyCanExecuteChanged();
        // Note: InitializeEditors() already calls OnPropertyChanged for all editors
    }

    /// <summary>
    /// Gets the project name to display in the header.
    /// </summary>
    public string ProjectName => _selectedProject?.Name ?? "(No Project Loaded)";
}