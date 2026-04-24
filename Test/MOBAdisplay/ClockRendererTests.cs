using Moba.Display.Rendering;

using SkiaSharp;

namespace Moba.Test.MOBAdisplay;

[TestFixture]
internal sealed class ClockRendererTests
{
    [Test]
    public void ComputeSecondHandEndpoint_IsTopAtZeroSeconds()
    {
        var renderer = new ClockRenderer();
        var center = new SKPoint(100, 100);
        var end = renderer.ComputeSecondHandEndpoint(center, 50, new DateTime(2026, 4, 24, 12, 0, 0));

        Assert.That(end.Y, Is.LessThan(center.Y));
    }

    [Test]
    public void ComputeSecondHandEndpoint_IsRightAt15Seconds()
    {
        var renderer = new ClockRenderer();
        var center = new SKPoint(100, 100);
        var end = renderer.ComputeSecondHandEndpoint(center, 50, new DateTime(2026, 4, 24, 12, 0, 15));

        Assert.That(end.X, Is.GreaterThan(center.X));
    }

    [Test]
    public void ComputeSecondHandEndpoint_IsBottomAt30Seconds()
    {
        var renderer = new ClockRenderer();
        var center = new SKPoint(100, 100);
        var end = renderer.ComputeSecondHandEndpoint(center, 50, new DateTime(2026, 4, 24, 12, 0, 30));

        Assert.That(end.Y, Is.GreaterThan(center.Y));
    }
}
