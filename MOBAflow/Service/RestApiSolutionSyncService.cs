// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.WinUI.Service;

using Common.Configuration;
using SharedUI.Interface;

using Domain;

using Microsoft.Extensions.Logging;

using System.ComponentModel;

using System.Text;

using System.Text.Json;

/// <summary>
/// Pushes the in-memory <see cref="Solution"/> to the MOBApi cache whenever it changes or MOBApi becomes reachable.
/// </summary>

public sealed class RestApiSolutionSyncService : IDisposable

{

    private const int DebounceMilliseconds = 500;

    private readonly AppSettings _appSettings;

    private readonly ISolutionSession _solutionSession;

    private readonly ILogger<RestApiSolutionSyncService> _logger;

    private readonly LocalMobApiClient _mobApiClient;

    private readonly object _debounceLock = new();

    private CancellationTokenSource? _debounceCts;

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

        AppSettings appSettings,

        ISolutionSession solutionSession,

        RestApiProcessService restApiProcessService,

        ILogger<RestApiSolutionSyncService> logger,

        LocalMobApiClient mobApiClient)

    {

        ArgumentNullException.ThrowIfNull(appSettings);

        ArgumentNullException.ThrowIfNull(mobApiClient);

        ArgumentNullException.ThrowIfNull(solutionSession);

        ArgumentNullException.ThrowIfNull(restApiProcessService);

        ArgumentNullException.ThrowIfNull(logger);

        _appSettings = appSettings;

        _solutionSession = solutionSession;

        _logger = logger;

        _mobApiClient = mobApiClient;

        solutionSession.SolutionLoaded += OnSolutionChanged;

        solutionSession.SolutionSaving += OnSolutionChanged;

        solutionSession.PropertyChanged += OnSolutionSessionPropertyChanged;

        solutionSession.ModelChanged += OnSolutionModelChanged;

        restApiProcessService.ApiBecameReachable += OnApiBecameReachable;

    }

    private void OnSolutionChanged(object? sender, EventArgs e) => QueuePush();

    private void OnSolutionModelChanged(object? sender, PropertyChangedEventArgs e) => QueuePush();

    private void OnSolutionSessionPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ISolutionSession.SelectedProject))
        {
            QueuePush();
        }
    }
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

        var solutionPath = _solutionSession.CurrentSolutionPath;
        if (string.IsNullOrWhiteSpace(solutionPath))
        {
            solutionPath = "mobaflow://in-memory";
        }

        var port = _appSettings.RestApi.Port > 0 ? _appSettings.RestApi.Port : 5001;

        if (_solutionSession.Solution.SchemaVersion != Solution.CurrentSchemaVersion)
        {
            _solutionSession.Solution.SchemaVersion = Solution.CurrentSchemaVersion;
        }

        var json = JsonSerializer.Serialize(_solutionSession.Solution, JsonOptions.Default);

        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        using var request = new HttpRequestMessage(HttpMethod.Put, "api/solution")

        {

            Content = content

        };

        request.Headers.TryAddWithoutValidation("X-MOBAflow-Solution-Path", solutionPath);

        var activeProjectName = _solutionSession.SelectedProject?.Name;
        if (!string.IsNullOrWhiteSpace(activeProjectName))
        {
            request.Headers.TryAddWithoutValidation("X-MOBAflow-Active-Project", activeProjectName);
        }

        var succeeded = false;

        try

        {

            using var response = await _mobApiClient.SendAsync(request, cancellationToken).ConfigureAwait(false);

            if (response.IsSuccessStatusCode)

            {

                succeeded = true;

                _logger.LogDebug("Solution pushed to MOBApi on port {Port}", port);

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

    public void Dispose()

    {

        if (_disposed)

        {

            return;

        }

        _disposed = true;

        _solutionSession.ModelChanged -= OnSolutionModelChanged;

        lock (_debounceLock)

        {

            _debounceCts?.Cancel();

            _debounceCts?.Dispose();

            _debounceCts = null;

        }
    }
}
