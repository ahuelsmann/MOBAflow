// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.Interface;

/// <summary>
/// Fetches the MOBAflow solution from MOBApi and activates it in the local runtime.
/// </summary>
public interface ISolutionRemoteLoader
{
    /// <summary>
    /// Gets the <c>updatedAt</c> timestamp of the last successfully applied solution, if any.
    /// </summary>
    DateTimeOffset? LastSyncedAt { get; }

    /// <summary>
    /// Fetches the solution from MOBApi when the remote snapshot is newer than the last applied one.
    /// </summary>
    Task SyncIfNeededAsync(string serverIp, int serverPort, CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetches and applies the solution from MOBApi even when the remote timestamp has not changed.
    /// </summary>
    Task ForceSyncAsync(string serverIp, int serverPort, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads the last cached solution from local storage when MOBAflow is unavailable.
    /// </summary>
    /// <returns><c>true</c> when cached data was applied successfully.</returns>
    Task<bool> TryLoadFromCacheAsync(
        MobileSolutionCacheEntry? cachedEntry = null,
        CancellationToken cancellationToken = default);
}