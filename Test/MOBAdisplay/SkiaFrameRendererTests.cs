// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
using Moba.Display.Rendering;

namespace Moba.Test.MOBAdisplay;

[TestFixture]
internal sealed class SkiaFrameRendererTests
{
    [Test]
    public void Render_WritesExpectedFrameSize()
    {
        using var renderer = new SkiaFrameRenderer();
        var frame = new byte[FrameDimensions.FrameByteCount];

        renderer.Render(new FrameContext(new DateTime(2026, 4, 24, 12, 0, 0), 7), frame);

        Assert.That(frame.Length, Is.EqualTo(FrameDimensions.FrameByteCount));
        Assert.That(frame.Any(b => b != 0), Is.True);
    }

    [Test]
    public void Render_ChangesWhenSecondChanges()
    {
        using var renderer = new SkiaFrameRenderer();
        var frameA = new byte[FrameDimensions.FrameByteCount];
        var frameB = new byte[FrameDimensions.FrameByteCount];

        renderer.Render(new FrameContext(new DateTime(2026, 4, 24, 12, 0, 1), 7), frameA);
        renderer.Render(new FrameContext(new DateTime(2026, 4, 24, 12, 0, 2), 7), frameB);

        Assert.That(frameA.SequenceEqual(frameB), Is.False);
    }
}