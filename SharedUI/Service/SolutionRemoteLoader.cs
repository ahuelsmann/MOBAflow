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
    private const int SolutionSchemaVersion = Solution.CurrentSchemaVersion;

    private static readonly JsonSerializerOptions MetaJsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly IMobaRuntime _mobaRuntime;
    private readonly MobileSolutionContext _mobileSolutionContext;
    private readonly IMobileRuntimeCoordinator? _mobileRuntimeCoordinator;
    private readonly IMobileSolutionStore? _mobileSolutionStore;
    private readonly IEventBus _eventBus;
    private readonly ILogger<SolutionRemoteLoader> _logger;
    private readonly HttpClient _httpClient;
    private readonly IUiDispatcher? _uiDispatcher;
    private readonly SemaphoreSlim _syncLock = new(1, 1);
    private DateTimeOffset? _lastSyncedAt;
    private bool _disposed;

    public SolutionRemoteLoader(
        IMobaRuntime mobaRuntime,
        MobileSolutionContext mobileSolutionContext,
        IEventBus eventBus,
        ILogger<SolutionRemoteLoader> logger,
        HttpClient httpClient,
        IMobileRuntimeCoordinator? mobileRuntimeCoordinator = null,
        IUiDispatcher? uiDispatcher = null,
        IMobileSolutionStore? mobileSolutionStore = null)
    {
        ArgumentNullException.ThrowIfNull(mobaRuntime);
        ArgumentNullException.ThrowIfNull(mobileSolutionContext);
        ArgumentNullException.ThrowIfNull(eventBus);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(httpClient);

        _mobaRuntime = mobaRuntime;
        _mobileSolutionContext = mobileSolutionContext;
        _mobileRuntimeCoordinator = mobileRuntimeCoordinator;
        _mobileSolutionStore = mobileSolutionStore;
        _eventBus = eventBus;
        _logger = logger;
        _httpClient = httpClient;
        _uiDispatcher = uiDispatcher;
    }

    /// <inheritdoc />
    public DateTimeOffset? LastSyncedAt => _lastSyncedAt;

    /// <inheritdoc />
    public Task SyncIfNeededAsync(string serverIp, int serverPort, CancellationToken cancellationToken = default) =>
        SyncInternalAsync(serverIp, serverPort, force: false, cancellationToken);

    /// <inheritdoc />
    public Task ForceSyncAsync(string serverIp, int serverPort, CancellationToken cancellationToken = default) =>
        SyncInternalAsync(serverIp, serverPort, force: true, cancellationToken);

    private async Task SyncInternalAsync(
        string serverIp,
        int serverPort,
        bool force,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(serverIp) || serverPort <= 0)
        {
            return;
        }

        await _syncLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var meta = await TryGetMetaAsync(serverIp, serverPort, cancellationToken).ConfigureAwait(false);
            if (meta == null && !force)
            {
                _logger.LogDebug("Remote solution meta unavailable from MOBApi");
                return;
            }

            if (!force && meta != null && _lastSyncedAt.HasValue && meta.UpdatedAt <= _lastSyncedAt.Value)
            {
                return;
            }

            var json = await TryGetSolutionJsonAsync(serverIp, serverPort, cancellationToken).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(json))
            {
                _logger.LogDebug("Remote solution JSON unavailable from MOBApi");
                return;
            }

            var validationResult = JsonValidationService.Validate(json, SolutionSchemaVersion);
            if (!validationResult.IsValid)
            {
                _logger.LogWarning("Remote solution validation failed: {Error}", validationResult.ErrorMessage);
                return;
            }

            var solution = JsonSerializer.Deserialize<Solution>(json, JsonOptions.Default);
            if (solution != null) SolutionMigrator.MigrateToCurrent(solution);
            if (solution == null || solution.Projects.Count == 0)
            {
                _logger.LogWarning("Remote solution deserialized empty or without projects");
                return;
            }

            var activeProjectName = meta?.ActiveProjectName ?? meta?.FirstProjectName;
            var activeProject = ResolveActiveProject(solution, activeProjectName);
            await ApplySolutionOnUiThreadAsync(solution, activeProjectName, cancellationToken).ConfigureAwait(false);

            // When MOBAsmart controls MOBAflow remotely, runtime signal aspects and Z21 state
            // come from MOBAflow snapshots. Re-activating the local runtime would publish stale
            // project defaults and race with remote signal commands.
            if (_mobileRuntimeCoordinator?.PreferRemoteRuntime != true)
            {
                await _mobaRuntime.ActivateProjectAsync(activeProject, cancellationToken).ConfigureAwait(false);
            }

            var syncedAt = meta?.UpdatedAt ?? DateTimeOffset.UtcNow;
            _lastSyncedAt = syncedAt;

            if (_mobileSolutionStore != null)
            {
                await _mobileSolutionStore.SaveAsync(
                    solution,
                    new SolutionSyncMeta(syncedAt, solution.Name, activeProject.Name),
                    cancellationToken).ConfigureAwait(false);
            }

            _eventBus.Publish(new SolutionSyncedEvent(syncedAt, solution.Name, activeProject.Name));
            _logger.LogInformation(
                "Solution synced from MOBApi: {SolutionName} / {ProjectName} with {LocomotiveCount} locomotive(s)",
                solution.Name,
                activeProject.Name,
                activeProject.Locomotives.Count);
        }
        finally
        {
            _syncLock.Release();
        }
    }

    /// <inheritdoc />
    public async Task<bool> TryLoadFromCacheAsync(
        MobileSolutionCacheEntry? cachedEntry = null,
        CancellationToken cancellationToken = default)
    {
        if (_mobileSolutionStore == null && cachedEntry == null)
        {
            return false;
        }

        await _syncLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var entry = cachedEntry ?? await _mobileSolutionStore!.TryLoadAsync(cancellationToken).ConfigureAwait(false);
            if (entry == null)
            {
                return false;
            }

            var activeProject = ResolveActiveProject(entry.Solution, entry.Meta.ActiveProjectName);
            await ApplySolutionOnUiThreadAsync(
                entry.Solution,
                entry.Meta.ActiveProjectName,
                cancellationToken).ConfigureAwait(false);

            if (_mobileRuntimeCoordinator?.PreferRemoteRuntime != true)
            {
                await _mobaRuntime.ActivateProjectAsync(activeProject, cancellationToken).ConfigureAwait(false);
            }

            _lastSyncedAt = entry.Meta.UpdatedAt;
            _eventBus.Publish(new SolutionSyncedEvent(
                entry.Meta.UpdatedAt,
                entry.Meta.SolutionName,
                activeProject.Name));
            _logger.LogInformation(
                "Loaded cached mobile solution {SolutionName} / {ProjectName}",
                entry.Meta.SolutionName,
                activeProject.Name);
            return true;
        }
        finally
        {
            _syncLock.Release();
        }
    }

    private async Task ApplySolutionOnUiThreadAsync(
        Solution solution,
        string? activeProjectName,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (_uiDispatcher != null)
        {
            await _uiDispatcher.InvokeOnUiAsync(() =>
            {
                _mobileSolutionContext.ApplySolution(solution, activeProjectName);
                return Task.CompletedTask;
            }).ConfigureAwait(false);
            return;
        }

        _mobileSolutionContext.ApplySolution(solution, activeProjectName);
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

        public string? SourcePath { get; init; }

        public string? ActiveProjectName { get; init; }

        public string? FirstProjectName { get; init; }
    }

    private static Project ResolveActiveProject(Solution solution, string? activeProjectName)
    {
        if (!string.IsNullOrWhiteSpace(activeProjectName))
        {
            var match = solution.Projects.FirstOrDefault(project =>
                string.Equals(project.Name, activeProjectName.Trim(), StringComparison.OrdinalIgnoreCase));
            if (match != null)
            {
                return match;
            }
        }

        return solution.Projects[0];
    }
}
