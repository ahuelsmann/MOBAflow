// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.Mobile;

using Interface;

using Microsoft.Extensions.Logging;

/// <summary>
/// Brings MOBAflow's solution to the phone: the one-time catalog import after a session starts, a forced sync
/// when MOBAflow announces a change before that import, the periodic sync afterwards and on-demand syncs with a
/// cache fallback. Applying the result to the pages stays with the root ViewModel.
/// </summary>
public sealed partial class MobileSolutionSynchronizer
{
    private readonly ISolutionRemoteLoader? _solutionRemoteLoader;
    private readonly ILogger<MobileSolutionSynchronizer> _logger;
    private readonly Lock _catalogImportGate = new();
    private Task _lastCatalogImport = Task.CompletedTask;
    private volatile bool _initialCatalogApplied;

    /// <summary>
    /// Initializes a new instance of the <see cref="MobileSolutionSynchronizer"/> class.
    /// </summary>
    /// <param name="logger">Logger for diagnostics.</param>
    /// <param name="solutionRemoteLoader">Optional: loads the solution from MOBApi or the phone's cache.</param>
    public MobileSolutionSynchronizer(
        ILogger<MobileSolutionSynchronizer> logger,
        ISolutionRemoteLoader? solutionRemoteLoader = null)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
        _solutionRemoteLoader = solutionRemoteLoader;
    }

    /// <summary>Gets whether the catalog of the current MOBAflow session has been imported and shown.</summary>
    public bool IsInitialCatalogApplied => _initialCatalogApplied;

    /// <summary>Marks the catalog as shown; later syncs only refresh it.</summary>
    public void MarkInitialCatalogApplied() => _initialCatalogApplied = true;

    /// <summary>Requires a new catalog import, for example after the session ended or the toggle changed.</summary>
    public void ResetInitialCatalog() => _initialCatalogApplied = false;

    /// <summary>
    /// Imports the catalog once per session: syncs the solution from <paramref name="endpoint"/> and then runs
    /// <paramref name="requestLatestSnapshot"/>. Returns false when the import was not needed; on true the caller
    /// shows the catalog and calls <see cref="MarkInitialCatalogApplied"/>.
    /// </summary>
    public async Task<bool> ImportInitialCatalogAsync(
        Func<bool> isConnectionEnabled,
        Func<MobaflowEndpoint?> endpoint,
        Func<Task> requestLatestSnapshot,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(isConnectionEnabled);
        ArgumentNullException.ThrowIfNull(endpoint);
        ArgumentNullException.ThrowIfNull(requestLatestSnapshot);
        if (!isConnectionEnabled() || _initialCatalogApplied)
        {
            return false;
        }

        // Imports run one after another: each waits for the previous one, then checks again.
        var done = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Task previousImport;
        lock (_catalogImportGate)
        {
            previousImport = _lastCatalogImport;
            _lastCatalogImport = done.Task;
        }

        try
        {
            // A previous import reports its own failure to its caller.
            await previousImport.WaitAsync(cancellationToken).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
            cancellationToken.ThrowIfCancellationRequested();
            if (!isConnectionEnabled() || _initialCatalogApplied)
            {
                return false;
            }

            if (_solutionRemoteLoader is not null && endpoint() is { } target)
            {
                try
                {
                    await _solutionRemoteLoader
                        .ForceSyncAsync(target.IpAddress, target.Port, cancellationToken)
                        .ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    LogInitialSyncFailed(_logger, ex);
                }
            }

            await requestLatestSnapshot().ConfigureAwait(false);
            return true;
        }
        finally
        {
            done.SetResult();
        }
    }

    /// <summary>
    /// Syncs after MOBAflow announced a solution change; once the catalog is shown, the periodic sync takes over.
    /// </summary>
    public Task SyncAfterPushAsync(MobaflowEndpoint endpoint, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        return _solutionRemoteLoader is null || _initialCatalogApplied
            ? Task.CompletedTask
            : _solutionRemoteLoader.ForceSyncAsync(endpoint.IpAddress, endpoint.Port, cancellationToken);
    }

    /// <summary>
    /// Refreshes the solution after a reachable health check once the catalog has been shown.
    /// </summary>
    public Task SyncIfNeededAsync(MobaflowEndpoint endpoint, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        return _solutionRemoteLoader is null || !_initialCatalogApplied
            ? Task.CompletedTask
            : _solutionRemoteLoader.SyncIfNeededAsync(endpoint.IpAddress, endpoint.Port, cancellationToken);
    }

    /// <summary>
    /// On-demand sync for the Control tab: loads the cached solution when the selected project has no
    /// locomotives, then forces a sync from <paramref name="endpoint"/> until the catalog is shown. Returns false
    /// when no remote sync is possible (no loader or no endpoint).
    /// </summary>
    public async Task<bool> RequestSyncAsync(
        bool selectedProjectHasLocomotives,
        MobaflowEndpoint? endpoint,
        CancellationToken cancellationToken)
    {
        if (_solutionRemoteLoader is null)
        {
            return false;
        }

        if (!selectedProjectHasLocomotives)
        {
            try
            {
                await _solutionRemoteLoader.TryLoadFromCacheAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                LogCacheLoadFailed(_logger, ex);
            }
        }

        if (endpoint is null)
        {
            return false;
        }

        if (!_initialCatalogApplied)
        {
            try
            {
                await _solutionRemoteLoader
                    .ForceSyncAsync(endpoint.IpAddress, endpoint.Port, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                LogOnDemandSyncFailed(_logger, ex);
            }
        }

        return true;
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Initial solution sync after hub connect failed")]
    private static partial void LogInitialSyncFailed(ILogger logger, Exception exception);

    [LoggerMessage(Level = LogLevel.Debug, Message = "On-demand cached solution load failed")]
    private static partial void LogCacheLoadFailed(ILogger logger, Exception exception);

    [LoggerMessage(Level = LogLevel.Debug, Message = "On-demand solution sync failed")]
    private static partial void LogOnDemandSyncFailed(ILogger logger, Exception exception);
}
