// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Test.MOBApi;

using Moba.MOBApi.Service;

[TestFixture]
internal sealed class RuntimeBroadcastMetricsTests
{
    [Test]
    public void RecordSnapshotBroadcast_TracksPayloadBytesAndCount()
    {
        var metrics = new RuntimeBroadcastMetrics();

        metrics.RecordSnapshotBroadcast(512);
        metrics.RecordSnapshotBroadcast(256);

        Assert.Multiple(() =>
        {
            Assert.That(metrics.LastSnapshotPayloadBytes, Is.EqualTo(256));
            Assert.That(metrics.TotalSnapshotBroadcastCount, Is.EqualTo(2));
            Assert.That(metrics.LastSnapshotBroadcastAt, Is.Not.Null);
        });
    }

    [Test]
    public void RecordSnapshotBroadcast_RejectsNegativePayloadBytes()
    {
        var metrics = new RuntimeBroadcastMetrics();

        Assert.Throws<ArgumentOutOfRangeException>(() => metrics.RecordSnapshotBroadcast(-1));
    }
}
