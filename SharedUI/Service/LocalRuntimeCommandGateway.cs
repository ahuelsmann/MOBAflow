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

    public Task SetTrackPowerAsync(bool isOn, CancellationToken cancellationToken = default) =>
        _mobaRuntime.SetTrackPowerAsync(isOn, cancellationToken);

    public Task SimulateFeedbackAsync(int inPort, CancellationToken cancellationToken = default) =>
        _mobaRuntime.SimulateFeedbackAsync(inPort, cancellationToken);

    public Task ResetJourneyAsync(Guid journeyId, CancellationToken cancellationToken = default) =>
        _mobaRuntime.ResetJourneyAsync(journeyId, cancellationToken);

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

    public Task SendTurnoutCommandAsync(
        int decoderAddress,
        int output,
        bool activate,
        bool queue = false,
        CancellationToken cancellationToken = default) =>
        _mobaRuntime.SendTurnoutCommandAsync(decoderAddress, output, activate, queue, cancellationToken);
}
