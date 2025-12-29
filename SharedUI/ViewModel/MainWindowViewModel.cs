// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using Backend.Interface;
using Backend.Manager;
using Backend.Service;

using Common.Configuration;
using Common.Extensions;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Domain;
using Domain.Enum;

using Interface;
using Service;  // For NullIoService

using Sound;

using System.Collections.ObjectModel;
using System.Diagnostics;

/// <summary>
/// Core ViewModel for main window functionality.
/// Partial classes handle: Selection, Solution, Journey, Workflow, Train, Z21, Settings.
/// </summary>
public partial class MainWindowViewModel : ObservableObject
{
    #region Fields
    // Core Services (required)
    private readonly IIoService _ioService;
    private readonly IZ21 _z21;
    private readonly IUiDispatcher _uiDispatcher;
    private readonly WorkflowService _workflowService;

    // Configuration
    private readonly AppSettings _settings;

    // Optional Services
    private readonly ICityService? _cityLibraryService;
    private readonly ISettingsService? _settingsService;
    private readonly AnnouncementService? _announcementService;

    // Execution Context (contains all action execution dependencies)
    private readonly ActionExecutionContext _executionContext;

    // Runtime State
    private JourneyManager? _journeyManager;
    private Timer? _z21AutoConnectTimer;
    
    // Event handlers for model change tracking
    private JourneyViewModel? _currentJourneyWithModelChangedSubscription;
    private EventHandler? _journeyModelChangedHandler;
    #endregion

    #region Constructor
    public MainWindowViewModel(
        IZ21 z21,
        WorkflowService workflowService,
        IUiDispatcher uiDispatcher,
        AppSettings settings,
        Solution solution,
        ActionExecutionContext executionContext,
        IIoService? ioService = null,  // ✅ Optional for WebApp/MAUI
        ICityService? cityLibraryService = null,
        ISettingsService? settingsService = null,
        AnnouncementService? announcementService = null)
    {
        _ioService = ioService ?? new NullIoService();  // Use null object pattern
        _z21 = z21;
        _workflowService = workflowService;
        _uiDispatcher = uiDispatcher;
        _settings = settings;
        _cityLibraryService = cityLibraryService;
        _settingsService = settingsService;
        _announcementService = announcementService;
        _executionContext = executionContext;

        // Subscribe to Solution changes
        Solution = solution;

        // ✅ Initialize Counter settings from AppSettings.Counter
        GlobalTargetLapCount = settings.Counter.TargetLapCount;
        UseTimerFilter = settings.Counter.UseTimerFilter;
        TimerIntervalSeconds = settings.Counter.TimerIntervalSeconds;

        // ✅ Subscribe to Z21 events immediately (like CounterViewModel does)
        // This ensures we receive status updates regardless of how connection was established
        _z21.Received += OnFeedbackReceived;  // For counter statistics
        _z21.OnSystemStateChanged += OnZ21SystemStateChanged;
        _z21.OnXBusStatusChanged += OnZ21XBusStatusChanged;
        _z21.OnVersionInfoChanged += OnZ21VersionInfoChanged;
        _z21.OnConnectionLost += HandleConnectionLost;
        _z21.OnConnectedChanged += OnZ21ConnectedChanged;

        // ✅ Subscribe to Traffic Monitor immediately (before connection)
        InitializeTrafficMonitor();
        
        // ✅ Initialize Track Statistics with defaults (will be updated when project loads)
        InitializeStatisticsFromFeedbackPoints();
        
        // ✅ Auto-connect to Z21 at startup (fire-and-forget, non-blocking)
        // Connection status will be updated via OnConnectedChanged event when Z21 responds
        _ = TryAutoConnectToZ21Async();

        // Load City Library at startup (fire-and-forget)
        if (_cityLibraryService != null)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await LoadCityLibraryAsync();
                    Debug.WriteLine($"✅ City Library loaded: {CityLibrary.Count} cities");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"❌ Failed to load City Library: {ex.Message}");
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
    private bool isDarkMode = true;  // Dark theme is default for WinUI

    /// <summary>
    /// Indicates whether the current solution has unsaved changes.
    /// </summary>
    [ObservableProperty]
    private bool hasUnsavedChanges;

    /// <summary>
    /// Indicates whether a solution with projects is currently loaded.
    /// </summary>
    [ObservableProperty]
    private bool hasSolution;

    /// <summary>
    /// Health status message for Speech Service (Azure).
    /// Updated by HealthCheckService via event.
    /// </summary>
    [ObservableProperty]
    private string speechHealthStatus = "Initializing...";

    /// <summary>
    /// Icon glyph for Speech Service health status.
    /// </summary>
    [ObservableProperty]
    private string speechHealthIcon = "\uE946"; // Sync

    /// <summary>
    /// Color for Speech Service health status icon.
    /// </summary>
    [ObservableProperty]
    private string speechHealthColor = "SystemFillColorCautionBrush";

    [ObservableProperty]
    private SolutionViewModel? solutionViewModel;

    [ObservableProperty]
    private ProjectViewModel? selectedProject;

    [ObservableProperty]
    private JourneyViewModel? selectedJourney;

    /// <summary>
    /// Called when SelectedJourney changes. Notifies ResetJourneyCommand to update its CanExecute state.
    /// </summary>
    partial void OnSelectedJourneyChanged(JourneyViewModel? value)
    {
        _ = value; // Suppress unused parameter warning
        ResetJourneyCommand?.NotifyCanExecuteChanged();
    }

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

    /// <summary>
    /// Error message to display in UI (Settings page).
    /// </summary>
    [ObservableProperty]
    private string errorMessage = string.Empty;

    /// <summary>
    /// Controls visibility of error message in UI.
    /// </summary>
    [ObservableProperty]
    private bool showErrorMessage;

    /// <summary>
    /// Controls visibility of success message in UI.
    /// </summary>
    [ObservableProperty]
    private bool showSuccessMessage;

    /// <summary>
    /// Selected theme index for UI theme picker (0=Light, 1=Dark, 2=System).
    /// </summary>
    [ObservableProperty]
    private int selectedThemeIndex = 1; // Default: Dark

    /// <summary>
    /// The currently selected object for SolutionPage properties panel.
    /// Displays: SelectedProject
    /// </summary>
    [ObservableProperty]
    private object? solutionPageSelectedObject;

    /// <summary>
    /// The currently selected object for JourneysPage properties panel.
    /// Displays: SelectedJourney, SelectedStation
    /// </summary>
    [ObservableProperty]
    private object? journeysPageSelectedObject;

    /// <summary>
    /// The currently selected object for WorkflowsPage properties panel.
    /// Displays: SelectedWorkflow, SelectedAction
    /// </summary>
    [ObservableProperty]
    private object? workflowsPageSelectedObject;

    [ObservableProperty]
    private bool isConnected;

    [ObservableProperty]
    private bool isTrackPowerOn;

    [ObservableProperty]
    private string statusText = "Disconnected";

    [ObservableProperty]
    private string serialNumber = "-";

    [ObservableProperty]
    private string firmwareVersion = "-";

    [ObservableProperty]
    private string hardwareType = "-";

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

    public event EventHandler? ExitApplicationRequested;


    /// <summary>
    /// Handles changes to the selected object on JourneysPage.
    /// Subscribes to ModelChanged events of journeys to track unsaved changes.
    /// </summary>
    partial void OnJourneysPageSelectedObjectChanged(object? value)
    {
        // Unsubscribe from previous journey's ModelChanged event
        if (_currentJourneyWithModelChangedSubscription != null && _journeyModelChangedHandler != null)
        {
            _currentJourneyWithModelChangedSubscription.ModelChanged -= _journeyModelChangedHandler;
            _currentJourneyWithModelChangedSubscription = null;
        }

        // Subscribe to new journey's ModelChanged event if selected object is a journey
        if (value is JourneyViewModel journeyVM)
        {
            _currentJourneyWithModelChangedSubscription = journeyVM;
            
            // Create handler that marks solution as having unsaved changes
            _journeyModelChangedHandler = (_, _) =>
            {
                HasUnsavedChanges = true;  // Mark as unsaved, but don't auto-save
            };
            
            journeyVM.ModelChanged += _journeyModelChangedHandler;
        }
    }
    #endregion

    #region Project Management
    [RelayCommand]
    private void AddProject()
    {
        // Solution is always available (DI singleton), so is SolutionViewModel
        var project = new Project { Name = "New Project" };
        Solution.Projects.Add(project);

        // Create ViewModel and add to SolutionViewModel
        var projectVM = new ProjectViewModel(project);
        SolutionViewModel!.Projects.Add(projectVM);

        // Select the newly created project
        SelectedProject = projectVM;

        // Update HasSolution flag
        HasSolution = true;

        SaveSolutionCommand.NotifyCanExecuteChanged();
        DeleteProjectCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand(CanExecute = nameof(CanDeleteProject))]
    private void DeleteProject()
    {
        if (SelectedProject == null) return;

        // Remove from Domain model
        Solution.Projects.Remove(SelectedProject.Model);

        // Remove from SolutionViewModel's Projects collection
        SolutionViewModel!.Projects.Remove(SelectedProject);

        // Select first project if available, otherwise clear
        SelectedProject = SolutionViewModel.Projects.FirstOrDefault();
        if (SelectedProject == null)
        {
            HasSolution = false;
        }

        SaveSolutionCommand.NotifyCanExecuteChanged();
        DeleteProjectCommand.NotifyCanExecuteChanged();
    }

    private bool CanDeleteProject() => SelectedProject != null;
    #endregion

    #region Lifecycle
    public void OnWindowClosing()
    {
        // Auto-save solution if there are unsaved changes
        if (HasUnsavedChanges && SaveSolutionCommand.CanExecute(null))
        {
            Debug.WriteLine("💾 Auto-saving solution on window close...");
            _ = SaveSolutionCommand.ExecuteAsync(null);
        }

        // Auto-save settings (Z21 IP, Counter settings, etc.)
        if (_settingsService != null && SaveSettingsCommand.CanExecute(null))
        {
            Debug.WriteLine("💾 Auto-saving settings on window close...");
            _ = SaveSettingsCommand.ExecuteAsync(null);
        }

        // Stop Z21 auto-connect retry timer
        if (_z21AutoConnectTimer != null)
        {
            try
            {
                _z21AutoConnectTimer.Dispose();
                _z21AutoConnectTimer = null;
            }
            catch (Exception ex)
            {
                this.LogError("Error disposing Z21 auto-connect timer", ex);
            }
        }

        // Dispose JourneyManager if initialized
        if (_journeyManager != null)
        {
            try
            {
                _journeyManager.Dispose();
                _journeyManager = null;
            }
            catch (Exception ex)
            {
                this.LogError("Error disposing JourneyManager", ex);
            }
        }

        // ✅ CRITICAL: Always send LAN_LOGOFF to Z21 on app exit
        // This prevents "zombie clients" on Z21 which can cause it to become unresponsive
        // Note: Fire-and-forget is acceptable here since we're exiting anyway,
        // but we give it a brief moment to complete
        try
        {
            var disconnectTask = _z21.DisconnectAsync();
            // Wait briefly (100ms) to allow LAN_LOGOFF to be sent
            // Don't block indefinitely as app needs to exit
            disconnectTask.Wait(TimeSpan.FromMilliseconds(100));
        }
        catch (TaskCanceledException) { /* Expected during shutdown */ }
        catch (OperationCanceledException) { /* Expected during shutdown */ }
        catch (AggregateException) { /* Task.Wait throws AggregateException */ }
        catch (Exception ex)
        {
            this.LogError("Error during Z21 disconnect", ex);
        }

        // Unsubscribe from all Z21 events
        _z21.OnSystemStateChanged -= OnZ21SystemStateChanged;
        _z21.OnVersionInfoChanged -= OnZ21VersionInfoChanged;
        _z21.OnConnectionLost -= HandleConnectionLost;
        _z21.OnConnectedChanged -= OnZ21ConnectedChanged;
        
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
        if (SelectedJourney == null) return;

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

        Debug.WriteLine($"✅ Added station from city: {city.Name} → {cityStation.Name}");
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

        Debug.WriteLine($"✅ City Library loaded: {CityLibrary.Count} cities");
    }
    #endregion

    #region Wagon Libraries
    [ObservableProperty]
    private ObservableCollection<GoodsWagon> goodsWagonLibrary =
    [
        new GoodsWagon { Name = "Goods Wagon 1", Cargo = CargoType.Container },
        new GoodsWagon { Name = "Goods Wagon 2", Cargo = CargoType.Coal },
        new GoodsWagon { Name = "Goods Wagon 3", Cargo = CargoType.Wood }
    ];

    [ObservableProperty]
    private ObservableCollection<PassengerWagon> passengerWagonLibrary =
    [
        new PassengerWagon { Name = "Passenger Wagon 1st Class", WagonClass = PassengerClass.First },
        new PassengerWagon { Name = "Passenger Wagon 2nd Class", WagonClass = PassengerClass.Second }
    ];
    #endregion

    #region Drag & Drop Commands
    [RelayCommand(CanExecute = nameof(CanAssignWorkflowToStation))]
    private void AssignWorkflowToStation(WorkflowViewModel? workflow)
    {
        if (SelectedStation == null || workflow == null) return;

        SelectedStation.WorkflowId = workflow.Model.Id;

        Debug.WriteLine($"✅ Assigned workflow '{workflow.Name}' to station '{SelectedStation.Name}'");
    }

    private bool CanAssignWorkflowToStation() => SelectedStation != null;

    [RelayCommand(CanExecute = nameof(CanAddLocomotiveToTrain))]
    private void AddLocomotiveToTrain(LocomotiveViewModel? locomotiveVM)
    {
        if (SelectedTrain == null || locomotiveVM == null || SelectedProject == null) return;

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
        SelectedProject.Model.Locomotives.Add(locomotiveCopy);

        // Add ID to Train
        SelectedTrain.Model.LocomotiveIds.Add(locomotiveCopy.Id);

        // Refresh Train collections
        SelectedTrain.RefreshCollections();

        Debug.WriteLine($"✅ Added locomotive '{locomotiveCopy.Name}' to train '{SelectedTrain.Name}'");
    }

    private bool CanAddLocomotiveToTrain() => SelectedTrain != null && SelectedProject != null;
    #endregion
}
