// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using Backend;
using Backend.Interface;

using Common.Configuration;
using Common.Runtime;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Interface;

using Microsoft.Extensions.Logging;

using System.Collections.ObjectModel;

/// <summary>
/// Mobile-optimized ViewModel for MAUI - focused on Z21 monitoring and feedback statistics.
/// </summary>
public sealed partial class MauiViewModel : ObservableObject
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
    private readonly INetworkProfileChangeNotifier _networkProfileChangeNotifier;
    private readonly ILogger<MauiViewModel> _logger;

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

    /// <summary>Coalesces rapid platform connectivity notifications before re-running REST discovery.</summary>
    private const int NetworkChangeDebounceMilliseconds = 750;

    private readonly SemaphoreSlim _initializationLock = new(1, 1);
    private Task? _initializationTask;
    private Task? _startupDiscoveryTask;
    private Task? _restApiHealthCheckTask;


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
    /// <param name="restApiClientRegistration">Optional: registers this app with the REST API for Overview client list (MAUI).</param>
    /// <param name="networkProfileChangeNotifier">Raises when device connectivity changes so cached LAN REST endpoints can be re-resolved.</param>
    /// <param name="logger">Logger for diagnostics.</param>
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
        IRestApiClientRegistration? restApiClientRegistration = null)
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

        _mobaRuntime.SnapshotChanged += OnRuntimeSnapshotChanged;
        _mobaRuntime.FeedbackReceived += OnFeedbackReceived;
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
        _networkProfileChangeNotifier.NetworkProfilePossiblyChanged += OnNetworkProfilePossiblyChanged;
        _networkProfileChangeNotifier.StartListening();

        await _uiDispatcher.InvokeOnUiAsync(() =>
        {
            LoadSettingsIntoViewModel();
            ApplyRuntimeSnapshot(_mobaRuntime.Current);
            InitializeStatistics();
            return Task.CompletedTask;
        }).ConfigureAwait(false);

        _startupDiscoveryTask ??= TryAutoDiscoverEndpointsAsync();
        _restApiHealthCheckTask ??= RestApiHealthCheckLoopAsync();

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

            // REST: same idea as Z21 — run discovery to completion (retries), in parallel with Z21 lookup
            var restDiscoveryTask = RestDiscoveryLoopAsync();

            // Z21 discovery: wait for result and connect immediately when found (parallel to REST)
            var z21Ip = await _z21DiscoveryService.DiscoverZ21Async(CancellationToken.None).ConfigureAwait(false);
            if (!string.IsNullOrEmpty(z21Ip))
            {
                _uiDispatcher.InvokeOnUi(() =>
                {
                    Z21IpAddress = z21Ip;
                });
                await _uiDispatcher.InvokeOnUiAsync(async () =>
                {
                    await ConnectCommand.ExecuteAsync(null);
                }).ConfigureAwait(false);
            }
            else
            {
                // Discovery found no Z21: try once with saved/default IP (e.g. app started with delay or Z21 on different subnet)
                await _uiDispatcher.InvokeOnUiAsync(async () =>
                {
                    if (!IsConnected && !string.IsNullOrWhiteSpace(Z21IpAddress))
                    {
                        _logger.LogInformation("Z21 discovery did not find device; trying saved/default IP");
                        await ConnectCommand.ExecuteAsync(null).ConfigureAwait(false);
                    }
                }).ConfigureAwait(false);
            }

            await restDiscoveryTask.ConfigureAwait(false);

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

            var (ip, port) = await _restDiscoveryService.DiscoverServerAsync().ConfigureAwait(false);
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
    private async Task ApplyDiscoveredRestEndpointAsync(string ip, int port)
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
            _uiDispatcher.InvokeOnUi(() => IsRestApiReachable = false);
            _lastRestApiDiscoverTime = DateTime.MinValue;

            var (ip, port) = await _restDiscoveryService.DiscoverServerAsync().ConfigureAwait(false);
            if (!string.IsNullOrEmpty(ip) && port.HasValue)
            {
                await ApplyDiscoveredRestEndpointAsync(ip, port.Value).ConfigureAwait(false);
                return;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "REST discovery after network change failed");
        }

        await RefreshRestApiReachableAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Loads settings from AppSettings singleton into ViewModel properties.
    /// Called during constructor after SettingsService has loaded the file.
    /// </summary>
    private void LoadSettingsIntoViewModel()
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
        try
        {
            _applicationLifetimeCts.Cancel();
        }
        catch (ObjectDisposedException)
        {
            // Already disposed.
        }
    }

    private async Task RestApiHealthCheckLoopAsync()
    {
        try
        {
            while (!_applicationLifetimeCts.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(30), _applicationLifetimeCts.Token).ConfigureAwait(false);
                await RefreshRestApiReachableAsync().ConfigureAwait(false);

                // When API is unreachable, re-run discovery periodically. Use shorter interval in the first 90s
                // so we find the server quickly when both apps are started together (e.g. from Visual Studio).
                var elapsedSinceStart = (DateTime.UtcNow - _appStartTimeUtc).TotalSeconds;
                var interval = elapsedSinceStart < RestApiStartupRetryWindowSeconds
                    ? RestApiRediscoverIntervalFirst90Seconds
                    : RestApiRediscoverIntervalSeconds;

                if (!IsRestApiReachable && (DateTime.UtcNow - _lastRestApiDiscoverTime).TotalSeconds >= interval)
                {
                    _lastRestApiDiscoverTime = DateTime.UtcNow;
                    try
                    {
                        var (restIp, restPort) = await _restDiscoveryService.DiscoverServerAsync().ConfigureAwait(false);
                        if (!string.IsNullOrEmpty(restIp) && restPort.HasValue)
                        {
                            await ApplyDiscoveredRestEndpointAsync(restIp, restPort.Value).ConfigureAwait(false);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "REST API re-discovery failed");
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when the MAUI host cancels the lifetime token during shutdown.
        }
    }

    /// <summary>
    /// Checks REST API reachability and updates IsRestApiReachable on the UI thread.
    /// </summary>
    private async Task RefreshRestApiReachableAsync()
    {
        if (string.IsNullOrWhiteSpace(RestApiIpAddress) || RestApiPort <= 0)
        {
            _uiDispatcher.InvokeOnUi(() => IsRestApiReachable = false);
            return;
        }
        try
        {
            var reachable = await _photoUploadService.HealthCheckAsync(RestApiIpAddress, RestApiPort).ConfigureAwait(false);
            _uiDispatcher.InvokeOnUi(() => IsRestApiReachable = reachable);

            if (reachable
                && RestApiRecentEndpointHistory.RecordRecentIp(_settings.RestApi, RestApiIpAddress.Trim()))
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
                        _restApiClientRegistration.RegisterAsync(RestApiIpAddress, RestApiPort),
                        "REST API client registration");
                }
            }
        }
        catch
        {
            _uiDispatcher.InvokeOnUi(() => IsRestApiReachable = false);
        }
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
        await _mobaRuntime.DisconnectAsync().ConfigureAwait(false);
        _uiDispatcher.InvokeOnUi(() => Z21ConnectionStatus = null);
    }

    [RelayCommand]
    private async Task SetTrackPowerAsync(bool turnOn)
    {
        await _mobaRuntime.SetTrackPowerAsync(turnOn).ConfigureAwait(false);
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

    // Last feedback time tracking for timer filter
    private readonly Dictionary<int, DateTime> _lastFeedbackTime = [];

    partial void OnCountOfFeedbackPointsChanged(int value)
    {
        _logger.LogTrace("OnCountOfFeedbackPointsChanged: {Value}", value);
        _settings.Counter.CountOfFeedbackPoints = value;
        InitializeStatistics();
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

        QueueSaveSettings();
    }

    partial void OnUseTimerFilterChanged(bool value)
    {
        _logger.LogTrace("OnUseTimerFilterChanged: {Value}", value);
        _settings.Counter.UseTimerFilter = value;
        QueueSaveSettings();
    }

    partial void OnTimerIntervalSecondsChanged(double value)
    {
        _logger.LogTrace("OnTimerIntervalSecondsChanged: {Value}", value);
        _settings.Counter.TimerIntervalSeconds = value;
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

        _lastFeedbackTime.Clear();
    }

    [RelayCommand]
    private void ResetCounters()
    {
        foreach (var stat in Statistics)
        {
            stat.Count = 0;
            stat.LastFeedbackTime = null;
            stat.LastLapTime = TimeSpan.Zero;
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

    private bool CanDecrementTimerInterval() => TimerIntervalSeconds > 1.0;

    [RelayCommand]
    private void IncrementTimerInterval()
    {
        TimerIntervalSeconds = Math.Round(TimerIntervalSeconds + 1.0, 1);
    }

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
        task.ContinueWith(
            t =>
            {
                if (t.Exception != null)
                {
                    _logger.LogWarning(t.Exception.GetBaseException(), "{Operation} failed", operationName);
                }
            },
            CancellationToken.None,
            TaskContinuationOptions.OnlyOnFaulted,
            TaskScheduler.Default);
    }

    #endregion

    #region Runtime Event Handlers

    private void OnRuntimeSnapshotChanged(object? sender, MobaRuntimeSnapshot snapshot)
    {
        _ = sender;
        _uiDispatcher.InvokeOnUi(() => ApplyRuntimeSnapshot(snapshot));
    }

    private void ApplyRuntimeSnapshot(MobaRuntimeSnapshot snapshot)
    {
        var previousConnectionState = IsConnected;

        IsConnected = snapshot.IsConnected;
        IsTrackPowerOn = snapshot.IsTrackPowerOn;
        MainCurrent = snapshot.MainCurrent;
        Temperature = snapshot.Temperature;
        SupplyVoltage = snapshot.SupplyVoltage;
        VccVoltage = snapshot.VccVoltage;

        Z21ConnectionStatus = snapshot.IsConnected
            ? "Connected"
            : string.Equals(snapshot.StatusText, "Disconnected", StringComparison.OrdinalIgnoreCase)
                ? null
                : snapshot.StatusText;

        if (snapshot.IsConnected && !previousConnectionState)
        {
            _settings.Z21.CurrentIpAddress = Z21IpAddress.Trim();
            QueueSaveSettings();
        }
    }

    private void OnFeedbackReceived(object? sender, FeedbackResult feedback)
    {
        _uiDispatcher.InvokeOnUi(() =>
        {
            if (_statisticsByInPort.TryGetValue(feedback.InPort, out var stat))
            {
                // Timer filter: Prevent duplicate counts from long trains
                if (UseTimerFilter)
                {
                    if (_lastFeedbackTime.TryGetValue(feedback.InPort, out DateTime lastTime))
                    {
                        var elapsed = (DateTime.Now - lastTime).TotalSeconds;
                        if (elapsed < TimerIntervalSeconds)
                        {
                            // Skip: Too soon after last feedback (same train still passing)
                            return;
                        }
                    }
                    _lastFeedbackTime[feedback.InPort] = DateTime.Now;
                }

                // Calculate lap time (time between two consecutive feedbacks)
                DateTime now = DateTime.Now;
                if (stat.LastFeedbackTime.HasValue)
                {
                    stat.LastLapTime = now - stat.LastFeedbackTime.Value;
                }

                // Update count and timestamp
                stat.Count++;
                stat.LastFeedbackTime = now;
            }
        });
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
                var discovered = await _restDiscoveryService.DiscoverServerAsync().ConfigureAwait(false);
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










