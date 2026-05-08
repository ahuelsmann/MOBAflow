// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.Service;

using Common.Configuration;
using Common.Events;
using Common.Extension;

using Microsoft.Extensions.Logging;

using System.Text.Json;
using System.Timers;

/// <summary>
/// Polls the REST API (MOBApi process) status and publishes events when status changes.
/// When the API is reachable, connects PhotoHubClient so WinUI receives photo upload notifications.
/// Uses EventBus for UI decoupling - no direct dispatcher or view model dependencies.
/// </summary>
public sealed class RestApiStatusService : IDisposable
{
    private const int PollIntervalWhenReachableMs = 30_000;  // 30 s when API is up
    private const int PollIntervalWhenWaitingMs = 2_000;      // 2 s while "Waiting for the REST API to start..."

    private readonly HttpClient _httpClient;
    private readonly AppSettings _appSettings;
    private readonly RestApiProcessService _restApiProcessService;
    private readonly PhotoHubClient _photoHubClient;
    private readonly IEventBus _eventBus;
    private readonly ILogger<RestApiStatusService> _logger;
    private readonly Timer _timer;
    private static readonly JsonSerializerOptions SJsonOptions = new() { PropertyNameCaseInsensitive = true };
    private bool _photoHubConnected;
    private readonly CancellationTokenSource _disposeCts = new();
    private bool _disposed;

    public RestApiStatusService(
        HttpClient httpClient,
        AppSettings appSettings,
        RestApiProcessService restApiProcessService,
        PhotoHubClient photoHubClient,
        IEventBus eventBus,
        ILogger<RestApiStatusService> logger)
    {
        _httpClient = httpClient;
        _appSettings = appSettings;
        _restApiProcessService = restApiProcessService;
        _photoHubClient = photoHubClient;
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
        if (_disposed)
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
        if (_disposed)
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
        if (_disposed)
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
        if (_disposed)
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
    public async Task RefreshAsync()
    {
        if (_disposed)
            return;
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
                var statusText = data != null
                    ? $"Running on port {data.Port}"
                    : $"Running on port {port}";

                // Publish event - UiThreadEventBusDecorator marshals to UI thread
                _eventBus.Publish(new RestApiStatusChangedEvent(statusText, isReachable: true, clients));

                SetPollInterval(PollIntervalWhenReachableMs);

                if (!_photoHubConnected)
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
            }
            else
            {
                var statusText = BuildUnreachableStatusText(port);
                _eventBus.Publish(new RestApiStatusChangedEvent(statusText, isReachable: false, clients: null));
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
            SetPollInterval(_appSettings.Application.AutoStartWebApp
                ? PollIntervalWhenWaitingMs
                : PollIntervalWhenReachableMs);
        }
    }

    private void SetPollInterval(int intervalMs)
    {
        if (_disposed)
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
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _restApiProcessService.ApiBecameReachable -= OnRestApiBecameReachable;
        _photoHubClient.PhotoUploaded -= OnPhotoUploadedAsync;
        _timer.Stop();
        _timer.Dispose();
        try { _disposeCts.Cancel(); } catch (ObjectDisposedException) { /* already disposed */ }
        _disposeCts.Dispose();

        // Disconnect SignalR so it doesn't keep the process alive on exit
        DisposePhotoHubClientAsync().Observe(ex => _logger.LogDebug(ex, "PhotoHubClient disconnect during dispose"));
    }

    private void QueueRefresh(string operationName)
    {
        if (_disposed)
        {
            return;
        }

        RefreshAsync().Observe(ex => _logger.LogDebug(ex, "{OperationName} failed", operationName));
    }

    private async Task DisposePhotoHubClientAsync()
    {
        var disposeTask = _photoHubClient.DisposeAsync().AsTask();
        var completedTask = await Task.WhenAny(disposeTask, Task.Delay(TimeSpan.FromSeconds(3))).ConfigureAwait(false);

        if (completedTask != disposeTask)
        {
            _logger.LogDebug("PhotoHubClient disconnect timed out during app exit");
            return;
        }

        await disposeTask.ConfigureAwait(false);
    }

    private sealed record StatusResponse(int Port, List<ClientDto>? ConnectedClients);

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