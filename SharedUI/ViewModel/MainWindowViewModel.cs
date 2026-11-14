// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using Backend.Interface;

using Common.Configuration;
using Common.Events;

using CommunityToolkit.Mvvm.ComponentModel;

using Interface;

using Microsoft.Extensions.Logging;

/// <summary>
/// The main window shell: solution new/open/save and shutdown through the session, the Z21 status and telemetry
/// projection, host command forwarding, the operating state and uploaded-photo routing.
/// Partial classes handle: Solution, Z21, OperatingState and PhotoAssignment. Page state lives in the page ViewModels.
/// </summary>
public sealed partial class MainWindowViewModel : ObservableObject
{
    #region Fields
    private const int ShutdownDisconnectTimeoutSeconds = 5;

    // Core Services (required)
    private readonly ISolutionSession _session;
    private readonly IRuntimeSnapshotProvider _runtimeSnapshots;
    private readonly IConnectionRuntime _runtimeConnection;
    private readonly IRuntimeCommandGateway _runtimeCommandGateway;
    private readonly IEventBus _eventBus;
    private readonly ILogger<MainWindowViewModel> _logger;

    // Configuration
    private readonly AppSettings _settings;

    private readonly IDialogService _dialogService;
    private readonly List<Guid> _eventBusSubscriptions = [];

    private bool _isShuttingDown;

    #endregion

    #region Constructor
    /// <summary>
    /// Initializes a new instance of the <see cref="MainWindowViewModel"/> class with all required backend services and configuration.
    /// </summary>
    /// <param name="runtimeSnapshots">Read access to runtime snapshots.</param>
    /// <param name="runtimeConnection">Runtime lifecycle and Z21 connection.</param>
    /// <param name="runtimeCommandGateway">Route for every operator command (local, recorded or remote).</param>
    /// <param name="eventBus">The event bus used to subscribe to backend domain events.</param>
    /// <param name="settingsPage">Shared settings presentation and persistence.</param>
    /// <param name="trainsPage">Host-owned train editor with the locomotive and wagon editors that receive uploaded photos.</param>
    /// <param name="stationsPage">Host-owned station editor, disposed with the shared workflow library at shutdown.</param>
    /// <param name="journeysPage">Host-owned journey editor whose reset command the main window binds.</param>
    /// <param name="diagnostics">Host-owned Messages pane state bound by the main window.</param>
    /// <param name="systemStatus">Host-owned speech, post-startup, REST API and synchronization status bound by the status bar.</param>
    /// <param name="solutionSession">Owns the loaded solution, its persistence and the project and journey selection.</param>
    /// <param name="logger">Logger used for diagnostic output.</param>
    /// <param name="dialogService">Asks before new or opened solutions stop connected runtimes.</param>
    public MainWindowViewModel(
        IRuntimeSnapshotProvider runtimeSnapshots,
        IConnectionRuntime runtimeConnection,
        IRuntimeCommandGateway runtimeCommandGateway,
        IEventBus eventBus,
        SettingsPageViewModel settingsPage,
        TrainsPageViewModel trainsPage,
        StationsPageViewModel stationsPage,
        JourneysPageViewModel journeysPage,
        ProjectDiagnosticsViewModel diagnostics,
        SystemStatusViewModel systemStatus,
        ISolutionSession solutionSession,
        ILogger<MainWindowViewModel> logger,
        IDialogService dialogService)
    {
        ArgumentNullException.ThrowIfNull(runtimeSnapshots);
        ArgumentNullException.ThrowIfNull(runtimeConnection);
        ArgumentNullException.ThrowIfNull(runtimeCommandGateway);
        ArgumentNullException.ThrowIfNull(eventBus);
        ArgumentNullException.ThrowIfNull(settingsPage);
        ArgumentNullException.ThrowIfNull(trainsPage);
        ArgumentNullException.ThrowIfNull(solutionSession);
        ArgumentNullException.ThrowIfNull(stationsPage);
        ArgumentNullException.ThrowIfNull(journeysPage);
        ArgumentNullException.ThrowIfNull(diagnostics);
        ArgumentNullException.ThrowIfNull(systemStatus);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(dialogService);

        _session = solutionSession;
        _runtimeSnapshots = runtimeSnapshots;
        _runtimeConnection = runtimeConnection;
        _runtimeCommandGateway = runtimeCommandGateway;
        _eventBus = eventBus;
        _settings = settingsPage.Settings;
        SettingsPage = settingsPage;
        TrainsPage = trainsPage;
        StationsPage = stationsPage;
        JourneysPage = journeysPage;
        Diagnostics = diagnostics;
        SystemStatus = systemStatus;
        _logger = logger;
        _dialogService = dialogService;

        _eventBusSubscriptions.Add(_eventBus.Subscribe<RuntimeSnapshotChangedEvent>(OnRuntimeSnapshotChanged));
        ApplyRuntimeSnapshot(_runtimeSnapshots.Current);

        AttachSolutionSession();
        HandleSelectedProjectChanged();
        SaveSolutionCommand.NotifyCanExecuteChanged();
        ConnectCommand.NotifyCanExecuteChanged();
        OnPropertyChanged(nameof(SelectedProject));

        // The REST, speech and post-startup inputs of the operating state live in the status ViewModel.
        SystemStatus.PropertyChanged += OnSystemStatusPropertyChanged;
        _eventBusSubscriptions.Add(eventBus.Subscribe<PhotoAssignedEvent>(OnPhotoAssigned));

        ApplyJourneyRuntimeSnapshots(_latestRuntimeSnapshot.JourneyStates);
        NotifyRuntimeCommandStatesChanged();
    }
    #endregion

    #region Properties

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

    /// <summary>Gets the host-owned project diagnostics bound by the main window's Messages pane.</summary>
    public ProjectDiagnosticsViewModel Diagnostics { get; }

    /// <summary>Gets the host-owned system status bound by the status bar and read by the operating state.</summary>
    public SystemStatusViewModel SystemStatus { get; }

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

    #endregion

    #region Lifecycle
    /// <summary>
    /// Stops runtime-driven UI updates and disconnects from the Z21 before the host window completes shutdown.
    /// Closing never asks about running trains and never stops them: they keep running on the Z21.
    /// </summary>
    /// <param name="releaseRuntimeObservers">
    /// Runs once shutdown is confirmed and before the runtimes are released, so host-owned runtime observers
    /// do not process the disconnect's packets and snapshots; it does not run when shutdown is cancelled.
    /// </param>
    public async Task<bool> PrepareForShutdownAsync(System.Action? releaseRuntimeObservers = null)
    {
        if (_isShuttingDown)
        {
            return true;
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

        releaseRuntimeObservers?.Invoke();

        using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(ShutdownDisconnectTimeoutSeconds));

        try
        {
            // Every runtime closes its Z21 connection without a stop command; the trains keep running.
            await _session.ReleaseRuntimesAsync(cancellationTokenSource.Token).ConfigureAwait(false);
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
        SystemStatus.PropertyChanged -= OnSystemStatusPropertyChanged;
        Diagnostics.Dispose();
        SystemStatus.Dispose();
        StationsPage.Dispose();
        JourneysPage.Dispose();
        // The host-owned workflow library is shared by the workflow, station and event-manager surfaces.
        StationsPage.WorkflowLibrary.Dispose();
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

    #endregion
}
