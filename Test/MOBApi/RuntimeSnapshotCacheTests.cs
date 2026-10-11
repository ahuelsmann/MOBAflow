// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Test.MOBApi;

using Moba.Common.Runtime;
using Moba.Domain;
using Moba.MOBApi.Service;

[TestFixture]
internal sealed class RuntimeSnapshotCacheTests
{
    private static readonly Guid ProjectA = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid ProjectB = Guid.Parse("22222222-2222-2222-2222-222222222222");

    [Test]
    public void Set_KeepsEachProjectsSnapshotSeparately()
    {
        var cache = new RuntimeSnapshotCache();

        cache.Set(RuntimeJsonSerializer.Serialize(new MobaRuntimeSnapshot { ProjectId = ProjectA, IsConnected = true, MainCurrent = 1 }));
        cache.Set(RuntimeJsonSerializer.Serialize(new MobaRuntimeSnapshot { ProjectId = ProjectB, IsConnected = false, MainCurrent = 2 }));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(cache.TryGet(ProjectA, out var a), Is.True);
            Assert.That(a.IsConnected, Is.True);
            Assert.That(RuntimeJsonSerializer.Deserialize(a.Json)!.MainCurrent, Is.EqualTo(1));
            Assert.That(cache.TryGet(ProjectB, out var b), Is.True);
            Assert.That(b.IsConnected, Is.False);
            Assert.That(RuntimeJsonSerializer.Deserialize(b.Json)!.MainCurrent, Is.EqualTo(2));
            Assert.That(cache.GetAll().Select(entry => entry.ProjectId), Is.EquivalentTo(new[] { ProjectA, ProjectB }));
        }
    }

    [Test]
    public void Set_RejectsSnapshotWithoutProject()
    {
        var cache = new RuntimeSnapshotCache();

        Assert.Throws<ArgumentException>(() => cache.Set(RuntimeJsonSerializer.Serialize(new MobaRuntimeSnapshot { IsConnected = true })));
        Assert.That(cache.GetAll(), Is.Empty);
    }

    [Test]
    public void Set_PreservesSignalBoxElements_WhenIncomingSnapshotOmitsThem()
    {
        var cache = new RuntimeSnapshotCache();
        var signalId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

        var withSignals = RuntimeJsonSerializer.Serialize(new MobaRuntimeSnapshot
        {
            ProjectId = ProjectA,
            IsConnected = true,
            SignalBoxElements =
            [
                new SignalBoxElementRuntimeSnapshot
                {
                    ElementId = signalId,
                    Name = "S1",
                    Kind = SignalBoxElementKind.Signal
                }
            ]
        });

        cache.Set(withSignals);

        var telemetryOnly = RuntimeJsonSerializer.Serialize(new MobaRuntimeSnapshot
        {
            ProjectId = ProjectA,
            IsConnected = true,
            MainCurrent = 99,
            SignalBoxElements = []
        });

        cache.Set(telemetryOnly);

        Assert.That(cache.TryGet(ProjectA, out var entry), Is.True);
        var restored = RuntimeJsonSerializer.Deserialize(entry.Json);
        Assert.Multiple(() =>
        {
            Assert.That(restored, Is.Not.Null);
            Assert.That(restored!.MainCurrent, Is.EqualTo(99));
            Assert.That(restored.SignalBoxElements, Has.Count.EqualTo(1));
            Assert.That(restored.SignalBoxElements[0].ElementId, Is.EqualTo(signalId));
        });
    }

    [Test]
    public void Set_PreservesUpdatedSignalAspect_WhenTelemetrySnapshotFollows()
    {
        var cache = new RuntimeSnapshotCache();
        var signalId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

        cache.Set(RuntimeJsonSerializer.Serialize(new MobaRuntimeSnapshot
        {
            ProjectId = ProjectA,
            IsConnected = true,
            SignalBoxElements =
            [
                new SignalBoxElementRuntimeSnapshot
                {
                    ElementId = signalId,
                    Name = "S1",
                    Kind = SignalBoxElementKind.Signal,
                    SignalAspect = SignalAspect.Hp0
                }
            ]
        }));

        cache.Set(RuntimeJsonSerializer.Serialize(new MobaRuntimeSnapshot
        {
            ProjectId = ProjectA,
            IsConnected = true,
            SignalBoxElements =
            [
                new SignalBoxElementRuntimeSnapshot
                {
                    ElementId = signalId,
                    Name = "S1",
                    Kind = SignalBoxElementKind.Signal,
                    SignalAspect = SignalAspect.Ks1
                }
            ]
        }));

        cache.Set(RuntimeJsonSerializer.Serialize(new MobaRuntimeSnapshot
        {
            ProjectId = ProjectA,
            IsConnected = true,
            MainCurrent = 42,
            SignalBoxElements = []
        }));

        Assert.That(cache.TryGet(ProjectA, out var entry), Is.True);
        var restored = RuntimeJsonSerializer.Deserialize(entry.Json);
        Assert.That(restored!.SignalBoxElements.Single().SignalAspect, Is.EqualTo(SignalAspect.Ks1));
    }

    [Test]
    public void Set_ReplacesStaleSignalBoxElements_WhenIncomingProjectChanges()
    {
        var cache = new RuntimeSnapshotCache();
        var staleId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var currentId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

        cache.Set(RuntimeJsonSerializer.Serialize(new MobaRuntimeSnapshot
        {
            ProjectId = ProjectA,
            IsConnected = true,
            SignalBoxElements =
            [
                new SignalBoxElementRuntimeSnapshot
                {
                    ElementId = staleId,
                    Name = "Cached Signal",
                    Kind = SignalBoxElementKind.Signal,
                    X = 0,
                    Y = 0
                }
            ]
        }));

        cache.Set(RuntimeJsonSerializer.Serialize(new MobaRuntimeSnapshot
        {
            ProjectId = ProjectA,
            IsConnected = true,
            SignalBoxElements =
            [
                new SignalBoxElementRuntimeSnapshot
                {
                    ElementId = currentId,
                    Name = "G2HBFA1",
                    Kind = SignalBoxElementKind.Signal,
                    X = 6,
                    Y = 4
                }
            ]
        }));

        Assert.That(cache.TryGet(ProjectA, out var entry), Is.True);
        var restored = RuntimeJsonSerializer.Deserialize(entry.Json);
        Assert.Multiple(() =>
        {
            Assert.That(restored!.SignalBoxElements, Has.Count.EqualTo(1));
            Assert.That(restored.SignalBoxElements[0].ElementId, Is.EqualTo(currentId));
            Assert.That(restored.SignalBoxElements[0].Name, Is.EqualTo("G2HBFA1"));
        });
    }

    [Test]
    public void Set_PreservesLocomotiveFleet_WhenIncomingSnapshotOmitsIt()
    {
        var cache = new RuntimeSnapshotCache();
        var locomotiveId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

        var withFleet = RuntimeJsonSerializer.Serialize(new MobaRuntimeSnapshot
        {
            ProjectId = ProjectA,
            IsConnected = true,
            LocomotiveFleet =
            [
                new LocomotiveFleetSnapshot
                {
                    LocomotiveId = locomotiveId,
                    Name = "BR 110",
                    DigitalAddress = 7
                }
            ]
        });

        cache.Set(withFleet);

        var telemetryOnly = RuntimeJsonSerializer.Serialize(new MobaRuntimeSnapshot
        {
            ProjectId = ProjectA,
            IsConnected = true,
            MainCurrent = 99,
            LocomotiveFleet = []
        });

        cache.Set(telemetryOnly);

        Assert.That(cache.TryGet(ProjectA, out var entry), Is.True);
        var restored = RuntimeJsonSerializer.Deserialize(entry.Json);
        Assert.Multiple(() =>
        {
            Assert.That(restored, Is.Not.Null);
            Assert.That(restored!.MainCurrent, Is.EqualTo(99));
            Assert.That(restored.LocomotiveFleet, Has.Count.EqualTo(1));
            Assert.That(restored.LocomotiveFleet[0].LocomotiveId, Is.EqualTo(locomotiveId));
        });
    }
}
