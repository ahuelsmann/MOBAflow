// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using Backend.Manager;

using Common.Configuration;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Domain;

using Interface;

using Moba.Backend.Interface;
using Moba.Common.Extensions;

using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

/// <summary>
/// Core ViewModel for main window functionality.
/// Partial classes handle: Selection, Solution, Journey, Workflow, Train, Z21, Settings.
/// </summary>
public partial class MainWindowViewModel : ObservableObject
{
    #region Fields

    private readonly IIoService _ioService;
    private readonly IZ21 _z21;
    private readonly IJourneyManagerFactory _journeyManagerFactory;
    private readonly IUiDispatcher _uiDispatcher;
    private readonly AppSettings _settings;
    private readonly ISettingsService? _settingsService;
    private readonly ICityService? _cityLibraryService;
    private JourneyManager? _journeyManager;

    #endregion

    #region Constructor

    public MainWindowViewModel(
        IIoService ioService,
        IZ21 z21,
        IJourneyManagerFactory journeyManagerFactory,
        IUiDispatcher uiDispatcher,
        AppSettings settings,
        Solution solution,
        ICityService? cityLibraryService = null,
        ISettingsService? settingsService = null)
    {
        _ioService = ioService;
        _z21 = z21;
        _journeyManagerFactory = journeyManagerFactory;
        _uiDispatcher = uiDispatcher;
        _settings = settings;
        _cityLibraryService = cityLibraryService;
        _settingsService = settingsService;

        // Subscribe to Solution changes
        Solution = solution;

        // Load City Library at startup (fire-and-forget)
        if (_cityLibraryService != null)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await LoadCityLibraryAsync();
                    System.Diagnostics.Debug.WriteLine($"✅ City Library loaded: {CityLibrary.Count} cities");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ Failed to load City Library: {ex.Message}");
                }
            });
        }
    }

    #endregion

    #region Properties

    [ObservableProperty]
    private Solution solution;

    [ObservableProperty]
    private string? currentSolutionPath;

    [ObservableProperty]
    private bool hasSolution;

    [ObservableProperty]
    private SolutionViewModel? solutionViewModel;

    [ObservableProperty]
    private ProjectViewModel? selectedProject;

    [ObservableProperty]
    private JourneyViewModel? selectedJourney;

    [ObservableProperty]
    private StationViewModel? selectedStation;

    [ObservableProperty]
    private WorkflowViewModel? selectedWorkflow;

    [ObservableProperty]
    private object? selectedAction;

    [ObservableProperty]
    private TrainViewModel? selectedTrain;

    /// <summary>
    /// The currently selected object to display in the properties panel.
    /// </summary>
    [ObservableProperty]
    private object? currentSelectedObject;

    [ObservableProperty]
    private bool isZ21Connected;

    [ObservableProperty]
    private bool isTrackPowerOn;

    [ObservableProperty]
    private string z21StatusText = "Disconnected";

    [ObservableProperty]
    private string simulateInPort = "1";

    /// <summary>
    /// Available cities with stations (loaded from master data).
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<City> availableCities = [];

    /// <summary>
    /// Currently selected city for adding stations to journeys.
    /// </summary>
    [ObservableProperty]
    private City? selectedCity;

    /// <summary>
    /// Gets the currently selected project as a ProjectViewModel.
    /// Returns null if no project is selected or SolutionViewModel is not initialized.
    /// </summary>
    public ProjectViewModel? CurrentProjectViewModel
    {
        get
        {
            // Use SelectedProject if available, otherwise fall back to first project
            if (SelectedProject != null)
                return SelectedProject;

            if (SolutionViewModel == null || SolutionViewModel.Projects.Count == 0)
                return null;

            // For now, return the first project (backward compatibility)
            return SolutionViewModel.Projects.FirstOrDefault();
        }
    }

    public event EventHandler? ExitApplicationRequested;

    #endregion

    #region Project Management

    [RelayCommand]
    private void AddProject()
    {
        // Add a simple project placeholder to the current Solution
        var project = new Project { Name = "New Project" };
        Solution.Projects.Add(project);

        // Create ViewModel and add to SolutionViewModel
        var projectVM = new ProjectViewModel(project);
        SolutionViewModel?.Projects.Add(projectVM);

        // Select the newly created project
        SelectedProject = projectVM;

        // Refresh view model and mark unsaved changes
        SolutionViewModel?.Refresh();

        SaveSolutionCommand.NotifyCanExecuteChanged();
    }

    #endregion

    #region Lifecycle

    public void OnWindowClosing()
    {
        if (_journeyManager != null)
        {
            try
            {
                _journeyManager.Dispose();
                _journeyManager = null;

                var z21 = _z21;

                _ = Task.Run(async () =>
                {
                    try { await z21.DisconnectAsync(); }
                    catch (TaskCanceledException) { }
                    catch (OperationCanceledException) { }
                    catch (Exception ex)
                    {
                        this.LogError("Error during Z21 shutdown", ex);
                    }
                });
            }
            catch (Exception ex)
            {
                this.LogError("Error scheduling Z21 shutdown", ex);
            }
        }

        _z21.OnSystemStateChanged -= OnZ21SystemStateChanged;
        _z21.OnConnectionLost -= HandleConnectionLost;
        ExitApplicationRequested?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void ExitApplication()
    {
        ExitApplicationRequested?.Invoke(this, EventArgs.Empty);
    }

    #endregion

    #region City Library

    [ObservableProperty]
    private ObservableCollection<City> cityLibrary = [];

    [ObservableProperty]
    private string citySearchText = string.Empty;

    partial void OnCitySearchTextChanged(string value)
    {
        if (_cityLibraryService == null) return;

        var filtered = _cityLibraryService.FilterCities(value);
        CityLibrary = new ObservableCollection<City>(filtered);
    }

    [RelayCommand(CanExecute = nameof(CanAddStationFromCity))]
    private void AddStationFromCity(City city)
    {
        if (SelectedJourney == null || city == null) return;

        // Get City's first station (Hauptbahnhof) - only the NAME
        var cityStation = city.Stations.FirstOrDefault();
        if (cityStation == null) return;

        // Create NEW Station object (copy name from City Library)
        var newStation = new Station
        {
            Name = cityStation.Name,
            InPort = 1,  // User must configure InPort!
            IsExitOnLeft = false,
            NumberOfLapsToStop = 1,
            WorkflowId = null
        };

        // Add JourneyStation to Journey
        SelectedJourney.Model.Stations.Add(newStation);

        // Refresh Journey's Stations collection
        SelectedJourney.RefreshStations();

        // Select the new station
        var stationVM = SelectedJourney.Stations.LastOrDefault();
        if (stationVM != null)
        {
            SelectedStation = stationVM;
        }

        System.Diagnostics.Debug.WriteLine($"✅ Added station from city: {city.Name} → {cityStation.Name}");
    }

    private bool CanAddStationFromCity() => SelectedJourney != null;

    /// <summary>
    /// Loads the city library from the city library service.
    /// Call this during initialization.
    /// </summary>
    public async Task LoadCityLibraryAsync()
    {
        if (_cityLibraryService == null) return;

        var cities = await _cityLibraryService.LoadCitiesAsync();
        CityLibrary = new ObservableCollection<City>(cities);

        System.Diagnostics.Debug.WriteLine($"✅ City Library loaded: {CityLibrary.Count} cities");
    }

    #endregion

    #region Wagon Libraries

    [ObservableProperty]
    private ObservableCollection<GoodsWagon> goodsWagonLibrary =
    [
        new GoodsWagon { Name = "Goods Wagon 1", Cargo = Domain.Enum.CargoType.Container },
        new GoodsWagon { Name = "Goods Wagon 2", Cargo = Domain.Enum.CargoType.Coal },
        new GoodsWagon { Name = "Goods Wagon 3", Cargo = Domain.Enum.CargoType.Wood }
    ];

    [ObservableProperty]
    private ObservableCollection<PassengerWagon> passengerWagonLibrary =
    [
        new PassengerWagon { Name = "Passenger Wagon 1st Class", WagonClass = Domain.Enum.PassengerClass.First },
        new PassengerWagon { Name = "Passenger Wagon 2nd Class", WagonClass = Domain.Enum.PassengerClass.Second }
    ];

    #endregion

    #region Drag & Drop Commands

    [RelayCommand(CanExecute = nameof(CanAssignWorkflowToStation))]
    private void AssignWorkflowToStation(WorkflowViewModel? workflow)
    {
        if (SelectedStation == null || workflow == null) return;

        SelectedStation.WorkflowId = workflow.Model.Id;

        System.Diagnostics.Debug.WriteLine($"✅ Assigned workflow '{workflow.Name}' to station '{SelectedStation.Name}'");
    }

    private bool CanAssignWorkflowToStation() => SelectedStation != null;

    [RelayCommand(CanExecute = nameof(CanAddLocomotiveToTrain))]
    private void AddLocomotiveToTrain(LocomotiveViewModel? locomotiveVM)
    {
        if (SelectedTrain == null || locomotiveVM == null || CurrentProjectViewModel == null) return;

        // Create a copy to avoid modifying the library object
        var locomotiveCopy = new Locomotive
        {
            Name = locomotiveVM.Model.Name,
            DigitalAddress = locomotiveVM.Model.DigitalAddress,
            Manufacturer = locomotiveVM.Model.Manufacturer,
            ArticleNumber = locomotiveVM.Model.ArticleNumber,
            Series = locomotiveVM.Model.Series,
            ColorPrimary = locomotiveVM.Model.ColorPrimary,
            ColorSecondary = locomotiveVM.Model.ColorSecondary,
            IsPushing = locomotiveVM.Model.IsPushing,
            Details = locomotiveVM.Model.Details
        };

        // Add to Project master list
        CurrentProjectViewModel.Model.Locomotives.Add(locomotiveCopy);

        // Add ID to Train
        SelectedTrain.Model.LocomotiveIds.Add(locomotiveCopy.Id);

        // Refresh Train collections
        SelectedTrain.RefreshCollections();

        System.Diagnostics.Debug.WriteLine($"✅ Added locomotive '{locomotiveCopy.Name}' to train '{SelectedTrain.Name}'");
    }

    private bool CanAddLocomotiveToTrain() => SelectedTrain != null && CurrentProjectViewModel != null;

    #endregion
}