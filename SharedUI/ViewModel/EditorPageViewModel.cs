// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using CommunityToolkit.Mvvm.ComponentModel;
using Moba.Backend.Model;
using Moba.SharedUI.Service;

/// <summary>
/// ViewModel for the Editor page with tabs for Journeys, Workflows, Trains, Locomotives, Wagons, and Settings.
/// </summary>
public partial class EditorPageViewModel : ObservableObject
{
    private readonly Project _project;
    private readonly Solution _solution;

    [ObservableProperty]
    private int _selectedTabIndex;

    public JourneyEditorViewModel JourneyEditor { get; }
    public WorkflowEditorViewModel WorkflowEditor { get; }
    public TrainEditorViewModel TrainEditor { get; }
    public LocomotiveEditorViewModel LocomotiveEditor { get; }
    public WagonEditorViewModel WagonEditor { get; }
    public SettingsEditorViewModel SettingsEditor { get; }

    public EditorPageViewModel(Solution solution, ValidationService? validationService = null)
    {
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
    public string ProjectName => _project?.Name ?? "(No Project Loaded)";
}
