// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.SharedUI.Service;

using Backend;
using Backend.Interface;
using Backend.Model;
using Domain;
using Interface;
using Common.Runtime;

/// <summary>
/// In-process <see cref="IMobaClient"/> implementation delegating directly to <see cref="IMobaRuntime"/>.
/// </summary>
public sealed class InProcessMobaClient : IMobaClient
{
    private readonly IMobaRuntime _runtime;

    /// <summary>
    /// Initializes a new instance of the <see cref="InProcessMobaClient"/> class.
    /// </summary>
    public InProcessMobaClient(IMobaRuntime runtime)
    {
        ArgumentNullException.ThrowIfNull(runtime);

        _runtime = runtime;
        _runtime.SnapshotChanged += OnRuntimeSnapshotChanged;
        _runtime.TrafficPacketLogged += OnTrafficPacketLogged;
        _runtime.FeedbackReceived += OnFeedbackReceived;
    }

    /// <inheritdoc />
    public MobaRuntimeSnapshot Current => _runtime.Current;

    /// <inheritdoc />
    public event EventHandler<MobaRuntimeSnapshot>? SnapshotChanged;

    /// <inheritdoc />
    public event EventHandler<Z21TrafficPacket>? TrafficPacketLogged;

    /// <inheritdoc />
    public event EventHandler<FeedbackResult>? FeedbackReceived;

    /// <inheritdoc />
    public Task ActivateProjectAsync(Project editableProject, CancellationToken cancellationToken = default)
        => _runtime.ActivateProjectAsync(editableProject, cancellationToken);

    /// <inheritdoc />
    public Task ConnectAsync(CancellationToken cancellationToken = default)
        => _runtime.ConnectAsync(cancellationToken);

    /// <inheritdoc />
    public Task DisconnectAsync(CancellationToken cancellationToken = default)
        => _runtime.DisconnectAsync(cancellationToken);

    /// <inheritdoc />
    public Task SetTrackPowerAsync(bool isOn, CancellationToken cancellationToken = default)
        => _runtime.SetTrackPowerAsync(isOn, cancellationToken);

    /// <inheritdoc />
    public void SetSystemStatePollingInterval(int intervalSeconds)
        => _runtime.SetSystemStatePollingInterval(intervalSeconds);

    /// <inheritdoc />
    public Task SetLocomotiveDriveAsync(int address, int speed, bool forward, CancellationToken cancellationToken = default)
        => _runtime.SetLocomotiveDriveAsync(address, speed, forward, cancellationToken);

    /// <inheritdoc />
    public Task SetLocomotiveFunctionAsync(int address, int functionIndex, bool isOn, CancellationToken cancellationToken = default)
        => _runtime.SetLocomotiveFunctionAsync(address, functionIndex, isOn, cancellationToken);

    /// <inheritdoc />
    public Task RequestLocomotiveInfoAsync(int address, CancellationToken cancellationToken = default)
        => _runtime.RequestLocomotiveInfoAsync(address, cancellationToken);

    /// <inheritdoc />
    public Task AcknowledgeFailSafeAsync(CancellationToken cancellationToken = default)
        => _runtime.AcknowledgeFailSafeAsync(cancellationToken);

    /// <inheritdoc />
    public Task SimulateFeedbackAsync(int inPort, CancellationToken cancellationToken = default)
        => _runtime.SimulateFeedbackAsync(inPort, cancellationToken);

    /// <inheritdoc />
    public Task ResetJourneyAsync(Guid journeyId, CancellationToken cancellationToken = default)
        => _runtime.ResetJourneyAsync(journeyId, cancellationToken);

    /// <inheritdoc />
    public Task SetSignalAspectAsync(SbSignal signal, CancellationToken cancellationToken = default)
        => _runtime.SetSignalAspectAsync(signal, cancellationToken);
    public Task SendTurnoutCommandAsync(int decoderAddress, int output, bool activate, bool queue = false, CancellationToken cancellationToken = default)
        => _runtime.SendTurnoutCommandAsync(decoderAddress, output, activate, queue, cancellationToken);

    /// <inheritdoc />
    public IReadOnlyList<Z21TrafficPacket> GetTrafficPackets()
        => _runtime.GetTrafficPackets();

    /// <inheritdoc />
    public void ClearTrafficMonitor()
        => _runtime.ClearTrafficMonitor();

    private void OnRuntimeSnapshotChanged(object? sender, MobaRuntimeSnapshot snapshot)
    {
        SnapshotChanged?.Invoke(this, snapshot);
    }

    private void OnTrafficPacketLogged(object? sender, Z21TrafficPacket packet)
    {
        TrafficPacketLogged?.Invoke(this, packet);
    }

    private void OnFeedbackReceived(object? sender, FeedbackResult feedback)
    {
        FeedbackReceived?.Invoke(this, feedback);
    }
}

