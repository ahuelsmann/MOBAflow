// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.Service;

using Backend.Interface;

using Common.Configuration;
using Common.Events;
using Common.Extension;

using Microsoft.Extensions.Logging;

using SharedUI.Interface;

using System.Text.Json;
using System.Timers;

/// <summary>
/// Polls the REST API (MOBApi process) status and publishes events when status changes.
/// When the API is reachable, connects PhotoHubClient so WinUI receives photo upload notifications.
/// Uses EventBus for UI decoupling - no direct dispatcher or view model dependencies.
/// </summary>
public sealed class RestApiStatusService : IAsyncDisposable
{
    private const int PollIntervalWhenReachableMs = 30_000;  // 30 s when API is up
    private const int PollIntervalWhenWaitingMs = 2_000;      // 2 s while "Waiting for the REST API to start..."

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
    private readonly Timer _timer;
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
        ILogger<RestApiStatusService> logger)
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
        _timer = new Timer(PollIntervalWhenWaitingMs);
        _timer.Elapsed += (_, _) => QueueRefresh("Timer refresh");

        _restApiProcessService.ApiBecameReachable += OnRestApiBecameReachable;
        _photoHubClient.PhotoUploaded += OnPhotoUploadedAsync;
    }

    private void OnRestApiBecameReachable(object? sender, int port)
    {
        QueueRefresh("Reachability event refresh");
    }

    /// <summary>
    /// Starts periodic status refresh (every 30s).
    /// </summary>
    public void Start()
    {
        if (IsDisposed)
        {
            return;
        }

        _timer.Start();
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

        _timer.Stop();
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

        _timer.Stop();
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

        _timer.Start();
        _logger.LogDebug("Health check polling resumed due to window activation");
        QueueRefresh("Activation resume refresh");
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
        var url = $"http://127.0.0.1:{port}/api/status";
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
                PublishSyncDiagnostics(data, restApiReachable: true);

                SetPollInterval(PollIntervalWhenReachableMs);

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
                        _logger.LogInformation("RuntimeHub host connected for MOBAsmart sync");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug(ex, "RuntimeHub host connect failed (will retry on next refresh)");
                    }
                }
            }
            else
            {
                var statusText = BuildUnreachableStatusText(port);
                _eventBus.Publish(new RestApiStatusChangedEvent(statusText, isReachable: false, clients: null));
                PublishSyncDiagnostics(data: null, restApiReachable: false);
                SetPollInterval(_appSettings.Application.AutoStartWebApp
                    ? PollIntervalWhenWaitingMs
                    : PollIntervalWhenReachableMs);
            }
        }
        catch (OperationCanceledException)
        {
            // Shutdown in progress – do not update UI or log
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "REST API status check failed");
            var portFallback = _appSettings.RestApi.Port > 0 ? _appSettings.RestApi.Port : 5001;
            var statusText = BuildUnreachableStatusText(portFallback);
            _eventBus.Publish(new RestApiStatusChangedEvent(statusText, isReachable: false, clients: null));
            PublishSyncDiagnostics(data: null, restApiReachable: false);
            SetPollInterval(_appSettings.Application.AutoStartWebApp
                ? PollIntervalWhenWaitingMs
                : PollIntervalWhenReachableMs);
        }
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

    private void SetPollInterval(int intervalMs)
    {
        if (IsDisposed)
        {
            return;
        }

        if (Math.Abs(_timer.Interval - intervalMs) > 0.001)
        {
            _timer.Interval = intervalMs;
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
        _timer.Stop();
        _timer.Dispose();
        try { _disposeCts.Cancel(); } catch (ObjectDisposedException) { /* already disposed */ }

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
            var completedTask = await Task.WhenAny(disconnectTask, Task.Delay(TimeSpan.FromSeconds(3))).ConfigureAwait(false);

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

    private void PublishSyncDiagnostics(StatusResponse? data, bool restApiReachable)
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
            LastSolutionPushSucceeded = _solutionSyncService.LastSolutionPushSucceeded
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