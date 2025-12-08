// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Moba.Backend.Interface;
using Moba.Backend.Manager;
using Moba.Common.Configuration;
using Moba.Common.Extensions;
using Moba.Domain;
using Moba.SharedUI.Enum;
using Moba.SharedUI.Helper;
using Moba.SharedUI.Interface;

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
    private readonly EntitySelectionManager _selectionManager;

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

        // Initialize EntitySelectionManager (simplified - no more ClearOtherSelections needed)
        _selectionManager = new EntitySelectionManager(NotifySelectionPropertiesChanged);

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
    private bool hasUnsavedChanges = false;

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
    /// Returns true if any entity is currently selected.
    /// Used to show/hide PropertyGrid content.
    /// </summary>
    public bool HasSelectedEntity =>
        SolutionViewModel != null ||
        SelectedProject != null ||
        SelectedJourney != null ||
        SelectedStation != null ||
        SelectedWorkflow != null ||
        SelectedTrain != null;

    /// <summary>
    /// Tracks which entity type was selected last for PropertyGrid display priority.
    /// </summary>
    [ObservableProperty]
    private MobaType currentSelectedEntityType = MobaType.None;

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
        HasUnsavedChanges = true;

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

        // Create Station from City's first station (Hauptbahnhof)
        var cityStation = city.Stations.FirstOrDefault();
        if (cityStation == null) return;

        var newStation = new Station
        {
            Name = cityStation.Name,
            Track = cityStation.Track,
            NumberOfLapsToStop = 1,
            Description = $"From {city.Name}",
            IsExitOnLeft = cityStation.IsExitOnLeft
        };

        // Add to Project master list (if not already there)
        if (CurrentProjectViewModel != null && !CurrentProjectViewModel.Model.Stations.Any(s => s.Id == newStation.Id))
        {
            CurrentProjectViewModel.Model.Stations.Add(newStation);
        }

        // Add ID to Journey
        SelectedJourney.Model.StationIds.Add(newStation.Id);

        // Refresh Journey's Stations collection
        SelectedJourney.RefreshStations();
        
        // Select the new station
        var stationVM = SelectedJourney.Stations.LastOrDefault();
        if (stationVM != null)
        {
            SelectedStation = stationVM;
        }

        HasUnsavedChanges = true;

        System.Diagnostics.Debug.WriteLine($"✅ Added station from city: {city.Name} → {newStation.Name}");
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
}