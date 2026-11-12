// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.Service;

using Backend.Interface;

using Common.Configuration;
using Common.Events;
using Common.Extension;

using Microsoft.Extensions.Logging;

using SharedUI.Interface;

using System.Text.Json;
using Contracts.Discovery;

/// <summary>
/// Polls the REST API (MOBApi process) status and publishes events when status changes.
/// When the API is reachable, connects PhotoHubClient so WinUI receives photo upload notifications.
/// Uses EventBus for UI decoupling - no direct dispatcher or view model dependencies.
/// </summary>
public sealed class RestApiStatusService : IAsyncDisposable
{
    private static readonly TimeSpan PollIntervalWhenReachable = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan PollIntervalWhenWaiting = TimeSpan.FromSeconds(2); // "Waiting for the REST API to start..."

    /// <summary>
    /// Re-check delays while the MOBAsmart synchronization is set up: the status read before this refresh connected
    /// the RuntimeHub host, and pushes may land after the read. A failed host connect is retried at the same pace.
    /// After the last delay the normal interval applies again.
    /// </summary>
    private static readonly TimeSpan[] SyncStartPollDelays =
    [
        TimeSpan.FromSeconds(1),
        TimeSpan.FromSeconds(2),
        TimeSpan.FromSeconds(5),
        TimeSpan.FromSeconds(10),
        TimeSpan.FromSeconds(10),
        TimeSpan.FromSeconds(10)
    ];

    private readonly HttpClient _httpClient;
    private readonly AppSettings _appSettings;
    private readonly RestApiProcessService _restApiProcessService;
    private readonly IPhotoHubClient _photoHubClient;
    private readonly RestApiRuntimeHubService _runtimeHubService;
    private readonly RestApiSolutionSyncService _solutionSyncService;
    private readonly IRuntimeHubHostClient _runtimeHubHostClient;
    private readonly IMobaRuntime _mobaRuntime;
    private readonly IEventBus _eventBus;
    private readonly ILogger<RestApiStatusService> _logger;
    private readonly ITimer _timer;
    private readonly Lock _pollLock = new();
    private TimeSpan _pollInterval = PollIntervalWhenWaiting;
    private bool _isPolling;
    private int _syncStartPolls;
    private int _hubConnectInProgress;
    private static readonly JsonSerializerOptions SJsonOptions = new() { PropertyNameCaseInsensitive = true };
    private bool _photoHubConnected;
    private bool _runtimeHubConnected;
    private readonly CancellationTokenSource _disposeCts = new();
    private readonly object _refreshTasksLock = new();
    private readonly HashSet<Task> _refreshTasks = [];
    private readonly object _disposeLock = new();
    private Task? _disposeTask;
    private int _disposeState;

    public RestApiStatusService(
        HttpClient httpClient,
        AppSettings appSettings,
        RestApiProcessService restApiProcessService,
        IPhotoHubClient photoHubClient,
        RestApiRuntimeHubService runtimeHubService,
        RestApiSolutionSyncService solutionSyncService,
        IRuntimeHubHostClient runtimeHubHostClient,
        IMobaRuntime mobaRuntime,
        IEventBus eventBus,
        ILogger<RestApiStatusService> logger,
        TimeProvider? timeProvider = null)
    {
        _httpClient = httpClient;
        _appSettings = appSettings;
        _restApiProcessService = restApiProcessService;
        _photoHubClient = photoHubClient;
        _runtimeHubService = runtimeHubService;
        _solutionSyncService = solutionSyncService;
        _runtimeHubHostClient = runtimeHubHostClient;
        _mobaRuntime = mobaRuntime;
        _eventBus = eventBus;
        _logger = logger;
        _httpClient.Timeout = TimeSpan.FromSeconds(5);
        _timer = (timeProvider ?? TimeProvider.System).CreateTimer(
            _ => OnPollTimerElapsed(),
            null,
            Timeout.InfiniteTimeSpan,
            Timeout.InfiniteTimeSpan);

        _restApiProcessService.ApiBecameReachable += OnRestApiBecameReachable;
        _photoHubClient.PhotoUploaded += OnPhotoUploadedAsync;
    }

    private void OnRestApiBecameReachable(object? sender, int port)
    {
        QueueRefresh("Reachability event refresh");
    }

    /// <summary>
    /// Starts periodic status refresh: every 2 s while the REST API starts, within seconds while the MOBAsmart
    /// synchronization is set up, then every 30 s.
    /// </summary>
    public void Start()
    {
        if (IsDisposed)
        {
            return;
        }

        SetPolling(true);
        QueueRefresh("Service start refresh");
    }

    /// <summary>
    /// Stops periodic refresh.
    /// </summary>
    public void Stop()
    {
        if (IsDisposed)
        {
            return;
        }

        SetPolling(false);
        try { _disposeCts.Cancel(); } catch (ObjectDisposedException) { /* already disposed */ }
    }

    /// <summary>
    /// Pauses polling when window is deactivated to conserve resources.
    /// </summary>
    public void PausePolling()
    {
        if (IsDisposed)
        {
            return;
        }

        SetPolling(false);
        _logger.LogDebug("Health check polling paused due to window deactivation");
    }

    /// <summary>
    /// Resumes polling when window is activated and triggers immediate refresh.
    /// </summary>
    public void ResumePolling()
    {
        if (IsDisposed)
        {
            return;
        }

        SetPolling(true);
        _logger.LogDebug("Health check polling resumed due to window activation");
        QueueRefresh("Activation resume refresh");
    }

    private void OnPollTimerElapsed()
    {
        lock (_pollLock)
        {
            if (!_isPolling || IsDisposed)
            {
                return;
            }

            // One-shot timer re-armed per tick, so an interval change restarts the countdown from now.
            _timer.Change(_pollInterval, Timeout.InfiniteTimeSpan);
        }

        QueueRefresh("Timer refresh");
    }

    private void SetPolling(bool isPolling)
    {
        lock (_pollLock)
        {
            if (IsDisposed)
            {
                return;
            }

            _isPolling = isPolling;
            _timer.Change(isPolling ? _pollInterval : Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
        }
    }

    /// <summary>
    /// Fetches REST API status and publishes events for status changes.
    /// EventBus subscribers (e.g., MainWindowViewModel) receive updates via UiThreadEventBusDecorator.
    /// </summary>
    public Task RefreshAsync()
    {
        lock (_refreshTasksLock)
        {
            if (IsDisposed)
            {
                return Task.CompletedTask;
            }

            var refreshTask = RefreshCoreAsync();
            _refreshTasks.Add(refreshTask);
            _ = refreshTask.ContinueWith(
                completedTask =>
                {
                    lock (_refreshTasksLock)
                    {
                        _refreshTasks.Remove(completedTask);
                    }
                },
                CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);
            return refreshTask;
        }
    }

    private async Task RefreshCoreAsync()
    {
        if (_disposeCts.Token.IsCancellationRequested)
            return;
        var port = _appSettings.RestApi.Port;
        if (port <= 0) port = 5001;
        var url = MobApiEndpoint.Local(port).Resolve("api/status");
        try
        {
            var response = await _httpClient.GetAsync(url, _disposeCts.Token);
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync(_disposeCts.Token);
                var data = JsonSerializer.Deserialize<StatusResponse>(json, SJsonOptions);
                var clients = data?.ConnectedClients?
                    .Select(c => new RestApiClientInfo
                    {
                        ClientId = c.ClientId ?? "",
                        DeviceName = c.DeviceName ?? "MOBAsmart",
                        ConnectedAt = c.ConnectedAt
                    })
                    .ToList() ?? [];
                var effectivePort = data is { Port: > 0 } ? data.Port : port;
                var statusText = $"Running on port {effectivePort}";

                // Publish event - UiThreadEventBusDecorator marshals to UI thread
                _eventBus.Publish(new RestApiStatusChangedEvent(statusText, isReachable: true, clients));

                // Overlapping refreshes (timer, reachability event, window activation) must not connect twice.
                if (Interlocked.CompareExchange(ref _hubConnectInProgress, 1, 0) != 0)
                {
                    // Another refresh is still connecting: keep the rows waiting without using up a re-check
                    // delay. That refresh schedules the next check when its connect attempt ends.
                    PublishSyncDiagnostics(data, restApiReachable: true, syncStarting: !IsSyncSettled(data));
                    return;
                }

                var hostConnectedNow = false;
                try
                {
                    if (!_runtimeHubConnected && IsInSyncStartPhase())
                    {
                        // Shows "Connecting..." while the RuntimeHub host connects below.
                        PublishSyncDiagnostics(data, restApiReachable: true, syncStarting: true);
                    }

                    if (!_photoHubClient.IsConnected)
                    {
                        try
                        {
                            await _photoHubClient.ConnectAsync("127.0.0.1", port);
                            _photoHubConnected = true;
                            _logger.LogInformation("PhotoHub connected for photo upload notifications");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogDebug(ex, "PhotoHub connect failed (will retry on next refresh)");
                        }
                    }

                    if (!_runtimeHubConnected)
                    {
                        try
                        {
                            await _runtimeHubService.ConnectHostAsync(port, _disposeCts.Token);
                            _runtimeHubConnected = true;
                            hostConnectedNow = true;
                            _logger.LogInformation("RuntimeHub host connected for MOBAsmart sync");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogDebug(ex, "RuntimeHub host connect failed (will retry on next refresh)");
                        }
                    }
                }
                finally
                {
                    Volatile.Write(ref _hubConnectInProgress, 0);
                }

                // The status above was read before this refresh connected the host and before pending pushes
                // landed: while the synchronization is set up, re-check within seconds instead of after 30 s.
                // A host that just connected always gets the prompt re-checks, however long the connect took.
                var syncStarting = ScheduleNextReachablePoll(data, restartStartPhase: hostConnectedNow);
                PublishSyncDiagnostics(data, restApiReachable: true, syncStarting);
            }
            else
            {
                PublishUnreachable(port);
            }
        }
        catch (OperationCanceledException)
        {
            // Shutdown in progress – do not update UI or log
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "REST API status check failed");
            PublishUnreachable(_appSettings.RestApi.Port > 0 ? _appSettings.RestApi.Port : 5001);
        }
    }

    private void PublishUnreachable(int port)
    {
        var statusText = BuildUnreachableStatusText(port);
        _eventBus.Publish(new RestApiStatusChangedEvent(statusText, isReachable: false, clients: null)
        {
            IsStarting = _appSettings.Application.AutoStartWebApp
        });
        PublishSyncDiagnostics(data: null, restApiReachable: false, syncStarting: false);

        lock (_pollLock)
        {
            // A REST API that comes back sets up the synchronization again.
            _syncStartPolls = 0;
            SetPollIntervalLocked(_appSettings.Application.AutoStartWebApp
                ? PollIntervalWhenWaiting
                : PollIntervalWhenReachable);
        }
    }

    private bool IsInSyncStartPhase()
    {
        lock (_pollLock)
        {
            return _syncStartPolls < SyncStartPollDelays.Length;
        }
    }

    /// <summary>
    /// Chooses the next poll after a reachable status: 30 s once the synchronization is set up, otherwise the next
    /// re-check delay. Returns whether the synchronization is still being set up.
    /// </summary>
    private bool ScheduleNextReachablePoll(StatusResponse? data, bool restartStartPhase)
    {
        lock (_pollLock)
        {
            if (restartStartPhase)
            {
                _syncStartPolls = 0;
            }

            if (IsSyncSettled(data))
            {
                _syncStartPolls = 0;
                SetPollIntervalLocked(PollIntervalWhenReachable);
                return false;
            }

            if (_syncStartPolls >= SyncStartPollDelays.Length)
            {
                // Still not set up: fall back to the normal interval; a failed host connect is retried there.
                SetPollIntervalLocked(PollIntervalWhenReachable);
                return false;
            }

            SetPollIntervalLocked(SyncStartPollDelays[_syncStartPolls++]);
            return true;
        }
    }

    /// <summary>
    /// The synchronization is set up when the server confirms the connected RuntimeHub host and every successful
    /// solution and snapshot push is visible in the status that was read.
    /// </summary>
    private bool IsSyncSettled(StatusResponse? data)
    {
        var hostConfirmed = _runtimeHubConnected
            && _runtimeHubHostClient.IsConnected
            && (data?.Runtime?.HasHost ?? false);
        var solutionPushNotVisible = _solutionSyncService.LastSolutionPushSucceeded
            && !(data?.Solution?.Available ?? false);
        var snapshotPushNotVisible = _runtimeHubService.LastRestCachePushSucceeded
            && !(data?.Runtime?.SnapshotCache?.Available ?? false);
        return hostConfirmed && !solutionPushNotVisible && !snapshotPushNotVisible;
    }

    private async Task DisconnectRuntimeHubHostAsync()
    {
        if (!_runtimeHubConnected && !_runtimeHubHostClient.IsConnected)
        {
            return;
        }

        try
        {
            await _runtimeHubService.DisconnectHostAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "RuntimeHub host disconnect failed");
        }
        finally
        {
            _runtimeHubConnected = false;
        }
    }

    /// <summary>Changes the poll interval; a running countdown restarts with the new interval. Caller holds the lock.</summary>
    private void SetPollIntervalLocked(TimeSpan interval)
    {
        if (IsDisposed || _pollInterval == interval)
        {
            return;
        }

        _pollInterval = interval;
        if (_isPolling)
        {
            _timer.Change(interval, Timeout.InfiniteTimeSpan);
        }
    }

    /// <summary>
    /// Builds a clear status message when the REST API is not reachable,
    /// depending on whether the RestApi process was started and Auto-start is enabled.
    /// </summary>
    private string BuildUnreachableStatusText(int port)
    {
        if (_appSettings.Application.AutoStartWebApp)
            return "Waiting for the REST API to start...";
        if (_restApiProcessService.IsRunning)
            return $"Not reachable (port {port}) – check connection";
        return "Not started – enable Auto-start in Settings";
    }

    /// <summary>
    /// Stops periodic refresh and disconnects PhotoHub (SignalR) so the process can exit cleanly.
    /// Call this before stopping the RestApi process so SignalR disconnects cleanly and does not start reconnect timers.
    /// </summary>
    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        lock (_disposeLock)
        {
            if (_disposeTask == null)
            {
                Volatile.Write(ref _disposeState, 1);
                _disposeTask = DisposeCoreAsync();
            }

            return new ValueTask(_disposeTask);
        }
    }

    private async Task DisposeCoreAsync()
    {
        _restApiProcessService.ApiBecameReachable -= OnRestApiBecameReachable;
        _photoHubClient.PhotoUploaded -= OnPhotoUploadedAsync;
        lock (_pollLock)
        {
            _isPolling = false;
            _timer.Dispose();
        }

        try { await _disposeCts.CancelAsync().ConfigureAwait(false); } catch (ObjectDisposedException) { /* already disposed */ }

        Task[] refreshTasks;
        lock (_refreshTasksLock)
        {
            refreshTasks = [.. _refreshTasks];
        }

        try
        {
            try
            {
                await Task.WhenAll(refreshTasks).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "REST API refresh failed while stopping");
            }

            await DisconnectRuntimeHubHostAsync().ConfigureAwait(false);
            await DisconnectPhotoHubClientAsync().ConfigureAwait(false);
        }
        finally
        {
            _disposeCts.Dispose();
            GC.SuppressFinalize(this);
        }
    }

    private void QueueRefresh(string operationName)
    {
        if (IsDisposed)
        {
            return;
        }

        RefreshAsync().Observe(ex => _logger.LogDebug(ex, "{OperationName} failed", operationName));
    }

    private async Task DisconnectPhotoHubClientAsync()
    {
        if (!_photoHubConnected && !_photoHubClient.IsConnected)
        {
            return;
        }

        try
        {
            var disconnectTask = _photoHubClient.DisconnectAsync();
            var completedTask = await Task.WhenAny(disconnectTask, Task.Delay(TimeSpan.FromSeconds(3), CancellationToken.None)).ConfigureAwait(false);

            if (completedTask != disconnectTask)
            {
                _logger.LogDebug("PhotoHubClient disconnect timed out during app exit");
                return;
            }

            await disconnectTask.ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "PhotoHubClient disconnect failed");
        }
        finally
        {
            _photoHubConnected = false;
        }
    }

    private bool IsDisposed => Volatile.Read(ref _disposeState) != 0;

    private void PublishSyncDiagnostics(StatusResponse? data, bool restApiReachable, bool syncStarting)
    {
        var localSnapshot = _mobaRuntime.Current;
        var runtime = data?.Runtime;
        var solution = data?.Solution;
        var snapshotCache = runtime?.SnapshotCache;

        _eventBus.Publish(new MobaflowSyncDiagnosticsChangedEvent(new MobaflowSyncDiagnostics
        {
            RestApiReachable = restApiReachable,
            HostClientConnected = _runtimeHubHostClient.IsConnected,
            ServerHasHost = runtime?.HasHost ?? false,
            LastHubPushAt = _runtimeHubService.LastHubPushAt,
            LastHubPushSucceeded = _runtimeHubService.LastHubPushSucceeded,
            LastServerBroadcastAt = runtime?.LastSnapshotBroadcastAt,
            RemoteClientCount = runtime?.RemoteClientCount ?? 0,
            SessionOperational = runtime?.SessionOperational ?? false,
            LocalSnapshotCreatedAt = localSnapshot.CreatedAt,
            Z21Connected = localSnapshot.IsConnected,
            HasActiveProject = false,
            LocalSignalBoxElementCount = localSnapshot.SignalBoxElements.Count,
            LocalLocomotiveFleetCount = localSnapshot.LocomotiveFleet.Count,
            RestCacheAvailable = snapshotCache?.Available ?? false,
            RestCacheUpdatedAt = snapshotCache?.UpdatedAt,
            RestCacheIsConnected = snapshotCache?.IsConnected ?? false,
            RestCacheSignalBoxElementCount = snapshotCache?.SignalBoxElementCount ?? 0,
            RestCacheLocomotiveFleetCount = snapshotCache?.LocomotiveFleetCount ?? 0,
            LastRestCachePushAt = _runtimeHubService.LastRestCachePushAt,
            LastRestCachePushSucceeded = _runtimeHubService.LastRestCachePushSucceeded,
            SolutionAvailable = solution?.Available ?? false,
            SolutionUpdatedAt = solution?.UpdatedAt,
            SolutionActiveProjectName = solution?.ActiveProjectName,
            LastSolutionPushAt = _solutionSyncService.LastSolutionPushAt,
            LastSolutionPushSucceeded = _solutionSyncService.LastSolutionPushSucceeded,
            SyncStarting = syncStarting,
            HostConnectPending = !_runtimeHubConnected
        }));
    }

    private sealed record StatusResponse(
        int Port,
        List<ClientDto>? ConnectedClients,
        RuntimeStatusDto? Runtime,
        SolutionStatusDto? Solution);

    private sealed record RuntimeStatusDto(
        bool HasHost,
        int RemoteClientCount,
        DateTimeOffset? LastSnapshotBroadcastAt,
        bool SessionOperational,
        SnapshotCacheStatusDto? SnapshotCache);

    private sealed record SnapshotCacheStatusDto(
        bool Available,
        DateTimeOffset? UpdatedAt,
        bool IsConnected,
        int SignalBoxElementCount,
        int LocomotiveFleetCount);

    private sealed record SolutionStatusDto(
        bool Available,
        DateTimeOffset? UpdatedAt,
        string? ActiveProjectName);

    /// <summary>REST status payload item; deserialized via primary constructor (avoids unused synthetic property setters).</summary>
    private sealed record ClientDto(string? ClientId, string? DeviceName, DateTime ConnectedAt);

    /// <summary>
    /// Publishes photo uploaded event for UI handling.
    /// MainWindowViewModel subscribes, determines the target entity based on active page, and performs assignment.
    /// </summary>
    private Task OnPhotoUploadedAsync(string photoPath, DateTime uploadedAt)
    {
        _ = uploadedAt;

        // Publish event - UiThreadEventBusDecorator marshals to UI thread
        // MainWindowViewModel determines the actual target (loco/wagon) and performs assignment
        _eventBus.Publish(new PhotoAssignedEvent(photoPath));

        return Task.CompletedTask;
    }
}