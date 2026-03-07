// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using Backend;
using Backend.Interface;
using Common.Configuration;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Interface;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Net;

/// <summary>
/// Mobile-optimized ViewModel for MAUI - focused on Z21 monitoring and feedback statistics.
/// </summary>
public sealed partial class MauiViewModel : ObservableObject
{
    private readonly IZ21 _z21;
    private readonly IUiDispatcher _uiDispatcher;
    private readonly AppSettings _settings;
    private readonly ISettingsService _settingsService;
    private readonly IRestDiscoveryService _restDiscoveryService;
    private readonly IZ21DiscoveryService _z21DiscoveryService;
    private readonly IPhotoUploadService _photoUploadService;
    private readonly IPhotoCaptureService _photoCaptureService;
    private readonly IRestApiClientRegistration? _restApiClientRegistration;

    /// <summary>Last time we registered with the REST API (for periodic re-register to stay in Overview list).</summary>
    private DateTime _lastRestApiRegisterTime = DateTime.MinValue;

    private const int RestApiReregisterIntervalSeconds = 120;

    /// <summary>Used to cancel the Z21 reconnect loop when we become connected.</summary>
    private CancellationTokenSource? _z21ReconnectCts;

    /// <summary>Last time we ran REST API discovery (for re-discovery when unreachable).</summary>
    private DateTime _lastRestApiDiscoverTime = DateTime.MinValue;

    /// <summary>App start time; used to retry discovery more often in the first 90s when both apps start together.</summary>
    private readonly DateTime _appStartTimeUtc = DateTime.UtcNow;

    private const int RestApiRediscoverIntervalSeconds = 25;
    private const int RestApiRediscoverIntervalFirst90Seconds = 10;
    private const int RestApiStartupRetryWindowSeconds = 90;

    /// <summary>
    /// Initializes a new instance of the <see cref="MauiViewModel"/> class for the MAUI mobile client.
    /// </summary>
    /// <param name="z21">The Z21 backend service used for digital command control and feedback.</param>
    /// <param name="uiDispatcher">Dispatcher used to marshal updates back to the MAUI UI thread.</param>
    /// <param name="settings">Application settings used to initialize default values.</param>
    /// <param name="settingsService">Service used to persist updated settings.</param>
    /// <param name="restDiscoveryService">Service used to discover the REST API endpoint.</param>
    /// <param name="z21DiscoveryService">Service used to discover the Z21 on the local network (optional).</param>
    /// <param name="photoUploadService">Service used to upload captured photos to the server.</param>
    /// <param name="photoCaptureService">Service used to capture photos on the device.</param>
    /// <param name="restApiClientRegistration">Optional: registers this app with the REST API for Overview client list (MAUI).</param>
    public MauiViewModel(
        IZ21 z21,
        IUiDispatcher uiDispatcher,
        AppSettings settings,
        ISettingsService settingsService,
        IRestDiscoveryService restDiscoveryService,
        IZ21DiscoveryService z21DiscoveryService,
        IPhotoUploadService photoUploadService,
        IPhotoCaptureService photoCaptureService,
        IRestApiClientRegistration? restApiClientRegistration = null)
    {
        ArgumentNullException.ThrowIfNull(z21);
        ArgumentNullException.ThrowIfNull(uiDispatcher);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(settingsService);
        ArgumentNullException.ThrowIfNull(restDiscoveryService);
        ArgumentNullException.ThrowIfNull(z21DiscoveryService);
        ArgumentNullException.ThrowIfNull(photoUploadService);
        ArgumentNullException.ThrowIfNull(photoCaptureService);
        _z21 = z21;
        _uiDispatcher = uiDispatcher;
        _settings = settings;
        _settingsService = settingsService;
        _restDiscoveryService = restDiscoveryService;
        _z21DiscoveryService = z21DiscoveryService;
        _photoUploadService = photoUploadService;
        _photoCaptureService = photoCaptureService;
        _restApiClientRegistration = restApiClientRegistration;

        // Subscribe to Z21 events
        _z21.Received += OnFeedbackReceived;
        _z21.OnSystemStateChanged += OnZ21SystemStateChanged;
        _z21.OnConnectedChanged += OnZ21ConnectedChanged;

        // ✅ Initialize with loaded settings (settings were loaded in SettingsService constructor)
        LoadSettingsIntoViewModel();
        
        // Auto-discover REST-API and optionally Z21 when addresses are empty (non-blocking)
        _ = TryAutoDiscoverEndpointsAsync();
        
        // REST API health check: initial check + periodic every 30s
        StartRestApiHealthCheckLoop();
        _ = RefreshRestApiReachableAsync();
        
        // Apply polling interval to Z21 on startup (5 seconds - not configurable)
        _z21.SetSystemStatePollingInterval(5);
        
        InitializeStatistics();
    }
    
    /// <summary>
    /// Runs REST-API and Z21 discovery in the background. Uses settings as fallback for REST; discovery can override.
    /// Z21 connect runs as soon as Z21 is discovered (no wait for REST). REST discovery retries with backoff so the
    /// server has time to come up when both apps start together (~10–15s).
    /// </summary>
    private async Task TryAutoDiscoverEndpointsAsync()
    {
        try
        {
            // Short delay for network stack (especially on Android)
            await Task.Delay(TimeSpan.FromMilliseconds(500)).ConfigureAwait(false);

            // Start REST discovery in background (updates RestApiIpAddress when found; does not block Z21)
            _ = RestDiscoveryLoopAsync();

            // Z21 discovery: wait for result and connect immediately when found (no wait for REST)
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
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Auto-discover failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Runs REST API discovery with retries and updates RestApiIpAddress when the server is found.
    /// Runs in background so Z21 connect is not delayed.
    /// </summary>
    private async Task RestDiscoveryLoopAsync()
    {
        var restDelaysMs = new[] { 0, 2000, 5000, 10000, 15000 }; // 0, +2s, +5s, +10s, +15s (~32s total window)
        foreach (var delayMs in restDelaysMs)
        {
            if (delayMs > 0)
                await Task.Delay(delayMs).ConfigureAwait(false);

            var (ip, port) = await _restDiscoveryService.DiscoverServerAsync().ConfigureAwait(false);
            if (!string.IsNullOrEmpty(ip) && port.HasValue)
            {
                _lastRestApiDiscoverTime = DateTime.UtcNow;
                _uiDispatcher.InvokeOnUi(() =>
                {
                    RestApiIpAddress = ip;
                    RestApiPort = port.Value;
                });
                _settings.RestApi.CurrentIpAddress = ip;
                _settings.RestApi.Port = port.Value;
                try { await _settingsService.SaveSettingsAsync(_settings).ConfigureAwait(false); } catch { /* ignore */ }
                await RefreshRestApiReachableAsync().ConfigureAwait(false);
                return;
            }
        }
    }
    
    /// <summary>
    /// Loads settings from AppSettings singleton into ViewModel properties.
    /// Called during constructor after SettingsService has loaded the file.
    /// </summary>
    private void LoadSettingsIntoViewModel()
    {
        Debug.WriteLine("═══════════════════════════════════════════════════════");
        Debug.WriteLine("🔄 MauiViewModel.LoadSettingsIntoViewModel START");
        Debug.WriteLine($"   AppSettings.Counter.CountOfFeedbackPoints: {_settings.Counter.CountOfFeedbackPoints}");
        Debug.WriteLine($"   AppSettings.Counter.TargetLapCount: {_settings.Counter.TargetLapCount}");
        Debug.WriteLine($"   AppSettings.Counter.UseTimerFilter: {_settings.Counter.UseTimerFilter}");
        Debug.WriteLine($"   AppSettings.Counter.TimerIntervalSeconds: {_settings.Counter.TimerIntervalSeconds}");
        
        // Z21 and REST API: load from settings as fallback so REST connect works when discovery fails
        if (!string.IsNullOrWhiteSpace(_settings.RestApi.CurrentIpAddress) && _settings.RestApi.Port > 0)
        {
            RestApiIpAddress = _settings.RestApi.CurrentIpAddress.Trim();
            RestApiPort = _settings.RestApi.Port;
        }
        CountOfFeedbackPoints = _settings.Counter.CountOfFeedbackPoints;
        GlobalTargetLapCount = _settings.Counter.TargetLapCount;
        UseTimerFilter = _settings.Counter.UseTimerFilter;
        TimerIntervalSeconds = _settings.Counter.TimerIntervalSeconds;
        
        Debug.WriteLine("───────────────────────────────────────────────────────");
        Debug.WriteLine("✅ Values loaded into ViewModel (REST: from settings as fallback, discovery can override):");
        Debug.WriteLine($"   RestApiIpAddress: {RestApiIpAddress}");
        Debug.WriteLine($"   RestApiPort: {RestApiPort}");
        Debug.WriteLine($"   CountOfFeedbackPoints: {CountOfFeedbackPoints}");
        Debug.WriteLine($"   GlobalTargetLapCount: {GlobalTargetLapCount}");
        Debug.WriteLine($"   UseTimerFilter: {UseTimerFilter}");
        Debug.WriteLine($"   TimerIntervalSeconds: {TimerIntervalSeconds}s");
        Debug.WriteLine("═══════════════════════════════════════════════════════");
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
        _ = RefreshRestApiReachableAsync();
    }

    partial void OnRestApiPortChanged(int value)
    {
        _ = RefreshRestApiReachableAsync();
    }

    /// <summary>
    /// Starts the periodic REST API health check loop (runs every 30s).
    /// Call once after construction.
    /// </summary>
    internal void StartRestApiHealthCheckLoop()
    {
        _ = RestApiHealthCheckLoopAsync();
    }

    private async Task RestApiHealthCheckLoopAsync()
    {
        while (true)
        {
            await Task.Delay(TimeSpan.FromSeconds(30)).ConfigureAwait(false);
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
                        _uiDispatcher.InvokeOnUi(() =>
                        {
                            RestApiIpAddress = restIp;
                            RestApiPort = restPort.Value;
                        });
                        await RefreshRestApiReachableAsync().ConfigureAwait(false);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"REST API re-discovery failed: {ex.Message}");
                }
            }
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

            if (reachable && _restApiClientRegistration != null)
            {
                var now = DateTime.UtcNow;
                var shouldRegister = _lastRestApiRegisterTime == DateTime.MinValue
                    || (now - _lastRestApiRegisterTime).TotalSeconds >= RestApiReregisterIntervalSeconds;
                if (shouldRegister)
                {
                    _lastRestApiRegisterTime = now;
                    _ = _restApiClientRegistration.RegisterAsync(RestApiIpAddress, RestApiPort);
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
        // Discovery-only: do not persist Z21 IP to settings.
    }

    [RelayCommand]
    private async Task ConnectAsync()
    {
        if (string.IsNullOrWhiteSpace(Z21IpAddress))
        {
            _uiDispatcher.InvokeOnUi(() => Z21ConnectionStatus = "Enter Z21 IP address");
            return;
        }

        if (!int.TryParse(_settings.Z21.DefaultPort, out var port) || port <= 0 || port > 65535)
            port = 21105;

        _uiDispatcher.InvokeOnUi(() => Z21ConnectionStatus = "Connecting...");

        try
        {
            var address = IPAddress.Parse(Z21IpAddress.Trim());
            await _z21.ConnectAsync(address, port).ConfigureAwait(false);
            // Success: status will be set to "Connected" in OnZ21ConnectedChanged when Z21 responds
        }
        catch (FormatException ex)
        {
            _uiDispatcher.InvokeOnUi(() => Z21ConnectionStatus = $"Invalid IP: {ex.Message}");
            Debug.WriteLine($"Z21 Connect failed (invalid IP): {ex.Message}");
        }
        catch (Exception ex)
        {
            _uiDispatcher.InvokeOnUi(() => Z21ConnectionStatus = $"Connection failed: {ex.Message}");
            Debug.WriteLine($"Z21 Connection failed: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task DisconnectAsync()
    {
        await _z21.DisconnectAsync().ConfigureAwait(false);
        _uiDispatcher.InvokeOnUi(() => Z21ConnectionStatus = null);
    }

    [RelayCommand]
    private async Task SetTrackPowerAsync(bool turnOn)
    {
        if (turnOn)
            await _z21.SetTrackPowerOnAsync().ConfigureAwait(false);
        else
            await _z21.SetTrackPowerOffAsync().ConfigureAwait(false);
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

    // Last feedback time tracking for timer filter
    private readonly Dictionary<int, DateTime> _lastFeedbackTime = [];

    partial void OnCountOfFeedbackPointsChanged(int value)
    {
        Debug.WriteLine($"🔔 OnCountOfFeedbackPointsChanged: {value}");
        _settings.Counter.CountOfFeedbackPoints = value;
        InitializeStatistics();
        _ = SaveSettingsAsync(); // Auto-save
    }

    partial void OnGlobalTargetLapCountChanged(int value)
    {
        Debug.WriteLine($"🔔 OnGlobalTargetLapCountChanged: {value}");
        _settings.Counter.TargetLapCount = value;
        
        // Update all existing statistics
        foreach (var stat in Statistics)
        {
            stat.TargetLapCount = value;
        }
        
        _ = SaveSettingsAsync(); // Auto-save
    }

    partial void OnUseTimerFilterChanged(bool value)
    {
        Debug.WriteLine($"🔔 OnUseTimerFilterChanged: {value}");
        _settings.Counter.UseTimerFilter = value;
        _ = SaveSettingsAsync(); // Auto-save
    }

    partial void OnTimerIntervalSecondsChanged(double value)
    {
        Debug.WriteLine($"🔔 OnTimerIntervalSecondsChanged: {value}");
        _settings.Counter.TimerIntervalSeconds = value;
        _ = SaveSettingsAsync(); // Auto-save
    }

    private void InitializeStatistics()
    {
        Statistics.Clear();
        for (int i = 1; i <= CountOfFeedbackPoints; i++)
        {
            Statistics.Add(new InPortStatistic
            {
                InPort = i,
                Name = $"Track {i}",
                Count = 0,
                TargetLapCount = GlobalTargetLapCount
            });
        }
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
            Debug.WriteLine("✅ Counter settings saved");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ Failed to save settings: {ex.Message}");
        }
    }

    #endregion

    #region Z21 Event Handlers

    private void OnZ21ConnectedChanged(bool connected)
    {
        _uiDispatcher.InvokeOnUi(() =>
        {
            IsConnected = connected;
            Z21ConnectionStatus = connected ? "Connected" : null;
        });

        if (connected)
        {
            _z21ReconnectCts?.Cancel();
            _z21ReconnectCts = null;
        }
        else
        {
            // Start reconnection loop: retry Connect every 30s until connected
            _z21ReconnectCts?.Cancel();
            _z21ReconnectCts = new CancellationTokenSource();
            _ = Z21ReconnectLoopAsync(_z21ReconnectCts.Token);
        }
    }

    /// <summary>
    /// Periodically attempts to reconnect to Z21 when disconnected (every 30s).
    /// Stops when connected or when the cancellation token is set.
    /// </summary>
    private async Task Z21ReconnectLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            if (cancellationToken.IsCancellationRequested)
                break;

            Debug.WriteLine("Z21 reconnecting...");
            await _uiDispatcher.InvokeOnUiAsync(async () =>
            {
                await ConnectCommand.ExecuteAsync(null).ConfigureAwait(false);
            }).ConfigureAwait(false);
        }
    }

    private void OnZ21SystemStateChanged(SystemState state)
    {
        _uiDispatcher.InvokeOnUi(() =>
        {
            IsTrackPowerOn = state.IsTrackPowerOn;
            MainCurrent = state.MainCurrent;
            Temperature = state.Temperature;
            SupplyVoltage = state.SupplyVoltage;
            VccVoltage = state.VccVoltage;
        });
    }

    private void OnFeedbackReceived(FeedbackResult feedback)
    {
        _uiDispatcher.InvokeOnUi(() =>
        {
            var stat = Statistics.FirstOrDefault(s => s.InPort == feedback.InPort);
            if (stat != null)
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
