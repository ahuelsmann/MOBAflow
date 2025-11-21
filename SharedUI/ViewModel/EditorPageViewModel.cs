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

    [ObservableProperty]
    private int _selectedTabIndex;

    public JourneyEditorViewModel JourneyEditor { get; }
    public WorkflowEditorViewModel WorkflowEditor { get; }
    public TrainEditorViewModel TrainEditor { get; }
    public LocomotiveEditorViewModel LocomotiveEditor { get; }
    public WagonEditorViewModel WagonEditor { get; }
    public SettingsEditorViewModel SettingsEditor { get; }

    public EditorPageViewModel(Project project, ValidationService? validationService = null)
    {
        _project = project;
        
        // Initialize all tab ViewModels with ValidationService
        JourneyEditor = new JourneyEditorViewModel(project, validationService);
        WorkflowEditor = new WorkflowEditorViewModel(project, validationService);
        TrainEditor = new TrainEditorViewModel(project, validationService);
        LocomotiveEditor = new LocomotiveEditorViewModel(project, validationService);
        WagonEditor = new WagonEditorViewModel(project, validationService);
        SettingsEditor = new SettingsEditorViewModel(project);
    }

    /// <summary>
    /// Gets the project name to display in the header.
    /// </summary>
    public string ProjectName => _project?.Name ?? "(No Project Loaded)";
}
