// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.WinUI.Service;



using Common.Configuration;

using Common.Events;



using Domain;



using Microsoft.Extensions.Logging;



using SharedUI.ViewModel;



using System.ComponentModel;

using System.Text;

using System.Text.Json;



/// <summary>

/// Pushes the in-memory <see cref="Solution"/> to the MOBApi cache whenever it changes or MOBApi becomes reachable.

/// Also pushes Z21 runtime settings when the Z21 connection is established or the endpoint changes.

/// </summary>

public sealed class RestApiSolutionSyncService : IDisposable

{

    private const int DebounceMilliseconds = 500;



    private readonly Solution _solution;

    private readonly AppSettings _appSettings;

    private readonly MainWindowViewModel _mainWindowViewModel;

    private readonly HttpClient _httpClient;

    private readonly IEventBus _eventBus;

    private readonly ILogger<RestApiSolutionSyncService> _logger;

    private readonly object _debounceLock = new();

    private readonly List<Guid> _eventSubscriptions = [];

    private CancellationTokenSource? _debounceCts;

    private string? _lastPushedZ21Ip;

    private bool _disposed;
    private readonly object _metricsLock = new();
    private DateTimeOffset? _lastSolutionPushAt;
    private bool _lastSolutionPushSucceeded;

    public DateTimeOffset? LastSolutionPushAt
    {
        get
        {
            lock (_metricsLock)
            {
                return _lastSolutionPushAt;
            }
        }
    }

    public bool LastSolutionPushSucceeded
    {
        get
        {
            lock (_metricsLock)
            {
                return _lastSolutionPushSucceeded;
            }
        }
    }



    public RestApiSolutionSyncService(

        Solution solution,

        AppSettings appSettings,

        IHttpClientFactory httpClientFactory,

        MainWindowViewModel mainWindowViewModel,

        RestApiProcessService restApiProcessService,

        IEventBus eventBus,

        ILogger<RestApiSolutionSyncService> logger)

    {

        ArgumentNullException.ThrowIfNull(solution);

        ArgumentNullException.ThrowIfNull(appSettings);

        ArgumentNullException.ThrowIfNull(httpClientFactory);

        ArgumentNullException.ThrowIfNull(mainWindowViewModel);

        ArgumentNullException.ThrowIfNull(restApiProcessService);

        ArgumentNullException.ThrowIfNull(eventBus);

        ArgumentNullException.ThrowIfNull(logger);



        _solution = solution;

        _appSettings = appSettings;

        _mainWindowViewModel = mainWindowViewModel;

        _httpClient = httpClientFactory.CreateClient(nameof(RestApiSolutionSyncService));

        _eventBus = eventBus;

        _logger = logger;



        mainWindowViewModel.SolutionLoaded += OnSolutionChanged;

        mainWindowViewModel.SolutionSaving += OnSolutionChanged;

        mainWindowViewModel.PropertyChanged += OnMainWindowViewModelPropertyChanged;

        restApiProcessService.ApiBecameReachable += OnApiBecameReachable;

        _eventSubscriptions.Add(_eventBus.Subscribe<Z21ConnectionEstablishedEvent>(_ => QueueRuntimeSettingsPush()));

    }



    private void OnSolutionChanged(object? sender, EventArgs e) => QueuePush();

    private void OnMainWindowViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainWindowViewModel.SelectedProject))
        {
            QueuePush();
        }
    }



    private void OnApiBecameReachable(object? sender, int port)
    {
        _ = port;
        _lastPushedZ21Ip = null;
        QueuePush();
        QueueRuntimeSettingsPush();
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



    /// <summary>

    /// Schedules a push of the current Z21 endpoint to MOBApi runtime settings.

    /// </summary>

    public void QueueRuntimeSettingsPush()

    {

        if (_disposed)

        {

            return;

        }



        _ = PushRuntimeSettingsDebouncedAsync();

    }



    private async Task PushRuntimeSettingsDebouncedAsync()

    {

        try

        {

            await Task.Delay(DebounceMilliseconds).ConfigureAwait(false);

            var port = _appSettings.RestApi.Port > 0 ? _appSettings.RestApi.Port : 5001;

            await PushRuntimeSettingsAsync(port, CancellationToken.None).ConfigureAwait(false);

        }

        catch (Exception ex)

        {

            _logger.LogDebug(ex, "Debounced runtime settings push failed");

        }

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

        var solutionPath = _mainWindowViewModel.CurrentSolutionPath;
        if (string.IsNullOrWhiteSpace(solutionPath))
        {
            solutionPath = "mobaflow://in-memory";
        }



        var port = _appSettings.RestApi.Port > 0 ? _appSettings.RestApi.Port : 5001;

        if (_solution.SchemaVersion != Solution.CurrentSchemaVersion)
        {
            _solution.SchemaVersion = Solution.CurrentSchemaVersion;
        }

        var json = JsonSerializer.Serialize(_solution, JsonOptions.Default);

        using var content = new StringContent(json, Encoding.UTF8, "application/json");



        using var request = new HttpRequestMessage(HttpMethod.Put, $"http://127.0.0.1:{port}/api/solution")

        {

            Content = content

        };

        request.Headers.TryAddWithoutValidation("X-MOBAflow-Solution-Path", solutionPath);

        var activeProjectName = _mainWindowViewModel.SelectedProject?.Name;
        if (!string.IsNullOrWhiteSpace(activeProjectName))
        {
            request.Headers.TryAddWithoutValidation("X-MOBAflow-Active-Project", activeProjectName);
        }



        var succeeded = false;

        try

        {

            var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);

            if (response.IsSuccessStatusCode)

            {

                succeeded = true;

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

        finally

        {

            RecordSolutionPush(succeeded);

        }

    }

    private void RecordSolutionPush(bool succeeded)

    {

        lock (_metricsLock)

        {

            _lastSolutionPushAt = DateTimeOffset.UtcNow;

            _lastSolutionPushSucceeded = succeeded;

        }

    }



    private async Task PushRuntimeSettingsAsync(int port, CancellationToken cancellationToken)

    {

        var z21Ip = _appSettings.Z21.CurrentIpAddress?.Trim();

        if (string.IsNullOrEmpty(z21Ip))

        {

            return;

        }



        if (string.Equals(_lastPushedZ21Ip, z21Ip, StringComparison.Ordinal))

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

                _lastPushedZ21Ip = z21Ip;

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

        foreach (var subscriptionId in _eventSubscriptions)

        {

            _eventBus.Unsubscribe(subscriptionId);

        }



        _eventSubscriptions.Clear();

        lock (_debounceLock)

        {

            _debounceCts?.Cancel();

            _debounceCts?.Dispose();

            _debounceCts = null;

        }

    }

}

