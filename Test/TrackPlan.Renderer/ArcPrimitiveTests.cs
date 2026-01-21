// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Test.TrackPlan.Renderer;

using Moba.TrackPlan.Renderer.Geometry;
using Moba.TrackPlan.Renderer.World;

/// <summary>
/// Unit tests for ArcPrimitive record and arc calculations.
/// Tests start/end point computation, sweep direction, and edge cases.
/// </summary>
[TestFixture]
public class ArcPrimitiveTests
{
    private const double Tolerance = 0.001;

    #region Record Equality Tests

    [Test]
    public void ArcPrimitive_WithSameValues_AreEqual()
    {
        var arc1 = new ArcPrimitive(new Point2D(100, 200), 500, Math.PI / 4, Math.PI / 6);
        var arc2 = new ArcPrimitive(new Point2D(100, 200), 500, Math.PI / 4, Math.PI / 6);

        Assert.That(arc1, Is.EqualTo(arc2));
    }

    [Test]
    public void ArcPrimitive_WithDifferentValues_AreNotEqual()
    {
        var arc1 = new ArcPrimitive(new Point2D(100, 200), 500, Math.PI / 4, Math.PI / 6);
        var arc2 = new ArcPrimitive(new Point2D(100, 200), 600, Math.PI / 4, Math.PI / 6);

        Assert.That(arc1, Is.Not.EqualTo(arc2));
    }

    #endregion

    #region Start Point Calculation Tests

    [Test]
    public void GetStartPoint_At0Degrees_PointsRight()
    {
        var arc = new ArcPrimitive(
            Center: new Point2D(0, 0),
            Radius: 100,
            StartAngleRad: 0,  // 0° = right
            SweepAngleRad: Math.PI / 4);

        var startPoint = GetArcStartPoint(arc);

        Assert.That(startPoint.X, Is.EqualTo(100).Within(Tolerance), "Start X should be center + radius");
        Assert.That(startPoint.Y, Is.EqualTo(0).Within(Tolerance), "Start Y should be same as center");
    }

    [Test]
    public void GetStartPoint_At90Degrees_PointsUp()
    {
        var arc = new ArcPrimitive(
            Center: new Point2D(0, 0),
            Radius: 100,
            StartAngleRad: Math.PI / 2,  // 90° = up
            SweepAngleRad: Math.PI / 4);

        var startPoint = GetArcStartPoint(arc);

        Assert.That(startPoint.X, Is.EqualTo(0).Within(Tolerance));
        Assert.That(startPoint.Y, Is.EqualTo(100).Within(Tolerance), "Start Y should be center + radius");
    }

    [Test]
    public void GetStartPoint_At180Degrees_PointsLeft()
    {
        var arc = new ArcPrimitive(
            Center: new Point2D(0, 0),
            Radius: 100,
            StartAngleRad: Math.PI,  // 180° = left
            SweepAngleRad: Math.PI / 4);

        var startPoint = GetArcStartPoint(arc);

        Assert.That(startPoint.X, Is.EqualTo(-100).Within(Tolerance), "Start X should be center - radius");
        Assert.That(startPoint.Y, Is.EqualTo(0).Within(Tolerance));
    }

    [Test]
    public void GetStartPoint_AtNegative90Degrees_PointsDown()
    {
        var arc = new ArcPrimitive(
            Center: new Point2D(0, 0),
            Radius: 100,
            StartAngleRad: -Math.PI / 2,  // -90° = down
            SweepAngleRad: Math.PI / 4);

        var startPoint = GetArcStartPoint(arc);

        Assert.That(startPoint.X, Is.EqualTo(0).Within(Tolerance));
        Assert.That(startPoint.Y, Is.EqualTo(-100).Within(Tolerance), "Start Y should be center - radius");
    }

    #endregion

    #region End Point Calculation Tests

    [Test]
    public void GetEndPoint_PositiveSweep_CounterClockwise()
    {
        // Start at 0°, sweep 90° CCW → end at 90° (top)
        var arc = new ArcPrimitive(
            Center: new Point2D(0, 0),
            Radius: 100,
            StartAngleRad: 0,
            SweepAngleRad: Math.PI / 2);  // +90° sweep

        var endPoint = GetArcEndPoint(arc);

        Assert.That(endPoint.X, Is.EqualTo(0).Within(Tolerance));
        Assert.That(endPoint.Y, Is.EqualTo(100).Within(Tolerance), "End should be at top (90°)");
    }

    [Test]
    public void GetEndPoint_NegativeSweep_Clockwise()
    {
        // Start at 0°, sweep -90° CW → end at -90° (bottom)
        var arc = new ArcPrimitive(
            Center: new Point2D(0, 0),
            Radius: 100,
            StartAngleRad: 0,
            SweepAngleRad: -Math.PI / 2);  // -90° sweep

        var endPoint = GetArcEndPoint(arc);

        Assert.That(endPoint.X, Is.EqualTo(0).Within(Tolerance));
        Assert.That(endPoint.Y, Is.EqualTo(-100).Within(Tolerance), "End should be at bottom (-90°)");
    }

    [Test]
    public void GetEndPoint_30DegreeSweep_CorrectPosition()
    {
        // Start at 0°, sweep 30° CCW
        var arc = new ArcPrimitive(
            Center: new Point2D(0, 0),
            Radius: 100,
            StartAngleRad: 0,
            SweepAngleRad: Math.PI / 6);  // 30°

        var endPoint = GetArcEndPoint(arc);

        Assert.That(endPoint.X, Is.EqualTo(100 * Math.Cos(Math.PI / 6)).Within(Tolerance));  // cos(30°) ≈ 0.866
        Assert.That(endPoint.Y, Is.EqualTo(100 * Math.Sin(Math.PI / 6)).Within(Tolerance));  // sin(30°) = 0.5
    }

    #endregion

    #region Arc Length Tests

    [TestCase(Math.PI / 6, 100)]   // 30° arc, R=100 → length = 100 * π/6 ≈ 52.36
    [TestCase(Math.PI / 2, 100)]   // 90° arc, R=100 → length = 100 * π/2 ≈ 157.08
    [TestCase(Math.PI, 100)]       // 180° arc, R=100 → length = 100 * π ≈ 314.16
    public void GetArcLength_CorrectLength(double sweepRad, double radius)
    {
        var arc = new ArcPrimitive(
            Center: new Point2D(0, 0),
            Radius: radius,
            StartAngleRad: 0,
            SweepAngleRad: sweepRad);

        var length = GetArcLength(arc);
        var expected = radius * Math.Abs(sweepRad);

        Assert.That(length, Is.EqualTo(expected).Within(Tolerance));
    }

    #endregion

    #region Large Arc Flag Tests

    [Test]
    public void IsLargeArc_SweepLessThan180_False()
    {
        var arc = new ArcPrimitive(
            Center: new Point2D(0, 0),
            Radius: 100,
            StartAngleRad: 0,
            SweepAngleRad: Math.PI / 2);  // 90°

        Assert.That(IsLargeArc(arc), Is.False);
    }

    [Test]
    public void IsLargeArc_SweepGreaterThan180_True()
    {
        var arc = new ArcPrimitive(
            Center: new Point2D(0, 0),
            Radius: 100,
            StartAngleRad: 0,
            SweepAngleRad: Math.PI * 1.5);  // 270°

        Assert.That(IsLargeArc(arc), Is.True);
    }

    [Test]
    public void IsLargeArc_NegativeSweepLessThan180_False()
    {
        var arc = new ArcPrimitive(
            Center: new Point2D(0, 0),
            Radius: 100,
            StartAngleRad: 0,
            SweepAngleRad: -Math.PI / 2);  // -90°

        Assert.That(IsLargeArc(arc), Is.False);
    }

    #endregion

    #region Helper Methods

    private static Point2D GetArcStartPoint(ArcPrimitive arc)
    {
        return new Point2D(
            arc.Center.X + arc.Radius * Math.Cos(arc.StartAngleRad),
            arc.Center.Y + arc.Radius * Math.Sin(arc.StartAngleRad));
    }

    private static Point2D GetArcEndPoint(ArcPrimitive arc)
    {
        return new Point2D(
            arc.Center.X + arc.Radius * Math.Cos(arc.StartAngleRad + arc.SweepAngleRad),
            arc.Center.Y + arc.Radius * Math.Sin(arc.StartAngleRad + arc.SweepAngleRad));
    }

    private static double GetArcLength(ArcPrimitive arc)
    {
        return arc.Radius * Math.Abs(arc.SweepAngleRad);
    }

    private static bool IsLargeArc(ArcPrimitive arc)
    {
        return Math.Abs(arc.SweepAngleRad) > Math.PI;
    }

    #endregion
}
