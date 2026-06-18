// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.SharedUI.Service;

using Backend.Interface;

using Domain;

using Interface;

/// <summary>
/// Executes runtime commands on the local MOBAflow runtime.
/// </summary>
public sealed class LocalRuntimeCommandGateway : IRuntimeCommandGateway
{
    private readonly IMobaRuntime _mobaRuntime;

    public LocalRuntimeCommandGateway(IMobaRuntime mobaRuntime)
    {
        _mobaRuntime = mobaRuntime;
    }

    public Task SetSignalAspectAsync(Guid signalId, SignalAspect aspect, CancellationToken cancellationToken = default)
    {
        return _mobaRuntime.SetSignalAspectAsync(signalId, aspect, cancellationToken);
    }

    public Task SetLocomotiveDriveAsync(int address, int speed, bool forward, CancellationToken cancellationToken = default)
    {
        return _mobaRuntime.SetLocomotiveDriveAsync(address, speed, forward, cancellationToken);
    }

    public Task SetLocomotiveFunctionAsync(int address, int functionIndex, bool isOn, CancellationToken cancellationToken = default)
    {
        return _mobaRuntime.SetLocomotiveFunctionAsync(address, functionIndex, isOn, cancellationToken);
    }
}
