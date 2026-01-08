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
        if (functionNumber < 0 || functionNumber > 28) return false;
        return (FunctionStates & (1u << functionNumber)) != 0;
    }

    /// <summary>
    /// Sets the state of a specific function.
    /// </summary>
    public void SetFunction(int functionNumber, bool isOn)
    {
        if (functionNumber < 0 || functionNumber > 28) return;
        if (isOn)
            FunctionStates |= (1u << functionNumber);
        else
            FunctionStates &= ~(1u << functionNumber);
    }
}

/// <summary>
/// Train Control settings including locomotive presets.
/// </summary>
public class TrainControlSettings
{
    /// <summary>
    /// Currently selected preset index (0, 1, or 2).
    /// </summary>
    public int SelectedPresetIndex { get; set; }

    /// <summary>
    /// Three locomotive presets for quick switching.
    /// </summary>
    public List<LocomotivePreset> Presets { get; set; } =
    [
        new LocomotivePreset { Name = "Lok 1", DccAddress = 3 },
        new LocomotivePreset { Name = "Lok 2", DccAddress = 4 },
        new LocomotivePreset { Name = "Lok 3", DccAddress = 5 }
    ];

    /// <summary>
    /// Speed ramp step size (1-20) for smooth acceleration/deceleration.
    /// </summary>
    public int SpeedRampStepSize { get; set; } = 5;

    /// <summary>
    /// Speed ramp interval in milliseconds (50-500).
    /// </summary>
    public int SpeedRampIntervalMs { get; set; } = 100;
}
