// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.MOBAdisplay;

using Moba.Display.Protocol;

[TestFixture]
[Category("Unit")]
internal sealed class DisplayProtocolVersionTests
{
    [TestCase(1, 0, 1, 1, true)]
    [TestCase(1, 2, 1, 1, true)]
    [TestCase(1, 0, 2, 0, false)]
    [TestCase(0, 1, 0, 2, false)]
    public void HasCompatibleMajorVersion_Should_ReturnExpectedResult(
        byte leftMajor,
        byte leftMinor,
        byte rightMajor,
        byte rightMinor,
        bool expected)
    {
        var left = new DisplayProtocolVersion(leftMajor, leftMinor);
        var right = new DisplayProtocolVersion(rightMajor, rightMinor);

        var compatible = left.HasCompatibleMajorVersion(right);

        Assert.That(compatible, Is.EqualTo(expected));
    }

    [Test]
    public void CompareTo_Should_OrderByMajorThenMinor()
    {
        var versions = new[]
        {
            new DisplayProtocolVersion(2, 0),
            new DisplayProtocolVersion(1, 2),
            new DisplayProtocolVersion(1, 0)
        };

        Array.Sort(versions);

        Assert.That(
            versions,
            Is.EqualTo(new[]
            {
                new DisplayProtocolVersion(1, 0),
                new DisplayProtocolVersion(1, 2),
                new DisplayProtocolVersion(2, 0)
            }));
    }
}
