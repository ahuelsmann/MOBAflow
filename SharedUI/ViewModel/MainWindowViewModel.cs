// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using Backend.Interface;
using Backend.Service.Validation;

using Common.Configuration;
using Common.Events;
using Common.Extension;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Domain;
using Domain.Enum;

using Interface;

using Microsoft.Extensions.Logging;

using System.Collections.ObjectModel;
using System.ComponentModel;

/// <summary>
/// Core ViewModel for main window functionality.
/// Partial classes handle: Solution, SolutionAutoSave, Workflow and Z21.
/// </summary>
public sealed partial class MainWindowViewModel : ObservableObject
{
    #region Fields
    private const int ShutdownDisconnectTimeoutSeconds = 5;

    // Core Services (required)
    private readonly ISolutionSession _session;
    private readonly IRuntimeSnapshotProvider _runtimeSnapshots;
    private readonly IConnectionRuntime _runtimeConnection;
    private readonly ITrafficMonitor _trafficMonitor;
    private readonly IRuntimeCommandGateway _runtimeCommandGateway;
    private readonly IEventBus _eventBus;
    private readonly ILogger<MainWindowViewModel> _logger;

    // Configuration
    private readonly AppSettings _settings;

    // Optional Services
    private readonly ISettingsService? _settingsService;
    private readonly IDialogService? _dialogService;

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
    /// <param name="runtimeSnapshots">Read access to runtime snapshots.</param>
    /// <param name="runtimeConnection">Runtime lifecycle and Z21 connection.</param>
    /// <param name="trafficMonitor">Z21 traffic monitor and polling diagnostics.</param>
    /// <param name="runtimeCommandGateway">Route for every operator command (local, recorded or remote).</param>
    /// <param name="eventBus">The event bus used to subscribe to backend domain events.</param>
    /// <param name="settingsPage">Shared settings presentation and persistence.</param>
    /// <param name="solutionSession">Owns the loaded solution, its persistence and the project and journey selection.</param>
    /// <param name="logger">Logger used for diagnostic output.</param>
    /// <param name="settingsService">Optional settings service used to persist changes.</param>
    /// <param name="dialogService">Optional dialog service for showing confirmation dialogs (WinUI only).</param>
    public MainWindowViewModel(
        LayoutColumnWidthsViewModel layoutColumnWidths,
        IRuntimeSnapshotProvider runtimeSnapshots,
        IConnectionRuntime runtimeConnection,
        ITrafficMonitor trafficMonitor,
        IRuntimeCommandGateway runtimeCommandGateway,
        IEventBus eventBus,
        SettingsPageViewModel settingsPage,
        TrainsPageViewModel trainsPage,
        StationsPageViewModel stationsPage,
        JourneysPageViewModel journeysPage,
        ISolutionSession solutionSession,
        ILogger<MainWindowViewModel> logger,
        ISettingsService? settingsService = null,
        IDialogService? dialogService = null,
        IProjectDiagnosticsService? projectDiagnosticsService = null)
    {
        ArgumentNullException.ThrowIfNull(layoutColumnWidths);
        ArgumentNullException.ThrowIfNull(runtimeSnapshots);
        ArgumentNullException.ThrowIfNull(runtimeConnection);
        ArgumentNullException.ThrowIfNull(trafficMonitor);
        ArgumentNullException.ThrowIfNull(runtimeCommandGateway);
        ArgumentNullException.ThrowIfNull(eventBus);
        ArgumentNullException.ThrowIfNull(settingsPage);
        ArgumentNullException.ThrowIfNull(trainsPage);
        ArgumentNullException.ThrowIfNull(solutionSession);
        ArgumentNullException.ThrowIfNull(stationsPage);
        ArgumentNullException.ThrowIfNull(journeysPage);
        ArgumentNullException.ThrowIfNull(logger);

        _session = solutionSession;
        _runtimeSnapshots = runtimeSnapshots;
        _runtimeConnection = runtimeConnection;
        _trafficMonitor = trafficMonitor;
        _runtimeCommandGateway = runtimeCommandGateway;
        _eventBus = eventBus;
        var settings = settingsPage.Settings;
        _settings = settings;
        SettingsPage = settingsPage;
        TrainsPage = trainsPage;
        StationsPage = stationsPage;
        JourneysPage = journeysPage;
        SettingsPage.PropertyChanged += OnSettingsPagePropertyChanged;
        SettingsPage.FeedbackPointCountChanged += OnFeedbackPointCountChanged;
        _logger = logger;
        _layoutColumnWidths = layoutColumnWidths;
        _layoutColumnWidths.LoadFrom(settings.Layout);
        _settingsService = settingsService;
        _dialogService = dialogService;
        _projectDiagnosticsService = projectDiagnosticsService;

        WorkflowLibrary.PropertyChanged += OnWorkflowLibraryPropertyChanged;

        _eventBusSubscriptions.Add(_eventBus.Subscribe<RuntimeSnapshotChangedEvent>(OnRuntimeSnapshotChanged));
        ApplyRuntimeSnapshot(_runtimeSnapshots.Current);

        AttachSolutionSession();
        HandleSelectedProjectChanged();
        SaveSolutionCommand.NotifyCanExecuteChanged();
        ConnectCommand.NotifyCanExecuteChanged();
        OnPropertyChanged(nameof(SelectedProject));

        GlobalTargetLapCount = settings.Counter.TargetLapCount;
        UseTimerFilter = settings.Counter.UseTimerFilter;
        TimerIntervalSeconds = settings.Counter.TimerIntervalSeconds;

        InitializeLayoutPanelStates();

        _eventBusSubscriptions.Add(eventBus.Subscribe<PostStartupStatusEvent>(e => UpdatePostStartupInitializationStatus(e.IsRunning, e.StatusText)));
        _eventBusSubscriptions.Add(eventBus.Subscribe<RestApiStatusChangedEvent>(OnRestApiStatusChanged));
        _eventBusSubscriptions.Add(eventBus.Subscribe<MobaflowSyncDiagnosticsChangedEvent>(OnMobaflowSyncDiagnosticsChanged));
        _eventBusSubscriptions.Add(eventBus.Subscribe<PhotoAssignedEvent>(OnPhotoAssigned));

        InitializeTrafficMonitor();

        InitializeStatisticsFromFeedbackPoints();
        ApplyJourneyRuntimeSnapshots(_latestRuntimeSnapshot.JourneyStates);
        NotifyRuntimeCommandStatesChanged();

        UpdateSolutionLoadedStatus();
    }
    #endregion

    #region Properties

    /// <summary>
    /// Observable column widths per page. Bound from grid ColumnDefinitions; when values are set
    /// (from loaded settings or from the resize behavior), the UI updates via binding.
    /// </summary>
    public LayoutColumnWidthsViewModel LayoutColumnWidths => _layoutColumnWidths;

    /// <summary>Gets the singleton workflow catalog and editor state shared by workflow surfaces.</summary>
    public WorkflowLibraryViewModel WorkflowLibrary => StationsPage.WorkflowLibrary;

    /// <summary>Gets the host-owned settings state shared with SettingsPage and shell surfaces.</summary>
    public SettingsPageViewModel SettingsPage { get; }

    /// <summary>Gets the host-owned locomotive editor shared by inventory and train pages.</summary>
    public LocomotivesPageViewModel LocomotivesPage => TrainsPage.LocomotivesPage;

    /// <summary>Gets the host-owned station/platform editor.</summary>
    public StationsPageViewModel StationsPage { get; }

    /// <summary>Gets the host-owned journey editor and city library shared with the journey page.</summary>
    public JourneysPageViewModel JourneysPage { get; }

    /// <summary>Gets the host-owned train composition editor.</summary>
    public TrainsPageViewModel TrainsPage { get; }

    /// <summary>Gets the host-owned wagon editor shared by inventory and train pages.</summary>
    public WagonsPageViewModel WagonsPage => TrainsPage.WagonsPage;

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
    /// Raised when the application should be closed (for example when the user selects an Exit command).
    /// </summary>
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
    /// <summary>
    /// Creates a new empty project in the current solution and selects it.
    /// </summary>
    [RelayCommand]
    private void AddProject()
    {
        _session.AddProject(new Project { Name = "New Project" });

        SaveSolutionCommand.NotifyCanExecuteChanged();
        DeleteProjectCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand(CanExecute = nameof(CanDeleteProject))]
    private async Task DeleteProjectAsync()
    {
        if (SelectedProject == null) return;
        if (_dialogService == null) return;

        // Show confirmation dialog; a connected runtime stops its trains when the project goes away.
        var stopsTrains = _session.GetConnectedProjectNames([SelectedProject.Model.Id]).Count > 0;
        var confirmed = await _dialogService.ShowConfirmationAsync(
            title: "Delete Project",
            message: stopsTrains
                ? "Do you really want to delete the project? Its runtime is connected to a Z21; "
                    + "all its locomotives are set to speed 0 first."
                : "Do you really want to delete the project?",
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

        var deletedProject = SelectedProject;

        // Clear all detail selections first; this prevents showing stale data from the deleted project.
        ClearAllSelections();

        // Removes the project and selects the first remaining one.
        _session.RemoveProject(deletedProject);

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

        // The dialog and a following save prompt need the UI context.
        if (!await ConfirmStopTrainsAsync("Closing MOBAflow").ConfigureAwait(true))
        {
            return false;
        }

        if (HasUnsavedChanges)
        {
            var saved = await _session
                .SaveAsync(allowPathSelection: string.IsNullOrWhiteSpace(CurrentSolutionPath))
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
            // Every runtime sets its locomotives to speed 0 and closes its Z21 connection.
            await _session.StopRuntimesAsync(cancellationTokenSource.Token).ConfigureAwait(false);
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

        await _session.DrainPendingSaveAsync().ConfigureAwait(false);
        return true;
    }

    private bool TryBeginShutdown()
    {
        if (_isShuttingDown)
        {
            return false;
        }

        _isShuttingDown = true;
        _session.BeginShutdown();
        SettingsPage.PropertyChanged -= OnSettingsPagePropertyChanged;
        SettingsPage.FeedbackPointCountChanged -= OnFeedbackPointCountChanged;
        WorkflowLibrary.PropertyChanged -= OnWorkflowLibraryPropertyChanged;
        StationsPage.Dispose();
        JourneysPage.Dispose();
        WorkflowLibrary.Dispose();
        TrainsPage.Dispose();
        LocomotivesPage.Dispose();
        WagonsPage.Dispose();
        foreach (var subscriptionId in _eventBusSubscriptions)
        {
            _eventBus.Unsubscribe(subscriptionId);
        }
        _eventBusSubscriptions.Clear();

        return true;
    }

    private void OnFeedbackPointCountChanged(object? sender, EventArgs e)
    {
        InitializeStatisticsFromFeedbackPoints();
    }

    private void OnSettingsPagePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SettingsPageViewModel.SelectedSpeechProviderId))
        {
            UpdateHealthStatus("Initializing...");
        }
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
}
