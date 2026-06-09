// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.SharedUI.Service;

using Common.Runtime;

/// <summary>
/// Pure projection helpers for applying runtime snapshots to UI state.
/// </summary>
public static class RuntimeSnapshotProjector
{
    /// <summary>
    /// Projects the shared connection, status and electrical state used by multiple ViewModels.
    /// </summary>
    public static RuntimeStatusProjection ProjectStatus(MobaRuntimeSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        return new RuntimeStatusProjection(
            snapshot.IsConnected,
            snapshot.IsTrackPowerOn,
            snapshot.StatusText,
            snapshot.SerialNumber,
            snapshot.FirmwareVersion,
            snapshot.HardwareType,
            snapshot.MainCurrent,
            snapshot.Temperature,
            snapshot.SupplyVoltage,
            snapshot.VccVoltage,
            snapshot.IsZ21Connecting,
            snapshot.HasSeenSuccessfulConnection,
            snapshot.IsManualDisconnectRequested,
            snapshot.IsEmergencyStopActive,
            snapshot.IsShortCircuitActive,
            snapshot.IsProgrammingModeActive,
            snapshot.LastFailSafeReason,
            snapshot.LastFailSafeAt,
            snapshot.IsOperatorAckRequired);
    }

    /// <summary>
    /// Projects the mobile connection label and whether a fresh connection should persist the entered IP address.
    /// </summary>
    public static MauiRuntimeProjection ProjectMaui(MobaRuntimeSnapshot snapshot, bool wasConnected)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        var connectionStatus = snapshot.IsConnected
            ? "Connected"
            : string.Equals(snapshot.StatusText, "Disconnected", StringComparison.OrdinalIgnoreCase)
                ? null
                : snapshot.StatusText;

        return new MauiRuntimeProjection(ProjectStatus(snapshot), connectionStatus, snapshot.IsConnected && !wasConnected);
    }

    /// <summary>
    /// Projects the train-control runtime data for one selected locomotive address.
    /// </summary>
    public static TrainControlRuntimeProjection ProjectTrainControl(
        MobaRuntimeSnapshot snapshot,
        bool wasConnected,
        int locomotiveAddress)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        snapshot.LocomotiveStates.TryGetValue(locomotiveAddress, out var locomotiveState);
        return new TrainControlRuntimeProjection(
            snapshot.IsConnected,
            wasConnected != snapshot.IsConnected,
            locomotiveState);
    }
}

public sealed record RuntimeStatusProjection(
    bool IsConnected,
    bool IsTrackPowerOn,
    string StatusText,
    string SerialNumber,
    string FirmwareVersion,
    string HardwareType,
    int MainCurrent,
    int Temperature,
    int SupplyVoltage,
    int VccVoltage,
    bool IsZ21Connecting,
    bool HasSeenSuccessfulConnection,
    bool IsManualDisconnectRequested,
    bool IsEmergencyStopActive,
    bool IsShortCircuitActive,
    bool IsProgrammingModeActive,
    string LastFailSafeReason,
    DateTimeOffset? LastFailSafeAt,
    bool IsOperatorAckRequired);

public sealed record MauiRuntimeProjection(
    RuntimeStatusProjection Status,
    string? Z21ConnectionStatus,
    bool ShouldPersistCurrentIpAddress);

public sealed record TrainControlRuntimeProjection(
    bool IsConnected,
    bool ConnectionChanged,
    LocomotiveRuntimeSnapshot? LocomotiveState);
