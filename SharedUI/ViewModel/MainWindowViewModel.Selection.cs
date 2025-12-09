// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using CommunityToolkit.Mvvm.Input;

using Moba.SharedUI.Enum;

/// <summary>
/// MainWindowViewModel - Selection Management
/// Handles all entity selection commands and change handlers.
/// </summary>
public partial class MainWindowViewModel
{
    #region Selection Commands

    [RelayCommand]
    private void SelectSolution()
    {
        // Clear all entity selections when selecting the solution
        SelectedProject = null;
        SelectedJourney = null;
        SelectedStation = null;
        SelectedWorkflow = null;
        SelectedAction = null;
        SelectedTrain = null;
        SelectedLocomotive = null;
        SelectedWagon = null;
        
        CurrentSelectedEntityType = MobaType.Solution;
        OnPropertyChanged(nameof(CurrentSelectedObject));
        NotifySelectionPropertiesChanged();
    }

    [RelayCommand]
    private void SelectProject(ProjectViewModel? project) =>
        _selectionManager.SelectEntity(project, MobaType.Project, SelectedProject, v => SelectedProject = v);

    [RelayCommand]
    private void SelectJourney(JourneyViewModel? journey) =>
        _selectionManager.SelectEntity(journey, MobaType.Journey, SelectedJourney, v => SelectedJourney = v);

    [RelayCommand]
    private void SelectStation(StationViewModel? station) =>
        _selectionManager.SelectEntity(station, MobaType.Station, SelectedStation, v => SelectedStation = v);

    [RelayCommand]
    private void SelectWorkflow(WorkflowViewModel? workflow) =>
        _selectionManager.SelectEntity(workflow, MobaType.Workflow, SelectedWorkflow, v => SelectedWorkflow = v);

    [RelayCommand]
    private void SelectAction(object? action) =>
        SelectedAction = action;

    [RelayCommand]
    private void SelectTrain(TrainViewModel? train) =>
        _selectionManager.SelectEntity(train, MobaType.Train, SelectedTrain, v => SelectedTrain = v);

    [RelayCommand]
    private void SelectLocomotive(LocomotiveViewModel? locomotive) =>
        _selectionManager.SelectEntity(locomotive, MobaType.Locomotive, SelectedLocomotive, v => SelectedLocomotive = v);

    [RelayCommand]
    private void SelectWagon(WagonViewModel? wagon) =>
        _selectionManager.SelectEntity(wagon, MobaType.Wagon, SelectedWagon, v => SelectedWagon = v);

    #endregion

    #region Selection Change Handlers

    partial void OnSelectedProjectChanged(ProjectViewModel? value)
    {
        if (value != null)
        {
            CurrentSelectedEntityType = MobaType.Project;
        }
        
        OnPropertyChanged(nameof(CurrentProjectViewModel));
        OnPropertyChanged(nameof(CurrentSelectedObject));
        NotifySelectionPropertiesChanged();
    }

    partial void OnSelectedJourneyChanged(JourneyViewModel? value)
    {
        AddStationToJourneyCommand.NotifyCanExecuteChanged();

        if (value != null)
        {
            CurrentSelectedEntityType = MobaType.Journey;
        }
        
        OnPropertyChanged(nameof(CurrentSelectedObject));
        NotifySelectionPropertiesChanged();
    }

    partial void OnSelectedStationChanged(StationViewModel? value)
    {
        if (value != null)
        {
            CurrentSelectedEntityType = MobaType.Station;
        }
        
        OnPropertyChanged(nameof(CurrentSelectedObject));
        NotifySelectionPropertiesChanged();
        RefreshCurrentSelectionCommand.Execute(null);  // Force refresh
    }

    partial void OnSelectedWorkflowChanged(WorkflowViewModel? value)
    {
        if (value != null)
        {
            CurrentSelectedEntityType = MobaType.Workflow;
        }
        
        OnPropertyChanged(nameof(CurrentSelectedObject));
        NotifySelectionPropertiesChanged();
        RefreshCurrentSelectionCommand.Execute(null);  // Force refresh
    }

    partial void OnSelectedActionChanged(object? value)
    {
        if (value != null)
        {
            CurrentSelectedEntityType = MobaType.Action;
        }
        
        OnPropertyChanged(nameof(CurrentSelectedObject));
        NotifySelectionPropertiesChanged();
        RefreshCurrentSelectionCommand.Execute(null);  // Force refresh
    }

    partial void OnSelectedTrainChanged(TrainViewModel? value)
    {
        if (value != null)
        {
            CurrentSelectedEntityType = MobaType.Train;
        }
        
        OnPropertyChanged(nameof(CurrentSelectedObject));
        NotifySelectionPropertiesChanged();
        RefreshCurrentSelectionCommand.Execute(null);  // Force refresh
    }

    partial void OnSelectedLocomotiveChanged(LocomotiveViewModel? value)
    {
        if (value != null)
        {
            CurrentSelectedEntityType = MobaType.Locomotive;
        }
        
        OnPropertyChanged(nameof(CurrentSelectedObject));
        NotifySelectionPropertiesChanged();
    }

    partial void OnSelectedWagonChanged(WagonViewModel? value)
    {
        if (value != null)
        {
            CurrentSelectedEntityType = MobaType.Wagon;
        }
        
        OnPropertyChanged(nameof(CurrentSelectedObject));
        NotifySelectionPropertiesChanged();
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Notifies all selection-related properties changed.
    /// </summary>

    /// <summary>
    /// Forces a refresh of CurrentSelectedObject even if selection hasnt changed.
    /// Used when clicking the same item again.
    /// </summary>
    [RelayCommand]
    private void RefreshCurrentSelection()
    {
        OnPropertyChanged(nameof(CurrentSelectedObject));
    }

    private void NotifySelectionPropertiesChanged()
    {
        OnPropertyChanged(nameof(HasSelectedEntity));
        OnPropertyChanged(nameof(HasSelectedJourney));
        OnPropertyChanged(nameof(HasSelectedWorkflow));
        OnPropertyChanged(nameof(HasSelectedTrain));
        OnPropertyChanged(nameof(CurrentSelectedObject));
    }

    /// <summary>
    /// Gets the most specific selected object for display in ContentControl.
    /// Priority: Action > Station > Journey > Workflow > Locomotive > Wagon > Train > Project > Solution
    /// </summary>
    public object? CurrentSelectedObject
    {
        get
        {
            // Highest priority: most specific selection
            if (SelectedAction != null) return SelectedAction;
            if (SelectedStation != null) return SelectedStation;
            if (SelectedJourney != null) return SelectedJourney;
            if (SelectedWorkflow != null) return SelectedWorkflow;
            if (SelectedLocomotive != null) return SelectedLocomotive;
            if (SelectedWagon != null) return SelectedWagon;
            if (SelectedTrain != null) return SelectedTrain;
            if (SelectedProject != null) return SelectedProject;
            if (SolutionViewModel != null) return SolutionViewModel;
            
            return null;
        }
    }

    /// <summary>
    /// Gets whether a Journey is currently selected.
    /// </summary>
    public bool HasSelectedJourney => SelectedJourney != null;

    /// <summary>
    /// Gets whether a Workflow is currently selected.
    /// </summary>
    public bool HasSelectedWorkflow => SelectedWorkflow != null;

    /// <summary>
    /// Gets whether a Train is currently selected.
    /// </summary>
    public bool HasSelectedTrain => SelectedTrain != null;

    #endregion
}
