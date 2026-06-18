// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.Service;

using Common.Configuration;

using Domain;

using Microsoft.Extensions.Logging;

using SharedUI.ViewModel;

using System.Text;
using System.Text.Json;

/// <summary>
/// Pushes the in-memory <see cref="Solution"/> to the MOBApi cache whenever it changes or MOBApi becomes reachable.
/// </summary>
public sealed class RestApiSolutionSyncService : IDisposable
{
    private const int DebounceMilliseconds = 500;

    private readonly Solution _solution;
    private readonly AppSettings _appSettings;
    private readonly HttpClient _httpClient;
    private readonly ILogger<RestApiSolutionSyncService> _logger;
    private readonly object _debounceLock = new();
    private CancellationTokenSource? _debounceCts;
    private bool _disposed;

    public RestApiSolutionSyncService(
        Solution solution,
        AppSettings appSettings,
        IHttpClientFactory httpClientFactory,
        MainWindowViewModel mainWindowViewModel,
        RestApiProcessService restApiProcessService,
        ILogger<RestApiSolutionSyncService> logger)
    {
        ArgumentNullException.ThrowIfNull(solution);
        ArgumentNullException.ThrowIfNull(appSettings);
        ArgumentNullException.ThrowIfNull(httpClientFactory);
        ArgumentNullException.ThrowIfNull(mainWindowViewModel);
        ArgumentNullException.ThrowIfNull(restApiProcessService);
        ArgumentNullException.ThrowIfNull(logger);

        _solution = solution;
        _appSettings = appSettings;
        _httpClient = httpClientFactory.CreateClient(nameof(RestApiSolutionSyncService));
        _logger = logger;

        mainWindowViewModel.SolutionLoaded += OnSolutionChanged;
        mainWindowViewModel.SolutionSaving += OnSolutionChanged;
        restApiProcessService.ApiBecameReachable += OnApiBecameReachable;
    }

    private void OnSolutionChanged(object? sender, EventArgs e) => QueuePush();

    private void OnApiBecameReachable(object? sender, int port)
    {
        _ = port;
        QueuePush();
    }

    /// <summary>
    /// Schedules a debounced push of the current solution to MOBApi.
    /// </summary>
    public void QueuePush()
    {
        if (_disposed)
        {
            return;
        }

        CancellationToken token;
        lock (_debounceLock)
        {
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
            await Task.Delay(DebounceMilliseconds, cancellationToken).ConfigureAwait(false);
            await PushAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Superseded by a newer push request.
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Debounced solution push failed");
        }
    }

    private async Task PushAsync(CancellationToken cancellationToken)
    {
        var port = _appSettings.RestApi.Port > 0 ? _appSettings.RestApi.Port : 5001;
        var json = JsonSerializer.Serialize(_solution, JsonOptions.Default);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        using var request = new HttpRequestMessage(HttpMethod.Put, $"http://127.0.0.1:{port}/api/solution")
        {
            Content = content
        };

        try
        {
            var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
            {
                _logger.LogDebug("Solution pushed to MOBApi on port {Port}", port);
                await PushRuntimeSettingsAsync(port, cancellationToken).ConfigureAwait(false);
                return;
            }

            var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            _logger.LogWarning(
                "Solution push to MOBApi failed with status {StatusCode}: {Body}",
                (int)response.StatusCode,
                body);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogDebug(ex, "MOBApi not reachable for solution push on port {Port}", port);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogDebug(ex, "Solution push to MOBApi timed out on port {Port}", port);
        }
    }

    private async Task PushRuntimeSettingsAsync(int port, CancellationToken cancellationToken)
    {
        var z21Ip = _appSettings.Z21.CurrentIpAddress?.Trim();
        if (string.IsNullOrEmpty(z21Ip))
        {
            return;
        }

        var z21Port = 21105;
        if (!string.IsNullOrWhiteSpace(_appSettings.Z21.DefaultPort)
            && int.TryParse(_appSettings.Z21.DefaultPort, out var parsedPort)
            && parsedPort > 0)
        {
            z21Port = parsedPort;
        }

        var body = JsonSerializer.Serialize(new { z21IpAddress = z21Ip, z21Port });
        using var content = new StringContent(body, Encoding.UTF8, "application/json");
        using var request = new HttpRequestMessage(HttpMethod.Put, $"http://127.0.0.1:{port}/api/runtime-settings")
        {
            Content = content
        };

        try
        {
            var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
            {
                _logger.LogDebug("Runtime settings pushed to MOBApi on port {Port}", port);
                return;
            }

            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            _logger.LogWarning(
                "Runtime settings push to MOBApi failed with status {StatusCode}: {Body}",
                (int)response.StatusCode,
                responseBody);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogDebug(ex, "MOBApi not reachable for runtime settings push on port {Port}", port);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogDebug(ex, "Runtime settings push to MOBApi timed out on port {Port}", port);
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        lock (_debounceLock)
        {
            _debounceCts?.Cancel();
            _debounceCts?.Dispose();
            _debounceCts = null;
        }
    }
}