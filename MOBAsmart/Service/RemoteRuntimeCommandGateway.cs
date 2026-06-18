// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.MAUI.Service;

using Domain;

using SharedUI.Interface;

/// <summary>
/// Sends runtime commands from MOBAsmart to MOBAflow via SignalR/REST.
/// </summary>
public sealed class RemoteRuntimeCommandGateway : IRuntimeCommandGateway
{
    private readonly IRuntimeHubRemoteClient _runtimeHubRemoteClient;

    public RemoteRuntimeCommandGateway(IRuntimeHubRemoteClient runtimeHubRemoteClient)
    {
        _runtimeHubRemoteClient = runtimeHubRemoteClient;
    }

    public Task SetSignalAspectAsync(Guid signalId, SignalAspect aspect, CancellationToken cancellationToken = default)
    {
        return _runtimeHubRemoteClient.SetSignalAspectAsync(signalId, aspect, cancellationToken);
    }

    public Task SetLocomotiveDriveAsync(int address, int speed, bool forward, CancellationToken cancellationToken = default)
    {
        return _runtimeHubRemoteClient.SetLocomotiveDriveAsync(address, speed, forward, cancellationToken);
    }

    public Task SetLocomotiveFunctionAsync(int address, int functionIndex, bool isOn, CancellationToken cancellationToken = default)
    {
        return _runtimeHubRemoteClient.SetLocomotiveFunctionAsync(address, functionIndex, isOn, cancellationToken);
    }
}
