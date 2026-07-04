// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.Service;

using Backend.Interface;
using Domain;
using Interface;

/// <summary>
/// Routes MOBAsmart control commands to the local Z21 (locomotives) or MOBAflow (signals/domain).
/// </summary>
public sealed class MobileRuntimeCoordinator : IRuntimeCommandGateway, IMobileRuntimeCoordinator
{
    private readonly LocalRuntimeCommandGateway _localGateway;
    private readonly IRuntimeHubRemoteClient _remoteClient;
    private bool _mobaflowSessionActive;
    private bool _localZ21Connected;
    public MobileRuntimeCoordinator(IMobaRuntime mobaRuntime, IRuntimeHubRemoteClient remoteClient)
    {
        ArgumentNullException.ThrowIfNull(mobaRuntime);
        ArgumentNullException.ThrowIfNull(remoteClient);
        _localGateway = new LocalRuntimeCommandGateway(mobaRuntime);
        _remoteClient = remoteClient;
    }

    /// <inheritdoc />
    public bool PreferRemoteRuntime => _mobaflowSessionActive;

    /// <inheritdoc />
    public bool CanExecuteCommands => _mobaflowSessionActive || _localZ21Connected;

    /// <inheritdoc />
    public bool IsLocalZ21Connected => _localZ21Connected;

    /// <inheritdoc />
    public void SetMobaflowSessionActive(bool isActive) => _mobaflowSessionActive = isActive;

    /// <inheritdoc />
    public void SetLocalZ21Connected(bool isConnected) => _localZ21Connected = isConnected;

    /// <inheritdoc />
    public Task SetSignalAspectAsync(Guid signalId, SignalAspect aspect, CancellationToken cancellationToken = default)
    {
        if (_mobaflowSessionActive)
        {
            return _remoteClient.SetSignalAspectAsync(signalId, aspect, cancellationToken);
        }

        if (_localZ21Connected)
        {
            return _localGateway.SetSignalAspectAsync(signalId, aspect, cancellationToken);
        }

        return NoOpRuntimeCommandGateway.Instance.SetSignalAspectAsync(signalId, aspect, cancellationToken);
    }

    /// <inheritdoc />
    public Task SetLocomotiveDriveAsync(int address, int speed, bool forward, CancellationToken cancellationToken = default)
    {
        // Locomotive I/O is always direct to the local Z21 when connected; MOBAflow sync is domain-only.
        if (_localZ21Connected)
        {
            return _localGateway.SetLocomotiveDriveAsync(address, speed, forward, cancellationToken);
        }

        if (_mobaflowSessionActive)
        {
            return _remoteClient.SetLocomotiveDriveAsync(address, speed, forward, cancellationToken);
        }

        return NoOpRuntimeCommandGateway.Instance.SetLocomotiveDriveAsync(address, speed, forward, cancellationToken);
    }

    /// <inheritdoc />
    public Task SetLocomotiveFunctionAsync(int address, int functionIndex, bool isOn, CancellationToken cancellationToken = default)
    {
        if (_localZ21Connected)
        {
            return _localGateway.SetLocomotiveFunctionAsync(address, functionIndex, isOn, cancellationToken);
        }

        if (_mobaflowSessionActive)
        {
            return _remoteClient.SetLocomotiveFunctionAsync(address, functionIndex, isOn, cancellationToken);
        }

        return NoOpRuntimeCommandGateway.Instance.SetLocomotiveFunctionAsync(address, functionIndex, isOn, cancellationToken);
    }
}