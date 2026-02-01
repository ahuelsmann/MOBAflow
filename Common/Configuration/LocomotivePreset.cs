// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Common.Configuration;

/// <summary>
/// Represents a locomotive preset with DCC address and runtime state.
/// Used for quick switching between up to 3 locomotives in Train Control.
/// This is a simple POCO for configuration persistence - not an ObservableObject.
/// </summary>
public class LocomotivePreset
{
    /// <summary>
    /// Display name for this preset (e.g., "ICE 401", "BR 218", "Vectron").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// DCC address of the locomotive (1-9999).
    /// </summary>
    public int DccAddress { get; set; } = 3;

    /// <summary>
    /// Current speed value (0-126 for 128 speed steps).
    /// Persisted to restore state when switching presets.
    /// </summary>
    public int Speed { get; set; }

    /// <summary>
    /// Current direction: true = forward, false = reverse.
    /// </summary>
    public bool IsForward { get; set; } = true;

    /// <summary>
    /// Function states F0-F28 as bitmask.
    /// F0 = bit 0, F1 = bit 1, ..., F28 = bit 28.
    /// </summary>
    public uint FunctionStates { get; set; }

    /// <summary>
    /// Gets or sets the state of a specific function.
    /// </summary>
    public bool GetFunction(int functionNumber)
    {
        return functionNumber >= 0 && functionNumber <= 28 && (FunctionStates & (1u << functionNumber)) != 0;
    }

    /// <summary>
    /// Sets the state of a specific function.
    /// </summary>
    public void SetFunction(int functionNumber, bool isOn)
    {
        if (functionNumber < 0 || functionNumber > 28) return;
        if (isOn)
            FunctionStates |= 1u << functionNumber;
        else
            FunctionStates &= ~(1u << functionNumber);
    }
}
