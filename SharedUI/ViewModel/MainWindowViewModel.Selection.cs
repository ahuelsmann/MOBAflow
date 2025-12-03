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
    private void SelectSolution() =>
        ClearOtherSelections(MobaType.Solution);

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
            ClearOtherSelections(MobaType.Project);
            CurrentSelectedEntityType = MobaType.Project;
        }
        
        OnPropertyChanged(nameof(CurrentProjectViewModel));
        NotifySelectionPropertiesChanged();
    }

    partial void OnSelectedJourneyChanged(JourneyViewModel? value)
    {
        AddStationToJourneyCommand.NotifyCanExecuteChanged();

        if (value != null)
        {
            ClearOtherSelections(MobaType.Journey);
            CurrentSelectedEntityType = MobaType.Journey;
        }
        
        NotifySelectionPropertiesChanged();
    }

    partial void OnSelectedStationChanged(StationViewModel? value)
    {
        if (value != null)
        {
            ClearOtherSelections(MobaType.Station);
            CurrentSelectedEntityType = MobaType.Station;
        }
        
        NotifySelectionPropertiesChanged();
    }

    partial void OnSelectedWorkflowChanged(WorkflowViewModel? value)
    {
        if (value != null)
        {
            ClearOtherSelections(MobaType.Workflow);
            CurrentSelectedEntityType = MobaType.Workflow;
        }
        
        NotifySelectionPropertiesChanged();
    }

    partial void OnSelectedTrainChanged(TrainViewModel? value)
    {
        if (value != null)
        {
            ClearOtherSelections(MobaType.Train);
            CurrentSelectedEntityType = MobaType.Train;
        }
        
        NotifySelectionPropertiesChanged();
    }

    partial void OnSelectedLocomotiveChanged(LocomotiveViewModel? value)
    {
        if (value != null)
        {
            ClearOtherSelections(MobaType.Locomotive);
            CurrentSelectedEntityType = MobaType.Locomotive;
        }
        
        NotifySelectionPropertiesChanged();
    }

    partial void OnSelectedWagonChanged(WagonViewModel? value)
    {
        if (value != null)
        {
            ClearOtherSelections(MobaType.Wagon);
            CurrentSelectedEntityType = MobaType.Wagon;
        }
        
        NotifySelectionPropertiesChanged();
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Clears all other selections except the specified type to prevent stacking in PropertyGrid.
    /// Station keeps Journey selected (parent context), Journey keeps Project selected.
    /// </summary>
    private void ClearOtherSelections(MobaType keepType)
    {
        // Keep Project selected when Journey or Station is selected (parent context)
        // Keep Journey selected when Station is selected (parent context)
        if (keepType != MobaType.Project && keepType != MobaType.Journey && keepType != MobaType.Station)
        {
            if (SelectedProject != null) SelectedProject = null;
        }
        
        if (keepType != MobaType.Journey && keepType != MobaType.Station)
        {
            if (SelectedJourney != null) SelectedJourney = null;
        }
        
        if (keepType != MobaType.Station)
        {
            if (SelectedStation != null) SelectedStation = null;
        }
        
        if (keepType != MobaType.Workflow)
        {
            if (SelectedWorkflow != null) SelectedWorkflow = null;
        }
        
        if (keepType != MobaType.Train)
        {
            if (SelectedTrain != null) SelectedTrain = null;
        }
        
        if (keepType != MobaType.Locomotive)
        {
            if (SelectedLocomotive != null) SelectedLocomotive = null;
        }
        
        if (keepType != MobaType.Wagon)
        {
            if (SelectedWagon != null) SelectedWagon = null;
        }
    }

    /// <summary>
    /// Notifies all selection-related properties changed (for PropertyGrid binding).
    /// </summary>
    private void NotifySelectionPropertiesChanged()
    {
        OnPropertyChanged(nameof(HasSelectedEntity));
        OnPropertyChanged(nameof(ShowSolutionProperties));
        OnPropertyChanged(nameof(ShowProjectProperties));
        OnPropertyChanged(nameof(ShowJourneyProperties));
        OnPropertyChanged(nameof(ShowStationProperties));
        OnPropertyChanged(nameof(ShowWorkflowProperties));
        OnPropertyChanged(nameof(ShowTrainProperties));
    }

    #endregion
}
