// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Common.Runtime;

/// <summary>
/// Immutable snapshot of the current MOBA runtime state for UI or API consumers.
/// </summary>
public sealed class MobaRuntimeSnapshot
{
    /// <summary>
    /// Gets an empty runtime snapshot representing a disconnected default state.
    /// </summary>
    public static MobaRuntimeSnapshot Empty { get; } = new();

    /// <summary>
    /// Gets a value indicating whether the Z21 is currently connected and responding.
    /// </summary>
    public bool IsConnected { get; init; }

    /// <summary>
    /// Gets a value indicating whether track power is currently enabled.
    /// </summary>
    public bool IsTrackPowerOn { get; init; }

    /// <summary>
    /// Gets the operator-facing status text for the runtime.
    /// </summary>
    public string StatusText { get; init; } = "Disconnected";

    /// <summary>
    /// Gets the Z21 serial number as display text.
    /// </summary>
    public string SerialNumber { get; init; } = "-";

    /// <summary>
    /// Gets the Z21 firmware version as display text.
    /// </summary>
    public string FirmwareVersion { get; init; } = "-";

    /// <summary>
    /// Gets the Z21 hardware type as display text.
    /// </summary>
    public string HardwareType { get; init; } = "-";

    /// <summary>
    /// Gets the main current in mA.
    /// </summary>
    public int MainCurrent { get; init; }

    /// <summary>
    /// Gets the programming track current in mA.
    /// </summary>
    public int ProgCurrent { get; init; }

    /// <summary>
    /// Gets the filtered main current in mA.
    /// </summary>
    public int FilteredMainCurrent { get; init; }

    /// <summary>
    /// Gets the Z21 temperature in Celsius.
    /// </summary>
    public int Temperature { get; init; }

    /// <summary>
    /// Gets the supply voltage in mV.
    /// </summary>
    public int SupplyVoltage { get; init; }

    /// <summary>
    /// Gets the VCC voltage in mV.
    /// </summary>
    public int VccVoltage { get; init; }

    /// <summary>
    /// Gets a value indicating whether a connection attempt is in progress.
    /// </summary>
    public bool IsZ21Connecting { get; init; } = true;

    /// <summary>
    /// Gets a value indicating whether a successful Z21 connection has been observed before.
    /// </summary>
    public bool HasSeenSuccessfulConnection { get; init; }

    /// <summary>
    /// Gets a value indicating whether the current disconnect was requested by the operator.
    /// </summary>
    public bool IsManualDisconnectRequested { get; init; }

    /// <summary>
    /// Gets a value indicating whether emergency stop is active.
    /// </summary>
    public bool IsEmergencyStopActive { get; init; }

    /// <summary>
    /// Gets a value indicating whether the Z21 reports a short circuit.
    /// </summary>
    public bool IsShortCircuitActive { get; init; }

    /// <summary>
    /// Gets a value indicating whether programming mode is active.
    /// </summary>
    public bool IsProgrammingModeActive { get; init; }

    /// <summary>
    /// Gets the last fail-safe reason used for operator messaging.
    /// </summary>
    public string LastFailSafeReason { get; init; } = "Waiting for the Z21 connection.";

    /// <summary>
    /// Gets the timestamp of the last fail-safe trigger, if any.
    /// </summary>
    public DateTimeOffset? LastFailSafeAt { get; init; }

    /// <summary>
    /// Gets a value indicating whether an explicit operator acknowledgement is required.
    /// </summary>
    public bool IsOperatorAckRequired { get; init; }

    /// <summary>
    /// Gets the current runtime state of all active journeys.
    /// </summary>
    public IReadOnlyDictionary<Guid, JourneyRuntimeSnapshot> JourneyStates { get; init; }
        = new Dictionary<Guid, JourneyRuntimeSnapshot>();

    /// <summary>
    /// Gets the latest runtime state of subscribed locomotives by DCC address.
    /// </summary>
    public IReadOnlyDictionary<int, LocomotiveRuntimeSnapshot> LocomotiveStates { get; init; }
        = new Dictionary<int, LocomotiveRuntimeSnapshot>();

    /// <summary>
    /// Gets the timestamp when the snapshot was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.Now;
}

