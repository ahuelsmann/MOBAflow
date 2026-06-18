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
}