// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.SharedUI;

using Moba.Common.Runtime;
using Moba.SharedUI.TrainControl;

/// <summary>
/// Readings and peaks of the train-control telemetry.
/// </summary>
[TestFixture]
internal sealed class TelemetryProjectionTests
{
    private static readonly TrainControlTelemetry Initial = new(100, 0, 90, 18000, 40, PeakMainCurrent: 500, PeakTemperature: 45);

    [Test]
    public void Apply_TakesTheReadingsAndRaisesOnlyHigherPeaks()
    {
        var lowerThanPeaks = TelemetryProjection.Apply(Initial, Snapshot(mainCurrent: 300, temperature: 42));
        var higherThanPeaks = TelemetryProjection.Apply(Initial, Snapshot(mainCurrent: 800, temperature: 50));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(lowerThanPeaks, Is.EqualTo(new TrainControlTelemetry(300, 5, 280, 17500, 42, 500, 45)));
            Assert.That(higherThanPeaks, Is.EqualTo(new TrainControlTelemetry(800, 5, 280, 17500, 50, 800, 50)));
        }
    }

    [Test]
    public void Apply_ReturnsNullWhenNoReadingChanged()
    {
        var unchanged = new MobaRuntimeSnapshot
        {
            MainCurrent = 100,
            ProgCurrent = 0,
            FilteredMainCurrent = 90,
            SupplyVoltage = 18000,
            Temperature = 40,
        };

        Assert.That(TelemetryProjection.Apply(Initial, unchanged), Is.Null);
    }

    private static MobaRuntimeSnapshot Snapshot(int mainCurrent, int temperature) =>
        new()
        {
            MainCurrent = mainCurrent,
            ProgCurrent = 5,
            FilteredMainCurrent = 280,
            SupplyVoltage = 17500,
            Temperature = temperature,
        };
}
