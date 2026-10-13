// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using Backend.Interface;

using Common.Configuration;
using Common.Events;
using Common.Extension;
using Common.Runtime;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Interface;

using Microsoft.Extensions.Logging;

using Mobile;

using Service;

using System.Collections.ObjectModel;

/// <summary>
/// Mobile-optimized ViewModel for MAUI - focused on Z21 monitoring and feedback statistics.
/// </summary>
public sealed partial class MauiViewModel : ObservableObject, IDisposable
{
    private readonly IRuntimeSnapshotProvider _runtimeSnapshots;
    private readonly IConnectionRuntime _runtimeConnection;
    private readonly ITrafficMonitor _trafficMonitor;
    private readonly IUiDispatcher _uiDispatcher;
    private readonly AppSettings _settings;
    private readonly ISettingsService _settingsService;
    private readonly MobaflowEndpointLocator _endpointLocator;
    private readonly MobaflowHubSession _hubSession;
    private readonly MobileRuntimeProjection _runtimeProjection;
    private readonly MobileSolutionSynchronizer _solutionSynchronizer;
    private readonly MobilePhotoUploader _photoUploader;
    private readonly IRuntimeHubRemoteClient? _runtimeHubRemoteClient;
    private readonly IRuntimeCommandGateway _runtimeCommandGateway;
    private readonly IMobileRuntimeCoordinator? _mobileRuntimeCoordinator;
    private readonly IBackgroundService? _backgroundService;
    private readonly INetworkProfileChangeNotifier _networkProfileChangeNotifier;
    private readonly ILogger<MauiViewModel> _logger;
    private readonly IEventBus _eventBus;
    private readonly List<Guid> _eventBusSubscriptions = [];

    private readonly object _networkChangeDebounceLock = new();
    private CancellationTokenSource? _networkChangeDebounceCts;

    /// <summary>Cancels long-running background loops when the host app is shutting down.</summary>
    private readonly CancellationTokenSource _applicationLifetimeCts = new();

    /// <summary>Coalesces rapid platform connectivity notifications before re-running REST discovery.</summary>
    private const int NetworkChangeDebounceMilliseconds = 750;

    private readonly SemaphoreSlim _initializationLock = new(1, 1);
    private readonly SemaphoreSlim _refreshRestApiLock = new(1, 1);
    private Task? _initializationTask;
    private Task? _startupDiscoveryTask;
    private Task? _restApiHealthCheckTask;
    private bool _isApplyingLoadedSettings;
    private bool _isStopping;

    /// <summary>
    /// Initializes a new instance of the <see cref="MauiViewModel"/> class for the MAUI mobile client.
    /// </summary>
    /// <param name="runtimeSnapshots">Read access to runtime snapshots.</param>
    /// <param name="runtimeConnection">Runtime lifecycle and Z21 connection.</param>
    /// <param name="trafficMonitor">Z21 traffic monitor and polling diagnostics.</param>
    /// <param name="runtimeCommandGateway">Route for every operator command (local, recorded or remote).</param>
    /// <param name="uiDispatcher">Dispatcher used to marshal updates back to the MAUI UI thread.</param>
    /// <param name="settings">Application settings used to initialize default values.</param>
    /// <param name="settingsService">Service used to persist updated settings.</param>
    /// <param name="endpointLocator">Finds and remembers the MOBApi endpoint on the LAN.</param>
    /// <param name="hubSession">MOBApi health check, client registration and runtime hub connection.</param>
    /// <param name="runtimeProjection">Locomotive fleet and signal-box data of the selected project.</param>
    /// <param name="solutionSynchronizer">Brings MOBAflow's solution to the phone.</param>
    /// <param name="photoUploader">Takes a photo and uploads it to MOBAflow.</param>
    /// <param name="networkProfileChangeNotifier">Raises when device connectivity changes so cached LAN REST endpoints can be re-resolved.</param>
    /// <param name="logger">Logger for diagnostics.</param>
    /// <param name="eventBus">Event bus used to observe runtime and feedback changes.</param>
    /// <param name="runtimeHubRemoteClient">Optional: SignalR client for remote runtime snapshots (MOBAsmart).</param>
    /// <param name="mobileRuntimeCoordinator">Optional: MOBAsmart hybrid local/remote runtime routing.</param>
    /// <param name="projectContext">Optional: synced MOBAflow solution for Control tab fleet list (MOBAsmart).</param>
    /// <param name="backgroundService">Optional: Android foreground service for background keep-alive (MOBAsmart).</param>
    public MauiViewModel(
        IRuntimeSnapshotProvider runtimeSnapshots,
        IConnectionRuntime runtimeConnection,
        ITrafficMonitor trafficMonitor,
        IRuntimeCommandGateway runtimeCommandGateway,
        IUiDispatcher uiDispatcher,
        AppSettings settings,
        ISettingsService settingsService,
        MobaflowEndpointLocator endpointLocator,
        MobaflowHubSession hubSession,
        MobileRuntimeProjection runtimeProjection,
        MobileSolutionSynchronizer solutionSynchronizer,
        MobilePhotoUploader photoUploader,
        INetworkProfileChangeNotifier networkProfileChangeNotifier,
        ILogger<MauiViewModel> logger,
        IEventBus eventBus,
        IRuntimeHubRemoteClient? runtimeHubRemoteClient = null,
        IMobileRuntimeCoordinator? mobileRuntimeCoordinator = null,
        IProjectContext? projectContext = null,
        IBackgroundService? backgroundService = null)
    {
        ArgumentNullException.ThrowIfNull(runtimeSnapshots);
        ArgumentNullException.ThrowIfNull(runtimeConnection);
        ArgumentNullException.ThrowIfNull(trafficMonitor);
        ArgumentNullException.ThrowIfNull(runtimeCommandGateway);
        ArgumentNullException.ThrowIfNull(uiDispatcher);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(settingsService);
        ArgumentNullException.ThrowIfNull(endpointLocator);
        ArgumentNullException.ThrowIfNull(hubSession);
        ArgumentNullException.ThrowIfNull(runtimeProjection);
        ArgumentNullException.ThrowIfNull(solutionSynchronizer);
        ArgumentNullException.ThrowIfNull(photoUploader);
        ArgumentNullException.ThrowIfNull(networkProfileChangeNotifier);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(eventBus);
        _runtimeSnapshots = runtimeSnapshots;
        _runtimeConnection = runtimeConnection;
        _trafficMonitor = trafficMonitor;
        _uiDispatcher = uiDispatcher;
        _settings = settings;
        _settingsService = settingsService;
        _endpointLocator = endpointLocator;
        _hubSession = hubSession;
        _runtimeProjection = runtimeProjection;
        _solutionSynchronizer = solutionSynchronizer;
        _photoUploader = photoUploader;
        _networkProfileChangeNotifier = networkProfileChangeNotifier;
        _logger = logger;
        _runtimeHubRemoteClient = runtimeHubRemoteClient;
        _runtimeCommandGateway = runtimeCommandGateway;
        _mobileRuntimeCoordinator = mobileRuntimeCoordinator;
        _backgroundService = backgroundService;
        _eventBus = eventBus;
        _projectContext = projectContext;

        _eventBusSubscriptions.Add(_eventBus.Subscribe<RuntimeSnapshotChangedEvent>(OnRuntimeSnapshotChanged));
        _eventBusSubscriptions.Add(_eventBus.Subscribe<SolutionSyncedEvent>(OnSolutionSyncedForSignalBox));
        _eventBusSubscriptions.Add(_eventBus.Subscribe<SolutionSyncedEvent>(OnSolutionSyncedForControlTab));

        ProjectNames = [];
        SelectedProjectIndex = -1;
        WireProjectContextForControlTab();

        if (_runtimeHubRemoteClient != null)
        {
            _eventBusSubscriptions.Add(_eventBus.Subscribe<RemoteRuntimeSnapshotChangedEvent>(OnRemoteRuntimeSnapshotChanged));
            _runtimeHubRemoteClient.SessionStateChanged += OnRuntimeHubSessionStateChangedAsync;
            _runtimeHubRemoteClient.SolutionUpdated += OnRuntimeHubSolutionUpdatedAsync;
        }
    }

    /// <summary>
    /// Initializes the mobile view model after the MAUI page has appeared.
    /// </summary>
    /// <param name="cancellationToken">Cancels the initialization wait.</param>
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        Task initializationTask;

        await _initializationLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            _initializationTask ??= InitializeCoreAsync();
            initializationTask = _initializationTask;
        }
        finally
        {
            _initializationLock.Release();
        }

        await initializationTask.WaitAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task InitializeCoreAsync()
    {
        await _uiDispatcher.InvokeOnUiAsync(() =>
        {
            LoadSettingsIntoViewModel();
            return Task.CompletedTask;
        }).ConfigureAwait(false);

        await _runtimeConnection.StartAsync(_applicationLifetimeCts.Token).ConfigureAwait(false);

        _networkProfileChangeNotifier.NetworkProfilePossiblyChanged += OnNetworkProfilePossiblyChanged;
        _networkProfileChangeNotifier.StartListening();

        await _uiDispatcher.InvokeOnUiAsync(() =>
        {
            ApplyLocalRuntimeSnapshot(_runtimeSnapshots.Current);
            InitializeStatistics();
            return Task.CompletedTask;
        }).ConfigureAwait(false);

        _startupDiscoveryTask ??= TryAutoDiscoverEndpointsAsync();
        _restApiHealthCheckTask ??= RestApiHealthCheckLoopAsync();

        if (IsMobaflowConnectionEnabled && HasStoredMobaflowEndpoint())
        {
            _mobaflowConnectCts = CancellationTokenSource.CreateLinkedTokenSource(_applicationLifetimeCts.Token);
            RunInBackground(
                ConnectToStoredEndpointAsync(_mobaflowConnectCts.Token),
                "Restore MOBAflow session on startup");
        }

        _trafficMonitor.SetSystemStatePollingInterval(5);
    }

    /// <summary>
    /// Runs LAN discovery for MOBApi (REST) and connects to the Z21 of the selected project. REST discovery is awaited
    /// (with retries), not fire-and-forget, so we do not health-check a stale saved IP before discovery runs. The phone
    /// never searches for a Z21: the selected project names it.
    /// </summary>
    private async Task TryAutoDiscoverEndpointsAsync()
    {
        try
        {
            // Short delay for network stack (especially on Android)
            await Task.Delay(TimeSpan.FromMilliseconds(500), _applicationLifetimeCts.Token).ConfigureAwait(false);

            Task? restDiscoveryTask = null;
            if (IsMobaflowConnectionEnabled && !HasStoredMobaflowEndpoint())
            {
                restDiscoveryTask = RestDiscoveryLoopAsync();

                // Fast REST discovery, anchored on the subnet of the selected project's Z21.
                var fast = await _endpointLocator
                    .DiscoverAsync(ToDiscoveryAnchor(Z21IpAddress), fullScan: false, _applicationLifetimeCts.Token)
                    .ConfigureAwait(false);
                if (fast is not null)
                {
                    await ApplyDiscoveredRestEndpointAsync(fast).ConfigureAwait(false);
                }
            }

            // Let runtime auto-connect try the selected project's Z21 first.
            await Task.Delay(TimeSpan.FromMilliseconds(1500), _applicationLifetimeCts.Token).ConfigureAwait(false);

            if (!_runtimeSnapshots.Current.IsConnected)
            {
                await _uiDispatcher.InvokeOnUiAsync(async () =>
                {
                    if (!IsConnected && !string.IsNullOrWhiteSpace(Z21IpAddress))
                    {
                        await ConnectCommand.ExecuteAsync(null).ConfigureAwait(false);
                    }
                }).ConfigureAwait(false);
            }

            if (restDiscoveryTask != null)
            {
                await restDiscoveryTask.ConfigureAwait(false);
            }

            if (IsMobaflowConnectionEnabled
                && !HasStoredMobaflowEndpoint()
                && !IsRestApiReachable
                && IsConnected
                && !string.IsNullOrWhiteSpace(Z21IpAddress))
            {
                await DiscoverRestApiWithAnchorAsync(Z21IpAddress).ConfigureAwait(false);
            }

            // First reachability update after startup discovery (avoids ctor-time check against obsolete REST IP)
            await RefreshRestApiReachableAsync(_applicationLifetimeCts.Token).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Auto-discovery failed");
        }
    }

    private async Task RestDiscoveryLoopAsync()
    {
        var found = await _endpointLocator
            .DiscoverWithRetriesAsync(() => ToDiscoveryAnchor(Z21IpAddress), _applicationLifetimeCts.Token)
            .ConfigureAwait(false);
        if (found is not null)
        {
            await ApplyDiscoveredRestEndpointAsync(found).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Shows a discovered endpoint (the locator already saved it) and checks whether MOBApi is reachable there.
    /// </summary>
    private async Task ApplyDiscoveredRestEndpointAsync(MobaflowEndpoint endpoint)
    {
        _uiDispatcher.InvokeOnUi(() =>
        {
            RestApiIpAddress = endpoint.IpAddress;
            RestApiPort = endpoint.Port;
        });

        await RefreshRestApiReachableAsync(_applicationLifetimeCts.Token).ConfigureAwait(false);
    }

    /// <summary>The subnet of the selected project's Z21 anchors the LAN discovery; null without a Z21.</summary>
    private static string? ToDiscoveryAnchor(string? z21IpAddress) =>
        string.IsNullOrWhiteSpace(z21IpAddress) ? null : z21IpAddress.Trim();

    private void OnNetworkProfilePossiblyChanged(object? sender, EventArgs e)
    {
        _ = sender;
        _ = e;
        CancellationToken token;
        lock (_networkChangeDebounceLock)
        {
            _networkChangeDebounceCts?.Cancel();
            _networkChangeDebounceCts?.Dispose();
            _networkChangeDebounceCts = new CancellationTokenSource();
            token = _networkChangeDebounceCts.Token;
        }

        RunInBackground(RunDebouncedNetworkChangeAsync(token), "Debounced network profile handling");
    }

    private async Task RunDebouncedNetworkChangeAsync(CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(NetworkChangeDebounceMilliseconds, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        try
        {
            if (!IsMobaflowConnectionEnabled)
            {
                await RefreshRestApiReachableAsync(cancellationToken).ConfigureAwait(false);
                return;
            }

            await RefreshRestApiReachableAsync(cancellationToken).ConfigureAwait(false);
            await MaybeDisableMobaflowConnectionWhenSessionLostAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "REST reachability refresh after network change failed");
        }
    }

    [RelayCommand]
    private Task RetryRestApiDiscoveryAsync()
    {
        if (!IsMobaflowConnectionEnabled)
        {
            IsMobaflowConnectionEnabled = true;
            return Task.CompletedTask;
        }

        RunInBackground(RetryMobaflowDiscoveryCoreAsync(), "Manual MOBAflow discovery retry");
        return Task.CompletedTask;
    }

    private async Task RetryMobaflowDiscoveryCoreAsync()
    {
        _endpointLocator.ResetDiscoveryCadence();
        await DiscoverMobaflowEndpointAsync(fullScan: true, ToDiscoveryAnchor(Z21IpAddress), _applicationLifetimeCts.Token).ConfigureAwait(false);
        await RefreshRestApiReachableAsync(_applicationLifetimeCts.Token, useConnectTimeout: false).ConfigureAwait(false);
        if (IsRestApiReachable)
        {
            await EnsureRuntimeHubConnectionAsync(true, forceReconnect: true).ConfigureAwait(false);
        }
    }

    private async Task DiscoverRestApiWithAnchorAsync(string? subnetAnchorIp)
    {
        var found = await _endpointLocator
            .DiscoverFullAsync(subnetAnchorIp, _applicationLifetimeCts.Token)
            .ConfigureAwait(false);
        if (found is not null)
        {
            await ApplyDiscoveredRestEndpointAsync(found).ConfigureAwait(false);
            return;
        }

        await RefreshRestApiReachableAsync(_applicationLifetimeCts.Token).ConfigureAwait(false);
    }

    /// <summary>
    /// Loads settings from AppSettings singleton into ViewModel properties.
    /// Called during constructor after SettingsService has loaded the file.
    /// </summary>
    private void LoadSettingsIntoViewModel()
    {
        _isApplyingLoadedSettings = true;
        try
        {
            var countOfFeedbackPoints = Math.Max(_settings.Counter.CountOfFeedbackPoints, 1);
            if (_settings.Counter.CountOfFeedbackPoints != countOfFeedbackPoints)
            {
                _settings.Counter.CountOfFeedbackPoints = countOfFeedbackPoints;
            }

            _logger.LogDebug(
                "LoadSettingsIntoViewModel: CountOfFeedbackPoints={Count}, TargetLapCount={Target}, UseTimerFilter={UseTimer}, TimerIntervalSeconds={Interval}",
                _settings.Counter.CountOfFeedbackPoints,
                _settings.Counter.TargetLapCount,
                _settings.Counter.UseTimerFilter,
                _settings.Counter.TimerIntervalSeconds);

            // REST API: load from settings as fallback so REST connect works when discovery fails
            if (!string.IsNullOrWhiteSpace(_settings.RestApi.CurrentIpAddress) && _settings.RestApi.Port > 0)
            {
                RestApiIpAddress = _settings.RestApi.CurrentIpAddress.Trim();
                RestApiPort = _settings.RestApi.Port;
            }

            if (HasStoredMobaflowEndpoint() && _settings.RestApi.IsConnectionEnabled)
            {
                IsMobaflowConnectionEnabled = true;
            }
            else
            {
                IsMobaflowConnectionEnabled = false;
                _settings.RestApi.IsConnectionEnabled = false;
            }

            UpdateRuntimeCoordinatorState();
            CountOfFeedbackPoints = countOfFeedbackPoints;
            GlobalTargetLapCount = _settings.Counter.TargetLapCount;
            UseTimerFilter = _settings.Counter.UseTimerFilter;
            TimerIntervalSeconds = _settings.Counter.TimerIntervalSeconds;

            _logger.LogDebug(
                "Settings applied to ViewModel: RestApi={RestIp}:{RestPort}, Z21={Z21}, CountOfFeedbackPoints={Count}, GlobalTargetLapCount={Laps}, UseTimerFilter={Timer}, TimerIntervalSeconds={Interval}",
                RestApiIpAddress,
                RestApiPort,
                Z21IpAddress,
                CountOfFeedbackPoints,
                GlobalTargetLapCount,
                UseTimerFilter,
                TimerIntervalSeconds);
        }
        finally
        {
            _isApplyingLoadedSettings = false;
        }
    }

    #region REST-API Connection

    [ObservableProperty]
    private string _restApiIpAddress = string.Empty;

    [ObservableProperty]
    private int _restApiPort = 5001;

    /// <summary>
    /// True when the REST API (WebApp/WinUI) is reachable via HealthCheck.
    /// </summary>
    [ObservableProperty]
    private bool _isRestApiReachable;

    partial void OnRestApiIpAddressChanged(string value)
    {
        _ = value;
        RunInBackground(RefreshRestApiReachableAsync(_applicationLifetimeCts.Token), "Refresh REST reachability (IP changed)");
    }

    partial void OnRestApiPortChanged(int value)
    {
        _ = value;
        RunInBackground(RefreshRestApiReachableAsync(_applicationLifetimeCts.Token), "Refresh REST reachability (port changed)");
    }

    /// <summary>
    /// Starts the periodic REST API health check loop (runs every 30s).
    /// Call once after construction.
    /// </summary>
    internal void StartRestApiHealthCheckLoop()
    {
        _restApiHealthCheckTask ??= RestApiHealthCheckLoopAsync();
    }

    /// <summary>
    /// Signals background tasks (REST health loop) to stop; call from the host before tearing down services.
    /// </summary>
    public void NotifyApplicationStopping()
    {
        if (_isStopping)
        {
            return;
        }

        _isStopping = true;

        RunInBackground(StopBackgroundServiceAsync(), "Stop foreground background service on shutdown");

        try
        {
            _applicationLifetimeCts.Cancel();
        }
        catch (ObjectDisposedException)
        {
            // Already disposed.
        }

        _networkProfileChangeNotifier.NetworkProfilePossiblyChanged -= OnNetworkProfilePossiblyChanged;
        _networkProfileChangeNotifier.StopListening();

        lock (_networkChangeDebounceLock)
        {
            _networkChangeDebounceCts?.Cancel();
            _networkChangeDebounceCts?.Dispose();
            _networkChangeDebounceCts = null;
        }

        foreach (var subscriptionId in _eventBusSubscriptions)
        {
            _eventBus.Unsubscribe(subscriptionId);
        }

        _eventBusSubscriptions.Clear();
    }

    public void Dispose()
    {
        if (_runtimeHubRemoteClient != null)
        {
            _runtimeHubRemoteClient.SessionStateChanged -= OnRuntimeHubSessionStateChangedAsync;
            _runtimeHubRemoteClient.SolutionUpdated -= OnRuntimeHubSolutionUpdatedAsync;
        }

        NotifyApplicationStopping();
        _applicationLifetimeCts.Dispose();
        _initializationLock.Dispose();
        _refreshRestApiLock.Dispose();
        _mobaflowConnectCts?.Cancel();
        _mobaflowConnectCts?.Dispose();
        _mobaflowConnectCts = null;
    }

    private async Task RestApiHealthCheckLoopAsync()
    {
        try
        {
            while (!_applicationLifetimeCts.IsCancellationRequested)
            {
                if (_settings.RestApi.IsConnectionEnabled)
                {
                    if (!IsRestApiReachable)
                    {
                        await TryPeriodicRestDiscoveryIfNeededAsync().ConfigureAwait(false);
                    }

                    await RefreshRestApiReachableAsync(_applicationLifetimeCts.Token).ConfigureAwait(false);
                    await MaybeDisableMobaflowConnectionWhenSessionLostAsync().ConfigureAwait(false);
                }

                var delay = _endpointLocator.GetHealthCheckDelay(
                    _settings.RestApi.IsConnectionEnabled,
                    IsRestApiReachable && _hubSession.IsConnected);
                await Task.Delay(delay, _applicationLifetimeCts.Token).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when the MAUI host cancels the lifetime token during shutdown.
        }
    }

    private async Task TryPeriodicRestDiscoveryIfNeededAsync()
    {
        if (!IsMobaflowConnectionEnabled || IsRestApiReachable)
        {
            return;
        }

        // Only a connected Z21 confirms the subnet the phone is on.
        var found = await _endpointLocator
            .DiscoverWhenDueAsync(IsConnected ? ToDiscoveryAnchor(Z21IpAddress) : null, _applicationLifetimeCts.Token)
            .ConfigureAwait(false);
        if (found is not null)
        {
            await ApplyDiscoveredRestEndpointAsync(found).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Checks REST API reachability and updates IsRestApiReachable on the UI thread.
    /// </summary>
    private async Task<bool> RefreshRestApiReachableAsync(
        CancellationToken cancellationToken,
        bool useConnectTimeout = true)
    {
        await _refreshRestApiLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return await RefreshRestApiReachableCoreAsync(useConnectTimeout).ConfigureAwait(false);
        }
        finally
        {
            _refreshRestApiLock.Release();
        }
    }

    private async Task<bool> RefreshRestApiReachableCoreAsync(bool useConnectTimeout = true)
    {
        if (!_settings.RestApi.IsConnectionEnabled)
        {
            await _uiDispatcher.InvokeOnUiAsync(() =>
            {
                IsRestApiReachable = false;
                UpdateRuntimeCoordinatorState();
                return Task.CompletedTask;
            }).ConfigureAwait(false);
            await EnsureRuntimeHubConnectionAsync(false).ConfigureAwait(false);
            return false;
        }

        var serverIp = _settings.RestApi.CurrentIpAddress?.Trim() ?? string.Empty;
        var serverPort = _settings.RestApi.Port;
        if (string.IsNullOrWhiteSpace(serverIp) || serverPort <= 0)
        {
            if (_settings.RestApi.IsConnectionEnabled && !_mobaflowConnectInProgress)
            {
                await DiscoverMobaflowEndpointAsync(false, ToDiscoveryAnchor(Z21IpAddress), _applicationLifetimeCts.Token).ConfigureAwait(false);
                serverIp = _settings.RestApi.CurrentIpAddress?.Trim() ?? string.Empty;
                serverPort = _settings.RestApi.Port;
            }

            if (string.IsNullOrWhiteSpace(serverIp) || serverPort <= 0)
            {
                await _uiDispatcher.InvokeOnUiAsync(() =>
                {
                    IsRestApiReachable = false;
                    return Task.CompletedTask;
                }).ConfigureAwait(false);
                return false;
            }
        }

        try
        {
            var endpoint = new MobaflowEndpoint(serverIp, serverPort);
            var reachable = await _hubSession
                .CheckHealthAsync(endpoint, connecting: useConnectTimeout && _mobaflowConnectInProgress)
                .ConfigureAwait(false);
            await _uiDispatcher.InvokeOnUiAsync(() =>
            {
                IsRestApiReachable = reachable;
                RestApiIpAddress = serverIp;
                RestApiPort = serverPort;
                return Task.CompletedTask;
            }).ConfigureAwait(false);

            if (reachable)
            {
                await _endpointLocator.RememberReachableAddressAsync(serverIp).ConfigureAwait(false);
            }

            if (reachable)
            {
                RunInBackground(_hubSession.RegisterClientWhenDueAsync(endpoint), "REST API client registration");
            }

            if (reachable)
            {
                RunInBackground(
                    _solutionSynchronizer.SyncIfNeededAsync(endpoint, _applicationLifetimeCts.Token),
                    "Solution sync from MOBApi");
            }

            await EnsureRuntimeHubConnectionAsync(reachable).ConfigureAwait(false);
            await UpdateRuntimeCoordinatorStateOnUiAsync().ConfigureAwait(false);
            return reachable;
        }
        catch
        {
            await _uiDispatcher.InvokeOnUiAsync(() =>
            {
                IsRestApiReachable = false;
                return Task.CompletedTask;
            }).ConfigureAwait(false);
            await EnsureRuntimeHubConnectionAsync(false).ConfigureAwait(false);
            return false;
        }
    }

    private async Task<bool> EnsureRuntimeHubConnectionAsync(bool reachable, bool forceReconnect = false)
    {
        var outcome = await _hubSession
            .EnsureConnectedAsync(reachable, forceReconnect, SelectRemoteProjectAsync, _applicationLifetimeCts.Token)
            .ConfigureAwait(false);
        if (outcome.HubConnected is { } hubConnected)
        {
            await SetRuntimeHubConnectedOnUiAsync(hubConnected).ConfigureAwait(false);
        }

        if (outcome.RemoteZ21Connected is { } remoteZ21Connected)
        {
            await SetRemoteZ21ConnectedOnUiAsync(remoteZ21Connected).ConfigureAwait(false);
        }

        return outcome.IsConnected;
    }

    #endregion

    #region Z21 Connection

    [ObservableProperty]
    private string _z21IpAddress = string.Empty;

    [ObservableProperty]
    private bool _isConnected;

    /// <summary>
    /// Short status/error message for Z21 connection (e.g. "Connecting...", "Connection failed: ...").
    /// </summary>
    [ObservableProperty]
    private string? _z21ConnectionStatus;

    [ObservableProperty]
    private bool _isTrackPowerOn;

    [ObservableProperty]
    private int _mainCurrent;

    [ObservableProperty]
    private int _temperature;

    [ObservableProperty]
    private int _supplyVoltage;

    [ObservableProperty]
    private int _vccVoltage;

    [RelayCommand]
    private async Task ConnectAsync()
    {
        if (string.IsNullOrWhiteSpace(Z21IpAddress))
        {
            _uiDispatcher.InvokeOnUi(() => Z21ConnectionStatus = "No Z21 assigned to the selected project");
            return;
        }

        _uiDispatcher.InvokeOnUi(() => Z21ConnectionStatus = "Connecting...");

        try
        {
            await _runtimeConnection.ConnectAsync(CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _uiDispatcher.InvokeOnUi(() => Z21ConnectionStatus = $"Connection failed: {ex.Message}");
            _logger.LogWarning(ex, "Z21 connection failed");
        }
    }

    [RelayCommand]
    private async Task DisconnectAsync()
    {
        _shouldReconnectLocalZ21OnResume = false;
        await _runtimeConnection.DisconnectAsync(CancellationToken.None).ConfigureAwait(false);
        _uiDispatcher.InvokeOnUi(() => Z21ConnectionStatus = null);
        RequestBackgroundServiceSync();
    }

    private bool _hideZ21TelemetryUntilTrackPowerOff;

    [RelayCommand]
    private async Task SetTrackPowerAsync(bool turnOn)
    {
        if (!turnOn)
        {
            _hideZ21TelemetryUntilTrackPowerOff = true;
            _uiDispatcher.InvokeOnUi(ClearZ21TelemetryValues);
        }
        else
        {
            _hideZ21TelemetryUntilTrackPowerOff = false;
        }

        await _runtimeCommandGateway.SetTrackPowerAsync(turnOn, CancellationToken.None).ConfigureAwait(false);
    }

    private void ClearZ21TelemetryValues()
    {
        MainCurrent = 0;
        Temperature = 0;
        SupplyVoltage = 0;
        VccVoltage = 0;
    }

    #endregion

    #region Feedback Statistics

    [ObservableProperty]
    private ObservableCollection<InPortStatistic> _statistics = [];

    [ObservableProperty]
    private int _countOfFeedbackPoints = 3;

    [ObservableProperty]
    private int _globalTargetLapCount = 10;

    [ObservableProperty]
    private bool _useTimerFilter;

    [ObservableProperty]
    private double _timerIntervalSeconds = 2.0;

    private IReadOnlyList<InPortCounterSnapshot> _localCounters = [];

    [ObservableProperty]
    public partial string CounterResetError { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string CounterPersistenceError { get; set; } = string.Empty;

    partial void OnCountOfFeedbackPointsChanged(int value)
    {
        _logger.LogTrace("OnCountOfFeedbackPointsChanged: {Value}", value);
        _settings.Counter.CountOfFeedbackPoints = value;
        InitializeStatistics();
        DecrementFeedbackPointsCommand.NotifyCanExecuteChanged();
        QueueSaveSettings();
    }

    partial void OnGlobalTargetLapCountChanged(int value)
    {
        _logger.LogTrace("OnGlobalTargetLapCountChanged: {Value}", value);
        _settings.Counter.TargetLapCount = value;

        // Update all existing statistics
        foreach (var stat in Statistics)
        {
            stat.TargetLapCount = value;
        }

        DecrementTargetLapCountCommand.NotifyCanExecuteChanged();
        QueueSaveSettings();
    }

    partial void OnUseTimerFilterChanged(bool value)
    {
        _logger.LogTrace("OnUseTimerFilterChanged: {Value}", value);
        _settings.Counter.UseTimerFilter = value;
        DecrementTimerIntervalCommand.NotifyCanExecuteChanged();
        IncrementTimerIntervalCommand.NotifyCanExecuteChanged();
        QueueSaveSettings();
    }

    partial void OnTimerIntervalSecondsChanged(double value)
    {
        _logger.LogTrace("OnTimerIntervalSecondsChanged: {Value}", value);
        _settings.Counter.TimerIntervalSeconds = value;
        DecrementTimerIntervalCommand.NotifyCanExecuteChanged();
        QueueSaveSettings();
    }

    private void InitializeStatistics()
    {
        var updatedStatistics = new ObservableCollection<InPortStatistic>();
        for (int i = 1; i <= CountOfFeedbackPoints; i++)
        {
            var statistic = new InPortStatistic(_runtimeCommandGateway)
            {
                InPort = i,
                Name = $"Track {i}",
                Count = 0,
                TargetLapCount = GlobalTargetLapCount
            };
            updatedStatistics.Add(statistic);
        }

        _uiDispatcher.InvokeOnUi(() =>
        {
            // Replace the collection instance atomically to avoid MAUI BindableLayout reentrancy glitches.
            Statistics = updatedStatistics;
            ApplyLocalCounters();
        });

    }

    [RelayCommand]
    private async Task ResetCounters()
    {
        CounterResetError = string.Empty;
        try
        {
            await _runtimeCommandGateway.ResetInPortCountersAsync(CancellationToken.None).ConfigureAwait(true);

            _localCounters = _runtimeSnapshots.Current.InPortCounters;
            ApplyLocalCounters();
        }
        catch (Exception ex) when (ex is not OutOfMemoryException and not AccessViolationException)
        {
            // Report recoverable failures at the UI boundary; fatal process failures must propagate.
            CounterResetError = ex.Message;
            LogCounterResetFailed(_logger, ex);
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Resetting local InPort counters failed")]
    private static partial void LogCounterResetFailed(ILogger logger, Exception exception);

    [RelayCommand(CanExecute = nameof(CanDecrementFeedbackPoints))]
    private void DecrementFeedbackPoints()
    {
        if (CountOfFeedbackPoints > 1)
        {
            CountOfFeedbackPoints--;
        }
    }

    private bool CanDecrementFeedbackPoints() => CountOfFeedbackPoints > 1;

    [RelayCommand]
    private void IncrementFeedbackPoints()
    {
        CountOfFeedbackPoints++;
    }

    [RelayCommand(CanExecute = nameof(CanDecrementTargetLapCount))]
    private void DecrementTargetLapCount()
    {
        if (GlobalTargetLapCount > 1)
        {
            GlobalTargetLapCount--;
        }
    }

    private bool CanDecrementTargetLapCount() => GlobalTargetLapCount > 1;

    [RelayCommand]
    private void IncrementTargetLapCount()
    {
        GlobalTargetLapCount++;
    }

    [RelayCommand(CanExecute = nameof(CanDecrementTimerInterval))]
    private void DecrementTimerInterval()
    {
        if (TimerIntervalSeconds > 1.0)
        {
            TimerIntervalSeconds = Math.Round(TimerIntervalSeconds - 1.0, 1);
        }
    }

    private bool CanDecrementTimerInterval() => UseTimerFilter && TimerIntervalSeconds > 1.0;

    [RelayCommand(CanExecute = nameof(CanIncrementTimerInterval))]
    private void IncrementTimerInterval()
    {
        TimerIntervalSeconds = Math.Round(TimerIntervalSeconds + 1.0, 1);
    }

    private bool CanIncrementTimerInterval() => UseTimerFilter;

    /// <summary>
    /// Saves all settings to persistent storage.
    /// Called automatically when any counter setting changes.
    /// </summary>
    private async Task SaveSettingsAsync()
    {
        try
        {
            await _settingsService.SaveSettingsAsync(_settings).ConfigureAwait(false);
            _logger.LogDebug("Counter settings saved");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to save settings");
        }
    }

    private void QueueSaveSettings()
    {
        RunInBackground(SaveSettingsAsync(), "Persist MAUI settings");
    }

    private void RunInBackground(Task task, string operationName)
    {
        task.Observe(ex => _logger.LogWarning(ex, "{Operation} failed", operationName));
    }

    #endregion

    #region Runtime Event Handlers

    private void OnRuntimeSnapshotChanged(RuntimeSnapshotChangedEvent e)
    {
        ApplyLocalRuntimeSnapshot(e.Snapshot);
    }

    private void OnRemoteRuntimeSnapshotChanged(RemoteRuntimeSnapshotChangedEvent e)
    {
        ApplyRemoteRuntimeSnapshot(e.Snapshot);
    }

    private Task OnRuntimeHubSessionStateChangedAsync(bool isOperational)
    {
        _uiDispatcher.InvokeOnUiLowPriority(() =>
        {
            SetRemoteZ21Connected(isOperational);
            UpdateRuntimeCoordinatorState();

            if (!isOperational)
            {
                return;
            }

            if (_signalBoxTabActive)
            {
                RequestSignalBoxSnapshotRefresh();
            }

            if (_controlTabActive)
            {
                ApplyBestAvailableLocomotiveFleet();
                if (!_runtimeProjection.HasAnyLocomotiveFleetAvailable)
                {
                    RunInBackground(RequestSolutionSyncAsync(_applicationLifetimeCts.Token), "Solution sync after hub session restored");
                }
            }
        });
        return Task.CompletedTask;
    }

    private void ApplyLocalRuntimeSnapshot(MobaRuntimeSnapshot snapshot)
    {
        CounterPersistenceError = snapshot.InPortCounterPersistenceError ?? string.Empty;
        _localCounters = snapshot.InPortCounters;
        ApplyLocalCounters();
        var projection = RuntimeSnapshotProjector.ProjectMaui(snapshot);
        var status = projection.Status;

        IsConnected = status.IsConnected;
        IsTrackPowerOn = status.IsTrackPowerOn;

        if (!status.IsTrackPowerOn)
        {
            _hideZ21TelemetryUntilTrackPowerOff = false;
        }

        var showTelemetry = status.IsTrackPowerOn && !_hideZ21TelemetryUntilTrackPowerOff;
        if (showTelemetry)
        {
            MainCurrent = status.MainCurrent;
            Temperature = status.Temperature;
            SupplyVoltage = status.SupplyVoltage;
            VccVoltage = status.VccVoltage;
        }
        else
        {
            ClearZ21TelemetryValues();
        }

        Z21ConnectionStatus = projection.Z21ConnectionStatus;

        if (ShouldApplyLocalSignalBoxSnapshot())
        {
            RefreshSignalBoxElements(_runtimeProjection.MergeCachedAspects(snapshot.SignalBoxElements));
        }

        if (ShouldApplyLocalProjectSnapshot())
        {
            if (snapshot.LocomotiveFleet.Count > 0)
            {
                CacheRemoteLocomotiveFleet(snapshot.LocomotiveFleet);
            }

            ApplyBestAvailableLocomotiveFleet();
        }

        UpdateRuntimeCoordinatorState();
    }

    private bool ShouldApplyLocalProjectSnapshot()
    {
        return ShouldApplyLocalSignalBoxSnapshot();
    }

    private bool ShouldApplyLocalSignalBoxSnapshot()
    {
        if (_runtimeHubRemoteClient == null)
        {
            return true;
        }

        if (!IsMobaflowConnectionEnabled)
        {
            return true;
        }

        // When MOBAflow is enabled but the remote session is down, fall back to local/cached data.
        return _mobileRuntimeCoordinator?.PreferRemoteRuntime != true;
    }

    private void ApplyRemoteRuntimeSnapshot(MobaRuntimeSnapshot snapshot)
    {
        if (snapshot.SignalBoxElements.Count > 0)
        {
            var filteredElements = _runtimeProjection.FilterSignalBoxElementsToPlan(snapshot.SignalBoxElements);
            if (_runtimeProjection.ShouldCacheRemoteSignalBoxElements(filteredElements))
            {
                CacheRemoteSignalBoxElements(filteredElements);
            }

            ApplyBestAvailableSignalBoxElements();
        }

        if (snapshot.LocomotiveFleet.Count > 0)
        {
            if (_mobileRuntimeCoordinator?.PreferRemoteRuntime == true)
            {
                ApplyLiveRemoteLocomotiveFleet(snapshot.LocomotiveFleet);
            }
            else if (_runtimeProjection.ShouldCacheRemoteLocomotiveFleet(snapshot.LocomotiveFleet))
            {
                CacheRemoteLocomotiveFleet(snapshot.LocomotiveFleet);
                ApplyBestAvailableLocomotiveFleet();
            }
        }
        else if (_controlTabActive)
        {
            ApplyBestAvailableLocomotiveFleet();
        }

        if (snapshot.SignalBoxElements.Count > 0
            && snapshot.LocomotiveFleet.Count == 0
            && !_runtimeProjection.HasAnyLocomotiveFleetAvailable
            && _controlTabActive)
        {
            RunInBackground(
                RequestSolutionSyncAsync(_applicationLifetimeCts.Token),
                "Solution sync when snapshot has signals but no locomotive fleet");
        }
    }

    private void ApplyLocalCounters()
    {
        foreach (var stat in Statistics)
        {
            var counter = _localCounters.FirstOrDefault(item => item.InPort == stat.InPort);
            stat.Count = counter?.Count ?? 0;
            stat.LastFeedbackTime = counter?.LastFeedbackTime?.UtcDateTime;
            stat.LastLapTime = counter?.LastLapTime;
            stat.HasReceivedFirstLap = counter?.Count > 0;
        }
    }

    #endregion

    #region Photo Upload

    [ObservableProperty]
    private bool _isPhotoUploading;

    [ObservableProperty]
    private string? _photoUploadStatus;

    [ObservableProperty]
    private bool _photoUploadSuccess;

    [RelayCommand]
    private async Task CaptureAndUploadPhotoAsync()
    {
        try
        {
            IsPhotoUploading = true;
            PhotoUploadSuccess = false;
            PhotoUploadStatus = null;

            // Use the endpoint already known (e.g. when the status is green); only search the LAN without one.
            var knownEndpoint = string.IsNullOrWhiteSpace(RestApiIpAddress) || RestApiPort <= 0
                ? null
                : new MobaflowEndpoint(RestApiIpAddress.Trim(), RestApiPort);
            var result = await _photoUploader
                .CaptureAndUploadAsync(knownEndpoint, ToDiscoveryAnchor(Z21IpAddress), _applicationLifetimeCts.Token)
                .ConfigureAwait(false);
            PhotoUploadSuccess = result.Success;
            PhotoUploadStatus = result.Status;
        }
        finally
        {
            IsPhotoUploading = false;
        }
    }

    #endregion
}
