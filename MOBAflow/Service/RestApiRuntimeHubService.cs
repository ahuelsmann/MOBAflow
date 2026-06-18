// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.WinUI.Service;

using Backend.Interface;

using Common.Configuration;
using Common.Events;
using Common.Runtime;

using Microsoft.Extensions.Logging;

using SharedUI.Interface;

using System.Text;
using System.Text.Json;

/// <summary>
/// Connects the MOBAflow runtime host to MOBApi RuntimeHub and pushes snapshot updates.
/// </summary>
public sealed class RestApiRuntimeHubService : IDisposable
{
    private const int PushDebounceMilliseconds = 250;

    private readonly IRuntimeHubHostClient _runtimeHubHostClient;
    private readonly IMobaRuntime _mobaRuntime;
    private readonly IEventBus _eventBus;
    private readonly AppSettings _appSettings;
    private readonly HttpClient _httpClient;
    private readonly ILogger<RestApiRuntimeHubService> _logger;
    private readonly object _debounceLock = new();
    private readonly Guid _subscriptionId;
    private CancellationTokenSource? _debounceCts;
    private MobaRuntimeSnapshot? _pendingSnapshot;
    private bool _disposed;

    public RestApiRuntimeHubService(
        IRuntimeHubHostClient runtimeHubHostClient,
        IMobaRuntime mobaRuntime,
        IEventBus eventBus,
        AppSettings appSettings,
        IHttpClientFactory httpClientFactory,
        ILogger<RestApiRuntimeHubService> logger)
    {
        _runtimeHubHostClient = runtimeHubHostClient;
        _mobaRuntime = mobaRuntime;
        _eventBus = eventBus;
        _appSettings = appSettings;
        _httpClient = httpClientFactory.CreateClient(nameof(RestApiRuntimeHubService));
        _logger = logger;
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

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _eventBus.Unsubscribe(_subscriptionId);
        lock (_debounceLock)
        {
            _debounceCts?.Cancel();
            _debounceCts?.Dispose();
            _debounceCts = null;
        }

        DisconnectHostAsync().GetAwaiter().GetResult();
    }

    private void OnRuntimeSnapshotChanged(RuntimeSnapshotChangedEvent e)
    {
        QueuePush(e.Snapshot);
    }

    private void QueuePush(MobaRuntimeSnapshot snapshot)
    {
        if (_disposed)
        {
            return;
        }

        CancellationToken token;
        lock (_debounceLock)
        {
            _pendingSnapshot = snapshot;
            _debounceCts?.Cancel();
            _debounceCts?.Dispose();
            _debounceCts = new CancellationTokenSource();
            token = _debounceCts.Token;
        }

        _ = PushDebouncedAsync(token);
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
        if (_runtimeHubHostClient.IsConnected)
        {
            try
            {
                await _runtimeHubHostClient.PushSnapshotAsync(snapshot, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Runtime snapshot hub push failed, using REST fallback");
            }
        }

        await PushSnapshotRestFallbackAsync(snapshot, cancellationToken).ConfigureAwait(false);
    }

    private async Task PushSnapshotRestFallbackAsync(MobaRuntimeSnapshot snapshot, CancellationToken cancellationToken)
    {
        var port = _appSettings.RestApi.Port > 0 ? _appSettings.RestApi.Port : 5001;
        var json = RuntimeJsonSerializer.Serialize(snapshot);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        using var response = await _httpClient
            .PutAsync($"http://127.0.0.1:{port}/api/runtime/snapshot", content, cancellationToken)
            .ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogDebug("Runtime REST snapshot push returned {StatusCode}", (int)response.StatusCode);
        }
    }
}
