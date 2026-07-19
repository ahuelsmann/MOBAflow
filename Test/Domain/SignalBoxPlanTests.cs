// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.Domain;

/// <summary>
/// Tests for SignalBoxPlan aggregate invariants.
/// Validates cell uniqueness, cascading deletes, and referential integrity.
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
        Assert.That(_plan.Id, Is.Not.EqualTo(Guid.Empty));
        Assert.That(_plan.Name, Is.EqualTo("Signal box"));
        Assert.That(_plan.Grid, Is.Not.Null);
        Assert.That(_plan.Elements, Is.Not.Null);
        Assert.That(_plan.Elements, Is.Empty);
        Assert.That(_plan.Connections, Is.Not.Null);
        Assert.That(_plan.Connections, Is.Empty);
        Assert.That(_plan.Routes, Is.Not.Null);
        Assert.That(_plan.Routes, Is.Empty);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // AddElement - Cell Uniqueness
    // ═══════════════════════════════════════════════════════════════════════

    [Test]
    public void AddElement_WhenCellIsFree_ThenElementIsAdded()
    {
        var track = new SbTrackStraight { X = 0, Y = 0 };

        _plan.AddElement(track);

        Assert.That(_plan.Elements, Has.Count.EqualTo(1));
        Assert.That(_plan.Elements[0], Is.SameAs(track));
    }

    [Test]
    public void AddElement_WhenCellIsOccupied_ThenThrowsInvalidOperationException()
    {
        _plan.AddElement(new SbTrackStraight { X = 3, Y = 5 });

        Assert.That(
            () => _plan.AddElement(new SbSwitch { X = 3, Y = 5 }),
            Throws.TypeOf<InvalidOperationException>()
                .With.Message.Contains("[3,5]"));
    }

    [Test]
    public void AddElement_WhenNull_ThenThrowsArgumentNullException()
    {
        Assert.That(
            () => _plan.AddElement(null!),
            Throws.TypeOf<ArgumentNullException>());
    }

    [Test]
    public void AddElement_WhenDifferentCells_ThenBothAreAdded()
    {
        _plan.AddElement(new SbTrackStraight { X = 0, Y = 0 });
        _plan.AddElement(new SbTrackStraight { X = 1, Y = 0 });

        Assert.That(_plan.Elements, Has.Count.EqualTo(2));
    }

    // ═══════════════════════════════════════════════════════════════════════
    // RemoveElement - Cascading Deletes
    // ═══════════════════════════════════════════════════════════════════════

    [Test]
    public void RemoveElement_WhenElementExists_ThenReturnsTrue()
    {
        var track = new SbTrackStraight { X = 0, Y = 0 };
        _plan.AddElement(track);

        var result = _plan.RemoveElement(track.Id);

        Assert.That(result, Is.True);
        Assert.That(_plan.Elements, Is.Empty);
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
        _plan.AddElement(trackA);
        _plan.AddElement(trackB);
        _plan.AddElement(trackC);

        _plan.AddConnection(new SignalBoxConnection { FromElementId = trackA.Id, ToElementId = trackB.Id });
        _plan.AddConnection(new SignalBoxConnection { FromElementId = trackB.Id, ToElementId = trackC.Id });

        _plan.RemoveElement(trackB.Id);

        Assert.That(_plan.Connections, Is.Empty);
        Assert.That(_plan.Elements, Has.Count.EqualTo(2));
    }

    [Test]
    public void RemoveElement_WhenElementIsReferencedByRoute_ThenRouteIsCascadeDeleted()
    {
        var signalA = new SbSignal { X = 0, Y = 0 };
        var signalB = new SbSignal { X = 5, Y = 0 };
        var track = new SbTrackStraight { X = 2, Y = 0 };
        _plan.AddElement(signalA);
        _plan.AddElement(signalB);
        _plan.AddElement(track);

        _plan.AddRoute(new SignalBoxRoute
        {
            Name = "F1",
            StartSignalId = signalA.Id,
            EndSignalId = signalB.Id,
            ElementIds = [track.Id]
        });

        _plan.RemoveElement(track.Id);

        Assert.That(_plan.Routes, Is.Empty);
    }

    [Test]
    public void RemoveElement_WhenElementIsRouteStartSignal_ThenOnlyReferencingRouteIsDeleted()
    {
        var removedSignal = new SbSignal { X = 0, Y = 0 };
        var sharedEndSignal = new SbSignal { X = 1, Y = 0 };
        var unrelatedStartSignal = new SbSignal { X = 2, Y = 0 };
        _plan.Elements.AddRange([removedSignal, sharedEndSignal, unrelatedStartSignal]);
        var referencingRoute = new SignalBoxRoute
        {
            StartSignalId = removedSignal.Id,
            EndSignalId = sharedEndSignal.Id
        };
        var unrelatedRoute = new SignalBoxRoute
        {
            StartSignalId = unrelatedStartSignal.Id,
            EndSignalId = sharedEndSignal.Id
        };
        _plan.Routes.AddRange([referencingRoute, unrelatedRoute]);

        var removed = _plan.RemoveElement(removedSignal.Id);

        Assert.Multiple(() =>
        {
            Assert.That(removed, Is.True);
            Assert.That(_plan.Routes, Does.Not.Contain(referencingRoute));
            Assert.That(_plan.Routes, Does.Contain(unrelatedRoute));
        });
    }

    [Test]
    public void RemoveElement_WhenElementIsRouteEndSignal_ThenOnlyReferencingRouteIsDeleted()
    {
        var sharedStartSignal = new SbSignal { X = 0, Y = 0 };
        var removedSignal = new SbSignal { X = 1, Y = 0 };
        var unrelatedEndSignal = new SbSignal { X = 2, Y = 0 };
        _plan.Elements.AddRange([sharedStartSignal, removedSignal, unrelatedEndSignal]);
        var referencingRoute = new SignalBoxRoute
        {
            StartSignalId = sharedStartSignal.Id,
            EndSignalId = removedSignal.Id
        };
        var unrelatedRoute = new SignalBoxRoute
        {
            StartSignalId = sharedStartSignal.Id,
            EndSignalId = unrelatedEndSignal.Id
        };
        _plan.Routes.AddRange([referencingRoute, unrelatedRoute]);

        var removed = _plan.RemoveElement(removedSignal.Id);

        Assert.Multiple(() =>
        {
            Assert.That(removed, Is.True);
            Assert.That(_plan.Routes, Does.Not.Contain(referencingRoute));
            Assert.That(_plan.Routes, Does.Contain(unrelatedRoute));
        });
    }

    // ═══════════════════════════════════════════════════════════════════════
    // AddConnection - Referential Integrity
    // ═══════════════════════════════════════════════════════════════════════

    [Test]
    public void AddConnection_WhenBothElementsExist_ThenConnectionIsAdded()
    {
        var trackA = new SbTrackStraight { X = 0, Y = 0 };
        var trackB = new SbTrackStraight { X = 1, Y = 0 };
        _plan.AddElement(trackA);
        _plan.AddElement(trackB);

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
            Throws.TypeOf<InvalidOperationException>()
                .With.Message.Contains("Source element"));
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
            Throws.TypeOf<InvalidOperationException>()
                .With.Message.Contains("Target element"));
    }

    [Test]
    public void AddConnection_WhenNull_ThenThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => _plan.AddConnection(null!));
    }

    // ═══════════════════════════════════════════════════════════════════════
    // RemoveConnection
    // ═══════════════════════════════════════════════════════════════════════

    [Test]
    public void RemoveConnection_WhenConnectionExists_ThenReturnsTrue()
    {
        var trackA = new SbTrackStraight { X = 0, Y = 0 };
        var trackB = new SbTrackStraight { X = 1, Y = 0 };
        _plan.AddElement(trackA);
        _plan.AddElement(trackB);
        _plan.AddConnection(new SignalBoxConnection { FromElementId = trackA.Id, ToElementId = trackB.Id });

        var result = _plan.RemoveConnection(trackA.Id, trackB.Id);

        Assert.That(result, Is.True);
        Assert.That(_plan.Connections, Is.Empty);
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
        var sameSource = new SignalBoxConnection
        {
            FromElementId = sourceA.Id,
            ToElementId = targetB.Id
        };
        var sameTarget = new SignalBoxConnection
        {
            FromElementId = sourceB.Id,
            ToElementId = targetA.Id
        };
        var exact = new SignalBoxConnection
        {
            FromElementId = sourceA.Id,
            ToElementId = targetA.Id
        };
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

    // ═══════════════════════════════════════════════════════════════════════
    // AddRoute - Signal and Element Validation
    // ═══════════════════════════════════════════════════════════════════════

    [Test]
    public void AddRoute_WhenAllReferencesValid_ThenRouteIsAdded()
    {
        var signalA = new SbSignal { X = 0, Y = 0 };
        var signalB = new SbSignal { X = 5, Y = 0 };
        var sw = new SbSwitch { X = 2, Y = 0 };
        _plan.AddElement(signalA);
        _plan.AddElement(signalB);
        _plan.AddElement(sw);

        _plan.AddRoute(new SignalBoxRoute
        {
            Name = "F1",
            StartSignalId = signalA.Id,
            EndSignalId = signalB.Id,
            ElementIds = [sw.Id],
            SwitchPositions = new Dictionary<Guid, SwitchPosition> { [sw.Id] = SwitchPosition.Straight }
        });

        Assert.That(_plan.Routes, Has.Count.EqualTo(1));
    }

    [Test]
    public void AddRoute_WhenStartSignalMissing_ThenThrowsInvalidOperationException()
    {
        var signalB = new SbSignal { X = 5, Y = 0 };
        _plan.AddElement(signalB);

        Assert.That(
            () => _plan.AddRoute(new SignalBoxRoute
            {
                StartSignalId = Guid.NewGuid(),
                EndSignalId = signalB.Id
            }),
            Throws.TypeOf<InvalidOperationException>()
                .With.Message.Contains("Start signal"));
    }

    [Test]
    public void AddRoute_WhenStartIsNotSignal_ThenThrowsInvalidOperationException()
    {
        var track = new SbTrackStraight { X = 0, Y = 0 };
        var signal = new SbSignal { X = 5, Y = 0 };
        _plan.AddElement(track);
        _plan.AddElement(signal);

        Assert.That(
            () => _plan.AddRoute(new SignalBoxRoute
            {
                StartSignalId = track.Id,
                EndSignalId = signal.Id
            }),
            Throws.TypeOf<InvalidOperationException>()
                .With.Message.Contains("Start signal"));
    }

    [Test]
    public void AddRoute_WhenEndSignalMissing_ThenThrowsWithSignalId()
    {
        var startSignal = new SbSignal { X = 0, Y = 0 };
        var missingEndSignalId = Guid.NewGuid();
        _plan.AddElement(startSignal);
        var route = new SignalBoxRoute
        {
            StartSignalId = startSignal.Id,
            EndSignalId = missingEndSignalId
        };

        var exception = Assert.Throws<InvalidOperationException>(() => _plan.AddRoute(route));

        Assert.That(exception!.Message, Is.EqualTo(
            $"End signal {missingEndSignalId} does not exist or is not a signal element."));
    }

    [Test]
    public void AddRoute_WhenEndIsNotSignal_ThenThrowsInvalidOperationException()
    {
        var startSignal = new SbSignal { X = 0, Y = 0 };
        var endTrack = new SbTrackStraight { X = 1, Y = 0 };
        _plan.AddElement(startSignal);
        _plan.AddElement(endTrack);

        var exception = Assert.Throws<InvalidOperationException>(() => _plan.AddRoute(new SignalBoxRoute
        {
            StartSignalId = startSignal.Id,
            EndSignalId = endTrack.Id
        }));

        Assert.That(exception!.Message, Does.Contain(endTrack.Id.ToString()));
    }

    [Test]
    public void AddRoute_WhenNull_ThenThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => _plan.AddRoute(null!));
    }

    [Test]
    public void AddRoute_WhenRouteReferencesNonExistentElement_ThenThrowsInvalidOperationException()
    {
        var signalA = new SbSignal { X = 0, Y = 0 };
        var signalB = new SbSignal { X = 5, Y = 0 };
        var missingElementId = Guid.NewGuid();
        var secondMissingElementId = Guid.NewGuid();
        _plan.AddElement(signalA);
        _plan.AddElement(signalB);

        var exception = Assert.Throws<InvalidOperationException>(() => _plan.AddRoute(new SignalBoxRoute
        {
            StartSignalId = signalA.Id,
            EndSignalId = signalB.Id,
            ElementIds = [missingElementId, secondMissingElementId]
        }));

        Assert.That(exception!.Message, Is.EqualTo(
            $"Route references non-existent elements: {missingElementId}, {secondMissingElementId}"));
    }

    [Test]
    public void AddRoute_WhenSwitchPositionReferencesNonSwitch_ThenThrowsInvalidOperationException()
    {
        var signalA = new SbSignal { X = 0, Y = 0 };
        var signalB = new SbSignal { X = 5, Y = 0 };
        var track = new SbTrackStraight { X = 2, Y = 0 };
        var missingSwitchId = Guid.NewGuid();
        _plan.AddElement(signalA);
        _plan.AddElement(signalB);
        _plan.AddElement(track);

        var exception = Assert.Throws<InvalidOperationException>(() => _plan.AddRoute(new SignalBoxRoute
        {
            StartSignalId = signalA.Id,
            EndSignalId = signalB.Id,
            SwitchPositions = new Dictionary<Guid, SwitchPosition>
            {
                [track.Id] = SwitchPosition.Straight,
                [missingSwitchId] = SwitchPosition.DivergingLeft
            }
        }));

        Assert.That(exception!.Message, Is.EqualTo(
            $"Route references non-existent or non-switch elements for switch positions: {track.Id}, {missingSwitchId}"));
    }

    // ═══════════════════════════════════════════════════════════════════════
    // RemoveRoute
    // ═══════════════════════════════════════════════════════════════════════

    [Test]
    public void RemoveRoute_WhenRouteExists_ThenReturnsTrue()
    {
        var signalA = new SbSignal { X = 0, Y = 0 };
        var signalB = new SbSignal { X = 5, Y = 0 };
        _plan.AddElement(signalA);
        _plan.AddElement(signalB);

        var route = new SignalBoxRoute { StartSignalId = signalA.Id, EndSignalId = signalB.Id };
        _plan.AddRoute(route);

        var result = _plan.RemoveRoute(route.Id);

        Assert.That(result, Is.True);
        Assert.That(_plan.Routes, Is.Empty);
    }

    [Test]
    public void RemoveRoute_WhenRouteDoesNotExist_ThenReturnsFalse()
    {
        var result = _plan.RemoveRoute(Guid.NewGuid());

        Assert.That(result, Is.False);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // FindElement
    // ═══════════════════════════════════════════════════════════════════════

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

    // ═══════════════════════════════════════════════════════════════════════
    // Clear
    // ═══════════════════════════════════════════════════════════════════════

    [Test]
    public void Clear_WhenPlanHasData_ThenAllCollectionsAreEmpty()
    {
        var signalA = new SbSignal { X = 0, Y = 0 };
        var signalB = new SbSignal { X = 5, Y = 0 };
        _plan.AddElement(signalA);
        _plan.AddElement(signalB);
        _plan.AddConnection(new SignalBoxConnection { FromElementId = signalA.Id, ToElementId = signalB.Id });
        _plan.AddRoute(new SignalBoxRoute { StartSignalId = signalA.Id, EndSignalId = signalB.Id });

        _plan.Clear();

        Assert.That(_plan.Elements, Is.Empty);
        Assert.That(_plan.Connections, Is.Empty);
        Assert.That(_plan.Routes, Is.Empty);
    }

    [Test]
    public void SignalBoxTypes_InitializeBehavioralDefaults()
    {
        var element = new SbTrackStraight();
        var signal = new SbSignal();
        var route = new SignalBoxRoute();

        Assert.Multiple(() =>
        {
            Assert.That(element.Name, Is.EqualTo(string.Empty));
            Assert.That(signal.IsMultiplexed, Is.False);
            Assert.That(route.Name, Is.EqualTo(string.Empty));
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
