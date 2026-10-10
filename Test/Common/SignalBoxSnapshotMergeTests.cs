// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

using Moba.Common.Runtime;

using Moba.Domain;

namespace Moba.Test.Common;

[TestFixture]

internal sealed class SignalBoxSnapshotMergeTests

{

    [Test]

    public void MergeAspectsFromCache_PrefersCachedAspects_WhenIncomingUsesDefaults()

    {

        var elementId = Guid.NewGuid();

        var incoming =

            new List<SignalBoxElementRuntimeSnapshot>

            {

                new()

                {

                    ElementId = elementId,

                    Name = "S1",

                    Kind = SignalBoxElementKind.Signal

                }
            };

        var cached =

            new List<SignalBoxElementRuntimeSnapshot>

            {

                new()

                {

                    ElementId = elementId,

                    Name = "S1",

                    Kind = SignalBoxElementKind.Signal,

                    SignalAspect = SignalAspect.Ks1

                }
            };

        var merged = SignalBoxSnapshotMerge.MergeAspectsFromCache(incoming, cached);

        Assert.That(merged[0].SignalAspect, Is.EqualTo(SignalAspect.Ks1));

    }

    [Test]

    public void MergeAspectsFromCache_ReturnsCachedList_WhenIncomingIsEmpty()

    {

        var cached =

            new List<SignalBoxElementRuntimeSnapshot>

            {

                new()

                {

                    ElementId = Guid.NewGuid(),

                    Name = "S1",

                    Kind = SignalBoxElementKind.Signal,

                    SignalAspect = SignalAspect.Hp0

                }
            };

        var merged = SignalBoxSnapshotMerge.MergeAspectsFromCache([], cached);

        Assert.That(merged, Is.SameAs(cached));

    }

    [Test]
    public void MergeIncomingOverPrevious_PrefersIncomingAspect_AndKeepsMissingElements()
    {
        var sharedId = Guid.NewGuid();
        var previousOnlyId = Guid.NewGuid();
        var incoming = new List<SignalBoxElementRuntimeSnapshot>
        {
            new()
            {
                ElementId = sharedId,
                Kind = SignalBoxElementKind.Signal,
                SignalAspect = SignalAspect.Ks2
            }
        };
        var previous = new List<SignalBoxElementRuntimeSnapshot>
        {
            new()
            {
                ElementId = sharedId,
                Kind = SignalBoxElementKind.Signal,
                SignalAspect = SignalAspect.Hp0
            },
            new()
            {
                ElementId = previousOnlyId,
                Kind = SignalBoxElementKind.Signal,
                SignalAspect = SignalAspect.Ks1
            }
        };

        var merged = SignalBoxSnapshotMerge.MergeIncomingOverPrevious(incoming, previous);

        Assert.Multiple(() =>
        {
            Assert.That(merged, Has.Count.EqualTo(2));
            Assert.That(merged.Single(element => element.ElementId == sharedId).SignalAspect, Is.EqualTo(SignalAspect.Ks2));
            Assert.That(merged.Single(element => element.ElementId == previousOnlyId).SignalAspect, Is.EqualTo(SignalAspect.Ks1));
        });
    }

    [TestCase(true)]
    [TestCase(false)]
    public void Merge_NullIncoming_IsRejectedWithArgumentName(bool useCache)
    {
        var error = Assert.Throws<ArgumentNullException>(() => Merge(useCache, null!, []));

        Assert.That(error!.ParamName, Is.EqualTo("incoming"));
    }

    [TestCase(true, true)]
    [TestCase(true, false)]
    [TestCase(false, true)]
    [TestCase(false, false)]
    public void Merge_NullOrEmptyHistory_PreservesIncomingValues(bool useCache, bool nullHistory)
    {
        // The state contract preserves values; it does not require a particular list allocation.
        SignalBoxElementRuntimeSnapshot[] incoming =
        [
            new() { ElementId = Guid.NewGuid(), Kind = SignalBoxElementKind.Signal, SignalAspect = SignalAspect.Hp0 },
            new() { ElementId = Guid.NewGuid(), Kind = SignalBoxElementKind.Switch, SwitchPosition = SwitchPosition.Straight }
        ];

        var merged = Merge(useCache, incoming, nullHistory ? null : []);

        Assert.That(merged, Is.EqualTo(incoming));
    }

    [Test]
    public void MergeIncomingOverPrevious_EmptyIncoming_PreservesPreviousStates()
    {
        SignalBoxElementRuntimeSnapshot[] previous =
        [
            new() { ElementId = Guid.NewGuid(), Kind = SignalBoxElementKind.Switch, SwitchPosition = SwitchPosition.DivergingLeft }
        ];

        Assert.That(SignalBoxSnapshotMerge.MergeIncomingOverPrevious([], previous), Is.EqualTo(previous));
    }

    [TestCase(true, true)]
    [TestCase(true, false)]
    [TestCase(false, true)]
    [TestCase(false, false)]
    public void Merge_EmptyIncomingAndHistory_ReturnsEmpty(bool useCache, bool nullHistory)
    {
        Assert.That(Merge(useCache, [], nullHistory ? null : []), Is.Empty);
    }

    [TestCase(true)]
    [TestCase(false)]
    public void Merge_ConflictingStates_PrefersIncomingAndPreservesIncomingMetadata(bool useCache)
    {
        var signalId = Guid.NewGuid();
        var switchId = Guid.NewGuid();
        SignalBoxElementRuntimeSnapshot[] incoming =
        [
            new() { ElementId = signalId, Name = "Current signal", Kind = SignalBoxElementKind.Signal, X = 3, SignalAspect = SignalAspect.Hp0 },
            new() { ElementId = switchId, Name = "Current switch", Kind = SignalBoxElementKind.Switch, Address = 101, SwitchPosition = SwitchPosition.Straight }
        ];
        SignalBoxElementRuntimeSnapshot[] history =
        [
            new() { ElementId = switchId, Name = "Old switch", Kind = SignalBoxElementKind.Switch, Address = 99, SwitchPosition = SwitchPosition.DivergingRight },
            new() { ElementId = signalId, Name = "Old signal", Kind = SignalBoxElementKind.Signal, X = 1, SignalAspect = SignalAspect.Ks1 }
        ];

        var merged = Merge(useCache, incoming, history);

        Assert.Multiple(() =>
        {
            Assert.That(merged, Is.EqualTo(incoming));
            Assert.That(incoming[0].SignalAspect, Is.EqualTo(SignalAspect.Hp0));
            Assert.That(incoming[1].SwitchPosition, Is.EqualTo(SwitchPosition.Straight));
            Assert.That(history[0].SwitchPosition, Is.EqualTo(SwitchPosition.DivergingRight));
            Assert.That(history[1].SignalAspect, Is.EqualTo(SignalAspect.Ks1));
        });
    }

    [TestCase(true)]
    [TestCase(false)]
    public void Merge_MissingStates_UsesMatchingHistoryWithoutChangingInputs(bool useCache)
    {
        var signalId = Guid.NewGuid();
        var switchId = Guid.NewGuid();
        SignalBoxElementRuntimeSnapshot[] incoming =
        [
            new() { ElementId = switchId, Name = "Current switch", Kind = SignalBoxElementKind.Switch, Address = 201 },
            new() { ElementId = signalId, Name = "Current signal", Kind = SignalBoxElementKind.Signal, X = 8 }
        ];
        SignalBoxElementRuntimeSnapshot[] history =
        [
            new() { ElementId = signalId, Name = "Old signal", Kind = SignalBoxElementKind.Signal, SignalAspect = SignalAspect.Ks2 },
            new() { ElementId = switchId, Name = "Old switch", Kind = SignalBoxElementKind.Switch, SwitchPosition = SwitchPosition.DivergingLeft }
        ];
        SignalBoxElementRuntimeSnapshot[] expected =
        [
            new() { ElementId = switchId, Name = "Current switch", Kind = SignalBoxElementKind.Switch, Address = 201, SwitchPosition = SwitchPosition.DivergingLeft },
            new() { ElementId = signalId, Name = "Current signal", Kind = SignalBoxElementKind.Signal, X = 8, SignalAspect = SignalAspect.Ks2 }
        ];

        var merged = Merge(useCache, incoming, history);

        Assert.Multiple(() =>
        {
            Assert.That(merged, Is.EqualTo(expected));
            Assert.That(incoming[0].SwitchPosition, Is.Null);
            Assert.That(incoming[1].SignalAspect, Is.Null);
            Assert.That(history[0].Name, Is.EqualTo("Old signal"));
            Assert.That(history[1].Address, Is.Null);
        });
    }

    [TestCase(true)]
    [TestCase(false)]
    public void Merge_MatchingHistoryWithoutStates_KeepsStatesMissing(bool useCache)
    {
        var id = Guid.NewGuid();
        SignalBoxElementRuntimeSnapshot[] incoming = [new() { ElementId = id, Name = "Incoming", Kind = SignalBoxElementKind.Signal }];
        SignalBoxElementRuntimeSnapshot[] history = [new() { ElementId = id, Name = "Previous", Kind = SignalBoxElementKind.Signal }];

        Assert.That(Merge(useCache, incoming, history), Is.EqualTo(incoming));
    }

    [TestCase(true)]
    [TestCase(false)]
    public void Merge_NewIdsAndHistoryOnlyIds_RespectMergeScopeAndOrder(bool useCache)
    {
        var sharedId = Guid.NewGuid();
        var newId = Guid.NewGuid();
        var historyOnlyId = Guid.NewGuid();
        SignalBoxElementRuntimeSnapshot[] incoming =
        [
            new() { ElementId = newId, Name = "New", Kind = SignalBoxElementKind.Signal, SignalAspect = SignalAspect.Ks1 },
            new() { ElementId = sharedId, Name = "Shared", Kind = SignalBoxElementKind.Switch, SwitchPosition = SwitchPosition.Straight }
        ];
        SignalBoxElementRuntimeSnapshot[] history =
        [
            new() { ElementId = historyOnlyId, Name = "History only", Kind = SignalBoxElementKind.Signal, SignalAspect = SignalAspect.Ks2 },
            new() { ElementId = sharedId, Name = "Old shared", Kind = SignalBoxElementKind.Switch, SwitchPosition = SwitchPosition.DivergingLeft }
        ];

        var merged = Merge(useCache, incoming, history);
        var expected = useCache ? incoming : incoming.Concat(history.Take(1)).ToArray();

        Assert.That(merged, Is.EqualTo(expected));
    }

    private static IReadOnlyList<SignalBoxElementRuntimeSnapshot> Merge(
        bool useCache,
        IReadOnlyList<SignalBoxElementRuntimeSnapshot> incoming,
        IReadOnlyList<SignalBoxElementRuntimeSnapshot>? history) =>
        useCache
            ? SignalBoxSnapshotMerge.MergeAspectsFromCache(incoming, history)
            : SignalBoxSnapshotMerge.MergeIncomingOverPrevious(incoming, history);
}

