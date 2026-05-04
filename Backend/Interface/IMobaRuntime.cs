// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Backend.Interface;

using Common.Runtime;

using Domain;

using Model;

/// <summary>
/// Defines the in-process MOBA runtime that owns Z21 connection state and active project execution.
/// WinUI and MAUI inject this interface directly into SharedUI view models (no separate UI client facade).
/// </summary>
public interface IMobaRuntime
{
    /// <summary>
    /// Gets the latest runtime snapshot.
    /// </summary>
    MobaRuntimeSnapshot Current { get; }

    /// <summary>
    /// Raised whenever a new runtime snapshot is available.
    /// </summary>
    event EventHandler<MobaRuntimeSnapshot>? SnapshotChanged;

    /// <summary>
    /// Raised whenever a traffic packet is logged by the Z21 monitor.
    /// </summary>
    event EventHandler<Z21TrafficPacket>? TrafficPacketLogged;

    /// <summary>
    /// Raised whenever a feedback event is received from the active Z21 connection.
    /// </summary>
    event EventHandler<FeedbackResult>? FeedbackReceived;

    /// <summary>
    /// Activates the specified project for runtime execution.
    /// </summary>
    Task ActivateProjectAsync(Project editableProject, CancellationToken cancellationToken = default);

    /// <summary>
    /// Connects to the configured Z21 endpoint.
    /// </summary>
    Task ConnectAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Disconnects from the Z21.
    /// </summary>
    Task DisconnectAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Enables or disables track power.
    /// </summary>
    Task SetTrackPowerAsync(bool isOn, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets the system-state polling interval in seconds.
    /// </summary>
    void SetSystemStatePollingInterval(int intervalSeconds);

    /// <summary>
    /// Sends a locomotive drive command through the runtime.
    /// </summary>
    Task SetLocomotiveDriveAsync(int address, int speed, bool forward, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a locomotive function command through the runtime.
    /// </summary>
    Task SetLocomotiveFunctionAsync(int address, int functionIndex, bool isOn, CancellationToken cancellationToken = default);

    /// <summary>
    /// Requests the current state of a locomotive through the runtime.
    /// </summary>
    Task RequestLocomotiveInfoAsync(int address, CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears a latched fail-safe state after recovery.
    /// </summary>
    Task AcknowledgeFailSafeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Simulates a feedback input for testing purposes.
    /// </summary>
    Task SimulateFeedbackAsync(int inPort, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resets the runtime state of the specified journey.
    /// </summary>
    Task ResetJourneyAsync(Guid journeyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets a signal aspect through the runtime.
    /// </summary>
    Task SetSignalAspectAsync(SbSignal signal, CancellationToken cancellationToken = default);
    Task SendTurnoutCommandAsync(int decoderAddress, int output, bool activate, bool queue = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current traffic monitor packets.
    /// </summary>
    IReadOnlyList<Z21TrafficPacket> GetTrafficPackets();

    /// <summary>
    /// Clears the traffic monitor.
    /// </summary>
    void ClearTrafficMonitor();

    /// <summary>
    /// Requests the current system state from the Z21.
    /// Called when window is activated to refresh connection status.
    /// </summary>
    Task RequestSystemStateAsync(CancellationToken cancellationToken = default);
}

