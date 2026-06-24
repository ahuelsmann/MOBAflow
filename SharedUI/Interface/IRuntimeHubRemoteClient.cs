// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.SharedUI.Interface;

using Common.Runtime;

using Domain;

/// <summary>
/// MOBAsmart SignalR remote client for the runtime hub (receive snapshots, send commands).
/// </summary>
public interface IRuntimeHubRemoteClient : IAsyncDisposable
{
    event Func<MobaRuntimeSnapshot, Task>? SnapshotReceived;

    event Func<bool, Task>? SessionStateChanged;

    event Func<DateTimeOffset, Task>? SolutionUpdated;

    bool IsConnected { get; }

    bool HasActiveHost { get; }

    Task ConnectAsync(
        string serverIp,
        int serverPort,
        string clientId,
        CancellationToken cancellationToken = default,
        bool forceReconnect = false);

    Task DisconnectAsync();

    Task RequestLatestSnapshotAsync(CancellationToken cancellationToken = default);

    Task SetSignalAspectAsync(Guid signalId, SignalAspect aspect, CancellationToken cancellationToken = default);

    Task SetLocomotiveDriveAsync(int address, int speed, bool forward, CancellationToken cancellationToken = default);

    Task SetLocomotiveFunctionAsync(int address, int functionIndex, bool isOn, CancellationToken cancellationToken = default);
}
