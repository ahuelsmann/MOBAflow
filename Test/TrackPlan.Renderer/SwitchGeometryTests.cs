// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Test.TrackPlan.Renderer;

using Moba.TrackPlan.Renderer.Geometry;
using Moba.TrackPlan.Renderer.World;
using Moba.TrackPlan.TrackSystem;

/// <summary>
/// Unit tests for SwitchGeometry rendering.
/// Tests left and right switch variants with various angles.
/// 
/// Switch Structure:
///         C (diverging/branch)
///        /
///   A───┼───B (straight through)
///       │
///    Junction
/// </summary>
[TestFixture]
public class SwitchGeometryTests
{
    private const double Tolerance = 0.001;

    private static readonly SwitchRoutingModel StandardRouting = new()
    {
        InEndId = "A",
        StraightEndId = "B",
        DivergingEndId = "C"
    };

    #region Basic Switch Tests

    [Test]
    public void Render_Switch_ReturnsLineAndArc()
    {
        var start = new Point2D(0, 0);
        var spec = CreateSwitchSpec(length: 230, radius: 888, angle: 15, junctionOffset: 50);

        var primitives = SwitchGeometry.Render(start, 0, spec, StandardRouting, isLeftSwitch: true).ToList();

        Assert.That(primitives, Has.Count.EqualTo(2), "Switch should have line + arc");
        Assert.That(primitives[0], Is.TypeOf<LinePrimitive>(), "First primitive should be line");
        Assert.That(primitives[1], Is.TypeOf<ArcPrimitive>(), "Second primitive should be arc");
    }

    [Test]
    public void Render_SwitchAt0Degrees_StraightLineGoesRight()
    {
        var start = new Point2D(0, 0);
        var spec = CreateSwitchSpec(length: 230, radius: 888, angle: 15, junctionOffset: 50);

        var primitives = SwitchGeometry.Render(start, 0, spec, StandardRouting, isLeftSwitch: true).ToList();

        var line = primitives[0] as LinePrimitive;
        Assert.That(line!.From.X, Is.EqualTo(0).Within(Tolerance));
        Assert.That(line.From.Y, Is.EqualTo(0).Within(Tolerance));
        Assert.That(line.To.X, Is.EqualTo(230).Within(Tolerance), "Line end X should be start + length");
        Assert.That(line.To.Y, Is.EqualTo(0).Within(Tolerance), "Line end Y should be same as start");
    }

    #endregion

    #region Left Switch Tests

    [Test]
    public void Render_LeftSwitchAt0Degrees_ArcCenterIsAboveJunction()
    {
        var start = new Point2D(0, 0);
        var spec = CreateSwitchSpec(length: 230, radius: 888, angle: 15, junctionOffset: 50);

        var primitives = SwitchGeometry.Render(start, 0, spec, StandardRouting, isLeftSwitch: true).ToList();

        var arc = primitives[1] as ArcPrimitive;
        
        // Junction is at (50, 0) when start angle is 0°
        // For left switch, center should be above junction (positive Y)
        Assert.That(arc!.Center.X, Is.EqualTo(50).Within(Tolerance), "Arc center X should be at junction X");
        Assert.That(arc.Center.Y, Is.EqualTo(888).Within(Tolerance), "Arc center Y should be junction Y + radius");
    }

    [Test]
    public void Render_LeftSwitchAt0Degrees_PositiveSweep()
    {
        var start = new Point2D(0, 0);
        var spec = CreateSwitchSpec(length: 230, radius: 888, angle: 15, junctionOffset: 50);

        var primitives = SwitchGeometry.Render(start, 0, spec, StandardRouting, isLeftSwitch: true).ToList();

        var arc = primitives[1] as ArcPrimitive;
        Assert.That(arc!.SweepAngleRad, Is.GreaterThan(0), "Left switch should have positive sweep (CCW)");
    }

    [Test]
    public void Render_LeftSwitchAt90Degrees_ArcCenterIsLeftOfJunction()
    {
        var start = new Point2D(0, 0);
        var spec = CreateSwitchSpec(length: 230, radius: 888, angle: 15, junctionOffset: 50);

        var primitives = SwitchGeometry.Render(start, 90, spec, StandardRouting, isLeftSwitch: true).ToList();

        var arc = primitives[1] as ArcPrimitive;
        
        // Junction is at (0, 50) when start angle is 90°
        // For left switch at 90°, center should be to the left (negative X)
        Assert.That(arc!.Center.X, Is.EqualTo(-888).Within(Tolerance));
        Assert.That(arc.Center.Y, Is.EqualTo(50).Within(Tolerance));
    }

    #endregion

    #region Right Switch Tests

    [Test]
    public void Render_RightSwitchAt0Degrees_ArcCenterIsBelowJunction()
    {
        var start = new Point2D(0, 0);
        var spec = CreateSwitchSpec(length: 230, radius: 888, angle: 15, junctionOffset: 50);

        var primitives = SwitchGeometry.Render(start, 0, spec, StandardRouting, isLeftSwitch: false).ToList();

        var arc = primitives[1] as ArcPrimitive;
        
        // For right switch at 0°, center should be below junction (negative Y)
        Assert.That(arc!.Center.X, Is.EqualTo(50).Within(Tolerance));
        Assert.That(arc.Center.Y, Is.EqualTo(-888).Within(Tolerance), "Arc center Y should be junction Y - radius");
    }

    [Test]
    public void Render_RightSwitchAt0Degrees_NegativeSweep()
    {
        var start = new Point2D(0, 0);
        var spec = CreateSwitchSpec(length: 230, radius: 888, angle: 15, junctionOffset: 50);

        var primitives = SwitchGeometry.Render(start, 0, spec, StandardRouting, isLeftSwitch: false).ToList();

        var arc = primitives[1] as ArcPrimitive;
        Assert.That(arc!.SweepAngleRad, Is.LessThan(0), "Right switch should have negative sweep (CW)");
    }

    [Test]
    public void Render_RightSwitchAt90Degrees_ArcCenterIsRightOfJunction()
    {
        var start = new Point2D(0, 0);
        var spec = CreateSwitchSpec(length: 230, radius: 888, angle: 15, junctionOffset: 50);

        var primitives = SwitchGeometry.Render(start, 90, spec, StandardRouting, isLeftSwitch: false).ToList();

        var arc = primitives[1] as ArcPrimitive;
        
        // For right switch at 90°, center should be to the right (positive X)
        Assert.That(arc!.Center.X, Is.EqualTo(888).Within(Tolerance));
        Assert.That(arc.Center.Y, Is.EqualTo(50).Within(Tolerance));
    }

    #endregion

    #region Junction Position Tests

    [TestCase(0, 50, 0)]      // 0° heading
    [TestCase(90, 0, 50)]     // 90° heading
    [TestCase(180, -50, 0)]   // 180° heading
    [TestCase(270, 0, -50)]   // 270° heading
    public void Render_JunctionPosition_CorrectAtDifferentAngles(double angle, double expectedJunctionX, double expectedJunctionY)
    {
        var start = new Point2D(0, 0);
        var spec = CreateSwitchSpec(length: 230, radius: 888, angle: 15, junctionOffset: 50);

        var primitives = SwitchGeometry.Render(start, angle, spec, StandardRouting, isLeftSwitch: true).ToList();

        var arc = primitives[1] as ArcPrimitive;
        
        // Arc starts at junction - verify by calculating arc start point
        var arcStartX = arc!.Center.X + arc.Radius * Math.Cos(arc.StartAngleRad);
        var arcStartY = arc.Center.Y + arc.Radius * Math.Sin(arc.StartAngleRad);

        Assert.That(arcStartX, Is.EqualTo(expectedJunctionX).Within(Tolerance), $"Junction X at {angle}°");
        Assert.That(arcStartY, Is.EqualTo(expectedJunctionY).Within(Tolerance), $"Junction Y at {angle}°");
    }

    #endregion

    #region Sweep Angle Tests

    [Test]
    public void Render_15DegreeSwitch_Has15DegreeSweep()
    {
        var start = new Point2D(0, 0);
        var spec = CreateSwitchSpec(length: 230, radius: 888, angle: 15, junctionOffset: 50);

        var primitives = SwitchGeometry.Render(start, 0, spec, StandardRouting, isLeftSwitch: true).ToList();

        var arc = primitives[1] as ArcPrimitive;
        Assert.That(Math.Abs(arc!.SweepAngleRad), Is.EqualTo(DegToRad(15)).Within(Tolerance));
    }

    [Test]
    public void Render_30DegreeSwitch_Has30DegreeSweep()
    {
        var start = new Point2D(0, 0);
        var spec = CreateSwitchSpec(length: 230, radius: 500, angle: 30, junctionOffset: 50);

        var primitives = SwitchGeometry.Render(start, 0, spec, StandardRouting, isLeftSwitch: true).ToList();

        var arc = primitives[1] as ArcPrimitive;
        Assert.That(Math.Abs(arc!.SweepAngleRad), Is.EqualTo(DegToRad(30)).Within(Tolerance));
    }

    #endregion

    #region Symmetry Tests

    [Test]
    public void Render_LeftAndRightSwitch_AreMirrored()
    {
        var start = new Point2D(0, 0);
        var spec = CreateSwitchSpec(length: 230, radius: 888, angle: 15, junctionOffset: 50);

        var leftPrimitives = SwitchGeometry.Render(start, 0, spec, StandardRouting, isLeftSwitch: true).ToList();
        var rightPrimitives = SwitchGeometry.Render(start, 0, spec, StandardRouting, isLeftSwitch: false).ToList();

        var leftArc = leftPrimitives[1] as ArcPrimitive;
        var rightArc = rightPrimitives[1] as ArcPrimitive;

        // Centers should be mirrored across X-axis (same X, opposite Y)
        Assert.That(leftArc!.Center.X, Is.EqualTo(rightArc!.Center.X).Within(Tolerance));
        Assert.That(leftArc.Center.Y, Is.EqualTo(-rightArc.Center.Y).Within(Tolerance));

        // Sweep angles should be opposite
        Assert.That(leftArc.SweepAngleRad, Is.EqualTo(-rightArc.SweepAngleRad).Within(Tolerance));
    }

    #endregion

    #region Helper Methods

    private static TrackGeometrySpec CreateSwitchSpec(double length, double radius, double angle, double junctionOffset)
    {
        return new TrackGeometrySpec(
            GeometryKind: TrackGeometryKind.Switch,
            LengthMm: length,
            RadiusMm: radius,
            AngleDeg: angle,
            JunctionOffsetMm: junctionOffset);
    }

    private static double DegToRad(double deg) => deg * Math.PI / 180.0;

    #endregion
}
