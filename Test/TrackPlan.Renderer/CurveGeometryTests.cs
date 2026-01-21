// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Test.TrackPlan.Renderer;

using Moba.TrackPlan.Renderer.Geometry;
using Moba.TrackPlan.Renderer.World;
using Moba.TrackPlan.TrackSystem;

/// <summary>
/// Unit tests for CurveGeometry rendering.
/// Tests curve arc calculation with various angles and positions.
/// 
/// Coordinate System:
/// - X increases to the right →
/// - Y increases upward ↑
/// - Angles: 0° = right, 90° = up, counter-clockwise positive
/// </summary>
[TestFixture]
public class CurveGeometryTests
{
    private const double Tolerance = 0.001;

    #region Basic Curve Tests

    [Test]
    public void Render_CurveAt0Degrees_CenterIsAboveStart()
    {
        // Arrange: Curve starting at origin, heading right (0°)
        var start = new Point2D(0, 0);
        var spec = CreateCurveSpec(radius: 500, angleDeg: 30);

        // Act
        var primitives = CurveGeometry.Render(start, startAngleDeg: 0, spec).ToList();

        // Assert: Center should be directly above start (positive Y direction)
        Assert.That(primitives, Has.Count.EqualTo(1));
        var arc = primitives[0] as ArcPrimitive;
        Assert.That(arc, Is.Not.Null);
        Assert.That(arc!.Center.X, Is.EqualTo(0).Within(Tolerance), "Center X should be 0");
        Assert.That(arc.Center.Y, Is.EqualTo(500).Within(Tolerance), "Center Y should be radius (500)");
        Assert.That(arc.Radius, Is.EqualTo(500).Within(Tolerance));
    }

    [Test]
    public void Render_CurveAt90Degrees_CenterIsLeftOfStart()
    {
        // Arrange: Curve starting at origin, heading up (90°)
        var start = new Point2D(0, 0);
        var spec = CreateCurveSpec(radius: 500, angleDeg: 30);

        // Act
        var primitives = CurveGeometry.Render(start, startAngleDeg: 90, spec).ToList();

        // Assert: Center should be to the left of start (negative X direction)
        var arc = primitives[0] as ArcPrimitive;
        Assert.That(arc, Is.Not.Null);
        Assert.That(arc!.Center.X, Is.EqualTo(-500).Within(Tolerance), "Center X should be -radius");
        Assert.That(arc.Center.Y, Is.EqualTo(0).Within(Tolerance), "Center Y should be 0");
    }

    [Test]
    public void Render_CurveAt180Degrees_CenterIsBelowStart()
    {
        // Arrange: Curve starting at origin, heading left (180°)
        var start = new Point2D(100, 100);
        var spec = CreateCurveSpec(radius: 500, angleDeg: 30);

        // Act
        var primitives = CurveGeometry.Render(start, startAngleDeg: 180, spec).ToList();

        // Assert: Center should be below start (negative Y direction)
        var arc = primitives[0] as ArcPrimitive;
        Assert.That(arc, Is.Not.Null);
        Assert.That(arc!.Center.X, Is.EqualTo(100).Within(Tolerance));
        Assert.That(arc.Center.Y, Is.EqualTo(100 - 500).Within(Tolerance), "Center Y should be start.Y - radius");
    }

    [Test]
    public void Render_CurveAt270Degrees_CenterIsRightOfStart()
    {
        // Arrange: Curve starting at origin, heading down (270°)
        var start = new Point2D(0, 0);
        var spec = CreateCurveSpec(radius: 500, angleDeg: 30);

        // Act
        var primitives = CurveGeometry.Render(start, startAngleDeg: 270, spec).ToList();

        // Assert: Center should be to the right of start (positive X direction)
        var arc = primitives[0] as ArcPrimitive;
        Assert.That(arc, Is.Not.Null);
        Assert.That(arc!.Center.X, Is.EqualTo(500).Within(Tolerance), "Center X should be +radius");
        Assert.That(arc.Center.Y, Is.EqualTo(0).Within(Tolerance));
    }

    #endregion

    #region Sweep Angle Tests

    [Test]
    public void Render_30DegreeCurve_HasCorrectSweepAngle()
    {
        var start = new Point2D(0, 0);
        var spec = CreateCurveSpec(radius: 500, angleDeg: 30);

        var primitives = CurveGeometry.Render(start, startAngleDeg: 0, spec).ToList();

        var arc = primitives[0] as ArcPrimitive;
        Assert.That(arc!.SweepAngleRad, Is.EqualTo(DegToRad(30)).Within(Tolerance));
    }

    [Test]
    public void Render_90DegreeCurve_HasCorrectSweepAngle()
    {
        var start = new Point2D(0, 0);
        var spec = CreateCurveSpec(radius: 500, angleDeg: 90);

        var primitives = CurveGeometry.Render(start, startAngleDeg: 0, spec).ToList();

        var arc = primitives[0] as ArcPrimitive;
        Assert.That(arc!.SweepAngleRad, Is.EqualTo(DegToRad(90)).Within(Tolerance));
    }

    #endregion

    #region Arc Start Angle Tests

    [Test]
    public void Render_CurveAt0Degrees_ArcStartAnglePointsToStart()
    {
        // When curve starts at 0° (heading right), the arc should start pointing down
        // (from center to start = -90° from tangent)
        var start = new Point2D(0, 0);
        var spec = CreateCurveSpec(radius: 500, angleDeg: 30);

        var primitives = CurveGeometry.Render(start, startAngleDeg: 0, spec).ToList();

        var arc = primitives[0] as ArcPrimitive;
        // Arc start angle should be -90° (or 270°) = -π/2
        Assert.That(arc!.StartAngleRad, Is.EqualTo(-Math.PI / 2).Within(Tolerance));
    }

    [Test]
    public void Render_VerifyStartPointOnArc()
    {
        // The arc's start point should match the input start point
        var start = new Point2D(100, 200);
        var spec = CreateCurveSpec(radius: 500, angleDeg: 45);

        var primitives = CurveGeometry.Render(start, startAngleDeg: 45, spec).ToList();

        var arc = primitives[0] as ArcPrimitive;
        
        // Calculate where the arc actually starts
        var arcStartX = arc!.Center.X + arc.Radius * Math.Cos(arc.StartAngleRad);
        var arcStartY = arc.Center.Y + arc.Radius * Math.Sin(arc.StartAngleRad);

        Assert.That(arcStartX, Is.EqualTo(start.X).Within(Tolerance), "Arc start X should match input start X");
        Assert.That(arcStartY, Is.EqualTo(start.Y).Within(Tolerance), "Arc start Y should match input start Y");
    }

    #endregion

    #region Different Radii Tests

    [TestCase(100)]
    [TestCase(250)]
    [TestCase(500)]
    [TestCase(1000)]
    public void Render_DifferentRadii_CenterAtCorrectDistance(double radius)
    {
        var start = new Point2D(0, 0);
        var spec = CreateCurveSpec(radius: radius, angleDeg: 30);

        var primitives = CurveGeometry.Render(start, startAngleDeg: 0, spec).ToList();

        var arc = primitives[0] as ArcPrimitive;
        var distanceToCenter = Math.Sqrt(
            Math.Pow(arc!.Center.X - start.X, 2) + 
            Math.Pow(arc.Center.Y - start.Y, 2));

        Assert.That(distanceToCenter, Is.EqualTo(radius).Within(Tolerance));
    }

    #endregion

    #region Edge Cases

    [Test]
    public void Render_DiagonalStartAngle_CorrectGeometry()
    {
        // 45° diagonal heading
        var start = new Point2D(0, 0);
        var spec = CreateCurveSpec(radius: 500, angleDeg: 30);

        var primitives = CurveGeometry.Render(start, startAngleDeg: 45, spec).ToList();

        var arc = primitives[0] as ArcPrimitive;
        
        // At 45°, normal is at 135° (45° + 90°)
        // Center should be at (-500/√2, +500/√2) ≈ (-353.55, 353.55)
        var expectedCenterX = -500 * Math.Sin(DegToRad(45));
        var expectedCenterY = 500 * Math.Cos(DegToRad(45));

        Assert.That(arc!.Center.X, Is.EqualTo(expectedCenterX).Within(Tolerance));
        Assert.That(arc.Center.Y, Is.EqualTo(expectedCenterY).Within(Tolerance));
    }

    [Test]
    public void Render_NegativeStartAngle_HandledCorrectly()
    {
        var start = new Point2D(0, 0);
        var spec = CreateCurveSpec(radius: 500, angleDeg: 30);

        var primitives = CurveGeometry.Render(start, startAngleDeg: -45, spec).ToList();

        var arc = primitives[0] as ArcPrimitive;
        Assert.That(arc, Is.Not.Null);
        // -45° is same as 315°
        // Normal at 315° + 90° = 45° (or equivalently at -45° + 90° = 45°)
    }

    #endregion

    #region Helper Methods

    private static TrackGeometrySpec CreateCurveSpec(double radius, double angleDeg)
    {
        return new TrackGeometrySpec(
            GeometryKind: TrackGeometryKind.Curve,
            RadiusMm: radius,
            AngleDeg: angleDeg);
    }

    private static double DegToRad(double deg) => deg * Math.PI / 180.0;

    #endregion
}
