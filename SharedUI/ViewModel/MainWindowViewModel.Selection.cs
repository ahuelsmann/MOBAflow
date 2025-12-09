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

        CurrentSelectedObject = SolutionViewModel;  // ✅ Direct assignment!
        CurrentSelectedEntityType = MobaType.Solution;
        NotifySelectionPropertiesChanged();
    }

    [RelayCommand]
    private void SelectJourney(JourneyViewModel? journey)
    {
        // ✅ Clear child selections first (Station, Action)
        SelectedStation = null;
        SelectedAction = null;

        SelectedJourney = journey;

        // ✅ Force update even if same journey (re-selection scenario)
        if (journey != null)
        {
            CurrentSelectedObject = journey;
            CurrentSelectedEntityType = MobaType.Journey;
        }
    }

    [RelayCommand]
    private void SelectStation(StationViewModel? station)
    {
        // ✅ Clear child selections first (Action)
        SelectedAction = null;

        SelectedStation = station;

        // ✅ Force update even if same station (re-selection scenario)
        if (station != null)
        {
            CurrentSelectedObject = station;
            CurrentSelectedEntityType = MobaType.Station;
        }
    }

    [RelayCommand]
    private void SelectWorkflow(WorkflowViewModel? workflow)
    {
        // ✅ Clear child selections first (Action)
        SelectedAction = null;

        SelectedWorkflow = workflow;

        // ✅ Force update even if same workflow (re-selection scenario)
        if (workflow != null)
        {
            CurrentSelectedObject = workflow;
            CurrentSelectedEntityType = MobaType.Workflow;
        }
    }

    [RelayCommand]
    private void SelectAction(object? action) =>
        SelectedAction = action;

    [RelayCommand]
    private void SelectTrain(TrainViewModel? train)
    {
        SelectedTrain = train;

        // ✅ Force update even if same train (re-selection scenario)
        if (train != null)
        {
            CurrentSelectedObject = train;
            CurrentSelectedEntityType = MobaType.Train;
        }
    }

    #endregion

    #region Selection Change Handlers

    partial void OnSelectedProjectChanged(ProjectViewModel? value)
    {
        if (value != null)
        {
            CurrentSelectedObject = value;  // ✅ Direct assignment!
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
            CurrentSelectedObject = value;  // ✅ Direct assignment!
            CurrentSelectedEntityType = MobaType.Journey;
        }

        NotifySelectionPropertiesChanged();
    }

    partial void OnSelectedStationChanged(StationViewModel? value)
    {
        if (value != null)
        {
            CurrentSelectedObject = value;  // ✅ Direct assignment!
            CurrentSelectedEntityType = MobaType.Station;
        }

        NotifySelectionPropertiesChanged();
    }

    partial void OnSelectedWorkflowChanged(WorkflowViewModel? value)
    {
        if (value != null)
        {
            CurrentSelectedObject = value;  // ✅ Direct assignment!
            CurrentSelectedEntityType = MobaType.Workflow;
        }

        NotifySelectionPropertiesChanged();
    }

    partial void OnSelectedActionChanged(object? value)
    {
        if (value != null)
        {
            CurrentSelectedObject = value;  // ✅ Direct assignment!
            CurrentSelectedEntityType = MobaType.Action;
        }

        NotifySelectionPropertiesChanged();
    }

    partial void OnSelectedTrainChanged(TrainViewModel? value)
    {
        if (value != null)
        {
            CurrentSelectedObject = value;  // ✅ Direct assignment!
            CurrentSelectedEntityType = MobaType.Train;
        }

        NotifySelectionPropertiesChanged();
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