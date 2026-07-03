// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Test.Common;

using Moba.Common.Runtime;

[TestFixture]
internal sealed class LocomotiveFleetSnapshotComparerTests
{
    [Test]
    public void ContentEquals_ReturnsTrue_WhenSnapshotsMatch()
    {
        var locomotiveId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var left = new LocomotiveFleetSnapshot
        {
            LocomotiveId = locomotiveId,
            Name = "BR 110",
            DigitalAddress = 7,
            PhotoPath = "photos/locomotives/test.jpg",
            FunctionSymbols = ["headlight.png"],
            FunctionColors = ["#E81123"],
            FunctionLabels = ["Headlight"]
        };
        var right = new LocomotiveFleetSnapshot
        {
            LocomotiveId = locomotiveId,
            Name = "BR 110",
            DigitalAddress = 7,
            PhotoPath = "photos/locomotives/test.jpg",
            FunctionSymbols = ["headlight.png"],
            FunctionColors = ["#E81123"],
            FunctionLabels = ["Headlight"]
        };

        Assert.That(LocomotiveFleetSnapshotComparer.ContentEquals(left, right), Is.True);
    }

    [Test]
    public void ContentEquals_ReturnsFalse_WhenPhotoPathDiffers()
    {
        var locomotiveId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var left = new LocomotiveFleetSnapshot
        {
            LocomotiveId = locomotiveId,
            Name = "BR 110",
            PhotoPath = "photos/locomotives/a.jpg"
        };
        var right = new LocomotiveFleetSnapshot
        {
            LocomotiveId = locomotiveId,
            Name = "BR 110",
            PhotoPath = "photos/locomotives/b.jpg"
        };

        Assert.That(LocomotiveFleetSnapshotComparer.ContentEquals(left, right), Is.False);
    }

    [Test]
    public void OrderedContentEquals_ReturnsFalse_WhenOrderDiffers()
    {
        var fleetA = new[]
        {
            new LocomotiveFleetSnapshot { LocomotiveId = Guid.NewGuid(), Name = "A" },
            new LocomotiveFleetSnapshot { LocomotiveId = Guid.NewGuid(), Name = "B" }
        };
        var fleetB = new[]
        {
            fleetA[1],
            fleetA[0]
        };

        Assert.That(LocomotiveFleetSnapshotComparer.OrderedContentEquals(fleetA, fleetB), Is.False);
    }
}
