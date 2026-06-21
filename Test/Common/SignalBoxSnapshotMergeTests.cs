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

}

