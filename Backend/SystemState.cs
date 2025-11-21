// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
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
    /// Internal temperature in degrees Celsius (°C).
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
    /// </summary>
    public byte CentralState { get; set; }

    /// <summary>
    /// Extended central state byte (additional status flags).
    /// </summary>
    public byte CentralStateEx { get; set; }
}