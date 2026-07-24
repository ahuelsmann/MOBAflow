// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using Backend.Interface;
using Backend.Service;
using Backend.Service.Validation;

using Common.Configuration;
using Common.Events;
using Common.Extension;
using Common.Navigation;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Domain;
using Domain.Enum;

using Interface;

using Microsoft.Extensions.Logging;

using Service;

using System.Collections.ObjectModel;

// For NullIoService

/// <summary>
/// Core ViewModel for main window functionality.
/// Partial classes handle: Selection, Solution, SolutionAutoSave, Journey, Workflow, Train, Z21, Settings.
/// </summary>
public sealed partial class MainWindowViewModel : ObservableObject, IProjectContext
{
    #region Fields
    private const int ShutdownDisconnectTimeoutSeconds = 5;

    // Core Services (required)
    private readonly IIoService _ioService;
    private readonly IMobaRuntime _mobaRuntime;
    private readonly IRuntimeCommandGateway _runtimeCommandGateway;
    private readonly IUiDispatcher _uiDispatcher;
    private readonly IEventBus _eventBus;
    private readonly ILogger<MainWindowViewModel> _logger;
    private readonly ILoggerFactory? _loggerFactory;

    // Configuration
    private readonly AppSettings _settings;

    // Optional Services
    private readonly ICityService? _cityLibraryService;
    private readonly ISettingsService? _settingsService;
    private readonly AnnouncementService? _announcementService;
    private readonly Func<string, Task>? _speechTestAction;
    private readonly IFeatureTogglePageProvider? _featureTogglePageProvider;
    private readonly IDialogService? _dialogService;
    private readonly ILocomotiveWhistleAutomationService? _locomotiveWhistleAutomation;

    // Execution Context (contains all action execution dependencies)
    private readonly ActionExecutionContext _executionContext;

    // Layout column widths (observable, bound from grid columns; loaded from settings so UI reflects persisted values)
    private readonly LayoutColumnWidthsViewModel _layoutColumnWidths;
    private readonly List<Guid> _eventBusSubscriptions = [];

    private bool _isShuttingDown;

    #endregion

    #region Constructor
    /// <summary>
    /// Initializes a new instance of the <see cref="MainWindowViewModel"/> class with all required backend services and configuration.
    /// </summary>
    /// <param name="layoutColumnWidths">Observable column widths loaded from settings and bound by layout panels.</param>
    /// <param name="mobaRuntime">The in-process MOBA runtime (Z21, project activation, snapshots).</param>
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
    /// <param name="featureTogglePageProvider">Optional provider for feature toggle page metadata.</param>
    /// <param name="runtimeCommandGateway">Optional explicit command route used by UI control actions.</param>
    /// <param name="loggerFactory">Optional factory used to create loggers for nested view models (e.g. workflow command encoding).</param>
    /// <param name="dialogService">Optional dialog service for showing confirmation dialogs (WinUI only).</param>
    /// <param name="speechTestAction">Optional direct speech test action for the selected speaker engine.</param>
    public MainWindowViewModel(
        LayoutColumnWidthsViewModel layoutColumnWidths,
        IMobaRuntime mobaRuntime,
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
        IFeatureTogglePageProvider? featureTogglePageProvider = null,
        ILoggerFactory? loggerFactory = null,
        IDialogService? dialogService = null,
        Func<string, Task>? speechTestAction = null,
        ILocomotiveWhistleAutomationService? locomotiveWhistleAutomation = null,
        IProjectDiagnosticsService? projectDiagnosticsService = null,
        IRuntimeCommandGateway? runtimeCommandGateway = null,
        IWorkflowService? workflowService = null,
        IWorkflowTraceStore? workflowTraceStore = null)
    {
        ArgumentNullException.ThrowIfNull(layoutColumnWidths);
        ArgumentNullException.ThrowIfNull(mobaRuntime);
        ArgumentNullException.ThrowIfNull(eventBus);
        ArgumentNullException.ThrowIfNull(uiDispatcher);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(solution);
        ArgumentNullException.ThrowIfNull(executionContext);
        ArgumentNullException.ThrowIfNull(logger);

        _ioService = ioService ?? new NullIoService();  // Use null object pattern
        _mobaRuntime = mobaRuntime;
        _runtimeCommandGateway = runtimeCommandGateway ?? new LocalRuntimeCommandGateway(mobaRuntime);
        _uiDispatcher = uiDispatcher;
        _eventBus = eventBus;
        _settings = settings;
        _logger = logger;
        _loggerFactory = loggerFactory;
        _layoutColumnWidths = layoutColumnWidths;
        _layoutColumnWidths.LoadFrom(settings.Layout);
        _cityLibraryService = cityLibraryService;
        _settingsService = settingsService;
        _announcementService = announcementService;
        _speechTestAction = speechTestAction;
        _executionContext = executionContext;
        _featureTogglePageProvider = featureTogglePageProvider;
        _dialogService = dialogService;
        _locomotiveWhistleAutomation = locomotiveWhistleAutomation;
        _projectDiagnosticsService = projectDiagnosticsService;

        WorkflowLibrary = new WorkflowLibraryViewModel(
            this,
            dialogService,
            new WorkflowValidator(),
            loggerFactory?.CreateLogger<WorkflowLibraryViewModel>(),
            new WorkflowLibraryRuntimeServices
            {
                WorkflowService = workflowService,
                TraceStore = workflowTraceStore,
                ExecutionContext = executionContext,
                EventBus = eventBus
            });
        WorkflowLibrary.PropertyChanged += OnWorkflowLibraryPropertyChanged;

        _eventBusSubscriptions.Add(_eventBus.Subscribe<RuntimeSnapshotChangedEvent>(OnRuntimeSnapshotChanged));
        _eventBusSubscriptions.Add(_eventBus.Subscribe<VehicleUsageCheckpointCommittedEvent>(OnVehicleUsageCheckpointCommitted));
        ApplyRuntimeSnapshot(_mobaRuntime.Current);

        Solution = solution;

        GlobalTargetLapCount = settings.Counter.TargetLapCount;
        UseTimerFilter = settings.Counter.UseTimerFilter;
        TimerIntervalSeconds = settings.Counter.TimerIntervalSeconds;

        IsDarkMode = settings.Application.IsDarkMode;
        InitializeLayoutPanelStates();

        _eventBusSubscriptions.Add(eventBus.Subscribe<FeedbackReceivedEvent>(e => UpdateTrackStatistics((uint)e.InPort)));
        _eventBusSubscriptions.Add(eventBus.Subscribe<PostStartupStatusEvent>(e => UpdatePostStartupInitializationStatus(e.IsRunning, e.StatusText)));
        _eventBusSubscriptions.Add(eventBus.Subscribe<RestApiStatusChangedEvent>(OnRestApiStatusChanged));
        _eventBusSubscriptions.Add(eventBus.Subscribe<MobaflowSyncDiagnosticsChangedEvent>(OnMobaflowSyncDiagnosticsChanged));
        _eventBusSubscriptions.Add(eventBus.Subscribe<PhotoAssignedEvent>(OnPhotoAssigned));

        InitializeTrafficMonitor();

        InitializeStatisticsFromFeedbackPoints();

        InitializeFeatureToggleItems();

        UpdateSolutionLoadedStatus();
    }
    #endregion

    #region Properties
    [ObservableProperty]
    private Solution _solution;

    [ObservableProperty]
    private string? _currentSolutionPath;

    /// <summary>
    /// Indicates whether the in-memory solution contains changes that have not been persisted.
    /// </summary>
    [ObservableProperty]
    private bool _hasUnsavedChanges;

    [ObservableProperty]
    private bool _isDarkMode = true;  // Dark theme is default for WinUI

    /// <summary>
    /// Observable column widths per page. Bound from grid ColumnDefinitions; when values are set
    /// (from loaded settings or from the resize behavior), the UI updates via binding.
    /// </summary>
    public LayoutColumnWidthsViewModel LayoutColumnWidths => _layoutColumnWidths;

    /// <summary>Gets the singleton workflow catalog and editor state shared by workflow surfaces.</summary>
    public WorkflowLibraryViewModel WorkflowLibrary { get; }

    /// <summary>
    /// Application settings model shared with the shell. Exposed for UI behaviors that walk the visual tree
    /// (e.g. column width persistence) without using a service locator.
    /// </summary>
    public AppSettings ApplicationSettings => _settings;

    /// <summary>
    /// Optional settings persistence service (platform-provided). Used by shell-level UI behaviors.
    /// </summary>
    public ISettingsService? SettingsPersistence => _settingsService;

    /// <summary>
    /// Non-generic logger for UI shell subsystems (e.g. layout behaviors) that are not constructed via DI.
    /// </summary>
    public ILogger UiShellLogger => _logger;

    /// <summary>
    /// Called when IsDarkMode changes. Persists to AppSettings.
    /// </summary>
    partial void OnIsDarkModeChanged(bool value)
    {
        _settings.Application.IsDarkMode = value;
        PersistSettingsSafely();
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
    [NotifyCanExecuteChangedFor(nameof(AddStationCommand))]
    [NotifyCanExecuteChangedFor(nameof(AddStationFromCityCommand))]
    private JourneyViewModel? _selectedJourney;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(DeleteStationCommand))]
    private StationViewModel? _selectedStation;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(DeleteWorkflowCommand))]
    private WorkflowViewModel? _selectedWorkflow;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(DeleteActionCommand))]
    private object? _selectedAction;

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
    /// Raised when the application should be closed (for example when the user selects an Exit command).
    /// </summary>
    public event EventHandler? ExitApplicationRequested;

    /// <summary>
    /// Currently selected city for adding stations to journeys.
    /// </summary>
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
        var projectVm = new ProjectViewModel(project, _uiDispatcher, _ioService, _executionContext.SoundPlayer, _loggerFactory);
        SolutionViewModel!.Projects.Add(projectVm);

        // Select the newly created project
        SelectedProject = projectVm;

        // Update HasSolution flag
        HasSolution = true;

        SaveSolutionCommand.NotifyCanExecuteChanged();
        DeleteProjectCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand(CanExecute = nameof(CanDeleteProject))]
    private async Task DeleteProjectAsync()
    {
        if (SelectedProject == null) return;
        if (_dialogService == null) return;

        // Show confirmation dialog
        var confirmed = await _dialogService.ShowConfirmationAsync(
            title: "Delete Project",
            message: "Do you really want to delete the project?",
            confirmButtonText: "Yes",
            cancelButtonText: "No",
            isCancelDefault: true);

        if (!confirmed) return;

        // Create backup of current solution file (before deletion)
        var solutionPath = CurrentSolutionPath;
        if (!string.IsNullOrEmpty(solutionPath) && File.Exists(solutionPath))
        {
            try
            {
                var dir = Path.GetDirectoryName(solutionPath);
                var fileName = Path.GetFileNameWithoutExtension(solutionPath);
                var ext = Path.GetExtension(solutionPath);
                var backupPath = Path.Combine(dir ?? string.Empty, $"{fileName}.backup{ext}");
                File.Copy(solutionPath, backupPath, overwrite: true);
            }
            catch
            {
                // Backup failed – still perform deletion (user has confirmed)
                _logger.LogDebug("Failed to create backup before project deletion");
            }
        }

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

    private bool CanDeleteProject() => SelectedProject != null && _dialogService != null;
    #endregion

    #region Lifecycle
    /// <summary>
    /// Stops runtime-driven UI updates and disconnects from the Z21 before the host window completes shutdown.
    /// </summary>
    public async Task<bool> PrepareForShutdownAsync()
    {
        if (_isShuttingDown)
        {
            return true;
        }

        BeginSuppressSolutionAutoSave();
        try
        {
            await _mobaRuntime.CheckpointUsageAsync().ConfigureAwait(false);
            if (SynchronizeVehicleUsageFromRuntime())
            {
                MarkSolutionDirty();
            }
        }
        finally
        {
            EndSuppressSolutionAutoSave();
        }

        if (HasUnsavedChanges)
        {
            var saved = await SaveSolutionCoreAsync(
                    CurrentSolutionPath,
                    allowPathSelection: string.IsNullOrWhiteSpace(CurrentSolutionPath))
                .ConfigureAwait(false);
            if (!saved)
            {
                return false;
            }
        }

        if (!TryBeginShutdown())
        {
            return true;
        }

        using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(ShutdownDisconnectTimeoutSeconds));

        try
        {
            await _mobaRuntime.DisconnectAsync(cancellationTokenSource.Token).ConfigureAwait(false);
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
        return true;
    }

    private bool TryBeginShutdown()
    {
        if (_isShuttingDown)
        {
            return false;
        }

        _isShuttingDown = true;
        WorkflowLibrary.PropertyChanged -= OnWorkflowLibraryPropertyChanged;
        WorkflowLibrary.Dispose();
        foreach (var subscriptionId in _eventBusSubscriptions)
        {
            _eventBus.Unsubscribe(subscriptionId);
        }
        _eventBusSubscriptions.Clear();

        return true;
    }

    private void PersistSettingsSafely()
    {
        if (_settingsService == null)
        {
            return;
        }

        _settingsService.SaveSettingsAsync(_settings).Observe(ex => _logger.LogWarning(ex, "Persist MainWindow settings failed"));
    }

    private void ObserveBackgroundTask(Task task, string operationName)
    {
        task.Observe(ex => _logger.LogWarning(ex, "{OperationName} failed", operationName));
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
    private void AddStationFromCity(City? city)
    {
        if (SelectedJourney == null || city == null) return;

        // Get City's first station (Hauptbahnhof) - only the NAME
        var cityStation = city.Stations.FirstOrDefault();
        if (cityStation == null) return;

        // Create NEW Station object (copy name from City Library)
        var newStation = new Station
        {
            Name = cityStation.Name,
            IsExitOnLeft = false,
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
