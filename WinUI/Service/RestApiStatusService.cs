// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.Service;

using Common.Configuration;
using Microsoft.Extensions.Logging;
using SharedUI.Interface;
using SharedUI.ViewModel;
using System.Text.Json;

/// <summary>
/// Polls the REST API (RestApi project) status and updates MainWindowViewModel with status and connected clients.
/// When the API is reachable, connects PhotoHubClient so WinUI receives photo upload notifications and assigns the photo to the selected item.
/// </summary>
internal sealed class RestApiStatusService : IDisposable
{
    private const int PollIntervalWhenReachableMs = 30_000;  // 30 s when API is up
    private const int PollIntervalWhenWaitingMs = 2_000;      // 2 s while "Waiting for the REST API to start..."

    private readonly HttpClient _httpClient;
    private readonly AppSettings _appSettings;
    private readonly RestApiProcessService _restApiProcessService;
    private readonly PhotoHubClient _photoHubClient;
    private readonly MainWindowViewModel _viewModel;
    private readonly IUiDispatcher _uiDispatcher;
    private readonly ILogger<RestApiStatusService> _logger;
    private readonly System.Timers.Timer _timer;
    private static readonly JsonSerializerOptions s_jsonOptions = new() { PropertyNameCaseInsensitive = true };
    private bool _photoHubConnected;
    private readonly CancellationTokenSource _disposeCts = new();

    public RestApiStatusService(
        HttpClient httpClient,
        AppSettings appSettings,
        RestApiProcessService restApiProcessService,
        PhotoHubClient photoHubClient,
        MainWindowViewModel viewModel,
        IUiDispatcher uiDispatcher,
        ILogger<RestApiStatusService> logger)
    {
        _httpClient = httpClient;
        _appSettings = appSettings;
        _restApiProcessService = restApiProcessService;
        _photoHubClient = photoHubClient;
        _viewModel = viewModel;
        _uiDispatcher = uiDispatcher;
        _logger = logger;
        _httpClient.Timeout = TimeSpan.FromSeconds(5);
        _timer = new System.Timers.Timer(PollIntervalWhenWaitingMs);
        _timer.Elapsed += (_, _) => _ = RefreshAsync();

        _photoHubClient.PhotoUploaded += OnPhotoUploaded;
        _restApiProcessService.ApiBecameReachable += OnRestApiBecameReachable;
    }

    private void OnRestApiBecameReachable(object? sender, int port)
    {
        _ = RefreshAsync();
    }

    private async Task OnPhotoUploaded(string photoPath, DateTime uploadedAt)
    {
        _uiDispatcher.InvokeOnUi(() => _viewModel.AssignLatestPhoto(photoPath));
        await Task.CompletedTask;
    }

    /// <summary>
    /// Starts periodic status refresh (every 30s).
    /// </summary>
    public void Start()
    {
        _timer.Start();
        _ = RefreshAsync();
    }

    /// <summary>
    /// Stops periodic refresh.
    /// </summary>
    public void Stop()
    {
        _timer.Stop();
        try { _disposeCts.Cancel(); } catch (ObjectDisposedException) { /* already disposed */ }
    }

    /// <summary>
    /// Fetches REST API status and updates the ViewModel.
    /// </summary>
    public async Task RefreshAsync()
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
                var data = JsonSerializer.Deserialize<StatusResponse>(json, s_jsonOptions);
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
                _uiDispatcher.InvokeOnUi(() => _viewModel.UpdateRestApiStatus(statusText, isReachable: true, clients));

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
                _uiDispatcher.InvokeOnUi(() => _viewModel.UpdateRestApiStatus(statusText, false, null));
                SetPollInterval(_appSettings.Application?.AutoStartWebApp == true
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
            var portFallback = _appSettings.RestApi?.Port > 0 ? _appSettings.RestApi.Port : 5001;
            var statusText = BuildUnreachableStatusText(portFallback);
            _uiDispatcher.InvokeOnUi(() => _viewModel.UpdateRestApiStatus(statusText, false, null));
            SetPollInterval(_appSettings.Application?.AutoStartWebApp == true
                ? PollIntervalWhenWaitingMs
                : PollIntervalWhenReachableMs);
        }
    }

    private void SetPollInterval(int intervalMs)
    {
        if (_timer.Interval != intervalMs)
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
        if (_appSettings.Application?.AutoStartWebApp == true)
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
        _photoHubClient.PhotoUploaded -= OnPhotoUploaded;
        _restApiProcessService.ApiBecameReachable -= OnRestApiBecameReachable;
        _timer.Stop();
        _timer.Dispose();
        try { _disposeCts.Cancel(); } catch (ObjectDisposedException) { /* already disposed */ }
        _disposeCts.Dispose();

        // Disconnect SignalR so it doesn't keep the process alive on exit
        try
        {
            var disposeTask = Task.Run(() => _photoHubClient.DisposeAsync().AsTask());
            if (!disposeTask.Wait(TimeSpan.FromSeconds(3)))
                _logger.LogDebug("PhotoHubClient disconnect timed out during app exit");
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "PhotoHubClient disconnect during dispose");
        }
    }

    private sealed class StatusResponse
    {
        public string? Status { get; set; }
        public int Port { get; set; }
        public List<ClientDto>? ConnectedClients { get; set; }
    }

    private sealed class ClientDto
    {
        public string? ClientId { get; set; }
        public string? DeviceName { get; set; }
        public DateTime ConnectedAt { get; set; }
    }
}
