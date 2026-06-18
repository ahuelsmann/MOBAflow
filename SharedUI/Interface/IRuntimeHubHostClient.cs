// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.SharedUI.Interface;

using Common.Runtime;

/// <summary>
/// MOBAflow SignalR host client for the runtime hub (push snapshots, receive commands).
/// </summary>
public interface IRuntimeHubHostClient : IAsyncDisposable
{
    bool IsConnected { get; }

    Task ConnectAsync(string serverIp, int serverPort, CancellationToken cancellationToken = default);

    Task DisconnectAsync();

    Task PushSnapshotAsync(MobaRuntimeSnapshot snapshot, CancellationToken cancellationToken = default);
}
