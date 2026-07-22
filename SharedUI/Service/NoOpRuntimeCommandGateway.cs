// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.SharedUI.Service;

using Domain;

using Interface;

/// <summary>
/// Discards runtime commands when neither MOBAflow nor local Z21 is available.
/// </summary>
internal sealed class NoOpRuntimeCommandGateway : IRuntimeCommandGateway
{
    public static NoOpRuntimeCommandGateway Instance { get; } = new();

    private NoOpRuntimeCommandGateway()
    {
    }

    public Task SetTrackPowerAsync(bool isOn, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task SimulateFeedbackAsync(int inPort, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task ResetJourneyAsync(Guid journeyId, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task SetSignalAspectAsync(Guid signalId, SignalAspect aspect, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task SetLocomotiveDriveAsync(int address, int speed, bool forward, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task SetLocomotiveFunctionAsync(int address, int functionIndex, bool isOn, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task SendTurnoutCommandAsync(
        int decoderAddress,
        int output,
        bool activate,
        bool queue = false,
        CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
