// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Test.SharedUI;

using Domain;

using Moba.Common.Runtime;
using Moba.SharedUI.Service;

[TestFixture]
internal sealed class SignalBoxRuntimeSyncTests
{
    [Test]
    public void ApplyToPlan_Should_UpdateSignalAspectOnEditorModel()
    {
        var signalId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var plan = new SignalBoxPlan
        {
            Elements =
            [
                new SbSignal
                {
                    Id = signalId,
                    Name = "G2HBFA1",
                    SignalAspect = SignalAspect.Dunkel
                }
            ]
        };

        var changed = SignalBoxRuntimeSync.ApplyToPlan(
            plan,
            [
                new SignalBoxElementRuntimeSnapshot
                {
                    ElementId = signalId,
                    Kind = SignalBoxElementKind.Signal,
                    SignalAspect = SignalAspect.Hp0
                }
            ]);

        Assert.That(changed, Is.True);
        Assert.That(plan.Elements.OfType<SbSignal>().Single().SignalAspect, Is.EqualTo(SignalAspect.Hp0));
    }

    [Test]
    public void FilterToPlan_RemovesElementsNotInPlan()
    {
        var signalId = Guid.Parse("3d6c0ace-dde2-4329-95d5-8e474b65828f");
        var plan = new SignalBoxPlan
        {
            Elements =
            [
                new SbSignal
                {
                    Id = signalId,
                    Name = "G2HBFA1",
                    X = 6,
                    Y = 4
                }
            ]
        };

        var filtered = SignalBoxRuntimeSync.FilterToPlan(
            plan,
            [
                new SignalBoxElementRuntimeSnapshot
                {
                    ElementId = Guid.NewGuid(),
                    Name = "Cached Signal",
                    Kind = SignalBoxElementKind.Signal,
                    X = 0,
                    Y = 0
                },
                new SignalBoxElementRuntimeSnapshot
                {
                    ElementId = signalId,
                    Name = "G2HBFA1",
                    Kind = SignalBoxElementKind.Signal,
                    X = 6,
                    Y = 4,
                    SignalAspect = SignalAspect.Hp0
                }
            ]);

        Assert.Multiple(() =>
        {
            Assert.That(filtered, Has.Count.EqualTo(1));
            Assert.That(filtered[0].Name, Is.EqualTo("G2HBFA1"));
        });
    }

    [Test]
    public void BuildSnapshotsFromPlan_ProjectsPlanElements()
    {
        var signalId = Guid.Parse("3d6c0ace-dde2-4329-95d5-8e474b65828f");
        var plan = new SignalBoxPlan
        {
            Elements =
            [
                new SbSignal
                {
                    Id = signalId,
                    Name = "G2HBFA1",
                    X = 6,
                    Y = 4,
                    SignalAspect = SignalAspect.Hp0
                }
            ]
        };

        var snapshots = SignalBoxRuntimeSync.BuildSnapshotsFromPlan(plan);

        Assert.Multiple(() =>
        {
            Assert.That(snapshots, Has.Count.EqualTo(1));
            Assert.That(snapshots[0].ElementId, Is.EqualTo(signalId));
            Assert.That(snapshots[0].Name, Is.EqualTo("G2HBFA1"));
            Assert.That(snapshots[0].X, Is.EqualTo(6));
            Assert.That(snapshots[0].Y, Is.EqualTo(4));
        });
    }
}
