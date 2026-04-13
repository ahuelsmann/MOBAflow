// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.SharedUI.ViewModel;

using Common.Configuration;

/// <summary>
/// DCC speed-step limits for train control (shared by <see cref="TrainControlViewModel"/>).
/// </summary>
public static class TrainControlDccSpeed
{
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
}