// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using CommunityToolkit.Mvvm.Input;

using Enum;

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
        CurrentSelectedObject = SolutionViewModel;  // ✅ Direct assignment!
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
        }
    }

    #endregion

    #region Selection Change Handlers

    partial void OnSelectedProjectChanged(ProjectViewModel? value)
    {
        if (value != null)
        {
            CurrentSelectedObject = value;  // ✅ Direct assignment!
        }

        OnPropertyChanged(nameof(CurrentProjectViewModel));
    }

    partial void OnSelectedJourneyChanged(JourneyViewModel? value)
    {
        AddStationToJourneyCommand.NotifyCanExecuteChanged();

        if (value != null)
        {
            CurrentSelectedObject = value;  // ✅ Direct assignment!
        }
    }

    partial void OnSelectedStationChanged(StationViewModel? value)
    {
        if (value != null)
        {
            CurrentSelectedObject = value;  // ✅ Direct assignment!
        }
    }

    partial void OnSelectedWorkflowChanged(WorkflowViewModel? value)
    {
        if (value != null)
        {
            CurrentSelectedObject = value;  // ✅ Direct assignment!
        }
    }

    partial void OnSelectedActionChanged(object? value)
    {
        if (value != null)
        {
            CurrentSelectedObject = value;  // ✅ Direct assignment!
        }
    }

    partial void OnSelectedTrainChanged(TrainViewModel? value)
    {
        if (value != null)
        {
            CurrentSelectedObject = value;  // ✅ Direct assignment!
        }
    }

    partial void OnSelectedLocomotiveChanged(LocomotiveViewModel? value)
    {
        if (value != null)
        {
        }

        OnPropertyChanged(nameof(CurrentSelectedObject));
    }

    partial void OnSelectedWagonChanged(WagonViewModel? value)
    {
        if (value != null)
        {
        }

        OnPropertyChanged(nameof(CurrentSelectedObject));
    }

    #endregion
}