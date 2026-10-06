// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.SharedUI.Interface;

using Domain;

/// <summary>
/// Executes runtime control commands either locally (MOBAflow) or via MOBApi (MOBAsmart).
/// </summary>
public interface IRuntimeCommandGateway
{
    Task SetTrackPowerAsync(bool isOn, CancellationToken cancellationToken = default);

    /// <summary>Clears the latched fail-safe state after the connection has recovered.</summary>
    Task AcknowledgeFailSafeAsync(CancellationToken cancellationToken = default);

    Task SimulateFeedbackAsync(int inPort, CancellationToken cancellationToken = default);

    Task ResetJourneyAsync(Guid journeyId, CancellationToken cancellationToken = default);

    Task ResetInPortCountersAsync(CancellationToken cancellationToken = default);

    Task SetInPortCounterAsync(uint inPort, ulong value, CancellationToken cancellationToken = default);

    Task ResetInPortCounterAsync(uint inPort, CancellationToken cancellationToken = default);

    Task SetSignalAspectAsync(Guid signalId, SignalAspect aspect, CancellationToken cancellationToken = default);

    Task SetLocomotiveDriveAsync(int address, int speed, bool forward, CancellationToken cancellationToken = default);

    Task SetLocomotiveFunctionAsync(int address, int functionIndex, bool isOn, CancellationToken cancellationToken = default);

    /// <summary>Switches every function (F0-F31) of a locomotive off.</summary>
    Task SetAllLocomotiveFunctionsOffAsync(int address, CancellationToken cancellationToken = default);

    /// <summary>Asks the command station to report the current state of a locomotive.</summary>
    Task RequestLocomotiveInfoAsync(int address, CancellationToken cancellationToken = default);

    Task SendTurnoutCommandAsync(
        int decoderAddress,
        int output,
        bool activate,
        bool queue = false,
        CancellationToken cancellationToken = default);
}
