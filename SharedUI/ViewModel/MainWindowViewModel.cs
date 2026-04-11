// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using Backend;
using Backend.Service;
using Common.Configuration;
using Common.Events;
using Common.Navigation;
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
using System.Threading;

// For NullIoService

/// <summary>
/// Core ViewModel for main window functionality.
/// Partial classes handle: Selection, Solution, Journey, Workflow, Train, Z21, Settings.
/// </summary>
public sealed partial class MainWindowViewModel : ObservableObject
{
    #region Fields
    private const int ShutdownDisconnectTimeoutSeconds = 5;

    // Core Services (required)
    private readonly IIoService _ioService;
    private readonly IMobaClient _mobaClient;
    private readonly IUiDispatcher _uiDispatcher;
    private readonly ILogger<MainWindowViewModel> _logger;

    // Configuration
    private readonly AppSettings _settings;

    // Optional Services
    private readonly ICityService? _cityLibraryService;
    private readonly ISettingsService? _settingsService;
    private readonly AnnouncementService? _announcementService;
    private readonly IFeatureTogglePageProvider? _featureTogglePageProvider;

    // Execution Context (contains all action execution dependencies)
    private readonly ActionExecutionContext _executionContext;

    // Layout column widths (observable, bound from grid columns; loaded from settings so UI reflects persisted values)
    private readonly LayoutColumnWidthsViewModel _layoutColumnWidths;

    private bool _isShuttingDown;

    /// <summary>
    /// When greater than zero, PropertyChanged-driven solution auto-save is suppressed (bulk load / new solution).
    /// </summary>
    private int _solutionAutoSaveSuppressionCount;

    /// <summary>
    /// Ensures at most one solution file write runs at a time (avoids races on temp/rename writes).
    /// </summary>
    private readonly SemaphoreSlim _solutionSaveSemaphore = new(1, 1);

    /// <summary>
    /// Set to 1 after <see cref="DrainAndDisposeSolutionSaveSemaphoreAsync"/> has run (idempotent shutdown).
    /// </summary>
    private int _solutionSaveSemaphoreDrainStarted;

    #endregion

    #region Constructor
    /// <summary>
    /// Initializes a new instance of the <see cref="MainWindowViewModel"/> class with all required backend services and configuration.
    /// </summary>
    /// <param name="mobaClient">The client used to talk to the active MOBA runtime.</param>
    /// <param name="eventBus">The event bus used to subscribe to backend domain events.</param>
    /// <param name="uiDispatcher">Dispatcher used to marshal callbacks onto the UI thread.</param>
    /// <param name="settings">Application-wide settings object.</param>
    /// <param name="solution">The currently loaded solution with all projects.</param>
    /// <param name="executionContext">Execution context containing shared dependencies for workflow actions.</param>
    /// <param name="logger">Logger used for diagnostic output.</param>
    /// <param name="ioService">Optional IO service implementation (null uses <see cref="NullIoService"/>).</param>
    /// <param name="cityLibraryService">Optional city library service for master data.</param>
    /// <param name="settingsService">Optional settings service used to persist changes.</param>
    /// <param name="announcementService">Optional announcement service for text-to-speech announcements.</param>
    /// <param name="photoHubClient">Optional PhotoHub client instance (WinUI only, loosely typed as <see cref="object"/>).</param>
    /// <param name="featureTogglePageProvider">Optional provider for feature toggle page metadata.</param>
    /// <param name="layoutColumnWidths">Observable column widths loaded from settings and bound by layout panels.</param>
    public MainWindowViewModel(
        LayoutColumnWidthsViewModel layoutColumnWidths,
        IMobaClient mobaClient,
        IEventBus eventBus,
        IUiDispatcher uiDispatcher,
        AppSettings settings,
        Solution solution,
        ActionExecutionContext executionContext,
        ILogger<MainWindowViewModel> logger,
        IIoService? ioService = null,  // Optional for WebApp/MAUI
        ICityService? cityLibraryService = null,
        ISettingsService? settingsService = null,
        AnnouncementService? announcementService = null,
        object? photoHubClient = null,  // Optional PhotoHubClient (only in WinUI, type is object to avoid assembly reference)
        IFeatureTogglePageProvider? featureTogglePageProvider = null)
    {
        ArgumentNullException.ThrowIfNull(layoutColumnWidths);
        ArgumentNullException.ThrowIfNull(mobaClient);
        ArgumentNullException.ThrowIfNull(eventBus);
        ArgumentNullException.ThrowIfNull(uiDispatcher);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(solution);
        ArgumentNullException.ThrowIfNull(executionContext);
        ArgumentNullException.ThrowIfNull(logger);

        _ioService = ioService ?? new NullIoService();  // Use null object pattern
        _mobaClient = mobaClient;
        _uiDispatcher = uiDispatcher;
        _settings = settings;
        _logger = logger;
        _layoutColumnWidths = layoutColumnWidths;
        _layoutColumnWidths.LoadFrom(settings.Layout);
        _cityLibraryService = cityLibraryService;
        _settingsService = settingsService;
        _announcementService = announcementService;
        _executionContext = executionContext;
        _featureTogglePageProvider = featureTogglePageProvider;
        _ = photoHubClient;

        _mobaClient.SnapshotChanged += OnMobaRuntimeSnapshotChanged;
        ApplyRuntimeSnapshot(_mobaClient.Current);

        Solution = solution;

        GlobalTargetLapCount = settings.Counter.TargetLapCount;
        UseTimerFilter = settings.Counter.UseTimerFilter;
        TimerIntervalSeconds = settings.Counter.TimerIntervalSeconds;

        IsDarkMode = settings.Application.IsDarkMode;
        InitializeLayoutPanelStates();

        eventBus.Subscribe<FeedbackReceivedEvent>(e => UpdateTrackStatistics((uint)e.InPort));
        eventBus.Subscribe<PostStartupStatusEvent>(e => UpdatePostStartupInitializationStatus(e.IsRunning, e.StatusText));

        InitializeTrafficMonitor();

        InitializeStatisticsFromFeedbackPoints();

        // Load City Library once on startup (background, non-blocking).
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

        InitializeFeatureToggleItems();
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
    /// Observable column widths per page. Bound from grid ColumnDefinitions; when values are set
    /// (from loaded settings or from the resize behavior), the UI updates via binding.
    /// </summary>
    public LayoutColumnWidthsViewModel LayoutColumnWidths => _layoutColumnWidths;

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
    [NotifyCanExecuteChangedFor(nameof(DeleteStationCommand))]
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
    [NotifyCanExecuteChangedFor(nameof(DeleteWorkflowCommand))]
    private WorkflowViewModel? _selectedWorkflow;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(DeleteActionCommand))]
    private object? _selectedAction;

    /// <summary>
    /// Generic handler for ViewModel PropertyChanged events.
    /// Triggers auto-save for any model property change.
    /// </summary>
    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (Volatile.Read(ref _solutionAutoSaveSuppressionCount) > 0)
        {
            return;
        }

        // Ignore UI-only or runtime-backed properties that must not persist the whole solution.
        // Journey: UpdateFromRuntimeSnapshot / UpdateFromSessionState refresh counters and station labels from Z21/backend.
        // Station: IsCurrentStation is runtime highlight when the active journey position changes.
        if (e.PropertyName is { } name &&
            (name is "IsSelected" or "IsExpanded" or "IsHighlighted" or "IsCurrentStation"
             or "CurrentStation" or "CurrentCounter" or "CurrentPos"))
        {
            return;
        }

        _ = SaveSolutionInternalAsync();
    }

    /// <summary>
    /// Increments suppression counter so <see cref="OnViewModelPropertyChanged"/> does not trigger auto-save.
    /// Must be paired with <see cref="EndSuppressSolutionAutoSave"/>.
    /// </summary>
    private void BeginSuppressSolutionAutoSave() => Interlocked.Increment(ref _solutionAutoSaveSuppressionCount);

    /// <summary>
    /// Decrements suppression counter started by <see cref="BeginSuppressSolutionAutoSave"/>.
    /// </summary>
    private void EndSuppressSolutionAutoSave() => Interlocked.Decrement(ref _solutionAutoSaveSuppressionCount);

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

    partial void OnJourneysPageSelectedObjectChanged(object? value)
    {
        _ = value;
        OnPropertyChanged(nameof(JourneysPagePropertiesTitle));
    }

    public string JourneysPagePropertiesTitle
    {
        get
        {
            if (JourneysPageSelectedObject is JourneyViewModel) return "Journey Properties";
            if (JourneysPageSelectedObject is StationViewModel) return "Station Properties";
            return "Properties";
        }
    }

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
    /// <summary>
    /// Raised when the application should be closed (for example when the user selects an Exit command).
    /// </summary>
    public event EventHandler? ExitApplicationRequested;

    [ObservableProperty]
    private City? _selectedCity;

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
    /// <summary>
    /// Creates a new empty project in the current solution and selects it.
    /// </summary>
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
    /// <summary>
    /// Stops runtime-driven UI updates and disconnects from the Z21 before the host window completes shutdown.
    /// </summary>
    public async Task PrepareForShutdownAsync()
    {
        if (!TryBeginShutdown())
        {
            return;
        }

        using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(ShutdownDisconnectTimeoutSeconds));

        try
        {
            await _mobaClient.DisconnectAsync(cancellationTokenSource.Token).ConfigureAwait(false);
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogWarning(ex, "Z21 disconnect timed out during shutdown after {TimeoutSeconds}s", ShutdownDisconnectTimeoutSeconds);
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogWarning(ex, "Z21 disconnect was canceled during shutdown");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Z21 disconnect");
        }

        await DrainAndDisposeSolutionSaveSemaphoreAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Waits until no in-flight solution save holds the semaphore, then releases managed resources.
    /// Safe to call once per application shutdown; subsequent calls are ignored.
    /// </summary>
    private async Task DrainAndDisposeSolutionSaveSemaphoreAsync()
    {
        if (Interlocked.CompareExchange(ref _solutionSaveSemaphoreDrainStarted, 1, 0) != 0)
        {
            return;
        }

        try
        {
            await _solutionSaveSemaphore.WaitAsync().ConfigureAwait(false);
            _solutionSaveSemaphore.Release();
        }
        catch (ObjectDisposedException)
        {
            return;
        }

        _solutionSaveSemaphore.Dispose();
    }

    private bool TryBeginShutdown()
    {
        if (_isShuttingDown)
        {
            return false;
        }

        _isShuttingDown = true;
        _mobaClient.SnapshotChanged -= OnMobaRuntimeSnapshotChanged;
        _mobaClient.TrafficPacketLogged -= OnTrafficPacketLogged;

        return true;
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

        // Ensure collection update happens on UI thread (WinUI requirement)
        _uiDispatcher.InvokeOnUi(() =>
        {
            CityLibrary = new ObservableCollection<City>(cities);
            Debug.WriteLine($"[OK] City Library loaded: {CityLibrary.Count} cities");
        });
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

    #endregion

    #region Signal Box / Viessmann Multiplex Signals (binding for Settings page)

    /// <summary>Invert polarity for address 1 (e.g. 201).</summary>
    public bool InvertPolarityOffset0Setting { get => _settings.SignalBox.InvertPolarityOffset0; set => SetSignalBoxInvert(0, value); }

    /// <summary>Invert polarity for address 2 (e.g. 202).</summary>
    public bool InvertPolarityOffset1Setting { get => _settings.SignalBox.InvertPolarityOffset1; set => SetSignalBoxInvert(1, value); }

    /// <summary>Invert polarity for address 3 (e.g. 203).</summary>
    public bool InvertPolarityOffset2Setting { get => _settings.SignalBox.InvertPolarityOffset2; set => SetSignalBoxInvert(2, value); }

    /// <summary>Invert polarity for address 4 (e.g. 204).</summary>
    public bool InvertPolarityOffset3Setting { get => _settings.SignalBox.InvertPolarityOffset3; set => SetSignalBoxInvert(3, value); }

    #endregion
}
