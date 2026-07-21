// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.WinUI.Service;

using Backend.Interface;

using Common.Events;
using Common.Runtime;

using Microsoft.Extensions.Logging;

using SharedUI.Interface;

using System.Text;
using System.Text.Json;

/// <summary>
/// Connects the MOBAflow runtime host to MOBApi RuntimeHub and pushes snapshot updates.
/// </summary>
public sealed class RestApiRuntimeHubService : IAsyncDisposable
{
    private const int PushDebounceMilliseconds = 75;

    private readonly IRuntimeHubHostClient _runtimeHubHostClient;
    private readonly IMobaRuntime _mobaRuntime;
    private readonly IEventBus _eventBus;
    private readonly ILogger<RestApiRuntimeHubService> _logger;
    private readonly HostControlPlaneSession? _hostSession;
    private readonly object _debounceLock = new();
    private readonly Guid _subscriptionId;
    private CancellationTokenSource? _debounceCts;
    private Task _debounceTask = Task.CompletedTask;
    private MobaRuntimeSnapshot? _pendingSnapshot;
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
        IMobaRuntime mobaRuntime,
        IEventBus eventBus,
        ILogger<RestApiRuntimeHubService> logger,
        HostControlPlaneSession? hostSession = null)
    {
        _runtimeHubHostClient = runtimeHubHostClient;
        _mobaRuntime = mobaRuntime;
        _eventBus = eventBus;
        _logger = logger;
        _hostSession = hostSession;
        _subscriptionId = _eventBus.Subscribe<RuntimeSnapshotChangedEvent>(OnRuntimeSnapshotChanged);
    }

    public async Task ConnectHostAsync(int port, CancellationToken cancellationToken = default)
    {
        if (!_runtimeHubHostClient.IsConnected)
        {
            await _runtimeHubHostClient.ConnectAsync("127.0.0.1", port, cancellationToken).ConfigureAwait(false);
        }

        await PushSnapshotImmediateAsync(_mobaRuntime.Current, cancellationToken).ConfigureAwait(false);
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

        _eventBus.Unsubscribe(_subscriptionId);
        CancellationTokenSource? debounceCts;
        Task debounceTask;
        lock (_debounceLock)
        {
            debounceCts = _debounceCts;
            _debounceCts = null;
            debounceTask = _debounceTask;
            _pendingSnapshot = null;
        }

        debounceCts?.Cancel();
        debounceCts?.Dispose();

        await debounceTask.ConfigureAwait(false);
        await DisconnectHostAsync().ConfigureAwait(false);
        GC.SuppressFinalize(this);
    }

    private void OnRuntimeSnapshotChanged(RuntimeSnapshotChangedEvent e)
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

            _pendingSnapshot = snapshot;
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
            MobaRuntimeSnapshot? snapshot;
            lock (_debounceLock)
            {
                snapshot = _pendingSnapshot;
                _pendingSnapshot = null;
            }

            if (snapshot == null)
            {
                return;
            }

            await PushSnapshotImmediateAsync(snapshot, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Runtime snapshot push failed");
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
        if (_hostSession?.IsEnrolled != true)
            return false;

        var json = RuntimeJsonSerializer.Serialize(snapshot);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        using var request = new HttpRequestMessage(HttpMethod.Put, "api/runtime/snapshot") { Content = content };
        using var response = await _hostSession.SendAsync(request, cancellationToken).ConfigureAwait(false);
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
