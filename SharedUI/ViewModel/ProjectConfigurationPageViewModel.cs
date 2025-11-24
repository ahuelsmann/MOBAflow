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

    partial void OnSelectedJourneyChanged(JourneyViewModel? value)
    {
        System.Diagnostics.Debug.WriteLine($"🔄 SelectedJourney changed to: {value?.Name ?? "null"}");
        DeleteJourneyCommand.NotifyCanExecuteChanged();
        AddStationCommand.NotifyCanExecuteChanged();
        System.Diagnostics.Debug.WriteLine($"   CanDeleteJourney is now: {CanDeleteJourney()}");
    }

    partial void OnSelectedStationChanged(StationViewModel? value)
    {
        System.Diagnostics.Debug.WriteLine($"🔄 SelectedStation changed to: {value?.Name ?? "null"}");
        DeleteStationCommand.NotifyCanExecuteChanged();
        System.Diagnostics.Debug.WriteLine($"   CanDeleteStation is now: {CanDeleteStation()}");
    }

    partial void OnSelectedWorkflowChanged(WorkflowViewModel? value)
    {
        System.Diagnostics.Debug.WriteLine($"🔄 SelectedWorkflow changed to: {value?.Name ?? "null"}");
        DeleteWorkflowCommand.NotifyCanExecuteChanged();
        System.Diagnostics.Debug.WriteLine($"   CanDeleteWorkflow is now: {CanDeleteWorkflow()}");
    }

    partial void OnSelectedTrainChanged(TrainViewModel? value)
    {
        System.Diagnostics.Debug.WriteLine($"🔄 SelectedTrain changed to: {value?.Name ?? "null"}");
        DeleteTrainCommand.NotifyCanExecuteChanged();
        System.Diagnostics.Debug.WriteLine($"   CanDeleteTrain is now: {CanDeleteTrain()}");
    }

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

    [RelayCommand(CanExecute = nameof(CanDeleteJourney))]
    private void DeleteJourney()
    {
        if (SelectedJourney == null) return;
        
        _project.Journeys.Remove(SelectedJourney.Model);
        Journeys.Remove(SelectedJourney);
        SelectedJourney = null;
    }

    private bool CanDeleteJourney() => SelectedJourney != null;

    #endregion

    #region Station Commands

    [RelayCommand(CanExecute = nameof(CanAddStation))]
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

    private bool CanAddStation() => SelectedJourney != null;

    [RelayCommand(CanExecute = nameof(CanDeleteStation))]
    private void DeleteStation()
    {
        if (SelectedStation == null || SelectedJourney == null) return;

        SelectedJourney.Model.Stations.Remove(SelectedStation.Model);
        SelectedJourney.Stations.Remove(SelectedStation);

        // Renumber remaining stations
        SelectedJourney.RenumberStations();
        SelectedStation = null;
    }

    private bool CanDeleteStation() => SelectedStation != null;

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

    [RelayCommand(CanExecute = nameof(CanDeleteWorkflow))]
    private void DeleteWorkflow()
    {
        if (SelectedWorkflow == null) return;

        // Check if workflow is referenced by any station
        bool isReferenced = _project.Journeys
            .SelectMany(j => j.Stations)
            .Any(s => s.Flow == SelectedWorkflow.Model);

        if (isReferenced)
        {
            // TODO: Show error message to user
            // For now, we just prevent deletion
            return;
        }

        _project.Workflows.Remove(SelectedWorkflow.Model);
        Workflows.Remove(SelectedWorkflow);

        // Update reference collection
        AvailableWorkflows.Remove(SelectedWorkflow.Model);
        SelectedWorkflow = null;
    }

    private bool CanDeleteWorkflow() => SelectedWorkflow != null;

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

    [RelayCommand(CanExecute = nameof(CanDeleteTrain))]
    private void DeleteTrain()
    {
        if (SelectedTrain == null) return;

        // Check if train is referenced by any journey
        bool isReferenced = _project.Journeys.Any(j => j.Train == SelectedTrain.Model);

        if (isReferenced)
        {
            // TODO: Show error message to user
            // For now, we just prevent deletion
            return;
        }

        _project.Trains.Remove(SelectedTrain.Model);
        Trains.Remove(SelectedTrain);

        // Update reference collection
        AvailableTrains.Remove(SelectedTrain.Model);
        SelectedTrain = null;
    }

    private bool CanDeleteTrain() => SelectedTrain != null;

    #endregion
}
