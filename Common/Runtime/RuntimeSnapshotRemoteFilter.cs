// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Common.Runtime;

/// <summary>
/// Builds MOBAsmart-facing runtime snapshots without locomotive states.
/// Mobile clients read drive/function feedback directly from the Z21 when connected.
/// </summary>
public static class RuntimeSnapshotRemoteFilter
{
    /// <summary>
    /// Returns a snapshot for MOBApi / SignalR transport: domain state only, no <see cref="MobaRuntimeSnapshot.LocomotiveStates"/>.
    /// </summary>
    public static MobaRuntimeSnapshot ForMobasmartBroadcast(MobaRuntimeSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        return new MobaRuntimeSnapshot
        {
            IsConnected = snapshot.IsConnected,
            IsTrackPowerOn = snapshot.IsTrackPowerOn,
            StatusText = snapshot.StatusText,
            SerialNumber = snapshot.SerialNumber,
            FirmwareVersion = snapshot.FirmwareVersion,
            HardwareType = snapshot.HardwareType,
            MainCurrent = snapshot.MainCurrent,
            ProgCurrent = snapshot.ProgCurrent,
            FilteredMainCurrent = snapshot.FilteredMainCurrent,
            Temperature = snapshot.Temperature,
            SupplyVoltage = snapshot.SupplyVoltage,
            VccVoltage = snapshot.VccVoltage,
            IsZ21Connecting = snapshot.IsZ21Connecting,
            HasSeenSuccessfulConnection = snapshot.HasSeenSuccessfulConnection,
            IsManualDisconnectRequested = snapshot.IsManualDisconnectRequested,
            IsEmergencyStopActive = snapshot.IsEmergencyStopActive,
            IsShortCircuitActive = snapshot.IsShortCircuitActive,
            IsProgrammingModeActive = snapshot.IsProgrammingModeActive,
            LastFailSafeReason = snapshot.LastFailSafeReason,
            LastFailSafeAt = snapshot.LastFailSafeAt,
            IsOperatorAckRequired = snapshot.IsOperatorAckRequired,
            JourneyStates = snapshot.JourneyStates,
            LocomotiveStates = new Dictionary<int, LocomotiveRuntimeSnapshot>(),
            LocomotiveFleet = snapshot.LocomotiveFleet,
            SignalBoxElements = snapshot.SignalBoxElements,
            CreatedAt = snapshot.CreatedAt
        };
    }
}
