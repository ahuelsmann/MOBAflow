// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.Service;

using Backend.Interface;

using Common.Events;
using Common.Validation;

using Domain;

using Interface;

using Microsoft.Extensions.Logging;

using System.Net.Http;
using System.Text.Json;

/// <summary>
/// Downloads the solution JSON from MOBApi, validates it, and activates the first project in the local runtime.
/// </summary>
public sealed class SolutionRemoteLoader : ISolutionRemoteLoader, IDisposable
{
    private const int SolutionSchemaVersion = 1;

    private static readonly JsonSerializerOptions MetaJsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly IMobaRuntime _mobaRuntime;
    private readonly MobileSolutionContext _mobileSolutionContext;
    private readonly IEventBus _eventBus;
    private readonly ILogger<SolutionRemoteLoader> _logger;
    private readonly HttpClient _httpClient;
    private readonly SemaphoreSlim _syncLock = new(1, 1);
    private DateTimeOffset? _lastSyncedAt;
    private bool _disposed;

    public SolutionRemoteLoader(
        IMobaRuntime mobaRuntime,
        MobileSolutionContext mobileSolutionContext,
        IEventBus eventBus,
        ILogger<SolutionRemoteLoader> logger,
        HttpClient httpClient)
    {
        ArgumentNullException.ThrowIfNull(mobaRuntime);
        ArgumentNullException.ThrowIfNull(mobileSolutionContext);
        ArgumentNullException.ThrowIfNull(eventBus);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(httpClient);

        _mobaRuntime = mobaRuntime;
        _mobileSolutionContext = mobileSolutionContext;
        _eventBus = eventBus;
        _logger = logger;
        _httpClient = httpClient;
    }

    /// <inheritdoc />
    public DateTimeOffset? LastSyncedAt => _lastSyncedAt;

    /// <inheritdoc />
    public async Task SyncIfNeededAsync(string serverIp, int serverPort, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(serverIp) || serverPort <= 0)
        {
            return;
        }

        await _syncLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var meta = await TryGetMetaAsync(serverIp, serverPort, cancellationToken).ConfigureAwait(false);
            if (meta == null)
            {
                return;
            }

            if (_lastSyncedAt.HasValue && meta.UpdatedAt <= _lastSyncedAt.Value)
            {
                return;
            }

            var json = await TryGetSolutionJsonAsync(serverIp, serverPort, cancellationToken).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(json))
            {
                return;
            }

            var validationResult = JsonValidationService.Validate(json, SolutionSchemaVersion);
            if (!validationResult.IsValid)
            {
                _logger.LogWarning("Remote solution validation failed: {Error}", validationResult.ErrorMessage);
                return;
            }

            var solution = JsonSerializer.Deserialize<Solution>(json, JsonOptions.Default);
            if (solution == null || solution.Projects.Count == 0)
            {
                _logger.LogWarning("Remote solution deserialized empty or without projects");
                return;
            }

            var activeProject = solution.Projects[0];
            _mobileSolutionContext.ApplySolution(solution);
            await _mobaRuntime.ActivateProjectAsync(activeProject, cancellationToken).ConfigureAwait(false);

            _lastSyncedAt = meta.UpdatedAt;
            _eventBus.Publish(new SolutionSyncedEvent(meta.UpdatedAt, solution.Name, activeProject.Name));
            _logger.LogInformation(
                "Solution synced from MOBApi: {SolutionName} / {ProjectName}",
                solution.Name,
                activeProject.Name);
        }
        finally
        {
            _syncLock.Release();
        }
    }

    private async Task<SolutionMetaResponse?> TryGetMetaAsync(string serverIp, int serverPort, CancellationToken cancellationToken)
    {
        var url = $"http://{serverIp.Trim()}:{serverPort}/api/solution/meta";
        try
        {
            using var response = await _httpClient.GetAsync(url, cancellationToken).ConfigureAwait(false);
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }

            response.EnsureSuccessStatusCode();
            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            return await JsonSerializer.DeserializeAsync<SolutionMetaResponse>(stream, MetaJsonOptions, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            _logger.LogDebug(ex, "Solution meta request failed for {Url}", url);
            return null;
        }
    }

    private async Task<string?> TryGetSolutionJsonAsync(string serverIp, int serverPort, CancellationToken cancellationToken)
    {
        var url = $"http://{serverIp.Trim()}:{serverPort}/api/solution";
        try
        {
            using var response = await _httpClient.GetAsync(url, cancellationToken).ConfigureAwait(false);
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }

            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogDebug(ex, "Solution download failed for {Url}", url);
            return null;
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _syncLock.Dispose();
    }

    private sealed class SolutionMetaResponse
    {
        public DateTimeOffset UpdatedAt { get; init; }
    }
}