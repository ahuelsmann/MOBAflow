// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Moba.Backend.Model;
using Moba.SharedUI.Service;
using System.Collections.ObjectModel;

/// <summary>
/// ViewModel for the Editor page with tabs for Journeys, Workflows, Trains, Locomotives, Wagons, and Settings.
/// Provides full CRUD operations for the entire Solution hierarchy.
/// </summary>
public partial class EditorPageViewModel : ObservableObject
{
    private readonly Solution _solution;
    private readonly ValidationService? _validationService;
    private readonly Project _project;

    [ObservableProperty]
    private int _selectedTabIndex;

    [ObservableProperty]
    private ObservableCollection<Project> _projects;

    [ObservableProperty]
    private Project? _selectedProject;

    public JourneyEditorViewModel JourneyEditor { get; private set; }
    public WorkflowEditorViewModel WorkflowEditor { get; private set; }
    public TrainEditorViewModel TrainEditor { get; private set; }
    public LocomotiveEditorViewModel LocomotiveEditor { get; private set; }
    public WagonEditorViewModel WagonEditor { get; private set; }
    public SettingsEditorViewModel SettingsEditor { get; private set; }

    public EditorPageViewModel(Solution solution, ValidationService? validationService = null)
    {
        _solution = solution;
        _validationService = validationService;
        
        // Initialize Projects collection
        _projects = new ObservableCollection<Project>(solution.Projects);
        
        // Select first project by default
        _selectedProject = solution.Projects.FirstOrDefault();
        
        // Initialize all tab ViewModels
        InitializeEditors();
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
        var project = _selectedProject ?? new Project { Name = "(No Project Loaded)" };
        
        System.Diagnostics.Debug.WriteLine($"🔄 InitializeEditors called - Project: {project.Name}, Journeys: {project.Journeys.Count}");
        
        JourneyEditor = new JourneyEditorViewModel(project, _validationService);
        WorkflowEditor = new WorkflowEditorViewModel(project, _validationService);
        TrainEditor = new TrainEditorViewModel(project, _validationService);
        LocomotiveEditor = new LocomotiveEditorViewModel(project, _validationService);
        WagonEditor = new WagonEditorViewModel(project, _validationService);
        
        // ✅ Settings are now at Solution level, not Project level
        SettingsEditor = new SettingsEditorViewModel(_solution.Settings);
        
        // Notify UI that all editors have been recreated
        OnPropertyChanged(nameof(JourneyEditor));
        OnPropertyChanged(nameof(WorkflowEditor));
        OnPropertyChanged(nameof(TrainEditor));
        OnPropertyChanged(nameof(LocomotiveEditor));
        OnPropertyChanged(nameof(WagonEditor));
        OnPropertyChanged(nameof(SettingsEditor));
        
        System.Diagnostics.Debug.WriteLine($"✅ InitializeEditors complete - JourneyEditor.Journeys.Count: {JourneyEditor.Journeys.Count}");
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
    public EditorPageViewModel(Solution solution, ValidationService? validationService = null)
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
        // Note: InitializeEditors() already calls OnPropertyChanged for all editors
        _solution = solution;
        
        // Get first project or create a temporary empty one (defensive programming)
        if (solution.Projects.Count == 0)
        {
            System.Diagnostics.Debug.WriteLine("⚠️ EditorPageViewModel: Solution has no projects, creating temporary empty project");
            _project = new Project { Name = "(Untitled Project)" };
            solution.Projects.Add(_project);
        }
        else
        {
            _project = solution.Projects[0];
        }
        
        // Initialize all tab ViewModels with ValidationService
        JourneyEditor = new JourneyEditorViewModel(_project, validationService);
        WorkflowEditor = new WorkflowEditorViewModel(_project, validationService);
        TrainEditor = new TrainEditorViewModel(_project, validationService);
        LocomotiveEditor = new LocomotiveEditorViewModel(_project, validationService);
        WagonEditor = new WagonEditorViewModel(_project, validationService);
        SettingsEditor = new SettingsEditorViewModel(_solution); // ✅ Pass Solution for Settings
    }

    /// <summary>
    /// Gets the project name to display in the header.
    /// </summary>
    public string ProjectName => _selectedProject?.Name ?? "(No Project Loaded)";
}
