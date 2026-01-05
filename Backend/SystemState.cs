// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Backend;

/// <summary>
/// Represents the current system state of the Z21 digital command station.
/// </summary>
public class SystemState
{
    /// <summary>
    /// Main track current in milliamperes (mA).
    /// </summary>
    public int MainCurrent { get; set; }

    /// <summary>
    /// Programming track current in milliamperes (mA).
    /// </summary>
    public int ProgCurrent { get; set; }

    /// <summary>
    /// Filtered main track current in milliamperes (mA).
    /// </summary>
    public int FilteredMainCurrent { get; set; }

    /// <summary>
    /// Internal temperature in degrees Celsius.
    /// </summary>
    public int Temperature { get; set; }

    /// <summary>
    /// Supply voltage in millivolts (mV).
    /// </summary>
    public int SupplyVoltage { get; set; }

    /// <summary>
    /// VCC voltage in millivolts (mV).
    /// </summary>
    public int VccVoltage { get; set; }

    /// <summary>
    /// Central state byte (bit flags for track power, emergency stop, etc.).
    /// Bit 0 (0x01): Emergency stop active
    /// Bit 1 (0x02): Track power OFF
    /// Bit 2 (0x04): Short circuit detected
    /// Bit 3 (0x08): Programming mode active
    /// </summary>
    public byte CentralState { get; set; }

    /// <summary>
    /// Extended central state byte (additional status flags).
    /// </summary>
    public byte CentralStateEx { get; set; }

    /// <summary>
    /// Gets whether track power is currently ON (bit 1 of CentralState NOT set).
    /// </summary>
    public bool IsTrackPowerOn => (CentralState & 0x02) == 0;

    /// <summary>
    /// Gets whether emergency stop is active (bit 0 of CentralState set).
    /// </summary>
    public bool IsEmergencyStop => (CentralState & 0x01) != 0;

    /// <summary>
    /// Gets whether a short circuit is detected (bit 2 of CentralState set).
    /// </summary>
    public bool IsShortCircuit => (CentralState & 0x04) != 0;

    /// <summary>
    /// Gets whether programming mode is active (bit 3 of CentralState set).
    /// </summary>
    public bool IsProgrammingMode => (CentralState & 0x08) != 0;
}
