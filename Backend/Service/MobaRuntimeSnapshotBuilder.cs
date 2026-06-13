// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Backend.Service;

using Common.Runtime;

using Domain;

using Model;

/// <summary>
/// Builds immutable <see cref="MobaRuntimeSnapshot"/> instances from runtime state.
/// </summary>
internal static class MobaRuntimeSnapshotBuilder
{
    public static MobaRuntimeSnapshot Create(
        MobaRuntimeTelemetryState telemetry,
        ActiveProjectContext? activeProjectContext)
    {
        var journeyStates = new Dictionary<Guid, JourneyRuntimeSnapshot>();
        var signalBoxElements = new List<SignalBoxElementRuntimeSnapshot>();

        if (activeProjectContext != null)
        {
            foreach (var journey in activeProjectContext.ActiveProject.Journeys)
            {
                var state = activeProjectContext.JourneyManager.GetState(journey.Id);
                if (state == null)
                {
                    continue;
                }

                journeyStates[journey.Id] = new JourneyRuntimeSnapshot
                {
                    JourneyId = journey.Id,
                    Counter = state.Counter,
                    CurrentPos = state.CurrentPos,
                    CurrentStationName = state.CurrentStationName,
                    LastFeedbackTime = state.LastFeedbackTime,
                    IsActive = state.IsActive
                };
            }

            foreach (var element in activeProjectContext.ActiveProject.SignalBoxPlan?.Elements ?? [])
            {
                switch (element)
                {
                    case SbSignal signal:
                        signalBoxElements.Add(new SignalBoxElementRuntimeSnapshot
                        {
                            ElementId = signal.Id,
                            Name = signal.Name,
                            Kind = SignalBoxElementKind.Signal,
                            X = signal.X,
                            Y = signal.Y,
                            SignalSystem = signal.SignalSystem,
                            SignalAspect = signal.SignalAspect
                        });
                        break;

                    case SbSwitch sw:
                        signalBoxElements.Add(new SignalBoxElementRuntimeSnapshot
                        {
                            ElementId = sw.Id,
                            Name = sw.Name,
                            Kind = SignalBoxElementKind.Switch,
                            X = sw.X,
                            Y = sw.Y,
                            Address = sw.Address,
                            SwitchPosition = sw.SwitchPosition
                        });
                        break;
                }
            }
        }

        return new MobaRuntimeSnapshot
        {
            IsConnected = telemetry.IsConnected,
            IsTrackPowerOn = telemetry.IsTrackPowerOn,
            StatusText = telemetry.StatusText,
            SerialNumber = telemetry.SerialNumber,
            FirmwareVersion = telemetry.FirmwareVersion,
            HardwareType = telemetry.HardwareType,
            MainCurrent = telemetry.MainCurrent,
            ProgCurrent = telemetry.ProgCurrent,
            FilteredMainCurrent = telemetry.FilteredMainCurrent,
            Temperature = telemetry.Temperature,
            SupplyVoltage = telemetry.SupplyVoltage,
            VccVoltage = telemetry.VccVoltage,
            IsZ21Connecting = telemetry.IsZ21Connecting,
            HasSeenSuccessfulConnection = telemetry.HasSeenSuccessfulConnection,
            IsManualDisconnectRequested = telemetry.IsManualDisconnectRequested,
            IsEmergencyStopActive = telemetry.IsEmergencyStopActive,
            IsShortCircuitActive = telemetry.IsShortCircuitActive,
            IsProgrammingModeActive = telemetry.IsProgrammingModeActive,
            LastFailSafeReason = telemetry.LastFailSafeReason,
            LastFailSafeAt = telemetry.LastFailSafeAt,
            IsOperatorAckRequired = telemetry.IsOperatorAckRequired,
            JourneyStates = journeyStates,
            LocomotiveStates = new Dictionary<int, LocomotiveRuntimeSnapshot>(telemetry.LocomotiveStates),
            SignalBoxElements = signalBoxElements,
            CreatedAt = DateTimeOffset.Now
        };
    }
}

/// <summary>
/// Mutable Z21 telemetry and connection flags owned by <see cref="MobaRuntimeService"/>.
/// </summary>
internal sealed class MobaRuntimeTelemetryState
{
    public bool IsConnected { get; set; }
    public bool IsTrackPowerOn { get; set; }
    public string StatusText { get; set; } = "Disconnected";
    public string SerialNumber { get; set; } = "-";
    public string FirmwareVersion { get; set; } = "-";
    public string HardwareType { get; set; } = "-";
    public int MainCurrent { get; set; }
    public int ProgCurrent { get; set; }
    public int FilteredMainCurrent { get; set; }
    public int Temperature { get; set; }
    public int SupplyVoltage { get; set; }
    public int VccVoltage { get; set; }
    public bool IsZ21Connecting { get; set; } = true;
    public bool HasSeenSuccessfulConnection { get; set; }
    public bool IsManualDisconnectRequested { get; set; }
    public bool IsEmergencyStopActive { get; set; }
    public bool IsShortCircuitActive { get; set; }
    public bool IsProgrammingModeActive { get; set; }
    public string LastFailSafeReason { get; set; } = "Waiting for the Z21 connection.";
    public DateTimeOffset? LastFailSafeAt { get; set; }
    public bool IsOperatorAckRequired { get; set; }
    public Dictionary<int, LocomotiveRuntimeSnapshot> LocomotiveStates { get; } = [];
}
