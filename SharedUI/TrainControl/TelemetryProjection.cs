// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.TrainControl;

using Common.Runtime;

/// <summary>
/// The command station readings shown on the train-control desk, with the peaks since the last reset.
/// </summary>
/// <param name="MainCurrent">Main track current in mA.</param>
/// <param name="ProgCurrent">Programming track current in mA.</param>
/// <param name="FilteredMainCurrent">Filtered main track current in mA.</param>
/// <param name="SupplyVoltage">Supply voltage in mV.</param>
/// <param name="Temperature">Command station temperature in °C.</param>
/// <param name="PeakMainCurrent">Highest main track current since the last reset.</param>
/// <param name="PeakTemperature">Highest temperature since the last reset.</param>
public sealed record TrainControlTelemetry(
    int MainCurrent,
    int ProgCurrent,
    int FilteredMainCurrent,
    int SupplyVoltage,
    int Temperature,
    int PeakMainCurrent,
    int PeakTemperature);

/// <summary>
/// Updates the train-control telemetry from runtime snapshots.
/// </summary>
public static class TelemetryProjection
{
    /// <summary>
    /// Takes the readings of <paramref name="snapshot"/> and raises the peaks; returns null when no reading changed.
    /// </summary>
    public static TrainControlTelemetry? Apply(TrainControlTelemetry current, MobaRuntimeSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(current);
        ArgumentNullException.ThrowIfNull(snapshot);
        if (current.MainCurrent == snapshot.MainCurrent
            && current.ProgCurrent == snapshot.ProgCurrent
            && current.FilteredMainCurrent == snapshot.FilteredMainCurrent
            && current.SupplyVoltage == snapshot.SupplyVoltage
            && current.Temperature == snapshot.Temperature)
        {
            return null;
        }

        return new TrainControlTelemetry(
            snapshot.MainCurrent,
            snapshot.ProgCurrent,
            snapshot.FilteredMainCurrent,
            snapshot.SupplyVoltage,
            snapshot.Temperature,
            Math.Max(current.PeakMainCurrent, snapshot.MainCurrent),
            Math.Max(current.PeakTemperature, snapshot.Temperature));
    }
}
