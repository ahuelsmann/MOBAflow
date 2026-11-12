// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using Common.Events;
using CommunityToolkit.Mvvm.ComponentModel;
using Interface;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;

/// <summary>
/// Owns the system status shown by the status bar and the Overview page: speech health, post-startup
/// initialization, the REST API with its connected clients, the MOBAsmart synchronization rows and the
/// solution-loaded state. It projects events delivered on the UI thread and the session's solution state;
/// the shell reads its REST, speech and post-startup values for the operating state.
/// While the REST API starts and the MOBAsmart synchronization connects, the affected rows are pending: they say
/// what they wait for and how long, so a slow start does not look broken.
/// </summary>
public sealed partial class SystemStatusViewModel : ObservableObject, IDisposable
{
    private static readonly TimeSpan SyncFreshnessWindow = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan RestCacheTolerance = TimeSpan.FromSeconds(30);

    /// <summary>
    /// How long a starting REST API shows progress. The host waits as long for MOBApi to answer; after that the
    /// row reports the slow start instead of spinning on.
    /// </summary>
    private static readonly TimeSpan RestApiStartPatience = TimeSpan.FromSeconds(30);

    private const string WaitingForRestApiText = "Waiting for the REST API...";

    private readonly IEventBus _eventBus;
    private readonly ISolutionSession _session;
    private readonly SettingsPageViewModel _settingsPage;
    private readonly TimeProvider _timeProvider;
    private readonly List<Guid> _eventBusSubscriptions = [];
    private DateTimeOffset? _restApiWaitingSince;
    private DateTimeOffset? _runtimeHubHostWaitingSince;
    private bool _isDisposed;

    /// <param name="eventBus">Delivers the post-startup, REST API and synchronization events on the UI thread.</param>
    /// <param name="session">Owns the solution path and the project selection for the solution-loaded status.</param>
    /// <param name="settingsPage">Shared settings state; a speech provider change resets the speech health.</param>
    /// <param name="timeProvider">Clock for the waiting time of pending rows; defaults to the system clock.</param>
    public SystemStatusViewModel(
        IEventBus eventBus,
        ISolutionSession session,
        SettingsPageViewModel settingsPage,
        TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(eventBus);
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(settingsPage);

        _eventBus = eventBus;
        _session = session;
        _settingsPage = settingsPage;
        _timeProvider = timeProvider ?? TimeProvider.System;

        _eventBusSubscriptions.Add(_eventBus.Subscribe<PostStartupStatusEvent>(e => UpdatePostStartupInitializationStatus(e.IsRunning, e.StatusText)));
        _eventBusSubscriptions.Add(_eventBus.Subscribe<RestApiStatusChangedEvent>(OnRestApiStatusChanged));
        _eventBusSubscriptions.Add(_eventBus.Subscribe<MobaflowSyncDiagnosticsChangedEvent>(OnMobaflowSyncDiagnosticsChanged));
        _settingsPage.PropertyChanged += OnSettingsPagePropertyChanged;
        _session.PropertyChanged += OnSessionPropertyChanged;

        UpdateSolutionLoadedStatus();
    }

    #region Speech health

    /// <summary>Health status message of the selected speech provider, updated by the host's health checks.</summary>
    [ObservableProperty]
    public partial string SpeechHealthStatus { get; private set; } = "Initializing...";

    /// <summary>Icon glyph for the speech health status.</summary>
    [ObservableProperty]
    public partial string SpeechHealthIcon { get; private set; } = "\uE946"; // Sync

    /// <summary>Color for the speech health status icon.</summary>
    [ObservableProperty]
    public partial string SpeechHealthColor { get; private set; } = "SystemFillColorCautionBrush";

    /// <summary>Gets the speech status text shown in the status bar for the current provider.</summary>
    public string SpeechStatusDisplayText => SpeechHealthStatus;

    /// <summary>Updates the speech health from a health-check status message.</summary>
    public void UpdateHealthStatus(string statusMessage)
    {
        ArgumentNullException.ThrowIfNull(statusMessage);

        SpeechHealthStatus = $"Speech: {statusMessage.TrimStart('✅', '❌', '⚠', '️', '⏳', ' ')}";

        if (statusMessage.Contains("Ready", StringComparison.OrdinalIgnoreCase))
        {
            SpeechHealthIcon = "\uE930"; // Checkmark circle
            SpeechHealthColor = "Green";
        }
        else if (statusMessage.Contains("Not Configured", StringComparison.OrdinalIgnoreCase) || statusMessage.Contains("Unavailable", StringComparison.OrdinalIgnoreCase))
        {
            SpeechHealthIcon = "\uE7BA"; // Warning
            SpeechHealthColor = "SystemFillColorCautionBrush";
        }
        else if (statusMessage.Contains("Failed", StringComparison.OrdinalIgnoreCase))
        {
            SpeechHealthIcon = "\uE711"; // Error
            SpeechHealthColor = "Red";
        }
        else // Initializing
        {
            SpeechHealthIcon = "\uE946"; // Sync
            SpeechHealthColor = "SystemFillColorCautionBrush";
        }

        OnPropertyChanged(nameof(SpeechStatusDisplayText));
    }

    private void OnSettingsPagePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SettingsPageViewModel.SelectedSpeechProviderId))
            UpdateHealthStatus("Initializing...");
    }

    #endregion

    #region Post-startup initialization

    /// <summary>Indicates whether post-startup initialization is running.</summary>
    [ObservableProperty]
    public partial bool IsPostStartupInitializationRunning { get; private set; }

    /// <summary>Status text for post-startup initialization.</summary>
    [ObservableProperty]
    public partial string PostStartupStatusText { get; private set; } = string.Empty;

    /// <summary>Updates the post-startup initialization status displayed in the status bar.</summary>
    public void UpdatePostStartupInitializationStatus(bool isRunning, string statusText)
    {
        if (string.IsNullOrWhiteSpace(statusText))
            statusText = isRunning ? "Initializing services..." : string.Empty;

        IsPostStartupInitializationRunning = isRunning;
        PostStartupStatusText = statusText;
    }

    #endregion

    #region REST API

    /// <summary>Human-readable REST API status (e.g. "Running on port 5001" or "Not reachable").</summary>
    [ObservableProperty]
    public partial string RestApiStatusText { get; private set; } = "Checking the REST API...";

    /// <summary>True when the REST API (MOBApi) is reachable at the configured port.</summary>
    [ObservableProperty]
    public partial bool RestApiIsReachable { get; private set; }

    /// <summary>True until the first status check and while the REST API starts (shows progress instead of a dot).</summary>
    [ObservableProperty]
    public partial bool RestApiIsPending { get; private set; } = true;

    /// <summary>How long the REST API has been starting, e.g. "(12 s)"; empty when it does not wait.</summary>
    [ObservableProperty]
    public partial string RestApiWaitingTime { get; private set; } = string.Empty;

    /// <summary>Clients currently connected to the REST API (e.g. MOBAsmart).</summary>
    [ObservableProperty]
    public partial ObservableCollection<RestApiClientInfo> RestApiConnectedClients { get; private set; } = [];

    /// <summary>True when no clients are connected (shows the "No clients connected" hint on the Overview page).</summary>
    [ObservableProperty]
    public partial bool RestApiConnectedClientsEmpty { get; private set; } = true;

    /// <summary>Updates the REST API status and connected clients from the result of GET /api/status.</summary>
    /// <param name="statusText">Status text of the check.</param>
    /// <param name="isReachable">Whether the REST API answered.</param>
    /// <param name="clients">Connected clients, or <see langword="null"/> when the REST API is not reachable.</param>
    /// <param name="isStarting">Whether an unreachable REST API is expected to start, so the row waits for it.</param>
    public void UpdateRestApiStatus(
        string statusText,
        bool isReachable,
        IReadOnlyList<RestApiClientInfo>? clients,
        bool isStarting = false)
    {
        var waiting = !isReachable && isStarting
            ? WaitingTimeSince(ref _restApiWaitingSince)
            : StopWaiting(ref _restApiWaitingSince);
        var isSlowStart = waiting >= RestApiStartPatience;

        RestApiStatusText = isSlowStart ? "REST API is slow to start, still checking..." : statusText;
        RestApiIsReachable = isReachable;
        RestApiIsPending = waiting.HasValue && !isSlowStart;
        RestApiWaitingTime = FormatWaitingTime(waiting);
        RestApiConnectedClients = clients is null
            ? []
            : new ObservableCollection<RestApiClientInfo>(clients);
        RestApiConnectedClientsEmpty = RestApiConnectedClients.Count == 0;
    }

    private void OnRestApiStatusChanged(RestApiStatusChangedEvent e) =>
        UpdateRestApiStatus(e.Status, e.IsReachable, e.Clients, e.IsStarting);

    #endregion

    #region MOBAsmart synchronization

    [ObservableProperty]
    public partial bool RuntimeHubHostIsHealthy { get; private set; }

    [ObservableProperty]
    public partial string RuntimeHubHostStatusText { get; private set; } = WaitingForRestApiText;

    /// <summary>True while the RuntimeHub host waits for the REST API or connects to it.</summary>
    [ObservableProperty]
    public partial bool RuntimeHubHostIsPending { get; private set; } = true;

    /// <summary>How long the RuntimeHub host has been connecting, e.g. "(3 s)"; empty when it does not connect.</summary>
    [ObservableProperty]
    public partial string RuntimeHubHostWaitingTime { get; private set; } = string.Empty;

    [ObservableProperty]
    public partial bool HubBroadcastIsHealthy { get; private set; }

    [ObservableProperty]
    public partial string HubBroadcastStatusText { get; private set; } = WaitingForRestApiText;

    /// <summary>True while the first snapshot push waits for the REST API or the RuntimeHub host.</summary>
    [ObservableProperty]
    public partial bool HubBroadcastIsPending { get; private set; } = true;

    [ObservableProperty]
    public partial bool RemoteSnapshotIsHealthy { get; private set; }

    [ObservableProperty]
    public partial string RemoteSnapshotStatusText { get; private set; } = "—";

    [ObservableProperty]
    public partial bool RuntimeSnapshotIsHealthy { get; private set; }

    [ObservableProperty]
    public partial string RuntimeSnapshotStatusText { get; private set; } = "—";

    [ObservableProperty]
    public partial bool RestCacheIsHealthy { get; private set; }

    [ObservableProperty]
    public partial string RestCacheStatusText { get; private set; } = WaitingForRestApiText;

    /// <summary>True while the REST API cache waits for the REST API or the first snapshot.</summary>
    [ObservableProperty]
    public partial bool RestCacheIsPending { get; private set; } = true;

    [ObservableProperty]
    public partial bool SolutionSyncIsHealthy { get; private set; }

    [ObservableProperty]
    public partial string SolutionSyncStatusText { get; private set; } = WaitingForRestApiText;

    /// <summary>True while the solution cache waits for the REST API or the first solution push.</summary>
    [ObservableProperty]
    public partial bool SolutionSyncIsPending { get; private set; } = true;

    [ObservableProperty]
    public partial bool SolutionLoadedIsHealthy { get; private set; }

    [ObservableProperty]
    public partial string SolutionLoadedStatusText { get; private set; } = "—";

    private void OnMobaflowSyncDiagnosticsChanged(MobaflowSyncDiagnosticsChangedEvent e) =>
        UpdateSyncDiagnostics(e.Diagnostics with { HasActiveProject = _session.SelectedProject != null });

    /// <summary>Projects one diagnostics record onto the six synchronization rows, then the solution-loaded row.</summary>
    private void UpdateSyncDiagnostics(MobaflowSyncDiagnostics diagnostics)
    {
        var setup = GetSyncSetup(diagnostics);
        UpdateRuntimeHubHostStatus(diagnostics, setup);
        UpdateHubBroadcastStatus(diagnostics, setup);
        UpdateRemoteSnapshotStatus(diagnostics);
        UpdateRuntimeSnapshotStatus(diagnostics);
        UpdateRestCacheStatus(diagnostics, setup);
        UpdateSolutionSyncStatus(diagnostics, setup);
        UpdateSolutionLoadedStatus();
    }

    /// <summary>
    /// The synchronization waits while the REST API starts and while the host still sets up the synchronization
    /// (it re-checks the REST API status soon); otherwise unhealthy rows report their problem.
    /// </summary>
    private SyncSetup GetSyncSetup(MobaflowSyncDiagnostics diagnostics)
    {
        if (!diagnostics.RestApiReachable)
            return RestApiIsPending ? SyncSetup.WaitingForRestApi : SyncSetup.Done;
        return diagnostics.SyncStarting ? SyncSetup.Connecting : SyncSetup.Done;
    }

    private static string DescribePending(SyncSetup setup, string connectingText) =>
        setup == SyncSetup.WaitingForRestApi ? WaitingForRestApiText : connectingText;

    private static bool IsRuntimeHubHostHealthy(MobaflowSyncDiagnostics diagnostics) =>
        diagnostics.HostClientConnected && diagnostics.ServerHasHost;

    private void UpdateRuntimeHubHostStatus(MobaflowSyncDiagnostics diagnostics, SyncSetup setup)
    {
        var isHealthy = IsRuntimeHubHostHealthy(diagnostics);
        var isPending = !isHealthy && setup != SyncSetup.Done;
        var waiting = isPending && setup == SyncSetup.Connecting
            ? WaitingTimeSince(ref _runtimeHubHostWaitingSince)
            : StopWaiting(ref _runtimeHubHostWaitingSince);

        RuntimeHubHostIsHealthy = isHealthy;
        RuntimeHubHostIsPending = isPending;
        RuntimeHubHostWaitingTime = FormatWaitingTime(waiting);
        RuntimeHubHostStatusText = isPending
            ? DescribePending(setup, diagnostics.HostClientConnected ? "Registering with the REST API..." : "Connecting...")
            : DescribeRuntimeHubHost(diagnostics, isHealthy);
    }

    private static string DescribeRuntimeHubHost(MobaflowSyncDiagnostics diagnostics, bool isHealthy)
    {
        if (!diagnostics.RestApiReachable)
            return "REST API unreachable";
        if (isHealthy)
            return "Connected";
        if (diagnostics.HostClientConnected)
            return "Host not registered on server";
        return diagnostics.HostConnectPending ? "Host client disconnected, retrying" : "Host client disconnected";
    }

    private void UpdateHubBroadcastStatus(MobaflowSyncDiagnostics diagnostics, SyncSetup setup)
    {
        var hostIsHealthy = IsRuntimeHubHostHealthy(diagnostics);
        HubBroadcastIsHealthy = diagnostics.LastHubPushSucceeded && IsRecent(diagnostics.LastHubPushAt, SyncFreshnessWindow);
        HubBroadcastIsPending = !HubBroadcastIsHealthy
            && setup != SyncSetup.Done
            && (!hostIsHealthy || !diagnostics.LastHubPushAt.HasValue);
        HubBroadcastStatusText = HubBroadcastIsPending
            ? DescribePending(setup, hostIsHealthy ? "Waiting for the first snapshot push..." : "Waiting for the RuntimeHub host...")
            : DescribeHubBroadcast(diagnostics);
    }

    private static string DescribeHubBroadcast(MobaflowSyncDiagnostics diagnostics)
    {
        if (!diagnostics.LastHubPushAt.HasValue)
            return "No snapshot push yet";
        return diagnostics.LastHubPushSucceeded ? FormatLastPush(diagnostics.LastHubPushAt) : "Last push failed";
    }

    private void UpdateRemoteSnapshotStatus(MobaflowSyncDiagnostics diagnostics)
    {
        RemoteSnapshotIsHealthy = diagnostics.RemoteClientCount > 0 && diagnostics.SessionOperational;
        RemoteSnapshotStatusText = DescribeRemoteSnapshot(diagnostics);
    }

    private static string DescribeRemoteSnapshot(MobaflowSyncDiagnostics diagnostics)
    {
        if (diagnostics.RemoteClientCount == 0)
            return "No remote clients";
        var session = diagnostics.SessionOperational ? "session active" : "session inactive";
        return $"{diagnostics.RemoteClientCount} remote client(s), {session}";
    }

    private void UpdateRuntimeSnapshotStatus(MobaflowSyncDiagnostics diagnostics)
    {
        RuntimeSnapshotIsHealthy = IsRecent(diagnostics.LocalSnapshotCreatedAt, SyncFreshnessWindow)
            || (diagnostics.Z21Connected && diagnostics.HasActiveProject);
        RuntimeSnapshotStatusText = RuntimeSnapshotIsHealthy
            ? $"Updated {FormatAge(diagnostics.LocalSnapshotCreatedAt)}, {diagnostics.LocalSignalBoxElementCount} signals, {diagnostics.LocalLocomotiveFleetCount} locos"
            : "No recent snapshot";
    }

    private void UpdateRestCacheStatus(MobaflowSyncDiagnostics diagnostics, SyncSetup setup)
    {
        RestCacheIsHealthy = diagnostics.RestCacheAvailable
            && diagnostics.RestCacheUpdatedAt.HasValue
            && IsRestCacheFresh(diagnostics);
        RestCacheIsPending = !RestCacheIsHealthy && setup != SyncSetup.Done && !diagnostics.RestCacheAvailable;
        RestCacheStatusText = RestCacheIsPending
            ? DescribePending(setup, "Waiting for the first snapshot...")
            : DescribeRestCache(diagnostics);
    }

    private static string DescribeRestCache(MobaflowSyncDiagnostics diagnostics)
    {
        if (!diagnostics.RestCacheAvailable)
            return "No snapshot cached";
        if (!diagnostics.RestCacheUpdatedAt.HasValue)
            return "Cache has no timestamp";
        return $"Updated {FormatAge(diagnostics.RestCacheUpdatedAt)}, {diagnostics.RestCacheSignalBoxElementCount} signals, {diagnostics.RestCacheLocomotiveFleetCount} locos";
    }

    private void UpdateSolutionSyncStatus(MobaflowSyncDiagnostics diagnostics, SyncSetup setup)
    {
        SolutionSyncIsHealthy = diagnostics.SolutionAvailable && diagnostics.SolutionUpdatedAt.HasValue;
        SolutionSyncIsPending = !SolutionSyncIsHealthy && setup != SyncSetup.Done && !diagnostics.SolutionAvailable;
        SolutionSyncStatusText = SolutionSyncIsPending
            ? DescribePending(setup, "Waiting for the solution...")
            : DescribeSolutionSync(diagnostics);
    }

    private static string DescribeSolutionSync(MobaflowSyncDiagnostics diagnostics)
    {
        if (!diagnostics.SolutionAvailable)
            return "No solution cached";
        if (!diagnostics.SolutionUpdatedAt.HasValue)
            return "Solution cache has no timestamp";
        return $"Updated {FormatAge(diagnostics.SolutionUpdatedAt)}, project {diagnostics.SolutionActiveProjectName ?? "—"}";
    }

    private void UpdateSolutionLoadedStatus()
    {
        var currentSolutionPath = _session.CurrentSolutionPath;
        if (string.IsNullOrWhiteSpace(currentSolutionPath))
        {
            SolutionLoadedIsHealthy = false;
            SolutionLoadedStatusText = _session.HasSolution
                ? "In-memory only, no solution file loaded"
                : "No solution loaded";
            return;
        }

        SolutionLoadedIsHealthy = true;
        var fileName = Path.GetFileName(currentSolutionPath);
        var projectName = _session.SelectedProject?.Name;
        SolutionLoadedStatusText = string.IsNullOrWhiteSpace(projectName)
            ? $"Loaded from {fileName}"
            : $"Loaded from {fileName}, project {projectName}";
    }

    private void OnSessionPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ISolutionSession.CurrentSolutionPath) or nameof(ISolutionSession.SelectedProject))
            UpdateSolutionLoadedStatus();
    }

    private static bool IsRestCacheFresh(MobaflowSyncDiagnostics diagnostics)
    {
        if (!diagnostics.RestCacheUpdatedAt.HasValue)
            return false;

        var cacheUpdatedAt = diagnostics.RestCacheUpdatedAt.Value.ToUniversalTime();
        var localCreatedAt = diagnostics.LocalSnapshotCreatedAt.ToUniversalTime();
        return cacheUpdatedAt + RestCacheTolerance >= localCreatedAt;
    }

    private static bool IsRecent(DateTimeOffset? timestamp, TimeSpan window) =>
        timestamp.HasValue && IsRecent(timestamp.Value, window);

    private static bool IsRecent(DateTimeOffset timestamp, TimeSpan window) =>
        DateTimeOffset.UtcNow - timestamp.ToUniversalTime() <= window;

    private static string FormatLastPush(DateTimeOffset? timestamp) => $"Last push {FormatAge(timestamp)}";

    private static string FormatAge(DateTimeOffset? timestamp)
    {
        if (!timestamp.HasValue)
            return "unknown";

        var age = DateTimeOffset.UtcNow - timestamp.Value.ToUniversalTime();
        if (age < TimeSpan.FromSeconds(5))
            return "just now";

        if (age < TimeSpan.FromMinutes(1))
            return $"{(int)age.TotalSeconds} s ago";

        if (age < TimeSpan.FromHours(1))
            return $"{(int)age.TotalMinutes} min ago";

        return $"{(int)age.TotalHours} h ago";
    }

    /// <summary>Starts the waiting time on the first pending update and returns how long the row has waited.</summary>
    private TimeSpan? WaitingTimeSince(ref DateTimeOffset? waitingSince)
    {
        var now = _timeProvider.GetUtcNow();
        waitingSince ??= now;
        return now - waitingSince.Value;
    }

    private static TimeSpan? StopWaiting(ref DateTimeOffset? waitingSince)
    {
        waitingSince = null;
        return null;
    }

    private static string FormatWaitingTime(TimeSpan? waiting)
    {
        if (waiting is not { } elapsed || elapsed < TimeSpan.FromSeconds(1))
            return string.Empty;

        return elapsed < TimeSpan.FromMinutes(1)
            ? $"({(int)elapsed.TotalSeconds} s)"
            : $"({(int)elapsed.TotalMinutes} min {elapsed.Seconds} s)";
    }

    /// <summary>What the synchronization rows still wait for.</summary>
    private enum SyncSetup
    {
        /// <summary>Nothing: an unhealthy row reports its problem.</summary>
        Done,

        /// <summary>The REST API is starting.</summary>
        WaitingForRestApi,

        /// <summary>The REST API answers and the host still connects the synchronization.</summary>
        Connecting
    }

    #endregion

    /// <summary>Detaches the event bus, settings and session subscriptions; the status keeps its last values.</summary>
    public void Dispose()
    {
        if (_isDisposed)
            return;

        _isDisposed = true;
        foreach (var subscriptionId in _eventBusSubscriptions)
            _eventBus.Unsubscribe(subscriptionId);
        _eventBusSubscriptions.Clear();
        _settingsPage.PropertyChanged -= OnSettingsPagePropertyChanged;
        _session.PropertyChanged -= OnSessionPropertyChanged;
    }
}
