// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.SharedUI.Interface;

using Domain;

/// <summary>
/// Executes runtime control commands either locally (MOBAflow) or via MOBApi (MOBAsmart).
/// </summary>
public interface IRuntimeCommandGateway
{
    Task SetTrackPowerAsync(bool isOn, CancellationToken cancellationToken = default);

    Task SimulateFeedbackAsync(int inPort, CancellationToken cancellationToken = default);

    Task ResetJourneyAsync(Guid journeyId, CancellationToken cancellationToken = default);

    Task SetSignalAspectAsync(Guid signalId, SignalAspect aspect, CancellationToken cancellationToken = default);

    Task SetLocomotiveDriveAsync(int address, int speed, bool forward, CancellationToken cancellationToken = default);

    Task SetLocomotiveFunctionAsync(int address, int functionIndex, bool isOn, CancellationToken cancellationToken = default);

    Task SendTurnoutCommandAsync(
        int decoderAddress,
        int output,
        bool activate,
        bool queue = false,
        CancellationToken cancellationToken = default);
}
