// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Common.Configuration;

/// <summary>
/// DCC speed step configuration (14, 28, or 128 steps).
/// 
/// DCC Specification:
/// - 14 Steps: Values 0-13 (0 = stop/emergency stop)
/// - 28 Steps: Values 0-27 (0 = stop/emergency stop)
/// - 128 Steps: Values 0-127 (0 = stop, 1 = emergency stop, 2-127 valid speed steps)
///   Actual highest speed step: 126 (127 reserved)
/// </summary>
public enum DccSpeedSteps
{
    /// <summary>
    /// 14 speed steps (legacy DCC).
    /// Highest actual speed step: 13
    /// </summary>
    Steps14 = 14,

    /// <summary>
    /// 28 speed steps (standard DCC).
    /// Highest actual speed step: 27
    /// </summary>
    Steps28 = 28,

    /// <summary>
    /// 128 speed steps (extended DCC).
    /// Highest actual speed step: 126 (127 = emergency stop reserved).
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
    /// Selected locomotive series name (e.g., "BR 103", "ICE 3").
    /// Empty string = no series selected, uses default Vmax.
    /// </summary>
    public string SelectedLocoSeries { get; set; } = string.Empty;

    /// <summary>
    /// Maximum speed (Vmax) of the selected locomotive in km/h.
    /// Default: 200 km/h. Used for SpeedKmh calculation.
    /// </summary>
    public int SelectedVmax { get; set; } = 200;

    /// <summary>
    /// Id der zuletzt in der Combobox „Lok aus Projekt“ gewählten Lokomotive.
    /// Wird beim Laden wiederhergestellt, wenn die Lok noch im aktuellen Projekt ist.
    /// </summary>
    public Guid? SelectedLocomotiveFromProjectId { get; set; }

    /// <summary>
    /// Locomotive presets for quick switching.
    /// </summary>
    public List<LocomotivePreset> Presets { get; set; } = [];
}
