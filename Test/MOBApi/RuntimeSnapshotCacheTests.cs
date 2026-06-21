// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Test.MOBApi;

using Moba.Common.Runtime;
using Moba.Domain;
using Moba.MOBApi.Service;

[TestFixture]
internal sealed class RuntimeSnapshotCacheTests
{
    [Test]
    public void Set_PreservesSignalBoxElements_WhenIncomingSnapshotOmitsThem()
    {
        var cache = new RuntimeSnapshotCache();
        var signalId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

        var withSignals = RuntimeJsonSerializer.Serialize(new MobaRuntimeSnapshot
        {
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

        cache.Set(withSignals, isConnected: true);

        var telemetryOnly = RuntimeJsonSerializer.Serialize(new MobaRuntimeSnapshot
        {
            IsConnected = true,
            MainCurrent = 99,
            SignalBoxElements = []
        });

        cache.Set(telemetryOnly, isConnected: true);

        Assert.That(cache.TryGet(out var entry), Is.True);
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
    public void Set_PreservesLocomotiveFleet_WhenIncomingSnapshotOmitsIt()
    {
        var cache = new RuntimeSnapshotCache();
        var locomotiveId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

        var withFleet = RuntimeJsonSerializer.Serialize(new MobaRuntimeSnapshot
        {
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

        cache.Set(withFleet, isConnected: true);

        var telemetryOnly = RuntimeJsonSerializer.Serialize(new MobaRuntimeSnapshot
        {
            IsConnected = true,
            MainCurrent = 99,
            LocomotiveFleet = []
        });

        cache.Set(telemetryOnly, isConnected: true);

        Assert.That(cache.TryGet(out var entry), Is.True);
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
