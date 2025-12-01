// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using Moba.Backend.Manager;
using Moba.Backend.Interface;
using Moba.Common.Configuration;
using Moba.Common.Extensions;
using Moba.Domain;
using Moba.SharedUI.Service;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

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
    private JourneyManager? _journeyManager;

    // Primary ctor for DI
    public MainWindowViewModel(
        IIoService ioService,
        IZ21 z21,
        IJourneyManagerFactory journeyManagerFactory,
        IUiDispatcher uiDispatcher,
        AppSettings settings,
        Solution solution)
    {
        _ioService = ioService;
        _z21 = z21;
        _journeyManagerFactory = journeyManagerFactory;
        _uiDispatcher = uiDispatcher;
        _settings = settings;

        // Subscribe to Z21 events
        _z21.OnSystemStateChanged += OnZ21SystemStateChanged;

        // Use injected Solution from DI container
        Solution = solution;
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
                FeedbackInPort = stationToCopy.FeedbackInPort
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
}
