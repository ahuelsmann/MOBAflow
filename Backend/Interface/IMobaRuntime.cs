// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Backend.Interface;

using Common.Runtime;

using Domain;

using Model;

/// <summary>
/// Read-only runtime snapshot access.
/// </summary>
public interface IRuntimeSnapshotProvider
{
    MobaRuntimeSnapshot Current { get; }
}

/// <summary>
/// Runtime lifecycle and Z21 connection commands.
/// </summary>
public interface IConnectionRuntime
{
    Task StartAsync(CancellationToken cancellationToken = default);

    Task ActivateProjectAsync(Project editableProject, CancellationToken cancellationToken = default);

    Task ConnectAsync(CancellationToken cancellationToken = default);

    Task DisconnectAsync(CancellationToken cancellationToken = default);

    Task SetTrackPowerAsync(bool isOn, CancellationToken cancellationToken = default);

    Task AcknowledgeFailSafeAsync(CancellationToken cancellationToken = default);

    Task RequestSystemStateAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Locomotive command role of the runtime.
/// </summary>
public interface ILocomotiveRuntime
{
    Task SetLocomotiveDriveAsync(int address, int speed, bool forward, CancellationToken cancellationToken = default);

    Task SetLocomotiveFunctionAsync(int address, int functionIndex, bool isOn, CancellationToken cancellationToken = default);

    Task SetAllLocomotiveFunctionsOffAsync(int address, CancellationToken cancellationToken = default);

    Task RequestLocomotiveInfoAsync(int address, CancellationToken cancellationToken = default);
}

/// <summary>
/// Signal, turnout and journey command role of the runtime.
/// </summary>
public interface ISignalTurnoutRuntime
{
    Task SimulateFeedbackAsync(int inPort, CancellationToken cancellationToken = default);

    Task ResetJourneyAsync(Guid journeyId, CancellationToken cancellationToken = default);

    Task SetSignalAspectAsync(SbSignal signal, CancellationToken cancellationToken = default);

    Task SetSignalAspectAsync(Guid signalId, SignalAspect signalAspect, CancellationToken cancellationToken = default);

    Task SendTurnoutCommandAsync(int decoderAddress, int output, bool activate, bool queue = false, CancellationToken cancellationToken = default);
}

/// <summary>
/// Runtime traffic monitoring and polling diagnostics.
/// </summary>
public interface ITrafficMonitor
{
    void SetSystemStatePollingInterval(int intervalSeconds);

    IReadOnlyList<Z21TrafficPacket> GetTrafficPackets();

    void ClearTrafficMonitor();
}

/// <summary>
/// Backward-compatible aggregate facade for existing runtime consumers.
/// Prefer the narrower role interfaces for new code.
/// </summary>
public interface IMobaRuntime :
    IRuntimeSnapshotProvider,
    IConnectionRuntime,
    ILocomotiveRuntime,
    ISignalTurnoutRuntime,
    ITrafficMonitor
{
}