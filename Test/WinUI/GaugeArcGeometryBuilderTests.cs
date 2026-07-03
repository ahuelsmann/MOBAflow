// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

#if WINDOWS

namespace Moba.Test.WinUI;

using Moba.WinUI.Controls;

[TestFixture]
internal sealed class GaugeArcGeometryBuilderTests
{
    [TestCase(0)]
    [TestCase(0.0005)]
    [TestCase(-0.1)]
    public void TryGetSweepArcDefinition_ReturnsFalse_WhenSweepTooSmall(double normalizedValue)
    {
        Assert.That(
            GaugeArcGeometryBuilder.TryGetSweepArcDefinition(normalizedValue, out _),
            Is.False);
    }

    [TestCase(double.NaN)]
    [TestCase(double.PositiveInfinity)]
    [TestCase(double.NegativeInfinity)]
    public void TryGetSweepArcDefinition_ReturnsFalse_WhenNormalizedValueInvalid(double normalizedValue)
    {
        Assert.That(
            GaugeArcGeometryBuilder.TryGetSweepArcDefinition(normalizedValue, out _),
            Is.False);
    }

    [Test]
    public void TryGetSweepArcDefinition_FullScale_EndsAtRightAnchor()
    {
        Assert.That(
            GaugeArcGeometryBuilder.TryGetSweepArcDefinition(1, out var definition),
            Is.True);

        Assert.That(definition.StartPoint.X, Is.EqualTo(GaugeVisualRules.GaugeCenterX - 100).Within(0.01));
        Assert.That(definition.StartPoint.Y, Is.EqualTo(GaugeVisualRules.GaugeCenterY).Within(0.01));
        Assert.That(definition.EndPoint.X, Is.EqualTo(GaugeVisualRules.GaugeCenterX + 100).Within(0.01));
        Assert.That(definition.EndPoint.Y, Is.EqualTo(GaugeVisualRules.GaugeCenterY).Within(0.01));
        Assert.That(definition.IsLargeArc, Is.False);
    }

    [Test]
    public void TryGetSweepArcDefinition_HalfScale_EndsAtTopAnchor()
    {
        Assert.That(
            GaugeArcGeometryBuilder.TryGetSweepArcDefinition(0.5, out var definition),
            Is.True);

        Assert.That(definition.EndPoint.X, Is.EqualTo(GaugeVisualRules.GaugeCenterX).Within(0.01));
        Assert.That(definition.EndPoint.Y, Is.EqualTo(GaugeVisualRules.GaugeCenterY - 100).Within(0.01));
    }

    [TestCase(0.01)]
    [TestCase(0.25)]
    [TestCase(0.75)]
    [TestCase(1.0)]
    [TestCase(1.5)]
    public void TryGetSweepArcDefinition_ProducesFiniteEndpoints(double normalizedValue)
    {
        Assert.That(
            GaugeArcGeometryBuilder.TryGetSweepArcDefinition(normalizedValue, out var definition),
            Is.True);

        Assert.That(double.IsFinite(definition.EndPoint.X), Is.True);
        Assert.That(double.IsFinite(definition.EndPoint.Y), Is.True);
        Assert.That(definition.Radius, Is.EqualTo(100).Within(0.01));
    }
}

#endif
