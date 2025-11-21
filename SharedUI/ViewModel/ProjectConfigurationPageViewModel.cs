// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using Backend.Model;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using System.Collections.ObjectModel;
using System.Linq;

/// <summary>
/// ViewModel for the Project Configuration page.
/// Provides CRUD operations for Journeys, Stations, Workflows, and Trains.
/// </summary>
public partial class ProjectConfigurationPageViewModel : ObservableObject
{
    private readonly Project _project;

    public ProjectConfigurationPageViewModel(Project project)
    {
        _project = project ?? throw new ArgumentNullException(nameof(project));

        // Wrap model collections in ViewModels
        Journeys = new ObservableCollection<JourneyViewModel>(
            _project.Journeys.Select(j => new JourneyViewModel(j))
        );

        Workflows = new ObservableCollection<WorkflowViewModel>(
            _project.Workflows.Select(w => new WorkflowViewModel(w))
        );

        Trains = new ObservableCollection<TrainViewModel>(
            _project.Trains.Select(t => new TrainViewModel(t))
        );

        // Available values for ComboBoxes
        AvailableWorkflows = new ObservableCollection<Workflow>(_project.Workflows);
        AvailableTrains = new ObservableCollection<Train>(_project.Trains);
    }

    #region Collections

    public ObservableCollection<JourneyViewModel> Journeys { get; }

    public ObservableCollection<WorkflowViewModel> Workflows { get; }

    public ObservableCollection<TrainViewModel> Trains { get; }

    // Reference collections for ComboBoxes
    public ObservableCollection<Workflow> AvailableWorkflows { get; }

    public ObservableCollection<Train> AvailableTrains { get; }

    #endregion

    #region Selected Items

    [ObservableProperty]
    private JourneyViewModel? selectedJourney;

    [ObservableProperty]
    private StationViewModel? selectedStation;

    [ObservableProperty]
    private WorkflowViewModel? selectedWorkflow;

    [ObservableProperty]
    private TrainViewModel? selectedTrain;

    #endregion

    #region Journey Commands

    [RelayCommand]
    private void AddJourney()
    {
        var newJourney = new Journey { Name = "New Journey" };
        _project.Journeys.Add(newJourney);

        var journeyVM = new JourneyViewModel(newJourney);
        Journeys.Add(journeyVM);
    }

    [RelayCommand]
    private void DeleteJourney(JourneyViewModel? journeyVM)
    {
        if (journeyVM == null) return;

        _project.Journeys.Remove(journeyVM.Model);
        Journeys.Remove(journeyVM);

        if (SelectedJourney == journeyVM)
        {
            SelectedJourney = null;
        }
    }

    #endregion

    #region Station Commands

    [RelayCommand]
    private void AddStation()
    {
        if (SelectedJourney == null) return;

        var newStation = new Station
        {
            Name = "New Station",
            Number = (uint)SelectedJourney.Model.Stations.Count + 1
        };

        SelectedJourney.Model.Stations.Add(newStation);

        var stationVM = new StationViewModel(newStation);
        SelectedJourney.Stations.Add(stationVM);
    }

    [RelayCommand]
    private void DeleteStation(StationViewModel? stationVM)
    {
        if (stationVM == null || SelectedJourney == null) return;

        SelectedJourney.Model.Stations.Remove(stationVM.Model);
        SelectedJourney.Stations.Remove(stationVM);

        // Renumber remaining stations
        SelectedJourney.RenumberStations();

        if (SelectedStation == stationVM)
        {
            SelectedStation = null;
        }
    }

    #endregion

    #region Workflow Commands

    [RelayCommand]
    private void AddWorkflow()
    {
        var newWorkflow = new Workflow { Name = "New Workflow" };
        _project.Workflows.Add(newWorkflow);

        var workflowVM = new WorkflowViewModel(newWorkflow);
        Workflows.Add(workflowVM);

        // Update reference collection
        AvailableWorkflows.Add(newWorkflow);
    }

    [RelayCommand]
    private void DeleteWorkflow(WorkflowViewModel? workflowVM)
    {
        if (workflowVM == null) return;

        // Check if workflow is referenced by any station
        bool isReferenced = _project.Journeys
            .SelectMany(j => j.Stations)
            .Any(s => s.Flow == workflowVM.Model);

        if (isReferenced)
        {
            // TODO: Show error message to user
            // For now, we just prevent deletion
            return;
        }

        _project.Workflows.Remove(workflowVM.Model);
        Workflows.Remove(workflowVM);

        // Update reference collection
        AvailableWorkflows.Remove(workflowVM.Model);

        if (SelectedWorkflow == workflowVM)
        {
            SelectedWorkflow = null;
        }
    }

    #endregion

    #region Train Commands

    [RelayCommand]
    private void AddTrain()
    {
        var newTrain = new Train { Name = "New Train" };
        _project.Trains.Add(newTrain);

        var trainVM = new TrainViewModel(newTrain);
        Trains.Add(trainVM);

        // Update reference collection
        AvailableTrains.Add(newTrain);
    }

    [RelayCommand]
    private void DeleteTrain(TrainViewModel? trainVM)
    {
        if (trainVM == null) return;

        // Check if train is referenced by any journey
        bool isReferenced = _project.Journeys.Any(j => j.Train == trainVM.Model);

        if (isReferenced)
        {
            // TODO: Show error message to user
            // For now, we just prevent deletion
            return;
        }

        _project.Trains.Remove(trainVM.Model);
        Trains.Remove(trainVM);

        // Update reference collection
        AvailableTrains.Remove(trainVM.Model);

        if (SelectedTrain == trainVM)
        {
            SelectedTrain = null;
        }
    }

    #endregion
}
