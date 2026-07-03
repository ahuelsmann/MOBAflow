// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.Common;

using Moba.Common.Configuration;
using Moba.SharedUI.ViewModel;

using NUnit.Framework;

[TestFixture]
internal sealed class TrainControlDccSpeedTests
{
    [Test]
    public void SpeedStepToKmh_AtMaxStep_ReturnsGaugeMax()
    {
        var maxStep = TrainControlDccSpeed.GetMaxSpeedStep(DccSpeedSteps.Steps128);

        var kmh = TrainControlDccSpeed.SpeedStepToKmh(maxStep, maxStep);

        Assert.That(kmh, Is.EqualTo(TrainControlDccSpeed.DefaultSpeedGaugeMaxKmh));
    }

    [Test]
    public void SpeedStepToKmh_AtHalfStep_ReturnsHalfGaugeMax()
    {
        var maxStep = TrainControlDccSpeed.GetMaxSpeedStep(DccSpeedSteps.Steps128);

        var kmh = TrainControlDccSpeed.SpeedStepToKmh(maxStep / 2, maxStep);

        Assert.That(kmh, Is.EqualTo(TrainControlDccSpeed.DefaultSpeedGaugeMaxKmh / 2));
    }

    [Test]
    public void KmhToSpeedStep_And_SpeedStepToKmh_AreInverseAt400Kmh()
    {
        const int targetKmh = 400;
        var maxStep = TrainControlDccSpeed.GetMaxSpeedStep(DccSpeedSteps.Steps128);

        var step = TrainControlDccSpeed.KmhToSpeedStep(targetKmh, TrainControlDccSpeed.DefaultSpeedGaugeMaxKmh, maxStep);
        var kmh = TrainControlDccSpeed.SpeedStepToKmh(step, maxStep);

        Assert.That(step, Is.EqualTo(maxStep));
        Assert.That(kmh, Is.EqualTo(targetKmh));
    }

    [Test]
    public void KmhToSpeedStep_Preset250_MapsBelowMaxStep()
    {
        var maxStep = TrainControlDccSpeed.GetMaxSpeedStep(DccSpeedSteps.Steps128);

        var step = TrainControlDccSpeed.KmhToSpeedStep(250, TrainControlDccSpeed.DefaultSpeedGaugeMaxKmh, maxStep);

        Assert.That(step, Is.EqualTo(79));
        Assert.That(TrainControlDccSpeed.SpeedStepToKmh(step, maxStep), Is.EqualTo(251));
    }

    [Test]
    public void SpeedStepToKmh_IgnoresLocomotiveVmax_UsesGaugeMax()
    {
        const int locomotiveVmax = 200;
        var maxStep = TrainControlDccSpeed.GetMaxSpeedStep(DccSpeedSteps.Steps128);

        var kmhAtMax = TrainControlDccSpeed.SpeedStepToKmh(maxStep, maxStep);
        var kmhAtHalf = TrainControlDccSpeed.SpeedStepToKmh(maxStep / 2, maxStep);

        Assert.That(kmhAtMax, Is.EqualTo(TrainControlDccSpeed.DefaultSpeedGaugeMaxKmh));
        Assert.That(kmhAtMax, Is.Not.EqualTo(locomotiveVmax));
        Assert.That(kmhAtHalf, Is.EqualTo(TrainControlDccSpeed.DefaultSpeedGaugeMaxKmh / 2));
    }
}
