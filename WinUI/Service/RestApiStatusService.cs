// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.Service;

using Common.Configuration;
using Microsoft.Extensions.Logging;
using SharedUI.Interface;
using SharedUI.ViewModel;
using System.Text.Json;

/// <summary>
/// Polls the REST API (RestApi project) status and updates MainWindowViewModel with status and connected clients.
/// </summary>
internal sealed class RestApiStatusService : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly AppSettings _appSettings;
    private readonly RestApiProcessService _restApiProcessService;
    private readonly MainWindowViewModel _viewModel;
    private readonly IUiDispatcher _uiDispatcher;
    private readonly ILogger<RestApiStatusService> _logger;
    private readonly System.Timers.Timer _timer;
    private static readonly JsonSerializerOptions s_jsonOptions = new() { PropertyNameCaseInsensitive = true };

    public RestApiStatusService(
        HttpClient httpClient,
        AppSettings appSettings,
        RestApiProcessService restApiProcessService,
        MainWindowViewModel viewModel,
        IUiDispatcher uiDispatcher,
        ILogger<RestApiStatusService> logger)
    {
        _httpClient = httpClient;
        _appSettings = appSettings;
        _restApiProcessService = restApiProcessService;
        _viewModel = viewModel;
        _uiDispatcher = uiDispatcher;
        _logger = logger;
        _httpClient.Timeout = TimeSpan.FromSeconds(5);
        _timer = new System.Timers.Timer(30_000); // 30 seconds
        _timer.Elapsed += (_, _) => _ = RefreshAsync();
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
    }

    /// <summary>
    /// Fetches REST API status and updates the ViewModel.
    /// </summary>
    public async Task RefreshAsync()
    {
        var port = _appSettings.RestApi.Port;
        if (port <= 0) port = 5001;
        var url = $"http://127.0.0.1:{port}/api/status";
        try
        {
            var response = await _httpClient.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
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
            }
            else
            {
                var statusText = BuildUnreachableStatusText(port);
                _uiDispatcher.InvokeOnUi(() => _viewModel.UpdateRestApiStatus(statusText, false, null));
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "REST API status check failed");
            var portFallback = _appSettings.RestApi?.Port > 0 ? _appSettings.RestApi.Port : 5001;
            var statusText = BuildUnreachableStatusText(portFallback);
            _uiDispatcher.InvokeOnUi(() => _viewModel.UpdateRestApiStatus(statusText, false, null));
        }
    }

    /// <summary>
    /// Builds a clear status message when the REST API is not reachable,
    /// depending on whether the RestApi process was started and Auto-start is enabled.
    /// </summary>
    private string BuildUnreachableStatusText(int port)
    {
        if (_restApiProcessService.IsRunning)
            return $"Not reachable (port {port}) – check connection";
        if (_appSettings.Application?.AutoStartWebApp == true)
            return $"Server may have failed to start (port {port})";
        return "Not started – enable Auto-start in Settings";
    }

    public void Dispose()
    {
        _timer.Dispose();
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
