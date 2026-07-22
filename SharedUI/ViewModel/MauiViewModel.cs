// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using Backend.Interface;

using Common.Configuration;
using Common.Discovery;
using Common.Events;
using Common.Extension;
using Common.Runtime;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Interface;

using Microsoft.Extensions.Logging;

using Service;

using System.Collections.ObjectModel;

/// <summary>
/// Mobile-optimized ViewModel for MAUI - focused on Z21 monitoring and feedback statistics.
/// </summary>
public sealed partial class MauiViewModel : ObservableObject, IDisposable
{
    private readonly IMobaRuntime _mobaRuntime;
    private readonly IUiDispatcher _uiDispatcher;
    private readonly AppSettings _settings;
    private readonly ISettingsService _settingsService;
    private readonly IRestDiscoveryService _restDiscoveryService;
    private readonly IZ21DiscoveryService _z21DiscoveryService;
    private readonly IPhotoUploadService _photoUploadService;
    private readonly IPhotoCaptureService _photoCaptureService;
    private readonly IRestApiClientRegistration? _restApiClientRegistration;
    private readonly IRuntimeSettingsClient? _runtimeSettingsClient;
    private readonly ISolutionRemoteLoader? _solutionRemoteLoader;
    private readonly IMobileSolutionStore? _mobileSolutionStore;
    private readonly IRuntimeHubRemoteClient? _runtimeHubRemoteClient;
    private readonly IRuntimeCommandGateway? _runtimeCommandGateway;
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

    /// <summary>Last time we registered with the REST API (for periodic re-register to stay in Overview list).</summary>
    private DateTime _lastRestApiRegisterTime = DateTime.MinValue;

    private const int RestApiReregisterIntervalSeconds = 120;

    /// <summary>Last time we ran REST API discovery (for re-discovery when unreachable).</summary>
    private DateTime _lastRestApiDiscoverTime = DateTime.MinValue;

    /// <summary>App start time; used to retry discovery more often in the first 90s when both apps start together.</summary>
    private readonly DateTime _appStartTimeUtc = DateTime.UtcNow;

    private const int RestApiRediscoverIntervalSeconds = 25;
    private const int RestApiRediscoverIntervalFirst90Seconds = 10;
    private const int RestApiStartupRetryWindowSeconds = 90;
    private static readonly TimeSpan RestApiConnectHealthCheckTimeout = TimeSpan.FromSeconds(4);
    private static readonly int[] RuntimeHubConnectRetryDelaysMs = [0, 500, 1500, 3000];

    private const int Z21EndpointSyncIntervalSeconds = 45;

    /// <summary>Last time we requested the Z21 endpoint from MOBAflow via MOBApi.</summary>
    private DateTime _lastZ21EndpointSyncTime = DateTime.MinValue;

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
    /// <param name="mobaRuntime">In-process MOBA runtime (Z21, snapshots, feedback).</param>
    /// <param name="uiDispatcher">Dispatcher used to marshal updates back to the MAUI UI thread.</param>
    /// <param name="settings">Application settings used to initialize default values.</param>
    /// <param name="settingsService">Service used to persist updated settings.</param>
    /// <param name="restDiscoveryService">Service used to discover the REST API endpoint.</param>
    /// <param name="z21DiscoveryService">Service used to discover the Z21 on the local network (optional).</param>
    /// <param name="photoUploadService">Service used to upload captured photos to the server.</param>
    /// <param name="photoCaptureService">Service used to capture photos on the device.</param>
    /// <param name="networkProfileChangeNotifier">Raises when device connectivity changes so cached LAN REST endpoints can be re-resolved.</param>
    /// <param name="logger">Logger for diagnostics.</param>
    /// <param name="eventBus">Event bus used to observe runtime and feedback changes.</param>
    /// <param name="restApiClientRegistration">Optional: registers this app with the REST API for Overview client list (MAUI).</param>
    /// <param name="runtimeSettingsClient">Optional: reads Z21 endpoint that MOBAflow pushed to MOBApi.</param>
    /// <param name="solutionRemoteLoader">Optional: syncs the MOBAflow solution from MOBApi into the local runtime.</param>
    /// <param name="mobileSolutionStore">Optional: persists synced solution and signal-box data for offline use.</param>
    /// <param name="runtimeHubRemoteClient">Optional: SignalR client for remote runtime snapshots (MOBAsmart).</param>
    /// <param name="runtimeCommandGateway">Optional: routes control commands to MOBAflow via MOBApi (MOBAsmart).</param>
    /// <param name="mobileRuntimeCoordinator">Optional: MOBAsmart hybrid local/remote runtime routing.</param>
    /// <param name="projectContext">Optional: synced MOBAflow solution for Control tab fleet list (MOBAsmart).</param>
    /// <param name="backgroundService">Optional: Android foreground service for background keep-alive (MOBAsmart).</param>
    public MauiViewModel(
        IMobaRuntime mobaRuntime,
        IUiDispatcher uiDispatcher,
        AppSettings settings,
        ISettingsService settingsService,
        IRestDiscoveryService restDiscoveryService,
        IZ21DiscoveryService z21DiscoveryService,
        IPhotoUploadService photoUploadService,
        IPhotoCaptureService photoCaptureService,
        INetworkProfileChangeNotifier networkProfileChangeNotifier,
        ILogger<MauiViewModel> logger,
        IEventBus eventBus,
        IRestApiClientRegistration? restApiClientRegistration = null,
        IRuntimeSettingsClient? runtimeSettingsClient = null,
        ISolutionRemoteLoader? solutionRemoteLoader = null,
        IMobileSolutionStore? mobileSolutionStore = null,
        IRuntimeHubRemoteClient? runtimeHubRemoteClient = null,
        IRuntimeCommandGateway? runtimeCommandGateway = null,
        IMobileRuntimeCoordinator? mobileRuntimeCoordinator = null,
        IProjectContext? projectContext = null,
        IBackgroundService? backgroundService = null)
    {
        ArgumentNullException.ThrowIfNull(mobaRuntime);
        ArgumentNullException.ThrowIfNull(uiDispatcher);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(settingsService);
        ArgumentNullException.ThrowIfNull(restDiscoveryService);
        ArgumentNullException.ThrowIfNull(z21DiscoveryService);
        ArgumentNullException.ThrowIfNull(photoUploadService);
        ArgumentNullException.ThrowIfNull(photoCaptureService);
        ArgumentNullException.ThrowIfNull(networkProfileChangeNotifier);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(eventBus);
        _mobaRuntime = mobaRuntime;
        _uiDispatcher = uiDispatcher;
        _settings = settings;
        _settingsService = settingsService;
        _restDiscoveryService = restDiscoveryService;
        _z21DiscoveryService = z21DiscoveryService;
        _photoUploadService = photoUploadService;
        _photoCaptureService = photoCaptureService;
        _networkProfileChangeNotifier = networkProfileChangeNotifier;
        _logger = logger;
        _restApiClientRegistration = restApiClientRegistration;
        _runtimeSettingsClient = runtimeSettingsClient;
        _solutionRemoteLoader = solutionRemoteLoader;
        _mobileSolutionStore = mobileSolutionStore;
        _runtimeHubRemoteClient = runtimeHubRemoteClient;
        _runtimeCommandGateway = runtimeCommandGateway;
        _mobileRuntimeCoordinator = mobileRuntimeCoordinator;
        _backgroundService = backgroundService;
        _eventBus = eventBus;
        _projectContext = projectContext;

        _eventBusSubscriptions.Add(_eventBus.Subscribe<RuntimeSnapshotChangedEvent>(OnRuntimeSnapshotChanged));
        _eventBusSubscriptions.Add(_eventBus.Subscribe<FeedbackReceivedEvent>(OnFeedbackReceived));
        _eventBusSubscriptions.Add(_eventBus.Subscribe<SolutionSyncedEvent>(OnSolutionSyncedForSignalBox));
        _eventBusSubscriptions.Add(_eventBus.Subscribe<SolutionSyncedEvent>(OnSolutionSyncedForControlTab));

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
        await _mobaRuntime.StartAsync(_applicationLifetimeCts.Token).ConfigureAwait(false);

        _networkProfileChangeNotifier.NetworkProfilePossiblyChanged += OnNetworkProfilePossiblyChanged;
        _networkProfileChangeNotifier.StartListening();

        await _uiDispatcher.InvokeOnUiAsync(() =>
        {
            LoadSettingsIntoViewModel();
            ApplyLocalRuntimeSnapshot(_mobaRuntime.Current);
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

        _mobaRuntime.SetSystemStatePollingInterval(5);
    }

    /// <summary>
    /// Runs LAN discovery for MOBApi (REST) and Z21. Matches the Z21 pattern: REST discovery is awaited (with retries),
    /// not fire-and-forget, so we do not health-check a stale saved IP before discovery runs. Z21 discovery runs in
    /// parallel with REST retries so connect latency stays low.
    /// </summary>
    private async Task TryAutoDiscoverEndpointsAsync()
    {
        try
        {
            // Short delay for network stack (especially on Android)
            await Task.Delay(TimeSpan.FromMilliseconds(500)).ConfigureAwait(false);

            Task? restDiscoveryTask = null;
            if (IsMobaflowConnectionEnabled && !HasStoredMobaflowEndpoint())
            {
                restDiscoveryTask = RestDiscoveryLoopAsync();

                // Fast REST discovery so MOBAflow runtime-settings are available before Z21 subnet scan.
                var (fastIp, fastPort) = await _restDiscoveryService
                    .DiscoverServerFastAsync(string.IsNullOrWhiteSpace(Z21IpAddress) ? null : Z21IpAddress.Trim())
                    .ConfigureAwait(false);
                if (!string.IsNullOrEmpty(fastIp) && fastPort.HasValue)
                {
                    await ApplyDiscoveredRestEndpointAsync(fastIp, fastPort.Value).ConfigureAwait(false);
                }
            }

            // Let runtime auto-connect try the saved IP before running multicast discovery.
            await Task.Delay(TimeSpan.FromMilliseconds(1500)).ConfigureAwait(false);

            if (!_mobaRuntime.Current.IsConnected && IsMobaflowConnectionEnabled)
            {
                await TryApplyZ21EndpointFromMobaFlowAsync(force: true).ConfigureAwait(false);
            }

            if (!_mobaRuntime.Current.IsConnected)
            {
                var z21Ip = await _z21DiscoveryService
                    .DiscoverZ21Async(Z21IpAddress, CancellationToken.None)
                    .ConfigureAwait(false);
                if (!string.IsNullOrEmpty(z21Ip))
                {
                    await _uiDispatcher.InvokeOnUiAsync(async () =>
                    {
                        Z21IpAddress = z21Ip;
                        await ConnectCommand.ExecuteAsync(null).ConfigureAwait(false);
                    }).ConfigureAwait(false);
                }
                else
                {
                    await _uiDispatcher.InvokeOnUiAsync(async () =>
                    {
                        if (!IsConnected && !string.IsNullOrWhiteSpace(Z21IpAddress))
                        {
                            _logger.LogInformation("Z21 discovery did not find device; trying saved/default IP");
                            await ConnectCommand.ExecuteAsync(null).ConfigureAwait(false);
                        }
                    }).ConfigureAwait(false);
                }
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
            await RefreshRestApiReachableAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Auto-discovery failed");
        }
    }

    private async Task RestDiscoveryLoopAsync()
    {
        _lastRestApiDiscoverTime = DateTime.UtcNow;

        // Fewer full discovery passes than before: each pass runs multicast + LAN HTTP subnet probe (expensive on Wi‑Fi)
        var restDelaysMs = new[] { 0, 5000, 15000 };
        foreach (var delayMs in restDelaysMs)
        {
            if (delayMs > 0)
                await Task.Delay(delayMs).ConfigureAwait(false);

            var anchor = string.IsNullOrWhiteSpace(Z21IpAddress) ? null : Z21IpAddress.Trim();
            var (ip, port) = await _restDiscoveryService.DiscoverServerAsync(anchor).ConfigureAwait(false);
            if (!string.IsNullOrEmpty(ip) && port.HasValue)
            {
                await ApplyDiscoveredRestEndpointAsync(ip, port.Value).ConfigureAwait(false);
                return;
            }
        }
    }

    /// <summary>
    /// Applies a discovered REST endpoint to the UI, settings, recent-IP history, and reachability state.
    /// </summary>
    private async Task ApplyDiscoveredRestEndpointAsync(string ip, int port, bool skipHealthCheck = false)
    {
        _lastRestApiDiscoverTime = DateTime.UtcNow;
        var trimmedIp = ip.Trim();
        _uiDispatcher.InvokeOnUi(() =>
        {
            RestApiIpAddress = trimmedIp;
            RestApiPort = port;
        });
        _settings.RestApi.CurrentIpAddress = trimmedIp;
        _settings.RestApi.Port = port;
        RestApiRecentEndpointHistory.RecordRecentIp(_settings.RestApi, trimmedIp);
        try
        {
            await _settingsService.SaveSettingsAsync(_settings).ConfigureAwait(false);
        }
        catch
        {
            // Ignore persistence errors (e.g. read-only storage); in-memory state still applies for this session.
        }

        if (skipHealthCheck)
        {
            await _uiDispatcher.InvokeOnUiAsync(() =>
            {
                IsRestApiReachable = true;
                return Task.CompletedTask;
            }).ConfigureAwait(false);
            return;
        }

        await RefreshRestApiReachableAsync().ConfigureAwait(false);
    }

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
                await RefreshRestApiReachableAsync().ConfigureAwait(false);
                return;
            }

            await RefreshRestApiReachableAsync().ConfigureAwait(false);
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
        _lastRestApiDiscoverTime = DateTime.MinValue;
        var anchor = !string.IsNullOrWhiteSpace(Z21IpAddress) ? Z21IpAddress.Trim() : null;
        await DiscoverMobaflowEndpointAsync(fullScan: true, anchor, _applicationLifetimeCts.Token).ConfigureAwait(false);
        await RefreshRestApiReachableAsync(useConnectTimeout: false).ConfigureAwait(false);
        if (IsRestApiReachable)
        {
            await EnsureRuntimeHubConnectionAsync(true, forceReconnect: true).ConfigureAwait(false);
        }
    }

    private async Task DiscoverRestApiWithAnchorAsync(string? subnetAnchorIp)
    {
        try
        {
            var (ip, port) = await _restDiscoveryService.DiscoverServerAsync(subnetAnchorIp).ConfigureAwait(false);
            if (!string.IsNullOrEmpty(ip) && port.HasValue)
            {
                await ApplyDiscoveredRestEndpointAsync(ip, port.Value).ConfigureAwait(false);
                return;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "REST discovery with anchor {Anchor} failed", subnetAnchorIp ?? "(none)");
        }

        await RefreshRestApiReachableAsync().ConfigureAwait(false);
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

            // Z21: load from settings so UI shows last used IP and we can auto-connect when discovery fails
            if (!string.IsNullOrWhiteSpace(_settings.Z21.CurrentIpAddress))
            {
                Z21IpAddress = _settings.Z21.CurrentIpAddress.Trim();
            }

            // Z21 and REST API: load from settings as fallback so REST connect works when discovery fails
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
        RunInBackground(RefreshRestApiReachableAsync(), "Refresh REST reachability (IP changed)");
    }

    partial void OnRestApiPortChanged(int value)
    {
        _ = value;
        RunInBackground(RefreshRestApiReachableAsync(), "Refresh REST reachability (port changed)");
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
        _mobaflowCatalogSyncLock.Dispose();
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

                    await RefreshRestApiReachableAsync().ConfigureAwait(false);
                    await MaybeDisableMobaflowConnectionWhenSessionLostAsync().ConfigureAwait(false);
                }

                await Task.Delay(GetRestApiHealthCheckDelay(), _applicationLifetimeCts.Token).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when the MAUI host cancels the lifetime token during shutdown.
        }
    }

    private TimeSpan GetRestApiHealthCheckDelay()
    {
        if (!_settings.RestApi.IsConnectionEnabled)
        {
            return TimeSpan.FromSeconds(30);
        }

        if (IsRestApiReachable && (_runtimeHubRemoteClient?.IsConnected ?? false))
        {
            return TimeSpan.FromSeconds(30);
        }

        var elapsedSinceStart = (DateTime.UtcNow - _appStartTimeUtc).TotalSeconds;
        var seconds = elapsedSinceStart < RestApiStartupRetryWindowSeconds
            ? RestApiRediscoverIntervalFirst90Seconds
            : 15;
        return TimeSpan.FromSeconds(seconds);
    }

    private async Task TryPeriodicRestDiscoveryIfNeededAsync()
    {
        if (!IsMobaflowConnectionEnabled || IsRestApiReachable)
        {
            return;
        }

        var now = DateTime.UtcNow;
        var intervalSeconds = (now - _appStartTimeUtc).TotalSeconds < RestApiStartupRetryWindowSeconds
            ? RestApiRediscoverIntervalFirst90Seconds
            : RestApiRediscoverIntervalSeconds;

        if (_lastRestApiDiscoverTime != DateTime.MinValue
            && (now - _lastRestApiDiscoverTime).TotalSeconds < intervalSeconds)
        {
            return;
        }

        _lastRestApiDiscoverTime = now;
        var anchor = IsConnected && !string.IsNullOrWhiteSpace(Z21IpAddress)
            ? Z21IpAddress.Trim()
            : null;

        try
        {
            var (ip, port) = await _restDiscoveryService
                .DiscoverServerFastAsync(anchor, _applicationLifetimeCts.Token)
                .ConfigureAwait(false);
            if (!string.IsNullOrEmpty(ip) && port.HasValue)
            {
                await ApplyDiscoveredRestEndpointAsync(ip, port.Value, skipHealthCheck: false).ConfigureAwait(false);
                return;
            }

            (ip, port) = await _restDiscoveryService
                .DiscoverServerAsync(anchor, _applicationLifetimeCts.Token)
                .ConfigureAwait(false);
            if (!string.IsNullOrEmpty(ip) && port.HasValue)
            {
                await ApplyDiscoveredRestEndpointAsync(ip, port.Value).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Periodic REST discovery failed");
        }
    }

    /// <summary>
    /// Checks REST API reachability and updates IsRestApiReachable on the UI thread.
    /// </summary>
    private Task<bool> RefreshRestApiReachableAsync(bool useConnectTimeout = true) =>
        RefreshRestApiReachableAsync(_applicationLifetimeCts.Token, useConnectTimeout);

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
                var anchor = !string.IsNullOrWhiteSpace(Z21IpAddress) ? Z21IpAddress.Trim() : null;
                await DiscoverMobaflowEndpointAsync(false, anchor, _applicationLifetimeCts.Token).ConfigureAwait(false);
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
            TimeSpan? healthCheckTimeout = useConnectTimeout && _mobaflowConnectInProgress
                ? RestApiConnectHealthCheckTimeout
                : null;
            var reachable = await _photoUploadService
                .HealthCheckAsync(serverIp, serverPort, healthCheckTimeout)
                .ConfigureAwait(false);
            await _uiDispatcher.InvokeOnUiAsync(() =>
            {
                IsRestApiReachable = reachable;
                RestApiIpAddress = serverIp;
                RestApiPort = serverPort;
                return Task.CompletedTask;
            }).ConfigureAwait(false);

            if (reachable
                && RestApiRecentEndpointHistory.RecordRecentIp(_settings.RestApi, serverIp))
            {
                try
                {
                    await _settingsService.SaveSettingsAsync(_settings).ConfigureAwait(false);
                }
                catch
                {
                    // Ignore persistence errors; recent list still helps discovery within this session.
                }
            }

            if (reachable && _restApiClientRegistration != null)
            {
                var now = DateTime.UtcNow;
                var shouldRegister = _lastRestApiRegisterTime == DateTime.MinValue
                    || (now - _lastRestApiRegisterTime).TotalSeconds >= RestApiReregisterIntervalSeconds;
                if (shouldRegister)
                {
                    _lastRestApiRegisterTime = now;
                    RunInBackground(
                        _restApiClientRegistration.RegisterAsync(serverIp, serverPort),
                        "REST API client registration");
                }
            }

            if (reachable && _solutionRemoteLoader != null && _mobaflowInitialCatalogApplied)
            {
                RunInBackground(
                    _solutionRemoteLoader.SyncIfNeededAsync(
                        serverIp,
                        serverPort,
                        _applicationLifetimeCts.Token),
                    "Solution sync from MOBApi");
            }

            if (reachable && !IsConnected)
            {
                RunInBackground(TryApplyZ21EndpointFromMobaFlowAsync(), "Sync Z21 endpoint from MOBAflow");
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
        if (_runtimeHubRemoteClient == null)
        {
            return false;
        }

        if (!_settings.RestApi.IsConnectionEnabled)
        {
            if (_runtimeHubRemoteClient.IsConnected)
            {
                try
                {
                    await _runtimeHubRemoteClient.DisconnectAsync().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Runtime hub disconnect failed");
                }
            }

            await SetRuntimeHubConnectedOnUiAsync(false).ConfigureAwait(false);
            await SetRemoteZ21ConnectedOnUiAsync(false).ConfigureAwait(false);
            return false;
        }

        var serverIp = _settings.RestApi.CurrentIpAddress?.Trim() ?? string.Empty;
        var serverPort = _settings.RestApi.Port;
        if (string.IsNullOrWhiteSpace(serverIp) || serverPort <= 0)
        {
            await SetRuntimeHubConnectedOnUiAsync(false).ConfigureAwait(false);
            await SetRemoteZ21ConnectedOnUiAsync(false).ConfigureAwait(false);
            return false;
        }

        if (!reachable)
        {
            // Keep the SignalR session when REST health flickers; SignalR reconnect handles transport gaps.
            if (_runtimeHubRemoteClient.IsConnected)
            {
                await SetRuntimeHubConnectedOnUiAsync(true).ConfigureAwait(false);
                await SetRemoteZ21ConnectedOnUiAsync(_runtimeHubRemoteClient.HasActiveHost).ConfigureAwait(false);
                return true;
            }

            return false;
        }

        if (_runtimeHubRemoteClient.IsConnected && !forceReconnect)
        {
            await SetRuntimeHubConnectedOnUiAsync(true).ConfigureAwait(false);
            await SetRemoteZ21ConnectedOnUiAsync(_runtimeHubRemoteClient.HasActiveHost).ConfigureAwait(false);
            return true;
        }

        if (forceReconnect && _runtimeHubRemoteClient.IsConnected)
        {
            try
            {
                await _runtimeHubRemoteClient.DisconnectAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Runtime hub disconnect before forced reconnect failed");
            }
        }

        var clientId = _restApiClientRegistration?.ClientId;
        if (string.IsNullOrWhiteSpace(clientId))
        {
            clientId = Guid.NewGuid().ToString("N");
        }

        foreach (var delayMs in RuntimeHubConnectRetryDelaysMs)
        {
            if (delayMs > 0)
            {
                try
                {
                    await Task.Delay(delayMs, _applicationLifetimeCts.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    return false;
                }
            }

            if (!_settings.RestApi.IsConnectionEnabled)
            {
                return false;
            }

            try
            {
                await _runtimeHubRemoteClient
                    .ConnectAsync(
                        serverIp,
                        serverPort,
                        clientId,
                        _applicationLifetimeCts.Token,
                        forceReconnect)
                    .ConfigureAwait(false);
                await SetRuntimeHubConnectedOnUiAsync(true).ConfigureAwait(false);
                await SetRemoteZ21ConnectedOnUiAsync(_runtimeHubRemoteClient.HasActiveHost).ConfigureAwait(false);
                return true;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogDebug(ex, "Runtime hub connect attempt failed (delay {DelayMs} ms)", delayMs);
            }
        }

        await SetRuntimeHubConnectedOnUiAsync(false).ConfigureAwait(false);
        return false;
    }

    #endregion

    #region Z21 Connection

    [ObservableProperty]
    private string _z21IpAddress = "192.168.0.111";

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

    partial void OnZ21IpAddressChanged(string value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            _settings.Z21.CurrentIpAddress = value.Trim();
            QueueSaveSettings();
        }
    }

    [RelayCommand]
    private async Task ConnectAsync()
    {
        if (string.IsNullOrWhiteSpace(Z21IpAddress))
        {
            _uiDispatcher.InvokeOnUi(() => Z21ConnectionStatus = "Enter Z21 IP address");
            return;
        }

        _settings.Z21.CurrentIpAddress = Z21IpAddress.Trim();
        _uiDispatcher.InvokeOnUi(() => Z21ConnectionStatus = "Connecting...");

        try
        {
            await _mobaRuntime.ConnectAsync().ConfigureAwait(false);
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
        await _mobaRuntime.DisconnectAsync().ConfigureAwait(false);
        _uiDispatcher.InvokeOnUi(() => Z21ConnectionStatus = null);
        RequestBackgroundServiceSync();
    }

    private async Task TryApplyZ21EndpointFromMobaFlowAsync(bool force = false)
    {
        if (_runtimeSettingsClient == null || IsConnected || _mobaRuntime.Current.IsConnected)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(RestApiIpAddress) || RestApiPort <= 0)
        {
            return;
        }

        if (!force
            && _lastZ21EndpointSyncTime != DateTime.MinValue
            && (DateTime.UtcNow - _lastZ21EndpointSyncTime).TotalSeconds < Z21EndpointSyncIntervalSeconds)
        {
            return;
        }

        _lastZ21EndpointSyncTime = DateTime.UtcNow;

        var (ip, port) = await _runtimeSettingsClient
            .GetZ21EndpointAsync(RestApiIpAddress, RestApiPort, _applicationLifetimeCts.Token)
            .ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(ip))
        {
            return;
        }

        var trimmedIp = ip.Trim();
        if (string.Equals(trimmedIp, Z21IpAddress, StringComparison.OrdinalIgnoreCase) && IsConnected)
        {
            return;
        }

        _logger.LogInformation("Applying Z21 endpoint from MOBAflow: {Z21Ip}:{Z21Port}", trimmedIp, port ?? 21105);
        await _uiDispatcher.InvokeOnUiAsync(async () =>
        {
            Z21IpAddress = trimmedIp;
            if (port.HasValue && port.Value > 0)
            {
                _settings.Z21.DefaultPort = port.Value.ToString();
            }

            if (!IsConnected)
            {
                await ConnectCommand.ExecuteAsync(null).ConfigureAwait(false);
            }
        }).ConfigureAwait(false);
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

        if (_runtimeCommandGateway is not null)
        {
            await _runtimeCommandGateway.SetTrackPowerAsync(turnOn).ConfigureAwait(false);
        }
        else
        {
            await _mobaRuntime.SetTrackPowerAsync(turnOn).ConfigureAwait(false);
        }
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

    // O(1) lookup for high-frequency feedback updates.
    private Dictionary<int, InPortStatistic> _statisticsByInPort = [];

    private readonly FeedbackCounterEngine _feedbackCounterEngine = new();

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
        var updatedByInPort = new Dictionary<int, InPortStatistic>();
        for (int i = 1; i <= CountOfFeedbackPoints; i++)
        {
            var statistic = new InPortStatistic
            {
                InPort = i,
                Name = $"Track {i}",
                Count = 0,
                TargetLapCount = GlobalTargetLapCount
            };
            updatedStatistics.Add(statistic);
            updatedByInPort[i] = statistic;
        }

        _uiDispatcher.InvokeOnUi(() =>
        {
            // Replace the collection instance atomically to avoid MAUI BindableLayout reentrancy glitches.
            Statistics = updatedStatistics;
            _statisticsByInPort = updatedByInPort;
        });

    }

    [RelayCommand]
    private void ResetCounters()
    {
        foreach (var stat in Statistics)
        {
            stat.Count = 0;
            stat.LastFeedbackTime = null;
            stat.LastLapTime = TimeSpan.Zero;
            stat.HasReceivedFirstLap = false;
        }
    }

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
                if (!HasAnyLocomotiveFleetAvailable())
                {
                    RunInBackground(RequestSolutionSyncAsync(), "Solution sync after hub session restored");
                }
            }
        });
        return Task.CompletedTask;
    }

    private void ApplyLocalRuntimeSnapshot(MobaRuntimeSnapshot snapshot)
    {
        var previousConnectionState = IsConnected;
        var projection = RuntimeSnapshotProjector.ProjectMaui(snapshot, previousConnectionState);
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

        if (projection.ShouldPersistCurrentIpAddress)
        {
            _settings.Z21.CurrentIpAddress = Z21IpAddress.Trim();
            QueueSaveSettings();
        }

        if (ShouldApplyLocalSignalBoxSnapshot())
        {
            var elements = snapshot.SignalBoxElements;
            if (_cachedRemoteSignalBoxElements is { Count: > 0 })
            {
                elements = SignalBoxSnapshotMerge.MergeAspectsFromCache(elements, _cachedRemoteSignalBoxElements);
            }

            RefreshSignalBoxElements(elements);
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
            var filteredElements = FilterSignalBoxElementsToPlan(snapshot.SignalBoxElements);
            if (ShouldCacheRemoteSignalBoxElements(filteredElements))
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
            else if (ShouldCacheRemoteLocomotiveFleet(snapshot.LocomotiveFleet))
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
            && !HasAnyLocomotiveFleetAvailable()
            && _controlTabActive)
        {
            RunInBackground(
                RequestSolutionSyncAsync(),
                "Solution sync when snapshot has signals but no locomotive fleet");
        }
    }

    private void OnFeedbackReceived(FeedbackReceivedEvent e)
    {
        ApplyFeedbackReceived(e.InPort);
    }

    private void ApplyFeedbackReceived(int inPort)
    {
        if (_statisticsByInPort.TryGetValue(inPort, out var stat))
        {
            _feedbackCounterEngine.ApplyFeedback(stat, UseTimerFilter, TimerIntervalSeconds);
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

            var localPath = await _photoCaptureService.CapturePhotoAsync().ConfigureAwait(false);
            if (string.IsNullOrEmpty(localPath))
            {
                PhotoUploadStatus = "Capture cancelled or not available.";
                return;
            }

            // Use current REST API endpoint when already known (e.g. when status was green); only run discovery when missing
            string? ip = null;
            int? port = null;
            if (!string.IsNullOrWhiteSpace(RestApiIpAddress) && RestApiPort > 0)
            {
                ip = RestApiIpAddress.Trim();
                port = RestApiPort;
            }
            if (string.IsNullOrEmpty(ip) || !port.HasValue)
            {
                var anchor = string.IsNullOrWhiteSpace(Z21IpAddress) ? null : Z21IpAddress.Trim();
                var discovered = await _restDiscoveryService.DiscoverServerAsync(anchor).ConfigureAwait(false);
                ip = discovered.ip;
                port = discovered.port;
            }

            if (string.IsNullOrEmpty(ip) || !port.HasValue)
            {
                PhotoUploadStatus = "⚠️ REST server not found\n\n" +
                                    "• Is MOBAflow (PC) running with\n  \"Auto-start REST API\" enabled?\n" +
                                    "• Are phone and PC on the same Wi‑Fi?\n" +
                                    "• Try again in a moment (discovery\n  runs automatically).\n\n" +
                                    "Server must listen on port 5001.";
                return;
            }

            // ✅ Upload photo WITHOUT entityId - WinUI will assign it to the currently selected item
            var tempId = Guid.NewGuid(); // Temporary ID for filename only
            var (success, serverPhotoPath, error) = await _photoUploadService.UploadPhotoAsync(ip, port.Value, localPath, "latest", tempId).ConfigureAwait(false);
            if (success)
            {
                PhotoUploadSuccess = true;
                PhotoUploadStatus = serverPhotoPath ?? "Uploaded successfully.";
                if (RestApiRecentEndpointHistory.RecordRecentIp(_settings.RestApi, ip.Trim()))
                {
                    try
                    {
                        await _settingsService.SaveSettingsAsync(_settings).ConfigureAwait(false);
                    }
                    catch
                    {
                        // Ignore persistence errors.
                    }
                }
            }
            else
            {
                PhotoUploadStatus = error ?? "Upload failed.";
            }
        }
        catch (Exception ex)
        {
            PhotoUploadStatus = $"Error: {ex.Message}";
        }
        finally
        {
            IsPhotoUploading = false;
        }
    }

    #endregion
}
