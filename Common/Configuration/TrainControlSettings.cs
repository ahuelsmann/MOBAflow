// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Common.Configuration;

/// <summary>
/// DCC speed step configuration (14, 28, or 128 steps).
/// </summary>
public enum DccSpeedSteps
{
    /// <summary>
    /// 14 speed steps (legacy DCC).
    /// </summary>
    Steps14 = 14,

    /// <summary>
    /// 28 speed steps (standard DCC).
    /// </summary>
    Steps28 = 28,

    /// <summary>
    /// 128 speed steps (extended DCC).
    /// Actual values: 0-126 (127 = emergency stop).
    /// </summary>
    Steps128 = 128
}

/// <summary>
/// Train Control page settings.
/// </summary>
public class TrainControlSettings
{
    /// <summary>
    /// DCC speed step configuration (14, 28, or 128 steps).
    /// Default: 128 steps (most modern decoders).
    /// </summary>
    public DccSpeedSteps SpeedSteps { get; set; } = DccSpeedSteps.Steps128;

    /// <summary>
    /// Currently selected preset index (0-2).
    /// </summary>
    public int SelectedPresetIndex { get; set; }

    /// <summary>
    /// Speed ramp step size (1-20).
    /// </summary>
    public int SpeedRampStepSize { get; set; } = 5;

    /// <summary>
    /// Speed ramp interval in milliseconds (50-500).
    /// </summary>
    public int SpeedRampIntervalMs { get; set; } = 100;

    /// <summary>
    /// Locomotive presets for quick switching.
    /// </summary>
    public List<LocomotivePreset> Presets { get; set; } = [];
}
