// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using Moba.Domain;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Moba.SharedUI.Service;

using System.Collections.ObjectModel;
using System.Linq;

/// <summary>
/// ViewModel for the Project Configuration page.
/// Provides CRUD operations for Journeys, Stations, Workflows, and Trains.
/// Exposes MainWindowViewModel selection properties for cross-view synchronization.
/// </summary>
public partial class ProjectConfigurationPageViewModel : ObservableObject
{
    private readonly Project _project;
    private readonly IPreferencesService? _preferencesService;
    private readonly WinUI.MainWindowViewModel? _mainWindowViewModel;

    public ProjectConfigurationPageViewModel(Project project, IPreferencesService? preferencesService = null, WinUI.MainWindowViewModel? mainWindowViewModel = null)
    {
        _project = project ?? throw new ArgumentNullException(nameof(project));
        _preferencesService = preferencesService;
        _mainWindowViewModel = mainWindowViewModel;
        
        // ✅ Subscribe to MainWindowViewModel selection changes
        if (_mainWindowViewModel != null)
        {
            _mainWindowViewModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(_mainWindowViewModel.SelectedJourney))
                    OnPropertyChanged(nameof(SelectedJourney));
                else if (e.PropertyName == nameof(_mainWindowViewModel.SelectedStation))
                    OnPropertyChanged(nameof(SelectedStation));
            };
        }

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

        // Load preferences
        if (_preferencesService != null)
        {
            AutoLoadEnabled = _preferencesService.AutoLoadLastSolution;
        }
    }

    #region Collections

    public ObservableCollection<JourneyViewModel> Journeys { get; }

    public ObservableCollection<WorkflowViewModel> Workflows { get; }

    public ObservableCollection<TrainViewModel> Trains { get; }

    // Reference collections for ComboBoxes
    public ObservableCollection<Workflow> AvailableWorkflows { get; }

    public ObservableCollection<Train> AvailableTrains { get; }

    #endregion

    #region Application Settings

    [ObservableProperty]
    private bool autoLoadEnabled = true;

    partial void OnAutoLoadEnabledChanged(bool value)
    {
        if (_preferencesService != null)
        {
            _preferencesService.AutoLoadLastSolution = value;
            System.Diagnostics.Debug.WriteLine($"✅ Auto-load setting changed to: {value}");
        }
    }

    #endregion

    #region Selected Items - Delegated to MainWindowViewModel for cross-view synchronization

    public JourneyViewModel? SelectedJourney
    {
        get => _mainWindowViewModel?.SelectedJourney;
        set
        {
            if (_mainWindowViewModel != null && _mainWindowViewModel.SelectedJourney != value)
            {
                _mainWindowViewModel.SelectedJourney = value;
                OnSelectedJourneyChanged(value);
            }
        }
    }

    public StationViewModel? SelectedStation
    {
        get => _mainWindowViewModel?.SelectedStation;
        set
        {
            if (_mainWindowViewModel != null && _mainWindowViewModel.SelectedStation != value)
            {
                _mainWindowViewModel.SelectedStation = value;
                OnSelectedStationChanged(value);
            }
        }
    }

    public WorkflowViewModel? SelectedWorkflow
    {
        get => _mainWindowViewModel?.SelectedWorkflow;
        set
        {
            if (_mainWindowViewModel != null)
                _mainWindowViewModel.SelectedWorkflow = value;
        }
    }

    public TrainViewModel? SelectedTrain
    {
        get => _mainWindowViewModel?.SelectedTrain;
        set
        {
            if (_mainWindowViewModel != null)
                _mainWindowViewModel.SelectedTrain = value;
        }
    }

    // Partial methods for side effects when selection changes
    private void OnSelectedJourneyChanged(JourneyViewModel? value)
    {
        System.Diagnostics.Debug.WriteLine($"🔄 SelectedJourney changed to: {value?.Name ?? "null"}");
        DeleteJourneyCommand.NotifyCanExecuteChanged();
        AddStationCommand.NotifyCanExecuteChanged();
        System.Diagnostics.Debug.WriteLine($"   CanDeleteJourney is now: {CanDeleteJourney()}");
    }

    private void OnSelectedStationChanged(StationViewModel? value)
    {
        System.Diagnostics.Debug.WriteLine($"🔄 SelectedStation changed to: {value?.Name ?? "null"}");
        DeleteStationCommand.NotifyCanExecuteChanged();
        System.Diagnostics.Debug.WriteLine($"   CanDeleteStation is now: {CanDeleteStation()}");
    }

    private void OnSelectedWorkflowChanged(WorkflowViewModel? value)
    {
        System.Diagnostics.Debug.WriteLine($"🔄 SelectedWorkflow changed to: {value?.Name ?? "null"}");
        DeleteWorkflowCommand.NotifyCanExecuteChanged();
        System.Diagnostics.Debug.WriteLine($"   CanDeleteWorkflow is now: {CanDeleteWorkflow()}");
    }

    private void OnSelectedTrainChanged(TrainViewModel? value)
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
            NumberOfLapsToStop = 2
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

        // Note: Journey.Train property removed in Clean Architecture refactoring
        // Train-Journey relationship check temporarily disabled
        bool isReferenced = false; // TODO: Implement new train relationship check

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
