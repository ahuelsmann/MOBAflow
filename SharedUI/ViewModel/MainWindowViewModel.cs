// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using Backend;
using Backend.Interface;
using Backend.Manager;
using Backend.Model;
using Backend.Protocol;
using Backend.Service;

using Common.Configuration;
using Common.Events;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Domain;
using Domain.Enum;

using Interface;

using Microsoft.Extensions.Logging;

using Service;

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;

// For NullIoService

/// <summary>
/// Core ViewModel for main window functionality.
/// Partial classes handle: Selection, Solution, Journey, Workflow, Train, Z21, Settings.
/// </summary>
public sealed partial class MainWindowViewModel : ObservableObject
{
    #region Fields
    // Core Services (required)
    private readonly IIoService _ioService;
    private readonly IZ21 _z21;
    private readonly IEventBus _eventBus;
    private readonly IUiDispatcher _uiDispatcher;
    private readonly IWorkflowService _workflowService;
    private readonly ILogger<MainWindowViewModel> _logger;

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
    #endregion

    #region Constructor
    public MainWindowViewModel(
        IZ21 z21,
        IEventBus eventBus,
        IWorkflowService workflowService,
        IUiDispatcher uiDispatcher,
        AppSettings settings,
        Solution solution,
        ActionExecutionContext executionContext,
        ILogger<MainWindowViewModel> logger,
        IIoService? ioService = null,  // Optional for WebApp/MAUI
        ICityService? cityLibraryService = null,
        ISettingsService? settingsService = null,
        AnnouncementService? announcementService = null,
        object? photoHubClient = null)  // Optional PhotoHubClient (only in WinUI, type is object to avoid assembly reference)
    {
        ArgumentNullException.ThrowIfNull(z21);
        ArgumentNullException.ThrowIfNull(eventBus);
        ArgumentNullException.ThrowIfNull(workflowService);
        ArgumentNullException.ThrowIfNull(uiDispatcher);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(solution);
        ArgumentNullException.ThrowIfNull(executionContext);
        ArgumentNullException.ThrowIfNull(logger);

        _ioService = ioService ?? new NullIoService();  // Use null object pattern
        _z21 = z21;
        _eventBus = eventBus;
        _workflowService = workflowService;
        _uiDispatcher = uiDispatcher;
        _settings = settings;
        _logger = logger;
        _cityLibraryService = cityLibraryService;
        _settingsService = settingsService;
        _announcementService = announcementService;
        _executionContext = executionContext;
        _ = photoHubClient;

        Solution = solution;

        GlobalTargetLapCount = settings.Counter.TargetLapCount;
        UseTimerFilter = settings.Counter.UseTimerFilter;
        TimerIntervalSeconds = settings.Counter.TimerIntervalSeconds;

        IsDarkMode = settings.Application.IsDarkMode;

        // Subscribe to Z21 events via EventBus (with automatic WeakReference cleanup)
        _eventBus.Subscribe<Z21ConnectionEstablishedEvent>(_ => OnZ21ConnectedChanged(true));
        _eventBus.Subscribe<Z21ConnectionLostEvent>(_ => OnZ21ConnectedChanged(false));
        _eventBus.Subscribe<SystemStateChangedEvent>(evt => OnZ21SystemStateChanged(CreateSystemState(evt)));
        _eventBus.Subscribe<XBusStatusChangedEvent>(evt => OnZ21XBusStatusChanged(CreateXBusStatus(evt)));
        _eventBus.Subscribe<VersionInfoChangedEvent>(evt => OnZ21VersionInfoChanged(CreateVersionInfo(evt)));

        // Keep legacy direct subscriptions for backward compatibility (optional)
        // CRITICAL: Wrap with UI thread marshalling to prevent WinUI SetValue COMException
        _z21.Received += (packet) => _uiDispatcher.InvokeOnUi(() => OnFeedbackReceived(packet));  // For counter statistics
        _z21.OnConnectionLost += () => _uiDispatcher.InvokeOnUi(() => HandleConnectionLost());

        InitializeTrafficMonitor();

        InitializeStatisticsFromFeedbackPoints();

        // Connection status will be updated via EventBus when Z21 responds
        _ = TryAutoConnectToZ21Async();

        if (_cityLibraryService != null)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await LoadCityLibraryAsync().ConfigureAwait(false);
                    Debug.WriteLine($"[OK] City Library loaded: {CityLibrary.Count} cities");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[ERROR] Failed to load City Library: {ex.Message}");
                }
            });
        }
    }
    #endregion

    #region Properties
    [ObservableProperty]
    private Solution _solution;

    [ObservableProperty]
    private string? _currentSolutionPath;

    [ObservableProperty]
    private bool _isDarkMode = true;  // Dark theme is default for WinUI

    /// <summary>
    /// Called when IsDarkMode changes. Persists to AppSettings.
    /// </summary>
    partial void OnIsDarkModeChanged(bool value)
    {
        _settings.Application.IsDarkMode = value;

        // Auto-save settings when theme changes
        if (_settingsService != null)
        {
            _ = _settingsService.SaveSettingsAsync(_settings);
        }
    }

    /// <summary>
    /// Indicates whether a solution with projects is currently loaded.
    /// </summary>
    [ObservableProperty]
    private bool _hasSolution;

    /// <summary>
    /// Health status message for Speech Service (Azure).
    /// Updated by HealthCheckService via event.
    /// </summary>
    [ObservableProperty]
    private string _speechHealthStatus = "Initializing...";

    /// <summary>
    /// Icon glyph for Speech Service health status.
    /// </summary>
    [ObservableProperty]
    private string _speechHealthIcon = "\uE946"; // Sync

    /// <summary>
    /// Color for Speech Service health status icon.
    /// </summary>
    [ObservableProperty]
    private string _speechHealthColor = "SystemFillColorCautionBrush";

    [ObservableProperty]
    private SolutionViewModel? _solutionViewModel;

    [ObservableProperty]
    private ProjectViewModel? _selectedProject;

    [ObservableProperty]
    private JourneyViewModel? _selectedJourney;

    /// <summary>
    /// Called when SelectedJourney changes. Subscribes to PropertyChanged for auto-save.
    /// </summary>
    partial void OnSelectedJourneyChanged(JourneyViewModel? value)
    {
        if (value != null)
        {
            value.PropertyChanged += OnViewModelPropertyChanged;
        }
        ResetJourneyCommand.NotifyCanExecuteChanged();
    }

    [ObservableProperty]
    private StationViewModel? _selectedStation;

    /// <summary>
    /// Called when SelectedStation changes. Subscribes to PropertyChanged for auto-save.
    /// </summary>
    partial void OnSelectedStationChanged(StationViewModel? value)
    {
        if (value != null)
        {
            value.PropertyChanged += OnViewModelPropertyChanged;
        }
    }

    [ObservableProperty]
    private WorkflowViewModel? _selectedWorkflow;

    [ObservableProperty]
    private object? _selectedAction;

    [ObservableProperty]
    private TrainViewModel? _selectedTrain;

    /// <summary>
    /// Called when SelectedTrain changes. Subscribes to PropertyChanged for auto-save.
    /// </summary>
    partial void OnSelectedTrainChanged(TrainViewModel? value)
    {
        if (value != null)
        {
            value.PropertyChanged += OnViewModelPropertyChanged;
        }
    }

    /// <summary>
    /// Generic handler for ViewModel PropertyChanged events.
    /// Triggers auto-save for any model property change.
    /// </summary>
    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        // Ignore certain properties that don't affect the model
        var ignoredProperties = new[] { "IsSelected", "IsExpanded", "IsHighlighted" };

        if (!ignoredProperties.Contains(e.PropertyName))
        {
            _ = SaveSolutionInternalAsync();
        }
    }

    /// <summary>
    /// The currently selected object to display in the properties panel.
    /// </summary>
    [ObservableProperty]
    private object? _currentSelectedObject;

    /// <summary>
    /// Error message to display in UI (Settings page).
    /// </summary>
    [ObservableProperty]
    private string _errorMessage = string.Empty;

    /// <summary>
    /// Controls visibility of error message in UI.
    /// </summary>
    [ObservableProperty]
    private bool _showErrorMessage;

    /// <summary>
    /// Controls visibility of success message in UI.
    /// </summary>
    [ObservableProperty]
    private bool _showSuccessMessage;

    /// <summary>
    /// Selected theme index for UI theme picker (0=Light, 1=Dark, 2=System).
    /// </summary>
    [ObservableProperty]
    private int _selectedThemeIndex = 1; // Default: Dark

    /// <summary>
    /// The currently selected object for SolutionPage properties panel.
    /// Displays: SelectedProject
    /// </summary>
    [ObservableProperty]
    private object? _solutionPageSelectedObject;

    /// <summary>
    /// The currently selected object for JourneysPage properties panel.
    /// Displays: SelectedJourney, SelectedStation
    /// </summary>
    [ObservableProperty]
    private object? _journeysPageSelectedObject;

    /// <summary>
    /// The currently selected object for WorkflowsPage properties panel.
    /// Displays: SelectedWorkflow, SelectedAction
    /// </summary>
    [ObservableProperty]
    private object? _workflowsPageSelectedObject;

    [ObservableProperty]
    private bool _isConnected;

    [ObservableProperty]
    private bool _isTrackPowerOn;

    [ObservableProperty]
    private string _statusText = "Disconnected";

    [ObservableProperty]
    private string _serialNumber = "-";

    [ObservableProperty]
    private string _firmwareVersion = "-";

    [ObservableProperty]
    private string _hardwareType = "-";

    [ObservableProperty]
    private string _simulateInPort = "1";

    /// <summary>
    /// Available cities with stations (loaded from master data).
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<City> _availableCities = [];

    /// <summary>
    /// Currently selected city for adding stations to journeys.
    /// </summary>
    [ObservableProperty]
    private City? _selectedCity;

    public event EventHandler? ExitApplicationRequested;

    /// <summary>
    /// Raised when navigation to a page is requested (e.g., from plugins).
    /// The string parameter is the navigation tag (e.g., "traincontrol", "journeys").
    /// </summary>
    public event EventHandler<string>? NavigationRequested;

    /// <summary>
    /// Requests navigation to the specified page tag.
    /// Used by plugins to trigger navigation without direct access to NavigationService.
    /// </summary>
    public void RequestNavigation(string tag)
    {
        NavigationRequested?.Invoke(this, tag);
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
        var projectVm = new ProjectViewModel(project);
        SolutionViewModel!.Projects.Add(projectVm);

        // Select the newly created project
        SelectedProject = projectVm;

        // Update HasSolution flag
        HasSolution = true;

        SaveSolutionCommand.NotifyCanExecuteChanged();
        DeleteProjectCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand(CanExecute = nameof(CanDeleteProject))]
    private void DeleteProject()
    {
        if (SelectedProject == null) return;

        // Store reference to deleted project
        var deletedProject = SelectedProject;

        // Remove from Domain model
        Solution.Projects.Remove(deletedProject.Model);

        // Remove from SolutionViewModel's Projects collection
        SolutionViewModel!.Projects.Remove(deletedProject);

        // Clear all detail selections after removing project
        // This prevents showing stale data from the deleted project
        ClearAllSelections();

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
        // Settings and Solution changes are now auto-saved immediately via PropertyChanged subscriptions
        // No need for conditional save on window close

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
                _logger.LogError(ex, "Error disposing Z21 auto-connect timer");
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
                _logger.LogError(ex, "Error disposing JourneyManager");
            }
        }

        // CRITICAL: Always send LAN_LOGOFF to Z21 on app exit
        // This prevents zombie clients on Z21 which can cause it to become unresponsive.
        // Async-first: trigger disconnect without synchronously blocking the UI thread.
        try
        {
            _ = _z21.DisconnectAsync();
        }
        catch (TaskCanceledException) { /* Expected during shutdown */ }
        catch (OperationCanceledException) { /* Expected during shutdown */ }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Z21 disconnect");
        }

        // Unsubscribe from all Z21 events
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
    private ObservableCollection<City> _cityLibrary = [];

    [ObservableProperty]
    private string _citySearchText = string.Empty;

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
        var stationVm = SelectedJourney.Stations.LastOrDefault();
        if (stationVm != null)
        {
            SelectedStation = stationVm;
        }

        Debug.WriteLine($"[OK] Added station from city: {city.Name} -> {cityStation.Name}");
    }

    private bool CanAddStationFromCity() => SelectedJourney != null;

    /// <summary>
    /// Loads the city library from the city library service.
    /// Call this during initialization.
    /// </summary>
    public async Task LoadCityLibraryAsync()
    {
        if (_cityLibraryService == null) return;

        var cities = await _cityLibraryService.LoadCitiesAsync().ConfigureAwait(false);
        CityLibrary = new ObservableCollection<City>(cities);

        Debug.WriteLine($"[OK] City Library loaded: {CityLibrary.Count} cities");
    }
    #endregion

    #region Wagon Libraries
    [ObservableProperty]
    private ObservableCollection<GoodsWagon> _goodsWagonLibrary =
    [
        new GoodsWagon { Name = "Goods Wagon 1", Cargo = CargoType.Container },
        new GoodsWagon { Name = "Goods Wagon 2", Cargo = CargoType.Coal },
        new GoodsWagon { Name = "Goods Wagon 3", Cargo = CargoType.Wood }
    ];

    [ObservableProperty]
    private ObservableCollection<PassengerWagon> _passengerWagonLibrary =
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

        Debug.WriteLine($"[OK] Assigned workflow '{workflow.Name}' to station '{SelectedStation.Name}'");
    }

    private bool CanAssignWorkflowToStation() => SelectedStation != null;

    [RelayCommand(CanExecute = nameof(CanAddLocomotiveToTrain))]
    private void AddLocomotiveToTrain(LocomotiveViewModel? locomotiveVm)
    {
        if (SelectedTrain == null || locomotiveVm == null || SelectedProject == null) return;

        // Create a copy to avoid modifying the library object
        var locomotiveCopy = new Locomotive
        {
            Name = locomotiveVm.Model.Name,
            DigitalAddress = locomotiveVm.Model.DigitalAddress,
            Manufacturer = locomotiveVm.Model.Manufacturer,
            ArticleNumber = locomotiveVm.Model.ArticleNumber,
            Series = locomotiveVm.Model.Series,
            ColorPrimary = locomotiveVm.Model.ColorPrimary,
            ColorSecondary = locomotiveVm.Model.ColorSecondary,
            IsPushing = locomotiveVm.Model.IsPushing,
            Details = locomotiveVm.Model.Details
        };

        // Add to Project master list
        SelectedProject.Model.Locomotives.Add(locomotiveCopy);

        // Add ID to Train
        SelectedTrain.Model.LocomotiveIds.Add(locomotiveCopy.Id);

        // Refresh Train collections
        SelectedTrain.RefreshCollections();

        Debug.WriteLine($"[OK] Added locomotive '{locomotiveCopy.Name}' to train '{SelectedTrain.Name}'");
    }

    private bool CanAddLocomotiveToTrain() => SelectedTrain != null && SelectedProject != null;
    #endregion

    private static SystemState CreateSystemState(SystemStateChangedEvent evt)
    {
        return new SystemState
        {
            MainCurrent = evt.MainCurrent,
            ProgCurrent = evt.ProgCurrent,
            FilteredMainCurrent = evt.FilteredMainCurrent,
            Temperature = evt.Temperature,
            SupplyVoltage = evt.SupplyVoltage,
            VccVoltage = evt.VccVoltage,
            CentralState = unchecked((byte)evt.CentralState),
            CentralStateEx = unchecked((byte)evt.CentralStateEx)
        };
    }

    private static XBusStatus CreateXBusStatus(XBusStatusChangedEvent evt)
        => new(evt.EmergencyStop, evt.TrackOff, evt.ShortCircuit, evt.Programming);

    private static Z21VersionInfo CreateVersionInfo(VersionInfoChangedEvent evt)
    {
        return new Z21VersionInfo
        {
            SerialNumber = evt.SerialNumber,
            HardwareTypeCode = unchecked((uint)evt.HardwareTypeCode),
            FirmwareVersionCode = unchecked((uint)evt.FirmwareVersionCode)
        };
    }
}
