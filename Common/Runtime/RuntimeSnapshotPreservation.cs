// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.



namespace Moba.Common.Runtime;



/// <summary>

/// Keeps project-derived runtime elements in snapshots when telemetry-only updates omit them.

/// </summary>

public static class RuntimeSnapshotPreservation

{

    /// <summary>

    /// Returns <paramref name="incoming"/> with signal-box elements and locomotive fleet taken from

    /// <paramref name="previous"/> when the incoming snapshot does not carry that state.

    /// </summary>

    public static MobaRuntimeSnapshot PreserveProjectElementsFrom(

        MobaRuntimeSnapshot incoming,

        MobaRuntimeSnapshot? previous)

    {

        ArgumentNullException.ThrowIfNull(incoming);



        var withSignalBox = PreserveSignalBoxElementsFrom(incoming, previous);

        return PreserveLocomotiveFleetFrom(withSignalBox, previous);

    }



    /// <summary>

    /// Returns <paramref name="incoming"/> with signal-box elements taken from <paramref name="previous"/>

    /// when the incoming snapshot does not carry any signal-box state.

    /// </summary>

    public static MobaRuntimeSnapshot PreserveSignalBoxElementsFrom(

        MobaRuntimeSnapshot incoming,

        MobaRuntimeSnapshot? previous)

    {

        ArgumentNullException.ThrowIfNull(incoming);



        if (incoming.SignalBoxElements.Count > 0)

        {

            return incoming;

        }



        if (previous?.SignalBoxElements is not { Count: > 0 })

        {

            return incoming;

        }



        return CopyWith(incoming, previous.SignalBoxElements, incoming.LocomotiveFleet);

    }



    /// <summary>

    /// Returns <paramref name="incoming"/> with locomotive fleet taken from <paramref name="previous"/>

    /// when the incoming snapshot does not carry any fleet state.

    /// </summary>

    public static MobaRuntimeSnapshot PreserveLocomotiveFleetFrom(

        MobaRuntimeSnapshot incoming,

        MobaRuntimeSnapshot? previous)

    {

        ArgumentNullException.ThrowIfNull(incoming);



        if (incoming.LocomotiveFleet.Count > 0)

        {

            return incoming;

        }



        if (previous?.LocomotiveFleet is not { Count: > 0 })

        {

            return incoming;

        }



        return CopyWith(incoming, incoming.SignalBoxElements, previous.LocomotiveFleet);

    }



    private static MobaRuntimeSnapshot CopyWith(

        MobaRuntimeSnapshot source,

        IReadOnlyList<SignalBoxElementRuntimeSnapshot> signalBoxElements,

        IReadOnlyList<LocomotiveFleetSnapshot> locomotiveFleet)

    {

        return new MobaRuntimeSnapshot

        {

            IsConnected = source.IsConnected,

            IsTrackPowerOn = source.IsTrackPowerOn,

            StatusText = source.StatusText,

            SerialNumber = source.SerialNumber,

            FirmwareVersion = source.FirmwareVersion,

            HardwareType = source.HardwareType,

            MainCurrent = source.MainCurrent,

            ProgCurrent = source.ProgCurrent,

            FilteredMainCurrent = source.FilteredMainCurrent,

            Temperature = source.Temperature,

            SupplyVoltage = source.SupplyVoltage,

            VccVoltage = source.VccVoltage,

            IsZ21Connecting = source.IsZ21Connecting,

            HasSeenSuccessfulConnection = source.HasSeenSuccessfulConnection,

            IsManualDisconnectRequested = source.IsManualDisconnectRequested,

            IsEmergencyStopActive = source.IsEmergencyStopActive,

            IsShortCircuitActive = source.IsShortCircuitActive,

            IsProgrammingModeActive = source.IsProgrammingModeActive,

            LastFailSafeReason = source.LastFailSafeReason,

            LastFailSafeAt = source.LastFailSafeAt,

            IsOperatorAckRequired = source.IsOperatorAckRequired,

            JourneyStates = source.JourneyStates,

            LocomotiveStates = source.LocomotiveStates,

            LocomotiveFleet = locomotiveFleet,

            SignalBoxElements = signalBoxElements,

            CreatedAt = source.CreatedAt

        };

    }

}

