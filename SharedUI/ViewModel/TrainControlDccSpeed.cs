// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.SharedUI.ViewModel;

using Common.Configuration;

/// <summary>
/// DCC speed-step limits for train control (shared by <see cref="TrainControlViewModel"/>).
/// </summary>
public static class TrainControlDccSpeed
{
    /// <summary>
    /// Full-scale maximum on the speed gauge and slider (km/h ring), shared by MOBAflow and MOBAsmart.
    /// </summary>
    public const int DefaultSpeedGaugeMaxKmh = 400;

    /// <summary>
    /// Returns the highest speed step index for the given DCC mode (e.g. 126 for 128-step mode).
    /// </summary>
    public static int GetMaxSpeedStep(DccSpeedSteps steps) => steps switch
    {
        DccSpeedSteps.Steps14 => 13,
        DccSpeedSteps.Steps28 => 27,
        DccSpeedSteps.Steps128 => 126,
        _ => throw new NotImplementedException(),
    };

    /// <summary>
    /// Converts a DCC speed step to km/h using the gauge full scale (not locomotive Vmax).
    /// </summary>
    public static int SpeedStepToKmh(int speedStep, int maxSpeedStep, int gaugeMaxKmh = DefaultSpeedGaugeMaxKmh)
    {
        if (maxSpeedStep == 0)
        {
            return 0;
        }

        var scaleMax = gaugeMaxKmh > 0 ? gaugeMaxKmh : DefaultSpeedGaugeMaxKmh;
        return (int)Math.Round((double)speedStep / maxSpeedStep * scaleMax);
    }

    /// <summary>
    /// Converts a target speed in km/h to the nearest DCC speed step (inverse of <see cref="SpeedStepToKmh"/>).
    /// </summary>
    public static int KmhToSpeedStep(int kmh, int gaugeMaxKmh, int maxSpeedStep)
    {
        if (maxSpeedStep == 0)
        {
            return 0;
        }

        var scaleMax = gaugeMaxKmh > 0 ? gaugeMaxKmh : DefaultSpeedGaugeMaxKmh;
        return (int)Math.Round((double)kmh / scaleMax * maxSpeedStep);
    }
}
