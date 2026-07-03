// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
#if WINDOWS
namespace Moba.Test.WinUI;
using Moba.WinUI.Controls;
[TestFixture]
internal sealed class GaugeVisualRulesTests
{
    [Test]
    public void CalculateOuterScaleLabelPosition_MinValue_IsCenteredOnRadialAnchor()
    {
        const double labelWidth = 24; // "0"
        const double labelHeight = GaugeVisualRules.OuterMarkerLabelHeight;
        var (anchorX, anchorY, left, top) = GaugeVisualRules.CalculateOuterScaleLabelPosition(
            angleDeg: 180, labelWidth, labelHeight);
        var centerX = GaugeVisualRules.CalculateMarkerLabelCenterX(left, labelWidth);
        var centerY = top + (labelHeight / 2);
        Assert.That(anchorX, Is.EqualTo(GaugeVisualRules.GaugeCenterX - GaugeVisualRules.OuterMarkerLabelDistance).Within(0.5));
        Assert.That(centerX, Is.EqualTo(anchorX).Within(0.5),
            "0 label must sit on the radial line at 9 o'clock.");
        Assert.That(centerY, Is.EqualTo(anchorY).Within(0.5));
        Assert.That(left, Is.GreaterThanOrEqualTo(GaugeVisualRules.LabelCanvasEdgeInset));
    }
    [Test]
    public void CalculateOuterScaleLabelPosition_MaxValue_IsCenteredOnRadialAnchor()
    {
        const double labelWidth = 54; // "400"
        const double labelHeight = GaugeVisualRules.OuterMarkerLabelHeight;
        var (anchorX, anchorY, left, top) = GaugeVisualRules.CalculateOuterScaleLabelPosition(
            angleDeg: 0, labelWidth, labelHeight);
        var centerX = GaugeVisualRules.CalculateMarkerLabelCenterX(left, labelWidth);
        var centerY = top + (labelHeight / 2);
        Assert.That(anchorX, Is.EqualTo(GaugeVisualRules.GaugeCenterX + GaugeVisualRules.OuterMarkerLabelDistance).Within(0.5));
        Assert.That(centerX, Is.EqualTo(anchorX).Within(0.5),
            "Max label must sit on the radial line at 3 o'clock.");
        Assert.That(centerY, Is.EqualTo(anchorY).Within(0.5));
        Assert.That(left + labelWidth, Is.LessThanOrEqualTo(GaugeVisualRules.GaugeCanvasWidth));
    }
    [Test]
    public void CalculateOuterScaleLabelPosition_MilliampereMax_ClearsArcAndStaysRadial()
    {
        const double labelWidth = 52; // "3000"
        const double labelHeight = GaugeVisualRules.OuterMarkerLabelHeight;
        var (anchorX, _, left, _) = GaugeVisualRules.CalculateOuterScaleLabelPosition(
            angleDeg: 0, labelWidth, labelHeight);
        var centerX = GaugeVisualRules.CalculateMarkerLabelCenterX(left, labelWidth);
        var arcOuterEdge = GaugeVisualRules.GaugeCenterX + GaugeVisualRules.OuterArcRadius;
        Assert.That(left, Is.GreaterThanOrEqualTo(arcOuterEdge + GaugeVisualRules.LabelArcClearance));
        Assert.That(centerX, Is.EqualTo(anchorX).Within(0.5));
    }
    [TestCase(50, 400)]
    [TestCase(100, 400)]
    [TestCase(250, 400)]
    [TestCase(250, 3000)]
    [TestCase(500, 3000)]
    [TestCase(1500, 3000)]
    public void CalculateOuterScaleLabelPosition_IntermediateTicks_StayOnRadialLine(int value, int scaleMax)
    {
        var labelText = value.ToString();
        var labelWidth = GaugeVisualRules.CalculateMarkerLabelWidth(labelText);
        var labelHeight = GaugeVisualRules.OuterMarkerLabelHeight;
        var percentage = (double)value / scaleMax;
        var angleDeg = 180 - (percentage * 180);
        var (anchorX, anchorY, left, top) = GaugeVisualRules.CalculateOuterScaleLabelPosition(
            angleDeg, labelWidth, labelHeight);
        var centerX = GaugeVisualRules.CalculateMarkerLabelCenterX(left, labelWidth);
        var centerY = top + (labelHeight / 2);
        Assert.That(centerX, Is.EqualTo(anchorX).Within(0.5),
            $"{labelText} must stay centered on its radial anchor.");
        Assert.That(centerY, Is.EqualTo(anchorY).Within(0.5),
            $"{labelText} must stay vertically centered on its radial anchor.");
    }
    [Test]
    public void CalculateMarkerLabelLeft_DoesNotNudgeOffRadialAnchor()
    {
        const double labelWidth = 54;
        var anchorX = GaugeVisualRules.GaugeCenterX + GaugeVisualRules.OuterMarkerLabelDistance;
        var left = GaugeVisualRules.CalculateMarkerLabelLeft(anchorX, labelWidth);
        Assert.That(GaugeVisualRules.CalculateMarkerLabelCenterX(left, labelWidth),
            Is.EqualTo(anchorX).Within(0.5));
    }
    [Test]
    public void CalculateMarkerLabelTop_CentersLabelOnRadialAnchor()
    {
        const double anchorY = GaugeVisualRules.GaugeCenterY;
        const double labelHeight = GaugeVisualRules.OuterMarkerLabelHeight;
        var top = GaugeVisualRules.CalculateMarkerLabelTop(anchorY, labelHeight);
        var centerY = top + (labelHeight / 2);
        Assert.That(centerY, Is.EqualTo(anchorY).Within(0.01));
    }
    [Test]
    public void GaugeCenterX_IsCanvasHorizontalCenter()
    {
        Assert.That(GaugeVisualRules.GaugeCenterX, Is.EqualTo(GaugeVisualRules.GaugeCanvasWidth / 2));
    }
    [Test]
    public void SecondaryMarkerLabelDistance_IsInsideOuterScaleLabels()
    {
        Assert.That(GaugeVisualRules.SecondaryMarkerLabelDistance,
            Is.LessThan(GaugeVisualRules.OuterMarkerLabelDistance),
            "Inner DCC labels must sit radially inward of outer scale labels.");
    }}
#endif
