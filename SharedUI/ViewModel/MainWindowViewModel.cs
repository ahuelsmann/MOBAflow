// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Moba.Backend.Interface;
using Moba.Backend.Manager;
using Moba.Common.Configuration;
using Moba.Common.Extensions;
using Moba.Domain;
using Moba.Domain.Enum;
using Moba.SharedUI.Service;

using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

/// <summary>
/// Base ViewModel for main window functionality (Z21 connection, solution management).
/// TreeView functionality has been removed - UI binding now happens directly to SolutionViewModel.
/// Platform-specific implementations (WinUI, MAUI) can extend this class.
/// </summary>
public partial class MainWindowViewModel : ObservableObject
{
    private readonly IIoService _ioService;
    private readonly IZ21 _z21;
    private readonly IJourneyManagerFactory _journeyManagerFactory;
    private readonly IUiDispatcher _uiDispatcher;
    private readonly AppSettings _settings;
    private readonly ISettingsService? _settingsService;
    private JourneyManager? _journeyManager;

    // Primary ctor for DI
    public MainWindowViewModel(
        IIoService ioService,
        IZ21 z21,
        IJourneyManagerFactory journeyManagerFactory,
        IUiDispatcher uiDispatcher,
        AppSettings settings,
        Solution solution,
        ICityLibraryService? cityLibraryService = null,
        ISettingsService? settingsService = null)
    {
        _ioService = ioService;
        _z21 = z21;
        _journeyManagerFactory = journeyManagerFactory;
        _uiDispatcher = uiDispatcher;
        _settings = settings;
        _cityLibraryService = cityLibraryService;
        _settingsService = settingsService;

        // Subscribe to Z21 events
        _z21.OnSystemStateChanged += OnZ21SystemStateChanged;

        // Use injected Solution from DI container
        Solution = solution;

        // Load city library asynchronously
        _ = LoadCityLibraryAsync();
    }

    [RelayCommand]
    private void AddProject()
    {
        // Add a simple project placeholder to the current Solution
        var project = new Project { Name = "New Project" };
        Solution.Projects.Add(project);

        // Refresh view model and mark unsaved changes
        SolutionViewModel?.Refresh();
        HasUnsavedChanges = true;

        SaveSolutionCommand.NotifyCanExecuteChanged();
    }

    #region Properties

    [ObservableProperty]
    private string title = "MOBAflow";

    [ObservableProperty]
    private string? currentSolutionPath;

    [ObservableProperty]
    private bool hasSolution;

    [ObservableProperty]
    private Solution solution = null!;

    [ObservableProperty]
    private SolutionViewModel? solutionViewModel;

    [ObservableProperty]
    private bool hasUnsavedChanges = false;

    [ObservableProperty]
    private JourneyViewModel? selectedJourney;

    [ObservableProperty]
    private StationViewModel? selectedStation;

    [ObservableProperty]
    private WorkflowViewModel? selectedWorkflow;

    [ObservableProperty]
    private TrainViewModel? selectedTrain;

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
    private ObservableCollection<Domain.City> availableCities = [];

    /// <summary>
    /// Currently selected city for adding stations to journeys.
    /// </summary>
    [ObservableProperty]
    private Backend.Data.City? selectedCity;

    /// <summary>
    /// Gets the currently selected project as a ProjectViewModel.
    /// Returns null if no project is selected or SolutionViewModel is not initialized.
    /// </summary>
    public ProjectViewModel? CurrentProjectViewModel
    {
        get
        {
            if (SolutionViewModel == null || SolutionViewModel.Projects.Count == 0)
                return null;

            // For now, return the first project (later can be extended to support project selection)
            return SolutionViewModel.Projects.FirstOrDefault();
        }
    }

    public event EventHandler? ExitApplicationRequested;

    #endregion

    #region Solution Management

    partial void OnSolutionChanged(Solution? value)
    {
        if (value == null)
        {
            HasSolution = false;
            SolutionViewModel = null;
            AvailableCities.Clear();
            OnPropertyChanged(nameof(CurrentProjectViewModel)); // ✅ Notify that project changed
            return;
        }

        // Ensure Solution always has at least one project
        if (value.Projects.Count == 0)
        {
            value.Projects.Add(new Project { Name = "(Untitled Project)" });
        }

        SolutionViewModel = new SolutionViewModel(value, _uiDispatcher);
        HasSolution = value.Projects.Count > 0;

        SaveSolutionCommand.NotifyCanExecuteChanged();
        ConnectToZ21Command.NotifyCanExecuteChanged();
        OnPropertyChanged(nameof(CurrentProjectViewModel)); // ✅ Notify that project changed

        LoadCities();
    }

    private void LoadCities()
    {
        AvailableCities.Clear();

        if (Solution?.Projects?.Count > 0)
        {
            var firstProject = Solution.Projects[0];
            if (firstProject?.Cities != null)
            {
                foreach (var city in firstProject.Cities)
                {
                    AvailableCities.Add(city);
                }
            }
        }
    }

    [RelayCommand]
    private async Task LoadCitiesFromFileAsync()
    {
        if (Solution.Projects.Count == 0) return;

        var (dataManager, _, error) = await _ioService.LoadDataManagerAsync();

        if (dataManager != null && Solution.Projects.Count > 0)
        {
            var firstProject = Solution.Projects[0];
            firstProject.Cities.Clear();
            // Note: City loading temporarily disabled due to Backend.Data.City vs Domain.City mismatch
            // TODO: Migrate CityDataManager to use Domain.City or create converter
            // firstProject.Cities.AddRange(dataManager.Cities);
            LoadCities();
        }
        else if (!string.IsNullOrEmpty(error))
        {
            throw new InvalidOperationException($"Failed to load cities: {error}");
        }
    }

    [RelayCommand(CanExecute = nameof(CanSaveSolution))]
    private async Task SaveSolutionAsync()
    {
        var (success, path, error) = await _ioService.SaveAsync(Solution, CurrentSolutionPath);
        if (success && path != null)
        {
            CurrentSolutionPath = path;
            HasUnsavedChanges = false;
        }
        else if (!string.IsNullOrEmpty(error))
        {
            throw new InvalidOperationException($"Failed to save solution: {error}");
        }
    }

    [RelayCommand]
    private async Task NewSolutionAsync()
    {
        var (success, userCancelled, error) = await _ioService.NewSolutionAsync(HasUnsavedChanges);

        if (userCancelled)
        {
            return;
        }

        if (!success)
        {
            if (error == "SAVE_REQUESTED")
            {
                if (SaveSolutionCommand.CanExecute(null))
                {
                    await SaveSolutionCommand.ExecuteAsync(null);
                }
            }
            else
            {
                return;
            }
        }

        // Clear existing Solution (DI singleton)
        Solution.Projects.Clear();
        Solution.Name = "New Solution";

        // Add default project
        Solution.Projects.Add(new Project
        {
            Name = "New Project",
            Journeys = new System.Collections.Generic.List<Journey>(),
            Workflows = new System.Collections.Generic.List<Workflow>(),
            Trains = new System.Collections.Generic.List<Train>()
        });

        SolutionViewModel?.Refresh();

        CurrentSolutionPath = null;
        HasUnsavedChanges = true;

        SaveSolutionCommand.NotifyCanExecuteChanged();
        ConnectToZ21Command.NotifyCanExecuteChanged();
    }

    partial void OnSelectedJourneyChanged(JourneyViewModel? value)
    {
        AddStationToJourneyCommand.NotifyCanExecuteChanged();
        
        // Clear station selection to show Journey properties in PropertyGrid
        SelectedStation = null;
    }

    [RelayCommand]
    private async Task LoadSolutionAsync()
    {
        var (loadedSolution, path, error) = await _ioService.LoadAsync();

        if (!string.IsNullOrEmpty(error))
        {
            throw new InvalidOperationException($"Failed to load solution: {error}");
        }

        if (loadedSolution != null)
        {
            // Update existing Solution instance
            Solution.Projects.Clear();
            foreach (var project in loadedSolution.Projects)
            {
                Solution.Projects.Add(project);
            }

            Solution.Name = loadedSolution.Name;

            SolutionViewModel?.Refresh();

            CurrentSolutionPath = path;
            HasUnsavedChanges = false;
            HasSolution = Solution.Projects.Count > 0;

            SaveSolutionCommand.NotifyCanExecuteChanged();
            ConnectToZ21Command.NotifyCanExecuteChanged();

            LoadCities();

            OnPropertyChanged(nameof(Solution));
        }
    }

    private bool CanSaveSolution() => true;

    #endregion

    #region Z21 Connection

    [RelayCommand(CanExecute = nameof(CanConnectToZ21))]
    private async Task ConnectToZ21Async()
    {
        if (!string.IsNullOrEmpty(_settings.Z21.CurrentIpAddress))
        {
            try
            {
                Z21StatusText = "Connecting...";
                var address = System.Net.IPAddress.Parse(_settings.Z21.CurrentIpAddress);

                int port = 21105;
                if (!string.IsNullOrEmpty(_settings.Z21.DefaultPort) && int.TryParse(_settings.Z21.DefaultPort, out var parsedPort))
                {
                    port = parsedPort;
                }

                await _z21.ConnectAsync(address, port);
                IsZ21Connected = true;
                Z21StatusText = $"Connected to {_settings.Z21.CurrentIpAddress}:{port}";

                if (Solution.Projects.Count > 0)
                {
                    var project = Solution.Projects[0];
                    var executionContext = new Moba.Backend.Services.ActionExecutionContext
                    {
                        Z21 = _z21
                    };

                    _journeyManager?.Dispose();
                    _journeyManager = _journeyManagerFactory.Create(_z21, project.Journeys, executionContext);
                }

                ConnectToZ21Command.NotifyCanExecuteChanged();
                DisconnectFromZ21Command.NotifyCanExecuteChanged();
                SetTrackPowerCommand.NotifyCanExecuteChanged();
            }
            catch (Exception ex)
            {
                Z21StatusText = $"Connection failed: {ex.Message}";
            }
        }
        else
        {
            Z21StatusText = "No IP address configured in AppSettings";
        }
    }

    [RelayCommand(CanExecute = nameof(CanDisconnectFromZ21))]
    private async Task DisconnectFromZ21Async()
    {
        try
        {
            Z21StatusText = "Disconnecting...";

            _journeyManager?.Dispose();
            _journeyManager = null;

            await _z21.DisconnectAsync();

            IsZ21Connected = false;
            IsTrackPowerOn = false;
            Z21StatusText = "Disconnected";

            ConnectToZ21Command.NotifyCanExecuteChanged();
            SetTrackPowerCommand.NotifyCanExecuteChanged();
        }
        catch (Exception ex)
        {
            Z21StatusText = $"Error: {ex.Message}";
        }
    }

    [RelayCommand]
    private void SimulateFeedback()
    {
        try
        {
            // Create JourneyManager if needed for simulation
            if (_journeyManager == null && Solution?.Projects.Count > 0)
            {
                var project = Solution.Projects[0];
                var executionContext = new Moba.Backend.Services.ActionExecutionContext
                {
                    Z21 = _z21
                };
                _journeyManager = _journeyManagerFactory.Create(_z21, project.Journeys, executionContext);
            }

            // Get InPort from selected journey or text field
            uint? selectedInPort = SelectedJourney?.InPort;

            int inPort;
            if (selectedInPort.HasValue)
            {
                inPort = unchecked((int)selectedInPort.Value);
            }
            else if (!int.TryParse(SimulateInPort, out inPort))
            {
                Z21StatusText = "Invalid InPort number";
                return;
            }

            _z21.SimulateFeedback(inPort);
            Z21StatusText = $"Simulated feedback for InPort {inPort}";
        }
        catch (Exception ex)
        {
            Z21StatusText = $"Error: {ex.Message}";
        }
    }

    [RelayCommand(CanExecute = nameof(CanToggleTrackPower))]
    private async Task SetTrackPowerAsync(bool turnOn)
    {
        try
        {
            if (turnOn)
            {
                await _z21.SetTrackPowerOnAsync();
                Z21StatusText = "Track power ON";
                IsTrackPowerOn = true;
            }
            else
            {
                await _z21.SetTrackPowerOffAsync();
                Z21StatusText = "Track power OFF";
                IsTrackPowerOn = false;
            }
        }
        catch (Exception ex)
        {
            Z21StatusText = $"Track power error: {ex.Message}";
        }
    }

    private bool CanConnectToZ21() => !IsZ21Connected;
    private bool CanDisconnectFromZ21() => IsZ21Connected;
    private bool CanToggleTrackPower() => IsZ21Connected;

    private void OnZ21SystemStateChanged(Backend.SystemState systemState)
    {
        _uiDispatcher.InvokeOnUi(() => UpdateZ21SystemState(systemState));
    }

    private void UpdateZ21SystemState(Backend.SystemState systemState)
    {
        IsTrackPowerOn = systemState.IsTrackPowerOn;

        var statusParts = new List<string>
        {
            "Connected",
            $"Current: {systemState.MainCurrent}mA",
            $"Temp: {systemState.Temperature}C"
        };

        if (systemState.IsEmergencyStop)
            statusParts.Add("WARNING: EMERGENCY STOP");

        if (systemState.IsShortCircuit)
            statusParts.Add("WARNING: SHORT CIRCUIT");

        if (systemState.IsProgrammingMode)
            statusParts.Add("Programming");

        Z21StatusText = string.Join(" | ", statusParts);

        this.Log($"Z21 System State: TrackPower={systemState.IsTrackPowerOn}, Current={systemState.MainCurrent}mA");
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
        ExitApplicationRequested?.Invoke(this, EventArgs.Empty);
    }

    #endregion

    #region Station Management

    [RelayCommand(CanExecute = nameof(CanAddStationToJourney))]
    private void AddStationToJourney()
    {
        if (SelectedCity == null || SelectedJourney == null)
            return;

        // Take the first station from the selected city
        if (SelectedCity.Stations.Count > 0)
        {
            var stationToCopy = SelectedCity.Stations[0];

            // Create a new Station instance (deep copy)
            var newStation = new Station
            {
                Name = stationToCopy.Name,
                Description = stationToCopy.Description,
                NumberOfLapsToStop = 2,
                InPort = stationToCopy.InPort
            };

            // Add to Journey model
            SelectedJourney.Model.Stations.Add(newStation);

            // Add to ViewModel
            var stationVM = new StationViewModel(newStation);
            SelectedJourney.Stations.Add(stationVM);

            HasUnsavedChanges = true;
        }
    }

    private bool CanAddStationToJourney() => true;

    #endregion

    #region Journey CRUD Commands

    [RelayCommand]
    private void AddJourney()
    {
        if (CurrentProjectViewModel == null) return;

        var newJourney = new Journey
        {
            Name = "New Journey",
            BehaviorOnLastStop = BehaviorOnLastStop.None
        };

        CurrentProjectViewModel.Model.Journeys.Add(newJourney);

        var journeyVM = new JourneyViewModel(newJourney);
        CurrentProjectViewModel.Journeys.Add(journeyVM);
        SelectedJourney = journeyVM;

        HasUnsavedChanges = true;
    }

    [RelayCommand(CanExecute = nameof(CanDeleteJourney))]
    private void DeleteJourney()
    {
        if (SelectedJourney == null || CurrentProjectViewModel == null) return;

        CurrentProjectViewModel.Model.Journeys.Remove(SelectedJourney.Model);
        CurrentProjectViewModel.Journeys.Remove(SelectedJourney);
        SelectedJourney = null;

        HasUnsavedChanges = true;
    }

    private bool CanDeleteJourney() => SelectedJourney != null;

    #endregion

    #region Station CRUD Commands

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
        SelectedStation = stationVM;

        HasUnsavedChanges = true;
    }

    [RelayCommand(CanExecute = nameof(CanDeleteStation))]
    private void DeleteStation()
    {
        if (SelectedStation == null || SelectedJourney == null) return;

        SelectedJourney.Model.Stations.Remove(SelectedStation.Model);
        SelectedJourney.Stations.Remove(SelectedStation);
        SelectedStation = null;

        HasUnsavedChanges = true;
    }

    private bool CanAddStation() => SelectedJourney != null;
    private bool CanDeleteStation() => SelectedStation != null;

    #endregion

    #region Workflow CRUD Commands

    [RelayCommand]
    private void AddWorkflow()
    {
        if (CurrentProjectViewModel == null) return;

        var newWorkflow = new Workflow { Name = "New Workflow" };
        CurrentProjectViewModel.Model.Workflows.Add(newWorkflow);

        var workflowVM = new WorkflowViewModel(newWorkflow);
        CurrentProjectViewModel.Workflows.Add(workflowVM);
        SelectedWorkflow = workflowVM;

        HasUnsavedChanges = true;
    }

    [RelayCommand(CanExecute = nameof(CanDeleteWorkflow))]
    private void DeleteWorkflow()
    {
        if (SelectedWorkflow == null || CurrentProjectViewModel == null) return;

        CurrentProjectViewModel.Model.Workflows.Remove(SelectedWorkflow.Model);
        CurrentProjectViewModel.Workflows.Remove(SelectedWorkflow);
        SelectedWorkflow = null;

        HasUnsavedChanges = true;
    }

    private bool CanDeleteWorkflow() => SelectedWorkflow != null;

    #endregion

    #region Train CRUD Commands

    [RelayCommand]
    private void AddTrain()
    {
        if (CurrentProjectViewModel == null) return;

        var newTrain = new Train { Name = "New Train" };
        CurrentProjectViewModel.Model.Trains.Add(newTrain);

        var trainVM = new TrainViewModel(newTrain);
        CurrentProjectViewModel.Trains.Add(trainVM);
        SelectedTrain = trainVM;

        HasUnsavedChanges = true;
    }

    [RelayCommand(CanExecute = nameof(CanDeleteTrain))]
    private void DeleteTrain()
    {
        if (SelectedTrain == null || CurrentProjectViewModel == null) return;

        CurrentProjectViewModel.Model.Trains.Remove(SelectedTrain.Model);
        CurrentProjectViewModel.Trains.Remove(SelectedTrain);
        SelectedTrain = null;

        HasUnsavedChanges = true;
    }

    private bool CanDeleteTrain() => SelectedTrain != null;

    #endregion

    #region Locomotive CRUD Commands

    [ObservableProperty]
    private LocomotiveViewModel? selectedLocomotive;

    [RelayCommand]
    private void AddLocomotive()
    {
        if (CurrentProjectViewModel == null) return;

        var newLocomotive = new Locomotive
        {
            Name = "New Locomotive",
            DigitalAddress = 3
        };

        CurrentProjectViewModel.Model.Locomotives.Add(newLocomotive);

        var locomotiveVM = new LocomotiveViewModel(newLocomotive);
        CurrentProjectViewModel.Locomotives.Add(locomotiveVM);
        SelectedLocomotive = locomotiveVM;

        HasUnsavedChanges = true;
    }

    [RelayCommand(CanExecute = nameof(CanDeleteLocomotive))]
    private void DeleteLocomotive()
    {
        if (SelectedLocomotive == null || CurrentProjectViewModel == null) return;

        CurrentProjectViewModel.Model.Locomotives.Remove(SelectedLocomotive.Model);
        CurrentProjectViewModel.Locomotives.Remove(SelectedLocomotive);
        SelectedLocomotive = null;

        HasUnsavedChanges = true;
    }

    private bool CanDeleteLocomotive() => SelectedLocomotive != null;

    #endregion

    #region Wagon CRUD Commands

    [ObservableProperty]
    private WagonViewModel? selectedWagon;

    [RelayCommand]
    private void AddPassengerWagon()
    {
        if (CurrentProjectViewModel == null) return;

        var newWagon = new PassengerWagon
        {
            Name = "New Passenger Wagon",
            WagonClass = PassengerClass.Second
        };

        CurrentProjectViewModel.Model.PassengerWagons.Add(newWagon);

        var wagonVM = new PassengerWagonViewModel(newWagon);
        CurrentProjectViewModel.Wagons.Add(wagonVM);
        SelectedWagon = wagonVM;

        HasUnsavedChanges = true;
    }

    [RelayCommand]
    private void AddGoodsWagon()
    {
        if (CurrentProjectViewModel == null) return;

        var newWagon = new GoodsWagon
        {
            Name = "New Goods Wagon",
            Cargo = CargoType.General
        };

        CurrentProjectViewModel.Model.GoodsWagons.Add(newWagon);

        var wagonVM = new GoodsWagonViewModel(newWagon);
        CurrentProjectViewModel.Wagons.Add(wagonVM);
        SelectedWagon = wagonVM;

        HasUnsavedChanges = true;
    }

    [RelayCommand(CanExecute = nameof(CanDeleteWagon))]
    private void DeleteWagon()
    {
        if (SelectedWagon == null || CurrentProjectViewModel == null) return;

        // Remove from appropriate model collection
        if (SelectedWagon.Model is PassengerWagon pw)
            CurrentProjectViewModel.Model.PassengerWagons.Remove(pw);
        else if (SelectedWagon.Model is GoodsWagon gw)
            CurrentProjectViewModel.Model.GoodsWagons.Remove(gw);

        CurrentProjectViewModel.Wagons.Remove(SelectedWagon);
        SelectedWagon = null;

        HasUnsavedChanges = true;
    }

    private bool CanDeleteWagon() => SelectedWagon != null;

    #endregion

    #region Workflow Actions Commands

    [RelayCommand]
    private void AddAnnouncement()
    {
        if (SelectedWorkflow == null) return;

        var newAction = new Domain.WorkflowAction
        {
            Name = "New Announcement",
            Number = (uint)(SelectedWorkflow.Model.Actions.Count + 1),
            Type = Domain.Enum.ActionType.Announcement,
            Parameters = new Dictionary<string, object>
            {
                ["Message"] = "Enter announcement text",
                ["VoiceName"] = "de-DE-KatjaNeural"
            }
        };

        SelectedWorkflow.Model.Actions.Add(newAction);
        var viewModel = new Action.AnnouncementViewModel(newAction);
        SelectedWorkflow.Actions.Add(viewModel);

        HasUnsavedChanges = true;
    }

    [RelayCommand]
    private void AddCommand()
    {
        if (SelectedWorkflow == null) return;

        var newAction = new Domain.WorkflowAction
        {
            Name = "New Command",
            Number = (uint)(SelectedWorkflow.Model.Actions.Count + 1),
            Type = Domain.Enum.ActionType.Command,
            Parameters = new Dictionary<string, object>
            {
                ["Bytes"] = new byte[] { 0x00 }
            }
        };

        SelectedWorkflow.Model.Actions.Add(newAction);
        var viewModel = new Action.CommandViewModel(newAction);
        SelectedWorkflow.Actions.Add(viewModel);

        HasUnsavedChanges = true;
    }

    [RelayCommand]
    private void AddAudio()
    {
        if (SelectedWorkflow == null) return;

        var newAction = new Domain.WorkflowAction
        {
            Name = "New Audio",
            Number = (uint)(SelectedWorkflow.Model.Actions.Count + 1),
            Type = Domain.Enum.ActionType.Audio,
            Parameters = new Dictionary<string, object>
            {
                ["FilePath"] = "sound.wav"
            }
        };

        SelectedWorkflow.Model.Actions.Add(newAction);
        var viewModel = new Action.AudioViewModel(newAction);
        SelectedWorkflow.Actions.Add(viewModel);

        HasUnsavedChanges = true;
    }

    #endregion

    #region Train Composition Commands

    [RelayCommand]
    private void AddLocomotiveToComposition()
    {
        if (SelectedTrain == null) return;

        // TODO: Enhance to select from global library
        var newLocomotive = new Locomotive
        {
            Name = "New Locomotive",
            DigitalAddress = 3
        };

        SelectedTrain.Model.Locomotives.Add(newLocomotive);
        var locomotiveVM = new LocomotiveViewModel(newLocomotive);
        SelectedTrain.Locomotives.Add(locomotiveVM);

        HasUnsavedChanges = true;
    }

    [RelayCommand]
    private void AddWagonToComposition()
    {
        if (SelectedTrain == null) return;

        // TODO: Enhance to select from global library
        var newWagon = new PassengerWagon
        {
            Name = "New Passenger Wagon",
            WagonClass = PassengerClass.Second
        };

        SelectedTrain.Model.Wagons.Add(newWagon);
        var wagonVM = new PassengerWagonViewModel(newWagon);
        SelectedTrain.Wagons.Add(wagonVM);

        HasUnsavedChanges = true;
    }

    #endregion

    #region City Library

    private readonly ICityLibraryService? _cityLibraryService;

    [ObservableProperty]
    private ObservableCollection<Domain.City> cityLibrary = [];

    [ObservableProperty]
    private string citySearchText = string.Empty;

    partial void OnCitySearchTextChanged(string value)
    {
        if (_cityLibraryService == null) return;

        var filtered = _cityLibraryService.FilterCities(value);
        CityLibrary = new ObservableCollection<Domain.City>(filtered);
    }

    [RelayCommand(CanExecute = nameof(CanAddStationFromCity))]
    private void AddStationFromCity(Domain.City city)
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

        SelectedJourney.Model.Stations.Add(newStation);

        var stationVM = new StationViewModel(newStation);
        SelectedJourney.Stations.Add(stationVM);
        SelectedStation = stationVM;

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
        CityLibrary = new ObservableCollection<Domain.City>(cities);
    }

    #endregion

    #region Settings

    /// <summary>
    /// Application settings - exposed for direct binding.
    /// Settings are stored in appsettings.json (not in Solution).
    /// </summary>
    public AppSettings Settings => _settings;

    // Wrapper properties for Settings page bindings
    public string Z21IpAddress
    {
        get => _settings.Z21.CurrentIpAddress;
        set
        {
            if (_settings.Z21.CurrentIpAddress != value)
            {
                _settings.Z21.CurrentIpAddress = value;
                OnPropertyChanged();
            }
        }
    }

    public string Z21Port
    {
        get => _settings.Z21.DefaultPort;
        set
        {
            if (_settings.Z21.DefaultPort != value)
            {
                _settings.Z21.DefaultPort = value;
                OnPropertyChanged();
            }
        }
    }

    public string CityLibraryPath
    {
        get => _settings.CityLibrary.FilePath;
        set
        {
            if (_settings.CityLibrary.FilePath != value)
            {
                _settings.CityLibrary.FilePath = value;
                OnPropertyChanged();
            }
        }
    }

    public bool CityLibraryAutoReload
    {
        get => _settings.CityLibrary.AutoReload;
        set
        {
            if (_settings.CityLibrary.AutoReload != value)
            {
                _settings.CityLibrary.AutoReload = value;
                OnPropertyChanged();
            }
        }
    }

    public string? SpeechKey
    {
        get => _settings.Speech.Key;
        set
        {
            if (_settings.Speech.Key != value)
            {
                _settings.Speech.Key = value ?? string.Empty;
                OnPropertyChanged();
            }
        }
    }

    public string SpeechRegion
    {
        get => _settings.Speech.Region;
        set
        {
            if (_settings.Speech.Region != value)
            {
                _settings.Speech.Region = value;
                OnPropertyChanged();
            }
        }
    }

    public int SpeechRate
    {
        get => _settings.Speech.Rate;
        set
        {
            if (_settings.Speech.Rate != value)
            {
                _settings.Speech.Rate = value;
                OnPropertyChanged();
            }
        }
    }

    public double SpeechVolume
    {
        get => _settings.Speech.Volume;
        set
        {
            if ((uint)value != _settings.Speech.Volume)
            {
                _settings.Speech.Volume = (uint)value;
                OnPropertyChanged();
            }
        }
    }

    public bool ResetWindowLayoutOnStart
    {
        get => _settings.Application.ResetWindowLayoutOnStart;
        set
        {
            if (_settings.Application.ResetWindowLayoutOnStart != value)
            {
                _settings.Application.ResetWindowLayoutOnStart = value;
                OnPropertyChanged();
            }
        }
    }

    public bool AutoLoadLastSolution
    {
        get => _settings.Application.AutoLoadLastSolution;
        set
        {
            if (_settings.Application.AutoLoadLastSolution != value)
            {
                _settings.Application.AutoLoadLastSolution = value;
                OnPropertyChanged();
            }
        }
    }

    public bool HealthCheckEnabled
    {
        get => _settings.HealthCheck.Enabled;
        set
        {
            if (_settings.HealthCheck.Enabled != value)
            {
                _settings.HealthCheck.Enabled = value;
                OnPropertyChanged();
            }
        }
    }

    public double HealthCheckIntervalSeconds
    {
        get => _settings.HealthCheck.IntervalSeconds;
        set
        {
            if (_settings.HealthCheck.IntervalSeconds != (int)value)
            {
                _settings.HealthCheck.IntervalSeconds = (int)value;
                OnPropertyChanged();
            }
        }
    }

    [ObservableProperty]
    private bool _showSuccessMessage;

    [ObservableProperty]
    private bool _showErrorMessage;

    [ObservableProperty]
    private string? _errorMessage;

    [RelayCommand]
    private async Task SaveSettingsAsync()
    {
        if (_settingsService == null) return;

        try
        {
            ShowErrorMessage = false;
            await _settingsService.SaveSettingsAsync(_settings);

            ShowSuccessMessage = true;

            // Auto-hide success message after 3 seconds
            await Task.Delay(3000);
            ShowSuccessMessage = false;
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            ShowErrorMessage = true;
        }
    }

    [RelayCommand]
    private async Task ResetToDefaultsAsync()
    {
        if (_settingsService == null) return;

        try
        {
            ShowErrorMessage = false;
            await _settingsService.ResetToDefaultsAsync();

            // Notify all settings properties changed
            OnPropertyChanged(nameof(Z21IpAddress));
            OnPropertyChanged(nameof(Z21Port));
            OnPropertyChanged(nameof(CityLibraryPath));
            OnPropertyChanged(nameof(CityLibraryAutoReload));
            OnPropertyChanged(nameof(SpeechKey));
            OnPropertyChanged(nameof(SpeechRegion));
            OnPropertyChanged(nameof(SpeechRate));
            OnPropertyChanged(nameof(SpeechVolume));
            OnPropertyChanged(nameof(ResetWindowLayoutOnStart));
            OnPropertyChanged(nameof(AutoLoadLastSolution));
            OnPropertyChanged(nameof(HealthCheckEnabled));
            OnPropertyChanged(nameof(HealthCheckIntervalSeconds));

            ShowSuccessMessage = true;
            await Task.Delay(3000);
            ShowSuccessMessage = false;
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            ShowErrorMessage = true;
        }
    }

    #endregion
}