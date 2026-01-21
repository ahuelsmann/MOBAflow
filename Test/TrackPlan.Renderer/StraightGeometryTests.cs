// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Test.TrackPlan.Renderer;

using Moba.TrackPlan.Renderer.Geometry;
using Moba.TrackPlan.Renderer.World;

/// <summary>
/// Unit tests for StraightGeometry rendering.
/// Tests straight line calculation at various angles and lengths.
/// </summary>
[TestFixture]
public class StraightGeometryTests
{
    private const double Tolerance = 0.001;

    #region Basic Tests

    [Test]
    public void Render_ReturnsOnePrimitive()
    {
        var start = new Point2D(0, 0);

        var primitives = StraightGeometry.Render(start, startAngleDeg: 0, lengthMm: 100).ToList();

        Assert.That(primitives, Has.Count.EqualTo(1));
        Assert.That(primitives[0], Is.TypeOf<LinePrimitive>());
    }

    [Test]
    public void Render_At0Degrees_GoesRight()
    {
        var start = new Point2D(0, 0);

        var primitives = StraightGeometry.Render(start, startAngleDeg: 0, lengthMm: 100).ToList();

        var line = primitives[0] as LinePrimitive;
        Assert.That(line!.From.X, Is.EqualTo(0).Within(Tolerance));
        Assert.That(line.From.Y, Is.EqualTo(0).Within(Tolerance));
        Assert.That(line.To.X, Is.EqualTo(100).Within(Tolerance), "End X should be start + length");
        Assert.That(line.To.Y, Is.EqualTo(0).Within(Tolerance), "End Y should be same as start");
    }

    [Test]
    public void Render_At90Degrees_GoesUp()
    {
        var start = new Point2D(0, 0);

        var primitives = StraightGeometry.Render(start, startAngleDeg: 90, lengthMm: 100).ToList();

        var line = primitives[0] as LinePrimitive;
        Assert.That(line!.To.X, Is.EqualTo(0).Within(Tolerance), "End X should be same as start");
        Assert.That(line.To.Y, Is.EqualTo(100).Within(Tolerance), "End Y should be start + length");
    }

    [Test]
    public void Render_At180Degrees_GoesLeft()
    {
        var start = new Point2D(100, 50);

        var primitives = StraightGeometry.Render(start, startAngleDeg: 180, lengthMm: 100).ToList();

        var line = primitives[0] as LinePrimitive;
        Assert.That(line!.To.X, Is.EqualTo(0).Within(Tolerance), "End X should be start - length");
        Assert.That(line.To.Y, Is.EqualTo(50).Within(Tolerance), "End Y should be same as start");
    }

    [Test]
    public void Render_At270Degrees_GoesDown()
    {
        var start = new Point2D(50, 100);

        var primitives = StraightGeometry.Render(start, startAngleDeg: 270, lengthMm: 100).ToList();

        var line = primitives[0] as LinePrimitive;
        Assert.That(line!.To.X, Is.EqualTo(50).Within(Tolerance), "End X should be same as start");
        Assert.That(line.To.Y, Is.EqualTo(0).Within(Tolerance), "End Y should be start - length");
    }

    #endregion

    #region Diagonal Angles

    [Test]
    public void Render_At45Degrees_GoesDiagonalUpRight()
    {
        var start = new Point2D(0, 0);

        var primitives = StraightGeometry.Render(start, startAngleDeg: 45, lengthMm: 100).ToList();

        var line = primitives[0] as LinePrimitive;
        var expected = 100 * Math.Cos(Math.PI / 4); // ≈ 70.71

        Assert.That(line!.To.X, Is.EqualTo(expected).Within(Tolerance));
        Assert.That(line.To.Y, Is.EqualTo(expected).Within(Tolerance));
    }

    [Test]
    public void Render_At135Degrees_GoesDiagonalUpLeft()
    {
        var start = new Point2D(100, 0);

        var primitives = StraightGeometry.Render(start, startAngleDeg: 135, lengthMm: 100).ToList();

        var line = primitives[0] as LinePrimitive;
        var offset = 100 * Math.Cos(Math.PI / 4); // ≈ 70.71

        Assert.That(line!.To.X, Is.EqualTo(100 - offset).Within(Tolerance));
        Assert.That(line.To.Y, Is.EqualTo(offset).Within(Tolerance));
    }

    #endregion

    #region Different Lengths

    [TestCase(50)]
    [TestCase(100)]
    [TestCase(230)]
    [TestCase(500)]
    public void Render_DifferentLengths_CorrectDistance(double length)
    {
        var start = new Point2D(0, 0);

        var primitives = StraightGeometry.Render(start, startAngleDeg: 30, lengthMm: length).ToList();

        var line = primitives[0] as LinePrimitive;
        var actualLength = Math.Sqrt(
            Math.Pow(line!.To.X - line.From.X, 2) +
            Math.Pow(line.To.Y - line.From.Y, 2));

        Assert.That(actualLength, Is.EqualTo(length).Within(Tolerance));
    }

    #endregion

    #region Different Start Positions

    [Test]
    public void Render_NonZeroStart_CorrectEndpoint()
    {
        var start = new Point2D(500, 300);

        var primitives = StraightGeometry.Render(start, startAngleDeg: 0, lengthMm: 100).ToList();

        var line = primitives[0] as LinePrimitive;
        Assert.That(line!.From.X, Is.EqualTo(500).Within(Tolerance));
        Assert.That(line.From.Y, Is.EqualTo(300).Within(Tolerance));
        Assert.That(line.To.X, Is.EqualTo(600).Within(Tolerance));
        Assert.That(line.To.Y, Is.EqualTo(300).Within(Tolerance));
    }

    #endregion

    #region Edge Cases

    [Test]
    public void Render_NegativeAngle_HandledCorrectly()
    {
        var start = new Point2D(0, 0);

        var primitives = StraightGeometry.Render(start, startAngleDeg: -45, lengthMm: 100).ToList();

        var line = primitives[0] as LinePrimitive;
        var expected = 100 * Math.Cos(Math.PI / 4); // ≈ 70.71

        Assert.That(line!.To.X, Is.EqualTo(expected).Within(Tolerance));
        Assert.That(line.To.Y, Is.EqualTo(-expected).Within(Tolerance), "-45° should go down-right");
    }

    [Test]
    public void Render_AngleGreaterThan360_HandledCorrectly()
    {
        var start = new Point2D(0, 0);

        // 450° = 90° (one full rotation + 90°)
        var primitives = StraightGeometry.Render(start, startAngleDeg: 450, lengthMm: 100).ToList();

        var line = primitives[0] as LinePrimitive;
        Assert.That(line!.To.X, Is.EqualTo(0).Within(Tolerance));
        Assert.That(line.To.Y, Is.EqualTo(100).Within(Tolerance), "450° should behave like 90°");
    }

    [Test]
    public void Render_ZeroLength_StartEqualsEnd()
    {
        var start = new Point2D(100, 200);

        var primitives = StraightGeometry.Render(start, startAngleDeg: 45, lengthMm: 0).ToList();

        var line = primitives[0] as LinePrimitive;
        Assert.That(line!.To.X, Is.EqualTo(line.From.X).Within(Tolerance));
        Assert.That(line.To.Y, Is.EqualTo(line.From.Y).Within(Tolerance));
    }

    #endregion
}
