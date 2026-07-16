// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Test.Backend;

using global::Moba.Backend.Service;
using global::Moba.Domain;

internal sealed class DecoderCvServiceTests
{
    private readonly DecoderCvService _service = new();

    [Test]
    public void ReplaceSnapshotValues_IsAtomicWhenValidationFails()
    {
        var snapshot = new DecoderCvSnapshot { Values = [new DecoderCvValue { Number = 1, Value = 3 }] };
        var profile = new LocomotiveDecoderProfile { Protocol = DecoderProtocol.Dcc, CvSnapshots = [snapshot] };

        Assert.Throws<ArgumentException>(() => _service.ReplaceSnapshotValues(profile, snapshot.Id,
        [
            new DecoderCvValue { Number = 1, Value = 4 },
            new DecoderCvValue { Number = 1, Value = 5 }
        ]));

        Assert.That(snapshot.Values.Single().Value, Is.EqualTo(3));
    }

    [Test]
    public void ExportImport_IsDeterministicAndSortsValues()
    {
        var snapshot = new DecoderCvSnapshot
        {
            Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Name = "Factory & tuned",
            CapturedAt = new DateTimeOffset(2026, 7, 16, 0, 0, 0, TimeSpan.Zero),
            Values =
            [
                new DecoderCvValue { Number = 29, Value = 6 },
                new DecoderCvValue { Number = 1, Value = 3 }
            ]
        };

        var exported = _service.Export(snapshot);
        var imported = _service.Import(exported, DecoderProtocol.Dcc);

        Assert.Multiple(() =>
        {
            Assert.That(imported.Values.Select(value => value.Number), Is.EqualTo(new[] { 1, 29 }));
            Assert.That(_service.Export(imported), Is.EqualTo(exported));
        });
    }

    [Test]
    public void Compare_ReportsAddedRemovedAndChangedInNumericOrder()
    {
        var previous = new DecoderCvSnapshot { Values = [new DecoderCvValue { Number = 1, Value = 3 }, new DecoderCvValue { Number = 2, Value = 4 }] };
        var current = new DecoderCvSnapshot { Values = [new DecoderCvValue { Number = 2, Value = 9 }, new DecoderCvValue { Number = 3, Value = 5 }] };

        var changes = _service.Compare(previous, current);

        Assert.Multiple(() =>
        {
            Assert.That(changes.Select(change => change.Number), Is.EqualTo(new[] { 1, 2, 3 }));
            Assert.That(changes.Select(change => change.Kind), Is.EqualTo(new[] { "Removed", "Changed", "Added" }));
        });
    }
}
