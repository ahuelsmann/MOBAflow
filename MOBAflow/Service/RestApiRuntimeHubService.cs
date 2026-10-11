// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.WinUI.Service;

using Backend.Interface;

using Common.Runtime;

using Microsoft.Extensions.Logging;

using SharedUI.Interface;

using System.Text;

/// <summary>
/// Connects the MOBAflow runtime host to MOBApi RuntimeHub and pushes snapshot updates.
/// </summary>
public sealed class RestApiRuntimeHubService : IAsyncDisposable
{
    private const int PushDebounceMilliseconds = 75;

    private static readonly Action<ILogger, Guid, Exception?> LogProjectPushFailed =
        LoggerMessage.Define<Guid>(
            LogLevel.Debug,
            new EventId(1, nameof(LogProjectPushFailed)),
            "Runtime snapshot push of project {ProjectId} failed");

    private readonly IRuntimeHubHostClient _runtimeHubHostClient;
    private readonly IProjectRuntimeSnapshots _host;
    private readonly ILogger<RestApiRuntimeHubService> _logger;
    private readonly LocalMobApiClient _mobApiClient;
    private readonly object _debounceLock = new();
    private CancellationTokenSource? _debounceCts;
    private Task _debounceTask = Task.CompletedTask;
    // Every project has its own runtime; the latest snapshot of each project waits for the next push.
    private readonly Dictionary<Guid, MobaRuntimeSnapshot> _pendingSnapshots = [];
    private int _disposeState;
    private readonly object _metricsLock = new();
    private DateTimeOffset? _lastHubPushAt;
    private bool _lastHubPushSucceeded;
    private DateTimeOffset? _lastRestCachePushAt;
    private bool _lastRestCachePushSucceeded;

    public DateTimeOffset? LastHubPushAt
    {
        get
        {
            lock (_metricsLock)
            {
                return _lastHubPushAt;
            }
        }
    }

    public bool LastHubPushSucceeded
    {
        get
        {
            lock (_metricsLock)
            {
                return _lastHubPushSucceeded;
            }
        }
    }

    public DateTimeOffset? LastRestCachePushAt
    {
        get
        {
            lock (_metricsLock)
            {
                return _lastRestCachePushAt;
            }
        }
    }

    public bool LastRestCachePushSucceeded
    {
        get
        {
            lock (_metricsLock)
            {
                return _lastRestCachePushSucceeded;
            }
        }
    }

    public RestApiRuntimeHubService(
        IRuntimeHubHostClient runtimeHubHostClient,
        IProjectRuntimeSnapshots host,
        ILogger<RestApiRuntimeHubService> logger,
        LocalMobApiClient mobApiClient)
    {
        _runtimeHubHostClient = runtimeHubHostClient;
        _host = host ?? throw new ArgumentNullException(nameof(host));
        _logger = logger;
        _mobApiClient = mobApiClient ?? throw new ArgumentNullException(nameof(mobApiClient));
        _host.RuntimeSnapshotChanged += OnRuntimeSnapshotChanged;
    }

    public async Task ConnectHostAsync(int port, CancellationToken cancellationToken = default)
    {
        if (!_runtimeHubHostClient.IsConnected)
        {
            await _runtimeHubHostClient.ConnectAsync("127.0.0.1", port, cancellationToken).ConfigureAwait(false);
        }

        foreach (var snapshot in _host.Snapshots)
        {
            await PushSnapshotImmediateAsync(snapshot, cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task DisconnectHostAsync()
    {
        await _runtimeHubHostClient.DisconnectAsync().ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposeState, 1) != 0)
        {
            return;
        }

        _host.RuntimeSnapshotChanged -= OnRuntimeSnapshotChanged;
        CancellationTokenSource? debounceCts;
        Task debounceTask;
        lock (_debounceLock)
        {
            debounceCts = _debounceCts;
            _debounceCts = null;
            debounceTask = _debounceTask;
            _pendingSnapshots.Clear();
        }

        if (debounceCts is not null)
        {
            await debounceCts.CancelAsync().ConfigureAwait(false);
            debounceCts.Dispose();
        }

        await debounceTask.ConfigureAwait(false);
        await DisconnectHostAsync().ConfigureAwait(false);
        GC.SuppressFinalize(this);
    }

    private void OnRuntimeSnapshotChanged(object? sender, ProjectRuntimeSnapshotEventArgs e)
    {
        QueuePush(e.Snapshot);
    }

    private void QueuePush(MobaRuntimeSnapshot snapshot)
    {
        if (Volatile.Read(ref _disposeState) != 0)
        {
            return;
        }

        CancellationToken token;
        lock (_debounceLock)
        {
            if (_disposeState != 0)
            {
                return;
            }

            _pendingSnapshots[snapshot.ProjectId] = snapshot;
            _debounceCts?.Cancel();
            _debounceCts?.Dispose();
            _debounceCts = new CancellationTokenSource();
            token = _debounceCts.Token;
            _debounceTask = PushDebouncedAsync(token);
        }
    }

    private async Task PushDebouncedAsync(CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(PushDebounceMilliseconds, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return;
        }

        List<MobaRuntimeSnapshot> snapshots;
        lock (_debounceLock)
        {
            snapshots = [.. _pendingSnapshots.Values];
            _pendingSnapshots.Clear();
        }

        // A failed push of one project must not hold back the snapshots of the other projects.
        foreach (var snapshot in snapshots)
        {
            try
            {
                await PushSnapshotImmediateAsync(snapshot, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                LogProjectPushFailed(_logger, snapshot.ProjectId, ex);
            }
        }
    }

    private async Task PushSnapshotImmediateAsync(MobaRuntimeSnapshot snapshot, CancellationToken cancellationToken)
    {
        var remoteSnapshot = RuntimeSnapshotRemoteFilter.ForMobasmartBroadcast(snapshot);
        var hubSucceeded = false;
        if (_runtimeHubHostClient.IsConnected)
        {
            try
            {
                await _runtimeHubHostClient.PushSnapshotAsync(remoteSnapshot, cancellationToken).ConfigureAwait(false);
                hubSucceeded = true;
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Runtime snapshot hub push failed, using REST fallback");
            }
        }

        RecordHubPush(hubSucceeded);

        var restSucceeded = await PushSnapshotRestFallbackAsync(remoteSnapshot, cancellationToken).ConfigureAwait(false);
        RecordRestCachePush(restSucceeded);
    }

    private async Task<bool> PushSnapshotRestFallbackAsync(MobaRuntimeSnapshot snapshot, CancellationToken cancellationToken)
    {
        var json = RuntimeJsonSerializer.Serialize(snapshot);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        using var request = new HttpRequestMessage(HttpMethod.Put, "api/runtime/snapshot") { Content = content };
        using var response = await _mobApiClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogDebug("Runtime REST snapshot push returned {StatusCode}", (int)response.StatusCode);
            return false;
        }

        return true;
    }

    private void RecordHubPush(bool succeeded)
    {
        lock (_metricsLock)
        {
            _lastHubPushAt = DateTimeOffset.UtcNow;
            _lastHubPushSucceeded = succeeded;
        }
    }

    private void RecordRestCachePush(bool succeeded)
    {
        lock (_metricsLock)
        {
            _lastRestCachePushAt = DateTimeOffset.UtcNow;
            _lastRestCachePushSucceeded = succeeded;
        }
    }
}
