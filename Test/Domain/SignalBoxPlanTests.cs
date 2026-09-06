// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.Domain;

/// <summary>
/// Tests for signal-box presentation aggregate invariants.
/// Operational routes are covered by the shared interlocking definition tests.
/// </summary>
[TestFixture]
internal class SignalBoxPlanTests
{
    private SignalBoxPlan _plan = null!;

    [SetUp]
    public void SetUp()
    {
        _plan = new SignalBoxPlan();
    }

    [Test]
    public void Constructor_InitializesDefaults()
    {
        Assert.Multiple(() =>
        {
            Assert.That(_plan.Id, Is.Not.EqualTo(Guid.Empty));
            Assert.That(_plan.Name, Is.EqualTo("Signal box"));
            Assert.That(_plan.Grid, Is.Not.Null);
            Assert.That(_plan.Elements, Is.Empty);
            Assert.That(_plan.Connections, Is.Empty);
        });
    }

    [Test]
    public void AddElement_WhenCellIsFree_ThenElementIsAdded()
    {
        var track = new SbTrackStraight { X = 0, Y = 0 };

        _plan.AddElement(track);

        Assert.That(_plan.Elements.Single(), Is.SameAs(track));
    }

    [Test]
    public void AddElement_WhenCellIsOccupied_ThenThrowsInvalidOperationException()
    {
        _plan.AddElement(new SbTrackStraight { X = 3, Y = 5 });

        Assert.That(
            () => _plan.AddElement(new SbSwitch { X = 3, Y = 5 }),
            Throws.TypeOf<InvalidOperationException>().With.Message.Contains("[3,5]"));
    }

    [Test]
    public void AddElement_WhenNull_ThenThrowsArgumentNullException()
    {
        Assert.That(() => _plan.AddElement(null!), Throws.TypeOf<ArgumentNullException>());
    }

    [Test]
    public void AddElement_WhenDifferentCells_ThenBothAreAdded()
    {
        _plan.AddElement(new SbTrackStraight { X = 0, Y = 0 });
        _plan.AddElement(new SbTrackStraight { X = 1, Y = 0 });

        Assert.That(_plan.Elements, Has.Count.EqualTo(2));
    }

    [Test]
    public void RemoveElement_WhenElementExists_ThenReturnsTrue()
    {
        var track = new SbTrackStraight { X = 0, Y = 0 };
        _plan.AddElement(track);

        var result = _plan.RemoveElement(track.Id);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.True);
            Assert.That(_plan.Elements, Is.Empty);
        });
    }

    [Test]
    public void RemoveElement_WhenElementDoesNotExist_ThenReturnsFalse()
    {
        var result = _plan.RemoveElement(Guid.NewGuid());

        Assert.That(result, Is.False);
    }

    [Test]
    public void RemoveElement_WhenElementHasConnections_ThenConnectionsAreCascadeDeleted()
    {
        var trackA = new SbTrackStraight { X = 0, Y = 0 };
        var trackB = new SbTrackStraight { X = 1, Y = 0 };
        var trackC = new SbTrackStraight { X = 2, Y = 0 };
        _plan.Elements.AddRange([trackA, trackB, trackC]);
        _plan.AddConnection(new SignalBoxConnection { FromElementId = trackA.Id, ToElementId = trackB.Id });
        _plan.AddConnection(new SignalBoxConnection { FromElementId = trackB.Id, ToElementId = trackC.Id });

        _plan.RemoveElement(trackB.Id);

        Assert.Multiple(() =>
        {
            Assert.That(_plan.Connections, Is.Empty);
            Assert.That(_plan.Elements, Has.Count.EqualTo(2));
        });
    }

    [Test]
    public void AddConnection_WhenBothElementsExist_ThenConnectionIsAdded()
    {
        var trackA = new SbTrackStraight { X = 0, Y = 0 };
        var trackB = new SbTrackStraight { X = 1, Y = 0 };
        _plan.Elements.AddRange([trackA, trackB]);

        _plan.AddConnection(new SignalBoxConnection
        {
            FromElementId = trackA.Id,
            ToElementId = trackB.Id,
            FromDirection = ConnectionPointDirection.East,
            ToDirection = ConnectionPointDirection.West
        });

        Assert.That(_plan.Connections, Has.Count.EqualTo(1));
    }

    [Test]
    public void AddConnection_WhenSourceElementMissing_ThenThrowsInvalidOperationException()
    {
        var track = new SbTrackStraight { X = 0, Y = 0 };
        _plan.AddElement(track);

        Assert.That(
            () => _plan.AddConnection(new SignalBoxConnection
            {
                FromElementId = Guid.NewGuid(),
                ToElementId = track.Id
            }),
            Throws.TypeOf<InvalidOperationException>().With.Message.Contains("Source element"));
    }

    [Test]
    public void AddConnection_WhenTargetElementMissing_ThenThrowsInvalidOperationException()
    {
        var track = new SbTrackStraight { X = 0, Y = 0 };
        _plan.AddElement(track);

        Assert.That(
            () => _plan.AddConnection(new SignalBoxConnection
            {
                FromElementId = track.Id,
                ToElementId = Guid.NewGuid()
            }),
            Throws.TypeOf<InvalidOperationException>().With.Message.Contains("Target element"));
    }

    [Test]
    public void AddConnection_WhenNull_ThenThrowsArgumentNullException()
    {
        Assert.That(() => _plan.AddConnection(null!), Throws.TypeOf<ArgumentNullException>());
    }

    [Test]
    public void RemoveConnection_WhenConnectionExists_ThenReturnsTrue()
    {
        var trackA = new SbTrackStraight { X = 0, Y = 0 };
        var trackB = new SbTrackStraight { X = 1, Y = 0 };
        _plan.Elements.AddRange([trackA, trackB]);
        _plan.AddConnection(new SignalBoxConnection { FromElementId = trackA.Id, ToElementId = trackB.Id });

        var result = _plan.RemoveConnection(trackA.Id, trackB.Id);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.True);
            Assert.That(_plan.Connections, Is.Empty);
        });
    }

    [Test]
    public void RemoveConnection_WhenConnectionDoesNotExist_ThenReturnsFalse()
    {
        var result = _plan.RemoveConnection(Guid.NewGuid(), Guid.NewGuid());

        Assert.That(result, Is.False);
    }

    [Test]
    public void RemoveConnection_WhenConnectionsShareEndpoints_ThenRemovesOnlyExactPair()
    {
        var sourceA = new SbTrackStraight();
        var sourceB = new SbTrackStraight();
        var targetA = new SbTrackStraight();
        var targetB = new SbTrackStraight();
        var sameSource = new SignalBoxConnection { FromElementId = sourceA.Id, ToElementId = targetB.Id };
        var sameTarget = new SignalBoxConnection { FromElementId = sourceB.Id, ToElementId = targetA.Id };
        var exact = new SignalBoxConnection { FromElementId = sourceA.Id, ToElementId = targetA.Id };
        _plan.Connections.AddRange([sameSource, sameTarget, exact]);

        var removed = _plan.RemoveConnection(sourceA.Id, targetA.Id);

        Assert.Multiple(() =>
        {
            Assert.That(removed, Is.True);
            Assert.That(_plan.Connections, Does.Contain(sameSource));
            Assert.That(_plan.Connections, Does.Contain(sameTarget));
            Assert.That(_plan.Connections, Does.Not.Contain(exact));
        });
    }

    [Test]
    public void FindElement_WhenElementExists_ThenReturnsElement()
    {
        var track = new SbTrackStraight { X = 0, Y = 0 };
        _plan.AddElement(track);

        var found = _plan.FindElement(track.Id);

        Assert.That(found, Is.SameAs(track));
    }

    [Test]
    public void FindElement_WhenElementDoesNotExist_ThenReturnsNull()
    {
        var found = _plan.FindElement(Guid.NewGuid());

        Assert.That(found, Is.Null);
    }

    [Test]
    public void Clear_WhenPlanHasData_ThenAllCollectionsAreEmpty()
    {
        var trackA = new SbTrackStraight { X = 0, Y = 0 };
        var trackB = new SbTrackStraight { X = 1, Y = 0 };
        _plan.Elements.AddRange([trackA, trackB]);
        _plan.AddConnection(new SignalBoxConnection { FromElementId = trackA.Id, ToElementId = trackB.Id });

        _plan.Clear();

        Assert.Multiple(() =>
        {
            Assert.That(_plan.Elements, Is.Empty);
            Assert.That(_plan.Connections, Is.Empty);
        });
    }

    [Test]
    public void SignalBoxTypes_InitializeBehavioralDefaults()
    {
        var element = new SbTrackStraight();
        var signal = new SbSignal();

        Assert.Multiple(() =>
        {
            Assert.That(element.Name, Is.EqualTo(string.Empty));
            Assert.That(signal.IsMultiplexed, Is.False);
            Assert.That(signal.SignalAspect, Is.EqualTo(SignalAspect.Hp0));
        });
    }

    [Test]
    public void GridConfig_WhenValuesArePositive_PreservesValues()
    {
        var grid = new GridConfig(1, 2, 3);

        Assert.Multiple(() =>
        {
            Assert.That(grid.Width, Is.EqualTo(1));
            Assert.That(grid.Height, Is.EqualTo(2));
            Assert.That(grid.CellSize, Is.EqualTo(3));
        });
    }

    [TestCase(0, 1, 1, "Width")]
    [TestCase(-1, 1, 1, "Width")]
    [TestCase(1, 0, 1, "Height")]
    [TestCase(1, -1, 1, "Height")]
    [TestCase(1, 1, 0, "CellSize")]
    [TestCase(1, 1, -1, "CellSize")]
    public void GridConfig_WhenValueIsNotPositive_ThrowsForParameter(
        int width,
        int height,
        int cellSize,
        string expectedParameter)
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            _ = new GridConfig(width, height, cellSize));

        Assert.That(exception!.ParamName, Is.EqualTo(expectedParameter));
    }

    [TestCase(0, 0)]
    [TestCase(1, 2)]
    public void GridPosition_WhenCoordinatesAreNonNegative_PreservesValues(int x, int y)
    {
        var position = new GridPosition(x, y);

        Assert.Multiple(() =>
        {
            Assert.That(position.X, Is.EqualTo(x));
            Assert.That(position.Y, Is.EqualTo(y));
        });
    }

    [TestCase(-1, 0, "X")]
    [TestCase(0, -1, "Y")]
    public void GridPosition_WhenCoordinateIsNegative_ThrowsForParameter(
        int x,
        int y,
        string expectedParameter)
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            _ = new GridPosition(x, y));

        Assert.That(exception!.ParamName, Is.EqualTo(expectedParameter));
    }
}
