// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Test.Contracts;

using Moba.Contracts.Runtime;

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

    [TestCase("Id")]
    [TestCase("Name")]
    [TestCase("Address")]
    public void IndividualMetadataChangesAreDetected(string field)
    {
        var left = new LocomotiveFleetSnapshot { LocomotiveId = Guid.NewGuid(), Name = "BR 110", DigitalAddress = 7 };
        var right = field switch
        {
            "Id" => left with { LocomotiveId = Guid.NewGuid() },
            "Name" => left with { Name = "BR 111" },
            _ => left with { DigitalAddress = 8 }
        };
        Assert.That(LocomotiveFleetSnapshotComparer.ContentEquals(left, right), Is.False);
    }

    [TestCase("Symbols", "Same")]
    [TestCase("Symbols", "BothNull")]
    [TestCase("Symbols", "BothEmpty")]
    [TestCase("Colors", "Same")]
    [TestCase("Colors", "BothNull")]
    [TestCase("Colors", "BothEmpty")]
    [TestCase("Labels", "Same")]
    [TestCase("Labels", "BothNull")]
    [TestCase("Labels", "BothEmpty")]
    [TestCase("Symbols", "LeftNull")]
    [TestCase("Symbols", "RightNull")]
    [TestCase("Symbols", "Shorter")]
    [TestCase("Symbols", "FirstDiffers")]
    [TestCase("Symbols", "LastDiffers")]
    [TestCase("Colors", "LeftNull")]
    [TestCase("Colors", "RightNull")]
    [TestCase("Colors", "Shorter")]
    [TestCase("Colors", "FirstDiffers")]
    [TestCase("Colors", "LastDiffers")]
    [TestCase("Labels", "LeftNull")]
    [TestCase("Labels", "RightNull")]
    [TestCase("Labels", "Shorter")]
    [TestCase("Labels", "FirstDiffers")]
    [TestCase("Labels", "LastDiffers")]
    public void FunctionCollectionsAreComparedByContent(string field, string scenario)
    {
        IReadOnlyList<string>? leftValues = scenario switch
        {
            "BothNull" or "LeftNull" => null,
            "BothEmpty" => [],
            _ => ["first", "last"]
        };
        IReadOnlyList<string>? rightValues = scenario switch
        {
            "BothNull" or "RightNull" => null,
            "BothEmpty" => [],
            "Shorter" => ["first"],
            "FirstDiffers" => ["changed", "last"],
            "LastDiffers" => ["first", "changed"],
            _ => ["first", "last"]
        };
        var left = WithFunctionCollection(field, leftValues);
        var right = WithFunctionCollection(field, rightValues);
        var expected = scenario is "Same" or "BothNull" or "BothEmpty";

        using (Assert.EnterMultipleScope())
        {
            Assert.That(LocomotiveFleetSnapshotComparer.ContentEquals(left, right), Is.EqualTo(expected));
            Assert.That(LocomotiveFleetSnapshotComparer.ContentEquals(right, left), Is.EqualTo(expected));
        }
    }

    [Test]
    public void OrderedComparisonAcceptsEqualDistinctListsAndEmptyFleets()
    {
        var first = new LocomotiveFleetSnapshot { LocomotiveId = Guid.NewGuid(), Name = "A" };
        var second = new LocomotiveFleetSnapshot { LocomotiveId = Guid.NewGuid(), Name = "B" };
        using (Assert.EnterMultipleScope())
        {
            Assert.That(LocomotiveFleetSnapshotComparer.OrderedContentEquals([first, second], [first with { }, second with { }]), Is.True);
            Assert.That(LocomotiveFleetSnapshotComparer.OrderedContentEquals([], []), Is.True);
        }
    }

    [Test]
    public void OrderedComparisonRejectsMissingLocomotivesAndChangedTail()
    {
        var first = new LocomotiveFleetSnapshot { LocomotiveId = Guid.NewGuid(), Name = "A" };
        var second = new LocomotiveFleetSnapshot { LocomotiveId = Guid.NewGuid(), Name = "B" };
        using (Assert.EnterMultipleScope())
        {
            Assert.That(LocomotiveFleetSnapshotComparer.OrderedContentEquals([first, second], [first]), Is.False);
            Assert.That(LocomotiveFleetSnapshotComparer.OrderedContentEquals([first], [first, second]), Is.False);
            Assert.That(LocomotiveFleetSnapshotComparer.OrderedContentEquals([first, second], [first, second with { Name = "changed" }]), Is.False);
        }
    }

    private static LocomotiveFleetSnapshot WithFunctionCollection(string field, IReadOnlyList<string>? values) => field switch
    {
        "Symbols" => new LocomotiveFleetSnapshot { FunctionSymbols = values },
        "Colors" => new LocomotiveFleetSnapshot { FunctionColors = values },
        _ => new LocomotiveFleetSnapshot { FunctionLabels = values }
    };
}
